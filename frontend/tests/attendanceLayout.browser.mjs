import assert from 'node:assert/strict';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const { chromium } = await import(pathToFileURL(resolve(process.env.PLAYWRIGHT_MODULE || '.tmp/pos-browser-tools/node_modules/playwright/index.mjs')).href);
const base = process.env.ATTENDANCE_TEST_URL || 'http://127.0.0.1:5173';
const output = resolve('.tmp/attendance-layout');
mkdirSync(output, { recursive: true });
const html = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<style>body{margin:0;background:#f5f5f5}#root{margin-left:250px;padding:24px;min-width:0}@media(max-width:767px){#root{margin-left:0;padding:12px}}</style></head><body><div id="root"></div>
<script type="module">
import RefreshRuntime from '/@react-refresh';
RefreshRuntime.injectIntoGlobalHook(window);
window.$RefreshReg$ = () => {}; window.$RefreshSig$ = () => type => type;
window.__vite_plugin_react_preamble_installed__ = true;
await import('/@vite/client');
await import('/tests/fixtures/AttendanceHarness.tsx');
</script></body></html>`;
const rows = ['Felipe Chica Salazar', 'Gisell Estefania Pin Aguirre'].map((employeeName, i) => ({
  id: `clock-${i}`, employeeId: `employee-${i}`, employeeName, workDate: '2026-11-11', status: i ? 2 : 1,
  clockInTimeUtc: '2026-11-11T18:55:00Z', breakStartedAtUtc: '2026-11-11T22:07:00Z',
  breakEndedAtUtc: '2026-11-11T22:37:00Z', clockInMethod: i ? 1 : 3,
  breakMinutes: 30, lateMinutes: i ? 15 : 0, earlyArrivalMinutes: 0,
  workedMinutes: i ? 480 : 0, overtimeMinutes: i ? 60 : 0, correctionCount: i ? 0 : 1,
}));
const browser = await chromium.launch({ channel: 'msedge', headless: true });
try {
  for (const width of [390, 1024, 1280, 1440]) {
    const page = await browser.newPage({ viewport: { width, height: 1000 } });
    const errors = [];
    page.on('pageerror', error => { errors.push(error.message); console.error(error.stack); });
    await page.route('**/__attendance_layout*', route => route.fulfill({ contentType: 'text/html', body: html }));
    await page.route('**/api/**', route => {
      assert.equal(route.request().method(), 'GET', 'Layout checks must not write data');
      const path = new URL(route.request().url()).pathname;
      if (path.endsWith('/clockings')) return route.fulfill({ json: rows });
      if (path.endsWith('/corrections')) return route.fulfill({ json: [{ id: 'correction', correctedByUserId: 'admin', correctedAtUtc: '2026-11-11T23:00:00Z', reason: 'Test correction', beforeJson: '{}', afterJson: '{}' }] });
      return route.fulfill({ json: [] });
    });
    await page.goto(`${base}/__attendance_layout?manage=1`);
    await page.getByText(rows[0].employeeName, { exact: true }).waitFor();
    await page.waitForTimeout(350);
    const overflow = await page.locator('.attendance-table').evaluate(table => {
      const content = table.querySelector('.ant-table-content');
      return { table: content.scrollWidth - content.clientWidth, document: document.documentElement.scrollWidth - innerWidth };
    });
    assert.ok(overflow.table <= 1 && overflow.document <= 1, `${width}px overflow: ${JSON.stringify(overflow)}`);
    if (width < 1200) {
      await page.locator('.attendance-table .ant-table-row-expand-icon').first().click();
      await page.screenshot({ path: resolve(output, `expanded-${width}.png`), fullPage: true });
      await page.getByText('Inicio descanso', { exact: true }).waitFor({ timeout: 8000 }).catch(error => { console.error(errors); throw error; });
      assert.ok(await page.getByText('Horas extra', { exact: true }).count());
    } else {
      assert.equal(await page.locator('.attendance-table .ant-table-row-expand-icon').count(), 0);
      assert.equal(await page.locator('.attendance-table .attendance-nowrap').first().evaluate(cell => cell.scrollWidth > cell.clientWidth), false);
      assert.equal(await page.locator('.attendance-table .ant-tag-orange', { hasText: 'Manual' }).count(), 1);
      assert.equal(await page.locator('.attendance-table .ant-tag-cyan', { hasText: 'Biométrico' }).count(), 1);
      assert.equal(await page.locator('.attendance-status-stack').first().evaluate(element =>
        getComputedStyle(element).alignItems), 'center');
    }
    await page.screenshot({ path: resolve(output, `attendance-${width}.png`), fullPage: true });
    await page.getByRole('button', { name: 'Corregir marcaci\u00f3n', exact: true }).first().click();
    await page.getByRole('dialog').waitFor();
    assert.ok((await page.getByRole('dialog').innerText()).includes(rows[0].employeeName));
    await page.getByRole('dialog').getByRole('button', { name: 'Close', exact: true }).click();
    await page.getByRole('dialog').waitFor({ state: 'hidden' });
    await page.getByRole('button', { name: 'Historial (1)', exact: true }).click();
    await page.getByText('Test correction', { exact: true }).waitFor();
    assert.deepEqual(errors, []);
    await page.goto(`${base}/__attendance_layout`);
    await page.getByText(rows[0].employeeName, { exact: true }).waitFor();
    assert.equal(await page.getByRole('button', { name: 'Corregir marcaci\u00f3n', exact: true }).count(), 0);
    console.log(`Attendance ${width}px: no horizontal overflow; details, actions and read-only permissions passed.`);
    await page.close();
  }
} finally { await browser.close(); }
