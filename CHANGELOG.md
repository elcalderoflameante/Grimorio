# Changelog

Todos los cambios relevantes del proyecto se registran aqui.

El formato se basa en Keep a Changelog y Versionado Semantico.

## [Sin versionar]

### Agregado

- Flujo en tiempo real para solicitudes publicas de mesa con SignalR.
- Endpoint para solicitud activa por mesa en canal publico.
- Estructura de documentacion por modulo (`docs/`, `backend/docs/`, `mobile/`).

### Cambiado

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
