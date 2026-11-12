import assert from 'node:assert/strict';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const { chromium } = await import(pathToFileURL(resolve(process.env.PLAYWRIGHT_MODULE || '.tmp/pos-browser-tools/node_modules/playwright/index.mjs')).href);
const base = process.env.STOCK_TEST_URL || 'http://127.0.0.1:5173';
const output = resolve('.tmp/stock-movement-balances');
mkdirSync(output, { recursive: true });
const common = { articleId: 'lettuce', articleName: 'Lechuga', warehouseId: 'main', warehouseName: 'Principal', baseUnitSymbol: 'g', movedAt: '2026-01-02T12:00:00Z' };
const movements = [
  { ...common, id: 'exit', type: 'ManualExit', quantity: 200, unitSymbol: 'g', baseQuantity: -200, stockBefore: 500, stockAfter: 300, reference: 'Salida de prueba' },
  { ...common, id: 'entry', type: 'PurchaseEntry', quantity: 1, unitSymbol: 'kg', baseQuantity: 1000, stockBefore: 300, stockAfter: 1300, reference: 'Compra de prueba' },
  { ...common, id: 'negative', type: 'Waste', quantity: 3.25, unitSymbol: 'g', baseQuantity: -3.25, stockBefore: 0, stockAfter: -3.25, reference: 'Negativo de prueba' },
  { ...common, id: 'legacy', type: 'ManualEntry', quantity: 5, unitSymbol: 'g', baseQuantity: 5, reference: 'Saldo no informado' },
];
const html = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head><body style="margin:0;padding:16px"><div id="root"></div>
<script type="module">
import RefreshRuntime from '/@react-refresh';
RefreshRuntime.injectIntoGlobalHook(window);
window.$RefreshReg$ = () => {}; window.$RefreshSig$ = () => type => type;
window.__vite_plugin_react_preamble_installed__ = true;
await import('/@vite/client');
const React = (await import('/node_modules/.vite/deps/react.js')).default;
const dom = await import('/node_modules/.vite/deps/react-dom_client.js');
const createRoot = dom.createRoot || dom.default.createRoot;
const antd = await import('/node_modules/.vite/deps/antd.js');
const App = antd.App || antd.default.App;
const dayjs = (await import('/node_modules/.vite/deps/dayjs.js')).default;
dayjs.extend((await import('/node_modules/.vite/deps/dayjs_plugin_utc.js')).default);
dayjs.extend((await import('/node_modules/.vite/deps/dayjs_plugin_timezone.js')).default);
const { AuthContext } = await import('/src/context/auth-context.ts');
const { default: StockMovements } = await import('/src/components/Inventory/StockMovements.tsx');
createRoot(document.getElementById('root')).render(React.createElement(AuthContext.Provider,
  { value: { hasPermission: () => false } }, React.createElement(App, null, React.createElement(StockMovements))));
</script></body></html>`;
const browser = await chromium.launch({ channel: 'msedge', headless: true });
try {
  for (const width of [390, 1440]) {
    const page = await browser.newPage({ viewport: { width, height: 900 } });
    const errors = [], writes = [], queries = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.route('**/__stock_balances_check', route => route.fulfill({ contentType: 'text/html', body: html }));
    await page.route('**/api/**', route => {
      const request = route.request(), url = new URL(request.url());
      if (request.method() !== 'GET') { writes.push(request.url()); return route.fulfill({ status: 500, body: '' }); }
      if (url.pathname.endsWith('/movimientos')) {
        queries.push(url.search);
        const type = url.searchParams.get('type');
        return route.fulfill({ json: type ? movements.filter(m => m.type === type) : movements });
      }
      if (url.pathname.endsWith('/articulos')) return route.fulfill({ json: [{ id: 'lettuce', name: 'Lechuga', baseUnitId: 'grams', baseUnitSymbol: 'g' }] });
      if (url.pathname.endsWith('/bodegas')) return route.fulfill({ json: [{ id: 'main', name: 'Principal' }] });
      return route.fulfill({ json: [] });
    });
    await page.goto(`${base}/__stock_balances_check`);
    await page.getByText('Salida de prueba', { exact: true }).waitFor({ timeout: 15000 }).catch(async error => {
      console.error('Browser errors:', errors, 'Requests:', queries, 'Body:', await page.locator('body').innerText());
      await page.screenshot({ path: resolve(output, `failure-${width}.png`), fullPage: true });
      throw error;
    });
    await page.getByRole('columnheader', { name: 'Stock', exact: true }).waitFor();
    await page.getByRole('columnheader', { name: 'Movimiento', exact: true }).waitFor();
    const exit = page.getByRole('row').filter({ hasText: 'Salida de prueba' });
    assert.match(await exit.innerText(), /500 g/); assert.match(await exit.innerText(), /300 g/);
    const entry = page.getByRole('row').filter({ hasText: 'Compra de prueba' });
    assert.match(await entry.innerText(), /\+1 kg/);
    assert.ok((await entry.innerText()).includes(`${(1300).toLocaleString('es-EC', { maximumFractionDigits: 4 })} g`));
    const negative = page.getByRole('row').filter({ hasText: 'Negativo de prueba' });
    assert.match(await negative.locator('.ant-typography-danger').innerText(), /-3,25 g/);
    const legacy = page.getByRole('row').filter({ hasText: 'Saldo no informado' });
    assert.match(await legacy.innerText(), /-\s*→\s*-/);
    await page.screenshot({ path: resolve(output, `movements-${width}.png`), fullPage: true });
    await page.locator('.ant-select').nth(2).click();
    await page.getByText('Salida manual', { exact: true }).last().click();
    await page.waitForFunction(() => document.querySelectorAll('tr[data-row-key]').length === 1);
    assert.match(await exit.innerText(), /500 g/); assert.match(await exit.innerText(), /300 g/);
    assert.ok(queries.some(q => q.includes('type=ManualExit')));
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
    assert.deepEqual(errors, []); assert.deepEqual(writes, []);
    await page.close();
    console.log(`Stock balances ${width}px: columns, units, negative/missing values and filtering passed; no writes.`);
  }
} finally { await browser.close(); }
