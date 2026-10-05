# Changelog

Todos los cambios relevantes del proyecto se registran aqui.

El formato se basa en Keep a Changelog y Versionado Semantico.

## [Sin versionar]

### Agregado

- Control opcional del walkie Android con volumen arriba y tonos locales de inicio, fin y ocupado; accesibilidad para eventos fuera de la app. Pantalla apagada pendiente de validación en los teléfonos.
- Walkie-talkie por sucursal en meseros, estaciones y panel web, con control de turnos, reconexión y coordinación de avisos TTS; servidor LiveKit propio mediante overlay Docker.
- Portada pública del restaurante en `/`, con identidad visual, fotos, filtros de platos, contacto y redes; acceso interno conservado en `/login` y `/dashboard`.
- Flujo en tiempo real para solicitudes publicas de mesa con SignalR.
- Endpoint para solicitud activa por mesa en canal publico.
- Estructura de documentacion por modulo (`docs/`, `backend/docs/`, `mobile/`).

### Cambiado

- Separación por dominio: la raíz de `erp` dirige a login o dashboard según la sesión, mientras el dominio público muestra la portada del restaurante.
- APK de producción de meseros `1.0.21+22` y estaciones `1.0.8+9`, disponibles desde el frontend. La voz requiere configurar y desplegar el servicio en el VPS.
- Estandar de clientes HTTP en frontend a nomenclatura `*Api`.
- Refactor de contexto de autenticacion (`AuthContext` + `useAuth`).
- Actualizacion de documentacion principal (README raiz y README frontend).

### Corregido

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
