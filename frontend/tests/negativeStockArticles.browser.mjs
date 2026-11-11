import assert from 'node:assert/strict';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const { chromium } = await import(pathToFileURL(resolve(process.env.PLAYWRIGHT_MODULE || '.tmp/pos-browser-tools/node_modules/playwright/index.mjs')).href);
const base = process.env.INVENTORY_TEST_URL || 'http://127.0.0.1:5173';
const output = resolve('.tmp/negative-stock-ui');
mkdirSync(output, { recursive: true });
const html = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head><body style="margin:0;padding:16px"><div id="root"></div>
<script type="module">
import RefreshRuntime from '/@react-refresh';
RefreshRuntime.injectIntoGlobalHook(window);
window.$RefreshReg$ = () => {}; window.$RefreshSig$ = () => type => type;
window.__vite_plugin_react_preamble_installed__ = true;
await import('/@vite/client');
await import('/tests/fixtures/InventoryArticleHarness.tsx');
</script></body></html>`;
const browser = await chromium.launch({ channel: 'msedge', headless: true });
try {
  for (const width of [390, 1440]) {
    const page = await browser.newPage({ viewport: { width, height: 1000 } });
    const errors = [], writes = [];
    const common = { categoryId: 'ingredients', categoryName: 'Ingredientes', baseUnitId: 'g', baseUnitName: 'Gramo', baseUnitSymbol: 'g', minStock: 0, stockAlertActive: true, isActive: true };
    let articles = [
      { ...common, id: 'lettuce', name: 'Lechuga', type: 'Ingredient', totalStock: -70, lowStock: true, allowNegativeStock: true },
      { ...common, id: 'soda', name: 'Gaseosa', type: 'FinishedProduct', totalStock: 0, lowStock: true },
    ];
    page.on('pageerror', error => errors.push(error.message));
    await page.route('**/__negative_stock_check*', route => route.fulfill({ contentType: 'text/html', body: html }));
    await page.route('**/api/**', route => {
      const request = route.request(), url = new URL(request.url());
      if (request.method() === 'PUT' && url.pathname.endsWith('/articulos/lettuce')) {
        const payload = request.postDataJSON(); writes.push(payload);
        articles = articles.map(a => a.id === 'lettuce' ? { ...a, ...payload } : a);
        return route.fulfill({ json: articles[0] });
      }
      if (request.method() !== 'GET') throw new Error('Unexpected write in UI check');
      if (url.pathname.endsWith('/articulos')) return route.fulfill({ json: articles });
      if (url.pathname.endsWith('/categorias')) return route.fulfill({ json: [{ id: 'ingredients', name: 'Ingredientes' }] });
      if (url.pathname.endsWith('/unidades')) return route.fulfill({ json: [{ id: 'g', name: 'Gramo', symbol: 'g' }] });
      return route.fulfill({ json: [] });
    });
    await page.goto(`${base}/__negative_stock_check?manage=1`);
    await page.getByText('Lechuga', { exact: true }).waitFor();
    const edit = () => page.getByRole('row').filter({ hasText: 'Lechuga' }).getByRole('button').filter({ has: page.locator('[aria-label="edit"]') });
    await edit().click();
    await page.getByRole('dialog').waitFor();
    assert.equal(await page.locator('#allowNegativeStock').getAttribute('aria-checked'), 'true');
    await page.screenshot({ path: resolve(output, `article-${width}.png`), fullPage: true });
    await page.locator('#allowNegativeStock').click();
    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
    await page.getByRole('dialog').waitFor({ state: 'hidden', timeout: 8000 }).catch(async error => {
      console.error('Errors:', errors, 'Writes:', writes, 'Dialog:', await page.getByRole('dialog').innerText());
      await page.screenshot({ path: resolve(output, `failure-${width}.png`), fullPage: true });
      throw error;
    });
    assert.equal(writes.at(-1).allowNegativeStock, false);
    await edit().click();
    assert.equal(await page.locator('#allowNegativeStock').getAttribute('aria-checked'), 'false');
    await page.locator('#allowNegativeStock').click();
    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
    await page.getByRole('dialog').waitFor({ state: 'hidden' });
    assert.equal(writes.at(-1).allowNegativeStock, true);
    await page.getByRole('button', { name: /Nuevo artículo/ }).click();
    assert.equal(await page.locator('#allowNegativeStock').getAttribute('aria-checked'), 'false');
    assert.equal(articles[0].totalStock, -70, 'Policy editing must not rewrite stock');
    await page.goto(`${base}/__negative_stock_check`);
    await page.getByText('Lechuga', { exact: true }).waitFor();
    assert.equal(await page.getByRole('button', { name: /Nuevo artículo/ }).count(), 0);
    assert.equal(await page.locator('[aria-label="edit"]').count(), 0);
    assert.deepEqual(errors, []);
    assert.equal(writes.length, 2);
    await page.close();
    console.log(`Negative-stock setting ${width}px: explicit opt-in/out, default off, old payload and view-only user passed; mock API only.`);
  }
} finally { await browser.close(); }
