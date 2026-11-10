import assert from 'node:assert/strict';
import { readFileSync, mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const { chromium } = await import(pathToFileURL(resolve(process.env.PLAYWRIGHT_MODULE || '.tmp/pos-browser-tools/node_modules/playwright/index.mjs')).href);
const base = process.env.HELP_TEST_URL || 'http://127.0.0.1:5173';
const catalog = JSON.parse(readFileSync('docs/manual/catalog.json', 'utf8'));
const readBody = article => readFileSync(resolve('docs/manual', article.file), 'utf8');
const output = resolve('.tmp/help-browser'); mkdirSync(output, { recursive: true });
const assetOutput = resolve('docs/manual/assets'); mkdirSync(assetOutput, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const warehouseId = '45e548d5-06d5-4dd2-a8c0-4b4277e99911';
const areaId = '17e548d5-06d5-4dd2-a8c0-4b4277e99922';
const operatorId = '22e548d5-06d5-4dd2-a8c0-4b4277e99933';
const articles = [{ id: 'lettuce', name: 'Lechuga', unitSymbol: 'kg' }, { id: 'soda', name: 'Bebida de prueba', unitSymbol: 'un' }];
const count = { id: 'check-count', areaId, areaName: 'Cocina de prueba', businessDate: '2026-11-09', counterName: 'Empleado de prueba', status: 'Submitted', submittedAt: '2026-11-10T02:00:00Z',
  lines: articles.map((a, i) => ({ id: a.id, articleId: a.id, articleName: a.name, warehouseName: 'Bodega de prueba', unitSymbol: a.unitSymbol, quantity: i ? 12 : 0.2,
    expectedQuantity: i ? 12 : 0.25, difference: i ? 0 : -0.05, countedAt: '2026-11-10T01:50:00Z', notes: i ? '' : 'Cantidad pesada. Datos ficticios.' })) };

async function setup(page, permissions = null, authenticated = true) {
  const state = { errors: [], writes: [], helpCalls: 0, images: 0, failDetail: false, failSearch: false, failImage: false, malicious: false };
  const payload = { exp: Math.floor(Date.now() / 1000) + 3600, permissions: permissions ?? [], [roleClaim]: permissions === null ? 'Administrador' : 'Personal' };
  const token = 'checks.' + Buffer.from(JSON.stringify(payload)).toString('base64url') + '.test-only';
  if (authenticated) await page.addInitScript(token => {
    if (sessionStorage.getItem('help-checks-seeded')) return;
    sessionStorage.setItem('help-checks-seeded', 'true');
    localStorage.setItem('accessToken', token); localStorage.setItem('branchId', 'test-branch');
    localStorage.setItem('user', JSON.stringify({ firstName: 'Usuario de prueba', lastName: '', roles: ['Administrador'] }));
  }, token);
  page.on('pageerror', error => state.errors.push(error.message));
  await page.route('https://fonts.googleapis.com/**', route => route.abort());
  await page.route('https://fonts.gstatic.com/**', route => route.abort());
  await page.route('**/hubs/**', route => route.fulfill({ status: 503, body: '' }));
  const canRead = article => permissions === null || article.permissionSets.length === 0 || article.permissionSets.some(set => set.every(p => permissions.includes(p)));
  await page.route('**/api/**', async route => {
    const request = route.request(), url = new URL(request.url()), path = url.pathname;
    if (request.method() !== 'GET') { state.writes.push(path); return route.fulfill({ status: 500, json: { message: 'Writes forbidden in help checks' } }); }
    if (path.startsWith('/api/help/')) {
      state.helpCalls++;
      assert.equal(request.headers().authorization, `Bearer ${token}`, 'Help and images use authenticated API calls');
      if (path === '/api/help/articles') {
        const query = url.searchParams.get('q') ?? '';
        if (query && state.failSearch) return route.fulfill({ status: 503, json: {} });
        const normalize = text => text.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase();
        const result = catalog.articles.filter(canRead).filter(article => normalize(query).split(/\s+/).every(term => normalize(`${article.title} ${article.summary} ${article.tags} ${readBody(article)}`).includes(term)));
        return route.fulfill({ json: { edition: catalog.edition, articles: result } });
      }
      if (path.startsWith('/api/help/assets/')) {
        const asset = catalog.assets.find(a => a.id === path.split('/').at(-1));
        if (!asset || !asset.articleIds.some(id => canRead(catalog.articles.find(a => a.id === id)))) return route.fulfill({ status: 404, json: {} });
        state.images++;
        return state.failImage ? route.fulfill({ status: 503, body: '' }) : route.fulfill({ contentType: 'image/png', body: readFileSync(resolve('docs/manual', asset.file)) });
      }
      const article = catalog.articles.find(a => a.id === path.split('/').at(-1));
      if (!article || !canRead(article)) return route.fulfill({ status: 404, json: {} });
      if (state.failDetail) return route.fulfill({ status: 503, json: {} });
      const markdown = state.malicious ? '<script>window.__helpXss=true</script>\n\n[Unsafe](javascript:alert(1))\n\n![tracking](https://tracking.invalid/pixel.png)\n\n**Safe text**' : readBody(article);
      return route.fulfill({ json: { article, markdown } });
    }
    let data = [];
    if (path.endsWith('/branches/current')) data = { name: 'Sucursal de prueba', timeZoneId: 'America/Guayaquil' };
    if (path === '/api/inventory/conteos/configuracion') data = { areas: [{ id: areaId, name: 'Cocina de prueba', isActive: true, userIds: [operatorId], rules: articles.map(a => ({ articleId: a.id, warehouseId, frequency: a.id === 'lettuce' ? 'Daily' : 'Weekly', weekDay: 1 })) }],
      users: [{ id: operatorId, name: 'Empleado de prueba' }], articles, warehouses: [{ id: warehouseId, name: 'Bodega de prueba' }] };
    if (path === '/api/inventory/conteos') data = [count];
    if (path === '/api/inventory/conteos/check-count') data = count;
    return route.fulfill({ json: data });
  });
  return state;
}

try {
  // Real ERP screens with synthetic data only. No POST/PUT requests are allowed.
  const capture = await browser.newPage({ viewport: { width: 1366, height: 1000 } });
  const captureState = await setup(capture);
  await capture.goto(`${base}/dashboard`);
  await capture.locator('.dashboard-sider .ant-menu-submenu-title').filter({ hasText: /^Inventario/ }).click();
  await capture.locator('.dashboard-sider .ant-menu-item').filter({ hasText: 'Conteos del personal' }).click();
  await capture.getByRole('tab', { name: 'Áreas y frecuencias', exact: true }).click();
  await capture.getByRole('button', { name: 'Editar', exact: true }).click();
  const dialog = capture.getByRole('dialog', { name: 'Editar área de conteo', exact: true });
  await dialog.getByText('Empleado de prueba', { exact: true }).waitFor();
  await dialog.getByText('Lechuga (kg)', { exact: true }).waitFor();
  await dialog.getByText('Semanal', { exact: true }).waitFor();
  await dialog.locator('.ant-select').last().waitFor();
  await capture.waitForTimeout(350);
  assert.equal(await dialog.getByLabel('Nombre', { exact: true }).inputValue(), 'Cocina de prueba', 'Existing area values populate after mounting');
  await dialog.screenshot({ path: `${assetOutput}/inventario-configuracion.png` });
  await dialog.getByRole('button', { name: 'Cancelar', exact: true }).click();
  await capture.getByRole('button', { name: /Nueva área/ }).click();
  const freshArea = capture.getByRole('dialog', { name: 'Nueva área de conteo', exact: true });
  assert.equal(await freshArea.getByLabel('Nombre', { exact: true }).inputValue(), '');
  assert.equal(await freshArea.getByRole('switch').getAttribute('aria-checked'), 'true');
  assert.equal(await freshArea.getByText('Lechuga (kg)', { exact: true }).count(), 0);
  await freshArea.getByRole('button', { name: 'Cancelar', exact: true }).click();
  await capture.getByRole('tab', { name: 'Pendientes de revisión', exact: true }).click();
  await capture.getByRole('button', { name: 'Revisar detalle', exact: true }).click();
  await capture.getByText('Cantidad pesada. Datos ficticios.', { exact: true }).waitFor();
  await capture.waitForTimeout(350);
  await capture.getByRole('dialog').screenshot({ path: `${assetOutput}/inventario-revision.png` });
  await capture.getByRole('dialog').getByRole('button').first().click();
  assert.ok((await capture.getByRole('link', { name: 'Ayuda del módulo' }).getAttribute('href')).endsWith('/inventario-revisar'));
  await capture.getByRole('tab', { name: 'Descargar app', exact: true }).click();
  assert.ok((await capture.getByRole('link', { name: 'Ayuda del módulo' }).getAttribute('href')).endsWith('/inventario-instalar'));
  const contextHelp = capture.getByRole('link', { name: 'Ayuda de esta pantalla', exact: true });
  assert.equal(await contextHelp.getAttribute('target'), '_blank');
  assert.ok((await contextHelp.getAttribute('href')).endsWith('/inventario-problemas'));
  assert.equal(new URL(capture.url()).pathname, '/dashboard', 'Opening help does not replace the ERP workflow');
  assert.deepEqual(captureState.writes, []); assert.deepEqual(captureState.errors, []);
  await capture.close();
  console.log('Synthetic screenshots and contextual inventory help links passed; no writes.');

  for (const width of [320, 390, 820, 1366]) {
    const page = await browser.newPage({ viewport: { width, height: 1000 } });
    const state = await setup(page);
    await page.goto(`${base}/ayuda/inventario-configurar`);
    await page.getByRole('heading', { name: 'Configurar áreas y frecuencias', exact: true }).waitFor();
    await page.locator('.help-image').waitFor();
    await page.waitForFunction(() => document.querySelector('.help-image')?.naturalWidth > 0);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false, `${width}px no horizontal overflow`);
    await page.screenshot({ path: `${output}/${width}.png`, fullPage: true });
    if (width < 801) {
      await page.getByRole('button', { name: 'Guías del ERP', exact: true }).click();
      await page.getByRole('navigation', { name: 'Manuales disponibles' }).getByRole('link', { name: 'Realizar y enviar un conteo', exact: true }).click();
      await page.getByRole('heading', { name: 'Realizar y enviar un conteo', exact: true }).waitFor();
      assert.equal(await page.getByRole('button', { name: 'Guías del ERP', exact: true }).getAttribute('aria-expanded'), 'false');
    }
    await page.getByRole('textbox', { name: 'Buscar en los manuales', exact: true }).fill('lechuga');
    await page.getByRole('region', { name: 'Resultados de búsqueda' }).getByRole('link', { name: 'Realizar y enviar un conteo', exact: true }).click();
    await page.getByRole('heading', { name: 'Realizar y enviar un conteo', exact: true }).waitFor();
    await page.getByRole('cell', { name: '0,2', exact: true }).waitFor();
    await page.goBack(); await page.goForward();
    await page.getByRole('heading', { name: 'Realizar y enviar un conteo', exact: true }).waitFor();
    await page.getByRole('textbox', { name: 'Buscar en los manuales', exact: true }).fill('zzz-no-existe');
    await page.getByText('No se encontraron guías con esos términos.', { exact: true }).waitFor();
    await page.getByRole('textbox', { name: 'Buscar en los manuales', exact: true }).fill('reclasifica');
    await page.getByRole('region', { name: 'Resultados de búsqueda' }).getByRole('link', { name: 'Corregir medios de pago', exact: true }).click();
    await page.getByRole('heading', { name: 'Corregir medios de pago', exact: true }).waitFor();
    await page.getByRole('heading', { name: 'Reemplazar el desglose', exact: true }).waitFor();
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false, `${width}px payment guide no overflow`);
    await page.screenshot({ path: `${output}/pagos-${width}.png`, fullPage: true });
    assert.deepEqual(state.writes, []); assert.deepEqual(state.errors, []); assert.ok(state.images > 0);
    await page.close();
    console.log(`Help ${width}px: direct links, authenticated captures, mobile navigation, search, tables and history passed.`);
  }
  const restricted = await browser.newPage({ viewport: { width: 390, height: 900 } });
  const restrictedState = await setup(restricted, ['Inventory.Counts.Execute']);
  await restricted.goto(`${base}/ayuda/inventario-contar`);
  await restricted.getByRole('heading', { name: 'Realizar y enviar un conteo', exact: true }).waitFor();
  await restricted.getByRole('button', { name: 'Guías del ERP', exact: true }).click();
  assert.equal(await restricted.getByRole('navigation', { name: 'Manuales disponibles' }).getByRole('link', { name: 'Configurar áreas y frecuencias', exact: true }).count(), 0);
  await restricted.goto(`${base}/ayuda/inventario-configurar`);
  await restricted.getByText('Esta guía no está disponible para tu sesión o no pudo cargarse.', { exact: true }).waitFor();
  await restricted.goto(`${base}/ayuda/ventas-corregir-pagos`);
  await restricted.getByText('Esta guía no está disponible para tu sesión o no pudo cargarse.', { exact: true }).waitFor();
  assert.equal(restrictedState.images, 0);
  await restricted.goto(`${base}/ayuda/inventario-contar`);
  restrictedState.failSearch = true;
  await restricted.getByRole('textbox', { name: 'Buscar en los manuales', exact: true }).fill('cantidad');
  await restricted.getByText('No se pudo realizar la búsqueda.', { exact: true }).waitFor();
  restrictedState.failSearch = false;
  await restricted.getByRole('button', { name: 'Reintentar', exact: true }).click();
  await restricted.getByRole('region', { name: 'Resultados de búsqueda' }).getByRole('link', { name: 'Realizar y enviar un conteo', exact: true }).waitFor();
  assert.deepEqual(restrictedState.writes, []); assert.deepEqual(restrictedState.errors, []);
  await restricted.close();

  const cashier = await browser.newPage({ viewport: { width: 390, height: 900 } });
  const cashierState = await setup(cashier, ['Billing.Cash.View', 'Billing.Cash.Charge']);
  await cashier.goto(`${base}/ayuda/ventas-cobrar`);
  await cashier.getByRole('heading', { name: 'Cobrar y dividir una cuenta', exact: true }).waitFor();
  await cashier.getByRole('button', { name: 'Guías del ERP', exact: true }).click();
  const navigation = cashier.getByRole('navigation', { name: 'Manuales disponibles' });
  await navigation.getByRole('link', { name: 'Abrir y cerrar tu caja', exact: true }).waitFor();
  assert.equal(await navigation.getByRole('link', { name: 'Corregir medios de pago', exact: true }).count(), 0);
  await cashier.goto(`${base}/ayuda/ventas-corregir-pagos`);
  await cashier.getByText('Esta guía no está disponible para tu sesión o no pudo cargarse.', { exact: true }).waitFor();
  assert.deepEqual(cashierState.writes, []); assert.deepEqual(cashierState.errors, []);
  await cashier.close();

  for (const width of [390, 1366]) {
    const all = await browser.newPage({ viewport: { width, height: 900 } });
    const allState = await setup(all);
    for (const article of catalog.articles.filter(article => article.sources)) {
      await all.goto(`${base}/ayuda/${article.id}`);
      await all.locator('.help-article h1').getByText(article.title, { exact: true }).waitFor();
      assert.equal(await all.evaluate(() => document.documentElement.scrollWidth > innerWidth), false, `${article.id} ${width}px fits`);
    }
    if (width < 801) await all.getByRole('button', { name: 'Guías del ERP', exact: true }).click();
    const nav = all.getByRole('navigation', { name: 'Manuales disponibles' });
    assert.equal(await nav.getByRole('link').count(), 33);
    assert.equal(await nav.evaluate(element => element.clientHeight < element.scrollHeight), true, 'Long catalogue has bounded scrolling');
    await nav.getByRole('link', { name: 'Usar el walkie de la sucursal', exact: true }).click();
    await all.locator('.help-article h1').getByText('Usar el walkie de la sucursal', { exact: true }).waitFor();
    await all.screenshot({ path: `${output}/expanded-${width}.png`, fullPage: true });
    assert.deepEqual(allState.writes, []); assert.deepEqual(allState.errors, []);
    await all.close();
    console.log(`Expanded manual ${width}px: all 21 new guides, long catalogue and layout passed.`);
  }

  const safety = await browser.newPage(); const safetyState = await setup(safety);
  safetyState.malicious = true;
  let tracking = 0;
  safety.on('request', request => { if (request.url().includes('tracking.invalid')) tracking++; });
  await safety.goto(`${base}/ayuda/inicio`);
  await safety.getByText('Safe text', { exact: true }).waitFor();
  assert.equal(await safety.evaluate(() => window.__helpXss), undefined);
  assert.equal(await safety.locator('a[href^="javascript:"]').count(), 0);
  assert.equal(tracking, 0);
  await safety.getByRole('button', { name: 'Cerrar sesión', exact: true }).click();
  await safety.waitForURL('**/login');
  await safety.goto(`${base}/ayuda/inicio`); await safety.waitForURL('**/login');
  await safety.close();

  const anonymous = await browser.newPage(); const anonymousState = await setup(anonymous, [], false);
  await anonymous.goto(`${base}/ayuda/inicio`); await anonymous.waitForURL('**/login');
  assert.equal(anonymousState.helpCalls, 0);
  await anonymous.close();
  console.log('Restricted navigation/direct access, retry, raw HTML/URL/image safety, logout and anonymous redirect passed.');
} catch (error) {
  for (const context of browser.contexts()) for (const page of context.pages())
    await page.screenshot({ path: `${output}/failure.png`, fullPage: true }).catch(() => {});
  throw error;
} finally { await browser.close(); }
