import assert from 'node:assert/strict';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { setTimeout as delay } from 'node:timers/promises';
import { createRequire } from 'node:module';

const { chromium } = await import(pathToFileURL(resolve(process.env.PLAYWRIGHT_MODULE || '.tmp/pos-browser-tools/node_modules/playwright/index.mjs')).href);
const base = process.env.TABLES_TEST_URL || 'http://127.0.0.1:5173';
const realtime = process.env.TABLES_REALTIME_CHECK_URL || 'http://127.0.0.1:5199';
const branchA = '11111111-1111-4111-8111-111111111111';
const branchB = '22222222-2222-4222-8222-222222222222';
const { HubConnectionBuilder, LogLevel } = createRequire(new URL('../package.json', import.meta.url))('@microsoft/signalr');
async function notify(token = 'branch-a', path = 'notify') {
  const response = await fetch(`${realtime}/fixture/${path}`, { method: 'POST', headers: { Authorization: `Bearer ${token}` } });
  assert.equal(response.status, 200);
}
async function connected(branch) {
  for (let i = 0; i < 100; i++) {
    const connections = await (await fetch(`${realtime}/fixture/connections`)).json();
    if (connections.includes(branch)) return;
    await delay(50);
  }
  throw new Error('Fixture SignalR connection did not start');
}
const output = resolve('.tmp/tables-map-refresh');
mkdirSync(output, { recursive: true });
const html = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head><body style="margin:0;padding:16px"><div id="root"></div>
<script type="module">
import RefreshRuntime from '/@react-refresh';
RefreshRuntime.injectIntoGlobalHook(window);
window.$RefreshReg$ = () => {}; window.$RefreshSig$ = () => type => type;
window.__vite_plugin_react_preamble_installed__ = true;
await import('/@vite/client');
await import('/tests/fixtures/TablesMapHarness.tsx');
</script></body></html>`;
const table = (branch, occupied) => ({
  id: `table-${branch}`, code: branch === branchA ? '1' : '22', branchId: branch,
  isActive: true, capacity: 4, area: 'Principal', currentStatus: occupied ? 'Occupied' : 'Free',
  currentOrderId: occupied ? 'order-new' : undefined, currentOrderWaiterName: occupied ? 'Mesero nuevo' : undefined,
  currentOrderStartedAt: occupied ? '2026-11-12T16:26:00Z' : undefined, pendingPaymentTotal: occupied ? 20.5 : 0,
});
// Intercepted fixture HTML lacks Chromium's normal loopback address-space classification.
const browser = await chromium.launch({ channel: 'msedge', headless: true, args: ['--disable-features=LocalNetworkAccessChecks'] });
try {
  const eventsA = [], eventsB = [];
  const connection = token => new HubConnectionBuilder().withUrl(`${realtime}/hubs/kitchen`, { accessTokenFactory: () => token }).configureLogging(LogLevel.None).build();
  const a = connection('branch-a'), b = connection('branch-b'), anonymous = connection('not-authorized');
  try {
    await assert.rejects(() => anonymous.start());
    a.on('pos:tables-changed', payload => eventsA.push(payload));
    b.on('pos:tables-changed', payload => eventsB.push(payload));
    await a.start(); await b.start();
    await connected(branchA); await connected(branchB);
    await notify();
    for (let i = 0; eventsA.length < 1 && i < 100; i++) await delay(20);
    assert.deepEqual(eventsA, [{ branchId: branchA }]);
    assert.deepEqual(eventsB, []);
    await notify('branch-b');
    for (let i = 0; eventsB.length < 1 && i < 100; i++) await delay(20);
    assert.deepEqual(eventsA, [{ branchId: branchA }]);
    assert.deepEqual(eventsB, [{ branchId: branchB }]);
    console.log('Real SignalR clients: unauthenticated access rejected, branch groups isolated and payload contains no order/customer data.');
  } finally { await a.stop(); await b.stop(); await anonymous.stop(); }
  for (const width of [390, 1440]) {
    const page = await browser.newPage({ viewport: { width, height: 900 } });
    await page.clock.install({ time: new Date('2026-11-12T17:00:00Z') });
    let occupied = false, failed = false, hold = false, release;
    const errors = [], requests = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') console.error(message.text()); });
    page.on('requestfailed', request => console.error('Failed fixture request:', request.url(), request.failure()?.errorText));
    await page.route('**/__tables_refresh*', route => route.fulfill({ contentType: 'text/html', body: html }));
    // Exercise the production subscriber against the isolated real SignalR host, not the ERP.
    await page.route('**/src/services/tableMapRealtime.ts*', route => {
      if (route.request().url().includes('fixture-real')) return route.continue();
      return route.fulfill({ contentType: 'application/javascript', body:
        `import {subscribeTableMap as subscribe} from '/src/services/tableMapRealtime.ts?fixture-real';
         export function subscribeTableMap(options){return subscribe({...options,apiUrl:${JSON.stringify(`${realtime}/api`)}});}` });
    });
    await page.route('**/api/tableservice/tables*', async route => {
      assert.equal(route.request().method(), 'GET');
      const branch = new URL(route.request().url()).searchParams.get('branchId');
      requests.push(branch);
      const payload = [table(branch, occupied)];
      if (hold && branch === branchA) await new Promise(resolve => { release = resolve; });
      await route.fulfill(failed ? { status: 503, json: { message: 'Sin conexion de prueba' } } : { json: payload });
    });
    await page.goto(`${base}/__tables_refresh`);
    await page.getByText('Libre', { exact: true }).waitFor();
    await connected(branchA);
    await page.clock.runFor(500);
    occupied = true;
    await notify();
    await page.clock.runFor(300);
    await page.getByText('Mesero nuevo', { exact: true }).waitFor();
    await page.getByText('34 min', { exact: true }).waitFor();
    await page.getByText('Mesa 1', { exact: true }).click();
    assert.equal(await page.locator('body').getAttribute('data-selected-table'), 'order-new');
    const beforeQuietPeriod = requests.length;
    await page.clock.fastForward(15_000);
    await delay(50);
    assert.equal(requests.length, beforeQuietPeriod, 'No 15-second backup requests');
    await notify('branch-b');
    await page.clock.runFor(300);
    assert.equal(requests.length, beforeQuietPeriod, 'Other branches must not trigger refresh');

    failed = true;
    await notify();
    await page.clock.runFor(300);
    await page.getByText('No se pudo actualizar el mapa de mesas', { exact: true }).waitFor();
    await page.clock.fastForward(60_000);
    await page.getByText('35 min', { exact: true }).waitFor();
    assert.equal(await page.getByText('Mesa 1', { exact: true }).count(), 1, 'Failure must retain the map');
    failed = false; occupied = false;
    await page.evaluate(() => window.dispatchEvent(new Event('focus')));
    await page.getByText('Libre', { exact: true }).waitFor();
    await page.getByText('No se pudo actualizar el mapa de mesas', { exact: true }).waitFor({ state: 'hidden' });
    occupied = true;
    await page.clock.fastForward(120_000);
    await page.getByText('Mesero nuevo', { exact: true }).waitFor();

    await page.evaluate(() => {
      window.__hidden = true;
      Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => window.__hidden ? 'hidden' : 'visible' });
      document.dispatchEvent(new Event('visibilitychange'));
    });
    const hiddenRequests = requests.length;
    await page.clock.fastForward(45_000);
    assert.equal(requests.length, hiddenRequests, 'Hidden tabs must not poll');
    occupied = true;
    await page.evaluate(() => { window.__hidden = false; document.dispatchEvent(new Event('visibilitychange')); });
    await page.getByText('Mesero nuevo', { exact: true }).waitFor();
    occupied = false;
    await notify('branch-a', 'disconnect');
    await page.clock.runFor(1_000);
    await page.getByText('Libre', { exact: true }).waitFor();
    await connected(branchA);
    occupied = true;
    await notify();
    await page.clock.runFor(300);
    await page.getByText('Mesero nuevo', { exact: true }).waitFor();
    await page.mouse.move(width - 2, 880);
    await page.screenshot({ path: resolve(output, `tables-${width}.png`), fullPage: true });

    hold = true;
    await notify();
    await page.clock.runFor(300);
    for (let i = 0; !release && i < 50; i++) await delay(20);
    assert.ok(release, 'Slow request started');
    const slowRequests = requests.length;
    await page.clock.fastForward(45_000);
    assert.equal(requests.length, slowRequests, 'Polling must not overlap slow requests');
    assert.equal(await page.getByText('Mesa 1', { exact: true }).count(), 1, 'Silent refresh must not blank the map');
    occupied = false;
    await notify();
    await page.clock.runFor(300);
    hold = false; release();
    await page.getByText('Libre', { exact: true }).waitFor();
    assert.ok(requests.length > slowRequests, 'An event received during an in-flight fetch must trigger a trailing refresh');
    hold = true; release = undefined;
    await notify();
    await page.clock.runFor(300);
    for (let i = 0; !release && i < 50; i++) await delay(20);
    assert.ok(release);
    await page.getByRole('button', { name: 'Cambiar sucursal', exact: true }).click();
    await page.getByText('Mesa 22', { exact: true }).waitFor();
    release();
    await delay(100);
    assert.equal(await page.getByText('Mesa 1', { exact: true }).count(), 0, 'Late response from previous branch must be ignored');
    assert.equal(await page.getByText('Mesa 22', { exact: true }).count(), 1);
    await connected(branchB);
    await page.clock.runFor(300);

    await page.getByRole('button', { name: 'Salir del mapa', exact: true }).click();
    const unmountedRequests = requests.length;
    await page.clock.fastForward(60_000);
    await page.evaluate(() => window.dispatchEvent(new Event('focus')));
    await delay(50);
    assert.equal(requests.length, unmountedRequests, 'Unmount must remove timers and listeners');
    assert.deepEqual(errors, []);
    console.log(`Tables map ${width}px: real SignalR, reconnect, 2-minute fallback, independent clock, focus/visibility, trailing refresh, branch isolation and cleanup passed.`);
    await page.close();
  }
} finally { await browser.close(); }
