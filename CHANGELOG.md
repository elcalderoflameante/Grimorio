# Changelog

Todos los cambios relevantes del proyecto se registran aqui.

El formato se basa en Keep a Changelog y Versionado Semantico.

## [Sin versionar]

### Agregado

- Primera base operativa de los grupos restantes del ERP: 21 guías adicionales (33 en total), portada central, ayuda contextual por pantalla, navegación con desplazamiento y controles de cobertura y permisos. Aceptación por responsables y dispositivos pendiente; producción sigue deshabilitada.

- Ayuda de Caja y ventas: apertura y cierre, pendientes de sucursal, cobros divididos, consulta y corrección interna de pagos. Cinco guías por permisos y enlaces contextuales en Caja y Ventas; sin modificar reglas, datos ni documentos fiscales.

- Piloto de Ayuda y manuales en `/ayuda/inicio`: Markdown centralizado, búsqueda y capturas autenticadas por permisos, y guías de inventarios. Documentación técnica excluida; producción deshabilitada por defecto. Requiere reconstruir API y frontend, sin cambiar reglas de inventario ni datos.

- Actualizador dentro de Grimorio Inventario `1.0.1+2`, siguiendo el flujo de Meseros: consulta periódica, descarga con SHA-256, instalador Android y aviso aplazado durante conteos para conservar cantidades sin confirmar.

- App Grimorio Inventario con PIN, áreas compartidas y conteos diarios/semanales sin modificar existencias. Submenú Inventario > Conteos del personal con diferencias históricas, revisión, reconteos, configuración y descarga de APK.

- Control opcional del walkie Android con volumen arriba y tonos locales de inicio, fin y ocupado; accesibilidad para eventos fuera de la app. Pantalla apagada pendiente de validación en los teléfonos.
- Walkie-talkie por sucursal en meseros, estaciones y panel web, con control de turnos, reconexión y coordinación de avisos TTS; servidor LiveKit propio mediante overlay Docker.
- Portada pública del restaurante en `/`, con identidad visual, fotos, filtros de platos, contacto y redes; acceso interno conservado en `/login` y `/dashboard`.
- Flujo en tiempo real para solicitudes publicas de mesa con SignalR.
- Endpoint para solicitud activa por mesa en canal publico.
- Estructura de documentacion por modulo (`docs/`, `backend/docs/`, `mobile/`).

### Cambiado

- Separación por dominio: la raíz de `erp` dirige a login o dashboard según la sesión, mientras el dominio público muestra la portada del restaurante.
- Dominio `www` separado en Caddy y redirigido al dominio público principal.
- APK de producción de meseros `1.0.22+23` y estaciones `1.0.9+10`, disponibles desde el frontend. Incluyen botón físico opcional y sonidos del walkie; la voz requiere configurar y desplegar el servicio en el VPS.
- Estandar de clientes HTTP en frontend a nomenclatura `*Api`.
- Refactor de contexto de autenticacion (`AuthContext` + `useAuth`).
- Actualizacion de documentacion principal (README raiz y README frontend).

### Corregido

- Carga de áreas de conteo: el formulario está montado antes de asignar valores y se reinicia al abrir, conservando las líneas existentes y evitando arrastrarlas a un área nueva.

- Limpieza de archivos no usados en frontend.
- Consolidacion de tipos `SpecialDate` y actualizacion de imports.
- Correcciones de lint en hooks, efectos y tipado.

### Seguridad

- Actualizado AutoMapper a `16.1.1` para mitigar vulnerabilidad reportada en `16.0.0`.

---

## Formato recomendado para nuevas versiones

### [x.y.z] - YYYY-MM-DD

#### Agregado
- Nueva funcionalidad.

#### Cambiado
- Cambio en comportamiento existente.

#### Corregido
- Correccion de bug.

#### Seguridad
- Mitigacion de vulnerabilidad.
