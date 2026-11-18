import assert from 'node:assert/strict';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const { chromium } = await import(pathToFileURL(resolve(process.env.PLAYWRIGHT_MODULE || '.tmp/pos-browser-tools/node_modules/playwright/index.mjs')).href);
const base = process.env.REPORT_TEST_URL || 'http://127.0.0.1:5173';
const output = resolve('.tmp/sales-promotion-report');
mkdirSync(output, { recursive: true });
const html = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
<body style="margin:0;padding:16px"><div id="root"></div><script type="module">
import RefreshRuntime from '/@react-refresh'; RefreshRuntime.injectIntoGlobalHook(window);
window.$RefreshReg$ = () => {}; window.$RefreshSig$ = () => type => type;
window.__vite_plugin_react_preamble_installed__ = true;
await import('/@vite/client'); await import('/tests/fixtures/SalesProfitabilityHarness.tsx');
</script></body></html>`;
function row(key, name, kind, promoId, promoName, qty, net) {
  return { rowKey: key, menuItemId: name === '12 alitas' ? 'wings' : 'combo', menuItemName: name, categoryName: 'Food',
    saleKind: kind, promotionId: promoId, promotionName: promoName, quantity: qty, grossSales: net * 1.15, netSales: net,
    taxAmount: net * 0.15, unitRecipeCost: 2, totalFoodCost: qty * 2, grossProfit: net - qty * 2,
    foodCostPercentage: 20, grossMarginPercentage: 80, discountAmount: kind === 'Promotion' ? 5 : 0,
    totalOrders: 2, hasMissingCosts: false, hasConversionWarnings: false, hasEstimatedQuantities: false };
}
const regular = row('normal', '12 alitas', 'Regular', undefined, undefined, 2, 20);
const tuesday = row('tuesday', '12 alitas', 'Promotion', 'tue', 'Martes de alitas', 3, 24);
const thursday = row('thursday', 'Combo alitas y gaseosa', 'Promotion', 'thu', 'Jueves de la reliquia glotona', 4, 60);
const unknown = { ...row('unknown', '12 alitas', 'Unclassified', undefined, undefined, 1, 8), hasEstimatedQuantities: true };
const ribeye = { ...row('ribeye', 'Ribeye', 'Promotion', 'rib', 'Segundo ribeye a mitad', 2, 15), pairEquivalents: 1 };
const report = { netSales: 127, foodCost: 24, grossProfit: 103, totalOrders: 6, foodCostPercentage: 18.9,
  grossMarginPercentage: 81.1, items: [{ ...regular, quantity: 6 }], cashRegisters: [],
  salesBreakdown: [regular, tuesday, thursday, unknown, ribeye], promotions: [tuesday, thursday, ribeye] };
// Intercepted fixture HTML lacks Chromium's usual loopback address-space classification.
const browser = await chromium.launch({ channel: 'msedge', headless: true, args: ['--disable-features=LocalNetworkAccessChecks'] });
try {
  for (const width of [390, 1440]) {
    const context = await browser.newContext({ viewport: { width, height: 1000 } });
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', e => errors.push(e.message));
    page.on('console', e => { if (e.type() === 'error') console.error(e.text()); });
    let requests = 0;
    await page.route('**/fixture-sales-report', route => route.fulfill({ contentType: 'text/html', body: html }));
    await page.route('**/api/**', route => {
      const url = new URL(route.request().url());
      if (url.pathname.endsWith('/cash/ventas/rentabilidad')) {
        requests++;
        assert.ok(url.searchParams.get('from')); assert.ok(url.searchParams.get('to'));
        return route.fulfill({ json: report });
      }
      return route.fulfill({ json: [] });
    });
    await page.goto(`${base}/fixture-sales-report`);
    try { await page.getByText('Martes de alitas', { exact: true }).waitFor(); }
    catch (error) {
      console.error({ errors, requests, text: await page.locator('body').innerText() });
      throw error;
    }
    const rows = () => page.locator('tr.ant-table-row');
    assert.equal(await rows().count(), 5);
    await page.screenshot({ path: resolve(output, `detail-${width}.png`), fullPage: true });
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'No page-level horizontal overflow');
    const search = page.getByPlaceholder('Buscar plato o promocion');
    await search.fill('jueves');
    assert.equal(await rows().count(), 1);
    assert.match(await rows().innerText(), /4\.00/);
    await page.getByText('Promociones', { exact: true }).click();
    assert.equal(await rows().count(), 1);
    assert.match(await rows().innerText(), /Jueves de la reliquia glotona/);
    await search.fill('');
    assert.equal(await rows().count(), 3);
    await page.getByText('Por plato', { exact: true }).click();
    assert.equal(await rows().count(), 1);
    await page.getByText('Detalle', { exact: true }).click();
    await page.getByRole('combobox', { name: 'Tipo de venta', exact: true }).click();
    await page.locator('.ant-select-dropdown').getByText('Sin promocion registrada', { exact: true }).click();
    assert.equal(await rows().count(), 1);
    assert.match(await rows().innerText(), /2\.00/);
    assert.equal(requests, 1, 'Presentation filters do not refetch or change financial totals');
    await page.screenshot({ path: resolve(output, `sales-${width}.png`), fullPage: true });
    assert.deepEqual(errors, []);
    await context.close();
    console.log(`Sales promotion report browser checks passed at ${width}px.`);
  }
} finally { await browser.close(); }
