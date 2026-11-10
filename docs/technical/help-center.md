# Centro de ayuda: arquitectura y entrega

## Decisión del piloto

Markdown en `docs/manual` es la fuente única. El lector usa React y la sesión
existente, no un segundo sitio VitePress ni otro sistema de usuarios. Publicar
un sitio estático exige proteger también HTML, imágenes e índices: ocultar
enlaces no basta. No se añade base de datos ni CMS.

Solo `catalog.json`, `articles/*.md` y `assets/*.png` se incrustan como recursos
del ensamblado API. No se copian a `wwwroot`, `frontend/public`, el bundle ni un
índice público. Los documentos técnicos, pendientes e históricos no se empaquetan.
El catálogo es una lista explícita; nunca se resuelven rutas aportadas por clientes.
Cambiar documentos requiere reconstruir la API: texto y permisos corresponden a
la compilación desplegada.

## Acceso y seguridad

- `/ayuda/:articleId?`: lector protegido por la sesión web.
- `GET /api/help/articles?q=...`: índice y búsqueda de artículos autorizados.
- `GET /api/help/articles/{id}`: metadatos y Markdown del artículo autorizado.
- `GET /api/help/assets/{id}`: imagen autorizada, recibida como blob con Bearer.

La validación JWT/sesión del ERP aplica antes del controlador. `permissionSets`
exige todos los permisos de un grupo y acepta cualquiera de los grupos; una
lista vacía permite cualquier sesión autenticada. Administrador conserva su
acceso existente. Leer no concede permisos. Desconocidos y denegados devuelven
404. Respuestas `no-store`; sin persistencia de artículos o búsquedas en navegador.

Solo se entregan imágenes referenciadas por una guía autorizada. El lector usa
`react-markdown` y `remark-gfm`, sin HTML crudo ni `rehype-raw`; rechaza imágenes
externas, protocolos ejecutables y rutas relativas arbitrarias. No incluir
secretos o datos personales aunque el acceso esté protegido.

## Desarrollo y publicación

Disponible automáticamente en Development y Vite dev. Producción deshabilitada
por defecto. Después de aceptación y autorización, habilitar ambas partes:

```text
Help__Enabled=true
VITE_HELP_ENABLED=true
```

La primera variable es del backend; la segunda es de compilación del frontend.
Recompilar la web y reiniciar la API. Estos controles no eluden autenticación y
todavía no se habilitan en el Compose de producción.

El contexto Docker de la API ahora es la raíz, para incluir el manual.
`deploy/cloudcone/docker-compose.yml` está actualizado. Un build manual desde raíz:

```sh
docker build -f backend/Grimorio.API/Dockerfile .
```

Solo se copian `backend/` y `docs/manual/`; `.dockerignore` excluye secretos,
`.env`, artefactos y aplicaciones ajenas al build API.

## Comprobaciones

```powershell
node tools/check-help-docs.mjs
dotnet run --project backend/tests/Help.Checks
node --experimental-strip-types --test frontend/tests/helpLinks.test.mjs frontend/tests/helpTopics.test.mjs
$env:PLAYWRIGHT_MODULE='C:\Projects\Grimorio\.tmp\pos-browser-tools\node_modules\playwright\index.mjs'
$env:HELP_TEST_URL='http://127.0.0.1:5173'
node frontend/tests/help.browser.mjs
```

La prueba HTTP usa un host aislado y autenticación sintética, sin arrancar el
ERP ni conectar a bases de datos. El navegador intercepta las APIs y prohíbe
escrituras. Regenera dos capturas de pantallas reales del ERP con datos ficticios
en `docs/manual/assets`; no son pantallas de producción ni de Android.

Después de compilar la API, `Help.Checks --published-api <ruta-a-Grimorio.API.dll>`
comprueba también que el ensamblado incluya exactamente los 36 recursos:
catálogo, 33 guías y dos capturas. No ejecuta el host de la API.
La comprobación de GitHub Actions se deja preparada; su ejecución remota no forma
parte de la verificación local. La imagen Docker y el VPS no se despliegan aquí.

Pendientes: probar instalación y conteo en un teléfono real; revisar el manual
con personal y administración; confirmar la versión desplegada. No confundir
lectura de código, pruebas con fixtures y aceptación en campo.

## Ampliación de caja y ventas

La entrega siguiente amplía a 33 guías y enlaza cada grupo del Dashboard a su
tema mediante `helpTopics.ts`. El botón de ayuda de escritorio abre el tema
actual en otra pestaña sin desmontar el formulario; el menú del usuario mantiene el índice general y acceso contextual en
móvil. Ver [cobertura y aceptación](manual-coverage.md) para fuentes y límites.

El 2026-11-10 se incorporan cinco guías contrastadas con `CashRegister`,
`SalesHistory`, `TableOrderView`, `CorrectPaymentMethodModal`, `CashController`
y los handlers de apertura, cierre y corrección. No se cambian sus reglas.
La guía de correcciones exige conjuntamente consulta de caja, corrección y
consulta de medios; ver la matriz explícita en el catálogo. Las comprobaciones
del servidor incluyen acceso permitido, denegado y búsqueda por estos permisos.

Caja y Ventas enlazan al manual desde su encabezado. La navegación móvil se
denomina Guías del ERP y el filtro de público incluye guías compartidas de caja.
Las pruebas de navegador usan el catálogo real con APIs interceptadas y no
realizan cobros. La aceptación de los procedimientos en operación queda pendiente.
