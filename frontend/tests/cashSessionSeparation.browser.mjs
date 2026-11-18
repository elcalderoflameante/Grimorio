import assert from 'node:assert/strict';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
const { chromium } = await import(pathToFileURL(resolve(process.env.PLAYWRIGHT_MODULE || '.tmp/pos-browser-tools/node_modules/playwright/index.mjs')).href);
const base = process.env.CASH_SEPARATION_TEST_URL || 'http://127.0.0.1:5173';
const output = resolve('.tmp/cash-session-separation'); mkdirSync(output, { recursive: true });
const html = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head><body><div id="root"></div><script type="module">
import RefreshRuntime from '/@react-refresh'; RefreshRuntime.injectIntoGlobalHook(window);
window.$RefreshReg$ = () => {}; window.$RefreshSig$ = () => type => type; window.__vite_plugin_react_preamble_installed__ = true;
await import('/@vite/client'); await import('/tests/fixtures/CashSessionSeparationHarness.tsx');</script></body></html>`;
const old = { id: 'session-old', cashRegisterId: 'register', cashRegisterName: 'Caja principal', cashRegisterCode: 'MAIN',
  openedByName: 'Cajero original', openedAt: '2026-11-16T17:00:00Z', openingBalance: 50, status: 'Open',
  totals: [{ methodId: 'cash', methodName: 'Efectivo', total: 100, isCash: true }], totalSales: 150,
  totalExpenses: 10, totalCashExpenses: 10, totalOrders: 2, totalCash: 100, expectedCash: 140 };
const preview = { token: 'preview-1', cutoffUtc: '2026-11-17T05:00:00Z', previousSession: { ...old, status: 'Closed' },
  newSession: { ...old, openingBalance: 20, totalSales: 105, totalExpenses: 3, expectedCash: 52 },
  movements: [
    { id: 'p1', kind: 'Payment', reference: 'Orden #597', date: '2026-11-16T23:00:00Z', amount: 150, movesToNewSession: false, isCancelled: false },
    { id: 'p2', kind: 'Payment', reference: 'Orden #598', date: '2026-11-17T05:00:00Z', amount: 105, movesToNewSession: true, isCancelled: false },
  ] };
const browser = await chromium.launch({ channel: 'msedge', headless: true, args: ['--disable-features=LocalNetworkAccessChecks'] });
try {
  for (const width of [390, 1440]) {
    const context = await browser.newContext({ viewport: { width, height: 1000 } });
    const page = await context.newPage(); const errors = []; page.on('pageerror', e => { errors.push(e.message); console.error(e.message); });
    page.on('console', e => { if (e.type() === 'error') console.error(e.text()); });
    await page.clock.install({ time: new Date('2026-11-17T20:00:00Z') });
    let reviews = [], confirms = [], scenario = 'success';
    await page.route('**/fixture-cash-separation**', route => route.fulfill({ contentType: 'text/html', body: html }));
    await page.route('**/api/**', route => {
      const path = new URL(route.request().url()).pathname;
      if (path.endsWith('/separar/vista-previa')) {
        reviews.push(route.request().postDataJSON()); return route.fulfill({ json: preview });
      }
      if (path.endsWith('/separar')) {
        confirms.push(route.request().postDataJSON());
        if (scenario === 'stale') return route.fulfill({ status: 400, json: { message: 'Los movimientos cambiaron. Revisa la vista previa.' } });
        if (scenario === 'retry' && confirms.length === 1) return route.abort('failed');
        return route.fulfill({ json: { id: 'audit', previousSessionId: 'session-old', newSessionId: 'session-new' } });
      }
      if (path.endsWith('/sesion-activa')) return route.fulfill({ json: old });
      return route.fulfill({ json: [] });
    });
    const confirm = () => page.getByRole('button', { name: 'Confirmar separacion', exact: true });
    async function fill() {
      await page.getByRole('spinbutton').fill('20');
      await page.getByLabel('Motivo', { exact: true }).fill('No se cerro la caja de ayer');
    }
    async function review() {
      await page.getByRole('button', { name: 'Revisar corte', exact: true }).click();
      await page.getByRole('cell', { name: /Orden #598/ }).waitFor();
    }
    await page.goto(`${base}/fixture-cash-separation`);
    try { await confirm().waitFor(); }
    catch (e) { console.error(await page.locator('body').innerText()); throw e; }
    assert.equal(await confirm().isDisabled(), true);
    await page.getByRole('button', { name: 'Revisar corte', exact: true }).click();
    assert.equal(reviews.length, 0, 'Incomplete form does not request a preview');
    await fill(); await review();
    assert.equal(reviews[0].cutoff, '2026-11-17T05:00:00.000Z', 'Cutoff uses branch timezone');
    assert.equal(reviews[0].previousActualCash, null);
    assert.equal(await confirm().isEnabled(), true);
    const confirmBox = await confirm().boundingBox();
    assert.ok(confirmBox && confirmBox.y >= 0 && confirmBox.y + confirmBox.height <= 1000, 'Confirmation stays visible inside viewport');
    await page.screenshot({ path: resolve(output, `preview-${width}.png`), fullPage: true });
    await page.getByLabel('Motivo', { exact: true }).fill('Cambio del motivo del corte');
    assert.equal(await confirm().isDisabled(), true, 'Editing form invalidates the preview');
    await review(); await confirm().click();
    await page.waitForFunction(() => document.body.dataset.result === 'session-new');
    assert.equal(confirms.length, 1); assert.equal(confirms[0].previewToken, 'preview-1');
    assert.equal(confirms[0].requestId, reviews.at(-1).requestId);

    scenario = 'retry'; reviews = []; confirms = [];
    await page.goto(`${base}/fixture-cash-separation`); await fill(); await review(); await confirm().click();
    await page.getByRole('button', { name: 'Reintentar confirmacion', exact: true }).click();
    await page.waitForFunction(() => document.body.dataset.result === 'session-new');
    assert.deepEqual(confirms[0], confirms[1], 'Network retry reuses the exact idempotency key and payload');

    scenario = 'stale'; reviews = []; confirms = [];
    await page.goto(`${base}/fixture-cash-separation`); await fill(); await review(); await confirm().click();
    await page.getByText('Los movimientos cambiaron. Revisa la vista previa.', { exact: true }).waitFor();
    assert.equal(await confirm().isDisabled(), true);
    await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
    assert.equal(confirms.length, 1, 'Cancelling does not submit another operation');

    await page.goto(`${base}/fixture-cash-separation?page&permission=no`);
    await page.getByRole('button', { name: /Actualizar/ }).waitFor();
    assert.equal(await page.getByRole('button', { name: 'Separar jornada', exact: true }).count(), 0);
    await page.goto(`${base}/fixture-cash-separation?page`);
    await page.getByRole('button', { name: 'Separar jornada', exact: true }).waitFor();
    assert.deepEqual(errors, []); await context.close();
    console.log(`Cash separation browser checks passed at ${width}px: preview, timezone, stale data, retry and permission gate.`);
  }
} finally { await browser.close(); }
