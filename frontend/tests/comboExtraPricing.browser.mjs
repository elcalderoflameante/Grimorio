import assert from 'node:assert/strict';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
const { chromium } = await import(pathToFileURL(resolve(process.env.PLAYWRIGHT_MODULE || '.tmp/pos-browser-tools/node_modules/playwright/index.mjs')).href);
const base = process.env.COMBO_TEST_URL || 'http://127.0.0.1:5173';
const html = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head><body style="margin:0;padding:12px"><div id="root"></div><script type="module">
import RefreshRuntime from '/@react-refresh'; RefreshRuntime.injectIntoGlobalHook(window);
window.$RefreshReg$=()=>{};window.$RefreshSig$=()=>type=>type;window.__vite_plugin_react_preamble_installed__=true;
await import('/@vite/client');await import('/tests/fixtures/ComboExtraPricingHarness.tsx');</script></body></html>`;
const combo = { id: 'combo', name: 'Combo hamburguesa alitas', menuCategoryId: 'category', categoryName: 'Combos',
  price: 10, isCombo: true, isActive: true, availableForSale: true, saleVisibility: 'MenuAndPromotions', hasModifiers: true,
  taxRatePercentage: 15, modifierGroups: [{ id: 'extras', name: 'Hamburguesa / Carne', isRequired: false,
    minSelections: 0, maxSelections: 1, allowDuplicates: false, isActive: true,
    options: [{ id: 'meat', name: 'Agrandar carne', priceDelta: 1.5, isTracked: false, isActive: true }] }] };
const promo = { id: 'promo', name: 'Lunes del mago', type: 'FixedPrice', fixedPrice: 6.99, paymentPolicy: 'CardAlternativePrice', cardPrice: 7.99,
  isActive: true, isCurrentlyActive: true, daysOfWeekMask: 0, menuItemIds: ['combo'], menuCategoryIds: [] };
mkdirSync(resolve('.tmp/combo-extra-pricing'), { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true, args: ['--disable-features=LocalNetworkAccessChecks'] });
try {
  for (const width of [390, 1440]) {
    const page = await browser.newPage({ viewport: { width, height: 900 } });
    const errors = [], writes = []; page.on('pageerror', error => { errors.push(error.message); console.error(error.message); });
    let saved;
    await page.route('**/__combo_extras*', route => route.fulfill({ contentType: 'text/html', body: html }));
    await page.route('**/api/**', route => {
      const path = new URL(route.request().url()).pathname, method = route.request().method();
      if (method !== 'GET') {
        assert.ok(path.endsWith('/pos/ordenes') || path.endsWith('/pos/ordenes/fixture/items'));
        const data = route.request().postDataJSON(); writes.push(data);
        assert.deepEqual(data.items[0].modifierSelections, [{ modifierOptionId: 'meat', quantity: 1 }]);
        assert.equal(data.items[0].promotionId, 'promo');
        saved = { id: 'fixture', number: 1, type: 'Takeout', status: 'Draft', total: 8.49,
          items: [{ id: 'line', menuItemId: 'combo', itemName: combo.name, status: 'Pending', quantity: 1, unitPrice: 11.5,
            totalPrice: 8.49, discountAmount: 3.01, promotionId: 'promo', modifierSelections: [{ modifierOptionId: 'meat', quantity: 1, unitPriceDelta: 1.5 }] }] };
        return route.fulfill({ json: saved });
      }
      if (path.endsWith('/menu/categorias')) return route.fulfill({ json: [{ id: 'category', name: 'Combos' }] });
      if (path.endsWith('/menu/items')) return route.fulfill({ json: [combo] });
      if (path.endsWith('/menu/items/combo')) return route.fulfill({ json: combo });
      if (path.endsWith('/pos/promociones/activas') || path.endsWith('/pos/promociones')) return route.fulfill({ json: [promo] });
      if (path.endsWith('/pos/ordenes/fixture')) return route.fulfill({ json: saved });
      return route.fulfill({ json: [] });
    });
    await page.goto(`${base}/__combo_extras`);
    await page.getByRole('button', { name: 'Promociones', exact: true }).click();
    await page.getByText(promo.name, { exact: true }).click();
    await page.locator('.ant-card').filter({ hasText: combo.name }).click();
    const modal = page.getByRole('dialog'); await modal.getByText('+$1.50', { exact: true }).waitFor();
    await modal.getByRole('button', { name: 'plus', exact: true }).click();
    await modal.getByRole('button', { name: 'Agregar al pedido', exact: true }).click();
    const total = () => page.getByText('Total (IVA incluido)', { exact: true }).locator('xpath=ancestor::div[1]');
    await total().getByText('$8.49', { exact: true }).waitFor();
    await page.screenshot({ path: resolve(`.tmp/combo-extra-pricing/new-${width}.png`), fullPage: true });
    await page.getByRole('button', { name: /Guardar borrador/ }).click();
    await page.waitForFunction(() => document.body.dataset.saved === 'yes'); assert.equal(writes.length, 1);
    await page.goto(`${base}/__combo_extras?reopen=1`);
    await total().getByText('$8.49', { exact: true }).waitFor();
    await page.getByRole('button', { name: /Guardar borrador/ }).click();
    await page.waitForFunction(() => document.body.dataset.saved === 'yes'); assert.equal(writes.length, 2);
    assert.deepEqual(errors, []);
    console.log(`Combo extras ${width}px: surcharge shown, selected promotion total $8.49, save/reopen/resave preserves selection and price; only mocked writes.`);
    await page.close();
  }
} finally { await browser.close(); }
