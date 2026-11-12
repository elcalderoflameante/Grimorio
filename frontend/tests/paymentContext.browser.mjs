import assert from 'node:assert/strict';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const { chromium } = await import(pathToFileURL(resolve(process.env.PLAYWRIGHT_MODULE || '.tmp/pos-browser-tools/node_modules/playwright/index.mjs')).href);
const base = process.env.PAYMENT_TEST_URL || 'http://127.0.0.1:5173';
const output = resolve('.tmp/payment-context');
mkdirSync(output, { recursive: true });
const html = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head><body style="margin:0;padding:12px"><div id="root"></div>
<script type="module">
import RefreshRuntime from '/@react-refresh';
RefreshRuntime.injectIntoGlobalHook(window);
window.$RefreshReg$ = () => {}; window.$RefreshSig$ = () => type => type;
window.__vite_plugin_react_preamble_installed__ = true;
await import('/@vite/client');
await import('/tests/fixtures/PaymentContextHarness.tsx');
</script></body></html>`;
const items = Array.from({ length: 30 }, (_, i) => ({
  id: `item-${i}`, menuItemId: `menu-${i}`, itemName: `Plato ${i + 1}`, quantity: 2, status: 'Ready',
  unitPrice: 5, totalPrice: 8, discountAmount: 2, promotionCashDiscountAmount: 2, promotionCardDiscountAmount: 0,
  modifierSelections: [],
}));
const order = { id: 'fixture', number: 597, status: 'Ready', type: 'Takeout', items, subtotal: 240, total: 240, tax: 0, discount: 60, createdAt: '2026-11-11T18:00:00Z' };
const browser = await chromium.launch({ channel: 'msedge', headless: true });
try {
  for (const [width, height] of [[390, 700], [660, 570], [1440, 900]]) {
    const page = await browser.newPage({ viewport: { width, height } });
    const errors = [];
    page.on('pageerror', error => { errors.push(error.message); console.error(error.stack); });
    await page.route('**/__payment_context*', route => route.fulfill({ contentType: 'text/html', body: html }));
    await page.route('**/api/**', route => {
      assert.equal(route.request().method(), 'GET', 'Never register real payments');
      const path = new URL(route.request().url()).pathname;
      if (path.endsWith('/pos/ordenes/fixture')) return route.fulfill({ json: order });
      if (path.endsWith('/sesion-activa')) return route.fulfill({ json: { id: 'session' } });
      if (path.endsWith('/metodos-pago')) return route.fulfill({ json: [
        { id: 'cash', name: 'Efectivo', isCash: true, isActive: true },
        { id: 'card', name: 'Tarjeta', isCard: true, isActive: true },
      ] });
      return route.fulfill({ json: [] });
    });
    await page.goto(`${base}/__payment_context`);
    await page.getByRole('button', { name: 'Cobrar', exact: true }).click();
    const modal = page.locator('.order-payment-modal');
    await modal.getByRole('tab', { name: 'Cuenta dividida' }).click();
    const subtotal = id => modal.getByLabel(`Subtotal de Cliente ${id}`, { exact: true });
    await subtotal(1).waitFor();
    await modal.getByRole('checkbox', { name: 'Seleccionar Plato 1', exact: true }).check();
    assert.equal(await subtotal(1).innerText(), '$8.00');
    await modal.getByRole('spinbutton', { name: 'Cantidad de Plato 1', exact: true }).fill('1');
    await modal.getByRole('spinbutton', { name: 'Cantidad de Plato 1', exact: true }).press('Tab');
    assert.equal(await subtotal(1).innerText(), '$4.00');
    await modal.getByRole('button', { name: 'Agregar cliente', exact: true }).click();
    assert.equal(await subtotal(2).innerText(), '$0.00');
    await modal.getByRole('checkbox', { name: 'Seleccionar Plato 2', exact: true }).check();
    assert.equal(await subtotal(2).innerText(), '$8.00');
    await modal.getByRole('tab', { name: 'Cliente 1', exact: true }).click();
    assert.equal(await subtotal(1).innerText(), '$4.00');
    const body = modal.locator('.ant-modal-body');
    await body.evaluate(element => { element.scrollTop = element.scrollHeight; });
    await page.waitForTimeout(350);
    await modal.screenshot({ path: resolve(output, `payment-${width}.png`) });
    const bounds = await modal.evaluate(element => {
      const rect = selector => element.querySelector(selector).getBoundingClientRect();
      const context = rect('.client-payment-context'), body = rect('.ant-modal-body'), title = rect('.order-payment-title');
      return { contextTop: context.top, contextBottom: context.bottom, bodyTop: body.top, bodyBottom: body.bottom, titleBottom: title.bottom,
        overflow: element.scrollWidth - element.clientWidth, scrollTop: element.querySelector('.ant-modal-body').scrollTop };
    });
    assert.ok(bounds.scrollTop > 100, 'Long order must genuinely scroll');
    assert.ok(Math.abs(bounds.contextTop - bounds.bodyTop) < 2, `Client summary must stick: ${JSON.stringify(bounds)}`);
    assert.ok(bounds.contextBottom <= bounds.bodyBottom && bounds.titleBottom <= bounds.bodyTop + 2);
    assert.ok(bounds.titleBottom > 0 && bounds.bodyBottom <= height);
    assert.ok(bounds.overflow <= 1);
    assert.equal(await modal.getByLabel('Saldo pendiente', { exact: true }).innerText(), '$240.00');
    await modal.locator('.ant-select').filter({ hasText: 'Efectivo' }).click();
    await page.locator('.ant-select-dropdown:visible .ant-select-item-option').filter({ hasText: 'Tarjeta' }).click();
    assert.equal(await subtotal(1).innerText(), '$5.00');
    assert.equal(await modal.getByLabel('Saldo pendiente', { exact: true }).innerText(), '$300.00');
    assert.deepEqual(errors, []);
    console.log(`Payment ${width}x${height}: fixed balance and client subtotal, selection quantities and independent clients passed; no writes.`);
    await page.close();
  }
} finally { await browser.close(); }
