import assert from 'node:assert/strict';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { setTimeout as delay } from 'node:timers/promises';
import { createRequire } from 'node:module';
const { chromium } = await import(pathToFileURL(resolve(process.env.PLAYWRIGHT_MODULE || '.tmp/pos-browser-tools/node_modules/playwright/index.mjs')).href);
const { HubConnectionBuilder, LogLevel } = createRequire(new URL('../package.json', import.meta.url))('@microsoft/signalr');
const base = process.env.TABLE_ALERTS_TEST_URL || 'http://127.0.0.1:5173';
const host = 'http://127.0.0.1:5198';
const A = '11111111-1111-4111-8111-111111111111', B = '22222222-2222-4222-8222-222222222222';
const row = (id, branchId = A) => ({ id, branchId, tableId: '33333333-3333-4333-8333-333333333333', tableCode: branchId === A ? '5' : '22', tableArea: 'Principal', type: 1, status: 1, requestedAt: new Date(Date.now() - 3 * 86400000).toISOString() });
async function notify(request = row('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'), path = 'notify') {
  const response = await fetch(`${host}/fixture/${path}`, { method: 'POST', headers: { Authorization: `Bearer ${request.branchId === A ? 'branch-a' : 'branch-b'}`, 'Content-Type': 'application/json' }, body: JSON.stringify(request) });
  assert.equal(response.status, 200);
}
async function waitUntil(check) {
  for (let i = 0; i < 120; i++) { if (await check()) return; await delay(50); }
  throw new Error('Timed out waiting for fixture state');
}
async function connected(branch) { await waitUntil(async () => (await (await fetch(`${host}/fixture/connections`)).json()).includes(branch)); }
const html = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head><body style="margin:0;padding:16px"><div id="root"></div><script type="module">
import RefreshRuntime from '/@react-refresh'; RefreshRuntime.injectIntoGlobalHook(window);
window.$RefreshReg$ = () => {}; window.$RefreshSig$ = () => type => type; window.__vite_plugin_react_preamble_installed__ = true;
await import('/@vite/client'); await import('/tests/fixtures/TableRequestAlertsHarness.tsx');</script></body></html>`;
mkdirSync(resolve('.tmp/table-request-alerts'), { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true, args: ['--disable-features=LocalNetworkAccessChecks'] });
try {
  const eventsA = [], eventsB = [], eventsPublic = [];
  const client = token => new HubConnectionBuilder().withUrl(`${host}/hubs/table-service`, { accessTokenFactory: () => token }).configureLogging(LogLevel.None).build();
  const a = client('branch-a'), b = client('branch-b'), publicClient = client('');
  try {
    for (const [connection, events] of [[a, eventsA], [b, eventsB], [publicClient, eventsPublic]]) {
      connection.on('tableService:new-request', request => events.push(request)); await connection.start();
    }
    await notify(); await waitUntil(() => eventsA.length === 1);
    assert.equal(eventsB.length, 0); assert.equal(eventsPublic.length, 0);
    await notify(row('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', B)); await waitUntil(() => eventsB.length === 1);
    assert.equal(eventsA.length, 1); assert.equal(eventsPublic.length, 0);
    console.log('SignalR: branch isolation and anonymous public QR isolation passed.');
  } finally { await a.stop(); await b.stop(); await publicClient.stop(); }

  for (const width of [390, 1440]) {
    const page = await browser.newPage({ viewport: { width, height: 900 } });
    const errors = []; page.on('pageerror', e => errors.push(e.message));
    await page.addInitScript(() => {
      window.__tones = 0; window.__closes = 0; window.__spoken = [];
      window.AudioContext = class {
        state = 'suspended'; currentTime = 0; destination = {};
        async resume() { this.state = 'running'; } async close() { this.state = 'closed'; window.__closes++; }
        createOscillator() { return { frequency: {}, connect() {}, disconnect() {}, start() { window.__tones++; }, stop() {} }; }
        createGain() { return { gain: { setValueAtTime() {}, linearRampToValueAtTime() {}, exponentialRampToValueAtTime() {} }, connect() {}, disconnect() {} }; }
      };
      window.SpeechSynthesisUtterance = class { constructor(text) { this.text = text; } };
      Object.defineProperty(window, 'speechSynthesis', { value: { getVoices: () => [{ lang: 'es-EC' }], speak(u) { window.__spoken.push(u.text); u.onend?.(); }, cancel() {} } });
    });
    let rows = [row('backlog')], loads = 0, failed = false, hold = false, release;
    let activeBranch = A;
    await page.route('**/__table_alerts', route => route.fulfill({ contentType: 'text/html', body: html }));
    await page.route('**/src/services/tableRequestRealtime.ts*', route => route.request().url().includes('fixture-real') ? route.continue()
      : route.fulfill({ contentType: 'application/javascript', body: `import {subscribeTableRequests as subscribe} from '/src/services/tableRequestRealtime.ts?fixture-real'; export function subscribeTableRequests(options){return subscribe({...options,apiUrl:'${host}/api'});}` }));
    await page.route('**/api/tableservice/requests*', async route => {
      loads++; assert.equal(new URL(route.request().url()).searchParams.get('activeOnly'), 'true');
      const payload = rows.filter(r => r.branchId === activeBranch && r.status < 4);
      if (hold) await new Promise(resolve => { release = resolve; });
      await route.fulfill(failed ? { status: 503, json: { message: 'Error de prueba' } } : { json: payload });
    });
    await page.route('**/api/tableservice/requests/*/*', async route => {
      const parts = new URL(route.request().url()).pathname.split('/');
      const request = rows.find(r => r.id === parts.at(-2)); assert.ok(request);
      request.status = parts.at(-1) === 'take' ? 2 : route.request().postDataJSON().status;
      request.takenByName = 'Cajero de prueba';
      await route.fulfill({ json: request });
    });
    await page.goto(`${base}/__table_alerts`);
    await page.getByRole('button', { name: 'Solicitudes de mesas', exact: true }).click();
    const section = id => page.getByRole('region', { name: `Solicitud ${id}`, exact: true });
    await section('backlog').waitFor(); await connected(A); await delay(400);
    assert.equal(await page.evaluate(() => window.__tones), 0);
    await page.getByRole('switch', { name: 'Sonido de solicitudes' }).click();
    await waitUntil(() => page.evaluate(() => window.__tones === 2));
    rows.push(row('new')); await notify(); await section('new').waitFor();
    await waitUntil(() => page.evaluate(() => window.__tones === 4));
    await notify(); await delay(400); assert.equal(await page.evaluate(() => window.__tones), 4);
    const before = loads; await notify(row('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', B)); await delay(350);
    assert.equal(loads, before);
    await section('new').getByRole('button', { name: /Tomar/ }).click();
    await section('new').getByText('Cajero de prueba', { exact: false }).waitFor();
    await section('new').getByRole('button', { name: /En proceso/ }).click();
    await section('new').getByRole('button', { name: /Completar/ }).click(); await section('new').waitFor({ state: 'hidden' });
    await page.getByRole('switch', { name: 'Voz de solicitudes' }).click();
    const speeches = await page.evaluate(() => window.__spoken.length);
    rows.push(row('muted-voice')); await notify(); await section('muted-voice').waitFor();
    assert.equal(await page.evaluate(() => window.__spoken.length), speeches);
    assert.equal(await page.evaluate(() => window.__tones), 6);
    failed = true; await notify(); await page.locator('.ant-alert').waitFor();
    assert.equal(await section('backlog').count(), 1); failed = false;
    await page.evaluate(() => window.dispatchEvent(new Event('focus'))); await page.locator('.ant-alert').waitFor({ state: 'hidden' });
    await notify(row('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'), 'disconnect');
    await page.getByText('Sin conexion en tiempo real', { exact: true }).waitFor();
    await page.getByText('Sin conexion en tiempo real', { exact: true }).waitFor({ state: 'hidden' });
    await connected(A); await delay(300);
    rows.push(row('after-reconnect')); await notify(); await section('after-reconnect').waitFor();
    await page.screenshot({ path: resolve(`.tmp/table-request-alerts/alerts-${width}.png`), fullPage: true });
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth));
    // A delayed response from the old branch must never repopulate the new branch.
    hold = true; await notify(); await waitUntil(() => !!release);
    activeBranch = B; rows.push(row('branch-b', B)); hold = false;
    await page.keyboard.press('Escape');
    for (const close of await page.locator('.ant-notification-notice-close').all()) await close.click();
    await page.locator('.ant-notification-notice').waitFor({ state: 'hidden' });
    await page.getByRole('button', { name: 'Cambiar sucursal', exact: true }).click();
    await page.getByRole('button', { name: 'Solicitudes de mesas', exact: true }).click();
    await section('branch-b').waitFor(); release(); await delay(300);
    assert.equal(await section('backlog').count(), 0); assert.ok(await page.evaluate(() => window.__closes > 0));
    await page.keyboard.press('Escape'); await page.getByRole('button', { name: 'Solo consulta', exact: true }).click();
    await page.getByRole('button', { name: 'Solicitudes de mesas', exact: true }).click();
    await section('branch-b').waitFor(); assert.equal(await section('branch-b').getByRole('button').count(), 0);
    await page.keyboard.press('Escape'); await page.getByRole('button', { name: 'Sin permiso', exact: true }).click();
    await page.getByRole('button', { name: 'Solicitudes de mesas', exact: true }).waitFor({ state: 'hidden' });
    await delay(300); const finalLoads = loads;
    await page.evaluate(() => window.dispatchEvent(new Event('focus'))); await delay(300);
    assert.equal(loads, finalLoads); assert.deepEqual(errors, []);
    console.log(`Alerts ${width}px: initial silence, opt-in audio, deduplication, voice mute, workflow, failure recovery, reconnect, branch cleanup and permissions passed.`);
    await page.close();
  }
} finally { await browser.close(); }
