# Estandares API Backend

Este documento define el estandar de rutas y convenciones para controladores del backend.

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

## Acciones especiales

Cuando no aplica CRUD puro:

- Preferir `POST /api/{resource}/{id}/{action}` para transiciones de estado.
- Preferir filtros por query params antes que crear rutas redundantes.

## Convenciones de calidad

- Cada endpoint nuevo debe tener contrato DTO claro (request/response).
- Mantener nombres consistentes con dominio de negocio.
- Usar codigos HTTP correctos segun resultado de la operacion.
- Agregar documentacion XML en controllers nuevos o refactorizados.

## Ubicacion de este documento

Este archivo vive en `backend/docs/` por ser estandar especifico del modulo backend.
