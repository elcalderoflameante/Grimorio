# Estandares API Backend

Este documento define el estandar de rutas y convenciones para controladores del backend.

## Idioma y nombres

- El codigo C#, TypeScript, DTOs, Commands, Queries, entidades, enums, servicios y permisos se nombran en ingles.
- Los textos visibles para el usuario pueden estar en espanol.
- Las rutas deben mantener el idioma y convencion del controlador existente. No mezclar un endpoint nuevo en ingles dentro de un controlador que ya expone rutas en espanol, ni al reves.
- En modulos historicos como `pos`, `cash` e `inventory`, las rutas publicas existentes usan segmentos en espanol. Los endpoints nuevos de esos modulos deben seguir ese patron salvo que se haga una migracion completa del modulo.
- En modulos alineados a rutas en ingles, como `scheduling`, los endpoints nuevos deben mantenerse en ingles.
- Los permisos siempre usan formato punteado en ingles: `Modulo.Recurso.Accion`, por ejemplo `POS.DirectSale.Create`.
- Las tablas, columnas y entidades EF se mantienen en ingles y PascalCase, usando el esquema correspondiente (`pos`, `billing`, `inv`, etc.).

## Convencion REST base

```text
GET    /api/{resource}
GET    /api/{resource}/{id}
POST   /api/{resource}
PUT    /api/{resource}/{id}
DELETE /api/{resource}/{id}
```

## Recursos anidados

```text
GET    /api/{parent}/{parentId}/{child}
POST   /api/{parent}/{parentId}/{child}
DELETE /api/{parent}/{parentId}/{child}/{childId}
```

## Scheduling (estado actual alineado)

### Work Areas / Roles / Templates / Shifts

- `GET /api/scheduling/work-areas`
- `GET /api/scheduling/work-areas/{id}`
- `POST /api/scheduling/work-areas`
- `PUT /api/scheduling/work-areas/{id}`
- `DELETE /api/scheduling/work-areas/{id}`

- `GET /api/scheduling/work-roles`
- `GET /api/scheduling/work-roles/{id}`
- `POST /api/scheduling/work-roles`
- `PUT /api/scheduling/work-roles/{id}`
- `DELETE /api/scheduling/work-roles/{id}`

- `GET /api/scheduling/shift-templates`
- `POST /api/scheduling/shift-templates`
- `PUT /api/scheduling/shift-templates/{id}`
- `DELETE /api/scheduling/shift-templates/{id}`

- `GET /api/scheduling/shifts`
- `GET /api/scheduling/shifts/{id}`
- `GET /api/scheduling/shifts/by-date`
- `GET /api/scheduling/shifts/free-employees`
- `POST /api/scheduling/shifts`
- `DELETE /api/scheduling/shifts/{id}`

### Employee Availability / Work Roles

- `GET /api/scheduling/employees/{employeeId}/availability`
- `POST /api/scheduling/employees/{employeeId}/availability`
- `DELETE /api/scheduling/employees/{employeeId}/availability/{id}`

- `GET /api/scheduling/employees/{employeeId}/work-roles`
- `POST /api/scheduling/employees/{employeeId}/work-roles`
- `DELETE /api/scheduling/employees/{employeeId}/work-roles/{workRoleId}`

## POS / Cash / Inventory (estado actual alineado)

Estos modulos mantienen rutas operativas en espanol para ser consistentes con el ERP actual.

### POS

- `GET /api/pos/ordenes`
- `GET /api/pos/ordenes/{id}`
- `POST /api/pos/ordenes`
- `POST /api/pos/ventas-directas`
- `PUT /api/pos/ordenes/{id}/items`
- `POST /api/pos/ordenes/{id}/confirmar`
- `POST /api/pos/ordenes/{id}/entregar`
- `POST /api/pos/ordenes/{id}/cancelar`
- `POST /api/pos/ordenes/items/{id}/cancelar`
- `PATCH /api/pos/ordenes/items/{id}/observacion`
- `PATCH /api/pos/ordenes/items/{id}/estado`

Compatibilidad temporal:

- `POST /api/pos/orden-items/{id}/cancelar`
- `PATCH /api/pos/orden-items/{id}/estado`

### Cash

- `GET /api/cash/sesion-activa`
- `GET /api/cash/sesiones`
- `POST /api/cash/abrir`
- `POST /api/cash/sesiones/{id}/cerrar`
- `POST /api/cash/cobrar/{orderId}`

### Inventory

- `GET /api/inventory/articulos`
- `GET /api/inventory/stock`
- `GET /api/inventory/movimientos`
- `POST /api/inventory/movimientos`

### Menu

- `GET /api/menu/items`
- `GET /api/menu/items/{id}`
- `POST /api/menu/items`
- `PUT /api/menu/items/{id}`
- `DELETE /api/menu/items/{id}`
- `PUT /api/menu/items/{id}/receta`
- `DELETE /api/menu/receta/{id}`
- `PUT /api/menu/items/{id}/modifiers`

## Acciones especiales

Cuando no aplica CRUD puro:

- Preferir `POST /api/{resource}/{id}/{action}` para transiciones de estado.
- Preferir filtros por query params antes que crear rutas redundantes.
- Si una accion afecta un recurso hijo sin depender del id del padre, usar un subrecurso claro antes que nombres hibridos. Ejemplo canonico POS: `PATCH /api/pos/ordenes/items/{id}/estado`.
- Mantener aliases temporales solo para compatibilidad con clientes existentes; el frontend y apps nuevas deben consumir la ruta canonica.

## Convenciones de calidad

- Cada endpoint nuevo debe tener contrato DTO claro (request/response).
- Mantener nombres consistentes con dominio de negocio.
- Usar codigos HTTP correctos segun resultado de la operacion.
- Agregar documentacion XML en controllers nuevos o refactorizados.
- Si se agrega o cambia un endpoint protegido, actualizar tambien `docs/permission-matrix.md`.
- Si se agrega una ruta canonica que reemplaza una ruta previa, actualizar clientes oficiales del repo y dejar la ruta previa como alias solo cuando haya riesgo de romper despliegues existentes.

## Ubicacion de este documento

Este archivo vive en `backend/docs/` por ser estandar especifico del modulo backend.
