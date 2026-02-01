# Estándares de Endpoints API - Grimorio

## 🔍 Análisis de Patrones Actuales

### ✅ Patrones Consistentes (Scheduling Controller)

```csharp
// Listado de recursos
GET /api/scheduling/work-areas
GET /api/scheduling/work-roles
GET /api/scheduling/shift-templates
GET /api/scheduling/shifts

// Obtener por ID
GET /api/scheduling/work-areas/{id}
GET /api/scheduling/work-roles/{id}
GET /api/scheduling/shift-templates/{id}
GET /api/scheduling/shifts/{id}

// Crear
POST /api/scheduling/work-areas
POST /api/scheduling/work-roles
POST /api/scheduling/shift-templates
POST /api/scheduling/shifts

// Actualizar
PUT /api/scheduling/work-areas/{id}
PUT /api/scheduling/work-roles/{id}
PUT /api/scheduling/shift-templates/{id}

// Eliminar
DELETE /api/scheduling/work-areas/{id}
DELETE /api/scheduling/work-roles/{id}
DELETE /api/scheduling/shift-templates/{id}
DELETE /api/scheduling/shifts/{id}
```

### ❌ Inconsistencias Identificadas

#### 1. **EmployeeAvailability - Patrón Corregido**
```csharp
// Patrón anidado consistente:
GET    /api/scheduling/employees/{employeeId}/availability
POST   /api/scheduling/employees/{employeeId}/availability
DELETE /api/scheduling/employees/{employeeId}/availability/{id}  // ✅ CONSISTENTE
```

**Estado:** Corregido y alineado con el estándar.

---

#### 2. **EmployeeWorkRole - Patrón Correcto**
```csharp
GET /api/scheduling/employees/{employeeId}/work-roles      ✅
POST /api/scheduling/employees/{employeeId}/work-roles      ✅
DELETE /api/scheduling/employees/{employeeId}/work-roles/{workRoleId}  ✅
```

**Patrón:** Recursos anidados bien implementados.

---

#### 3. **ShiftAssignment - Patrones Alineados**
```csharp
GET /api/scheduling/shifts?branchId=X&year=Y&month=Z     ✅ (listado con filtros)
GET /api/scheduling/shifts/free-employees?branchId=X&date=Y  ✅ (acción específica)
GET /api/scheduling/employees/{employeeId}/shifts?year=Y&month=Z  ✅ (recurso anidado)
GET /api/scheduling/shifts/by-date?branchId=X&date=Y      ✅ (filtro por query params)
GET /api/scheduling/shifts/{id}                           ✅ (por ID)
```

**Estado:** Rutas alineadas con query params y recursos anidados.

---

#### 4. **Users Controller - Demasiado Compacto**
```csharp
[HttpGet]                          // ✅
[HttpGet("{id}")]                  // ✅
[HttpPost]                         // ✅
[HttpPut("{id}")]                  // ✅
[HttpDelete("{id}")]               // ✅
[HttpPost("{id}/roles")]           // ✅ Acción secundaria
[HttpPost("{id}/change-password"]  // ✅ Acción secundaria
```

**Observación:** Correcto, pero sin comentarios o documentación (a diferencia de EmployeesController).

---

## ✨ Estándar Recomendado

### Regla General: RESTful CRUD

```
GET    /api/{resource}              → Listar (con query params para filtros)
GET    /api/{resource}/{id}         → Obtener uno
POST   /api/{resource}              → Crear
PUT    /api/{resource}/{id}         → Actualizar completo
PATCH  /api/{resource}/{id}         → Actualizar parcial
DELETE /api/{resource}/{id}         → Eliminar
```

### Para Recursos Anidados

```
GET    /api/{parent}/{parentId}/{child}           → Listar hijos
GET    /api/{parent}/{parentId}/{child}/{childId} → Obtener un hijo
POST   /api/{parent}/{parentId}/{child}           → Crear hijo
PUT    /api/{parent}/{parentId}/{child}/{childId} → Actualizar hijo
DELETE /api/{parent}/{parentId}/{child}/{childId} → Eliminar hijo
```

### Para Acciones Especiales

```
POST   /api/{resource}/{id}/{action}      → Ejecutar acción
GET    /api/{resource}/search              → Búsqueda especial
GET    /api/{resource}/by-date            → Filtro específico (mejor en query params)
```

---

## 🔧 Cambios Aplicados

### 1. **EmployeeAvailability - Alineado**

**Actual (alineado):**
```csharp
GET    /api/scheduling/employees/{employeeId}/availability
POST   /api/scheduling/employees/{employeeId}/availability
DELETE /api/scheduling/employees/{employeeId}/availability/{id}
```

---

### 2. **ShiftAssignment - Alineado**

**Actual (alineado):**
```csharp
GET /api/scheduling/shifts?branchId=X&year=Y&month=Z
GET /api/scheduling/shifts/{id}
GET /api/scheduling/shifts/free-employees?branchId=X&date=Y
GET /api/scheduling/employees/{empId}/shifts?year=Y&month=Z
GET /api/scheduling/shifts/by-date?branchId=X&date=Y
POST /api/scheduling/shifts
DELETE /api/scheduling/shifts/{id}
```

---

### 3. **Documentación - MEJORA**

**Patrones Observados:**
- ✅ `EmployeesController` - Bien documentado con `[HttpGet]`, `[HttpPost]`, etc.
- ❌ `UsersController` - Sin comentarios XML
- ❌ `SchedulingController` - Sin comentarios en métodos individuales

**Recomendación:** Agregar comentarios XML a todos los endpoints.

---

## 📋 Checklist de Implementación

- [ ] **Prioridad CRÍTICA:** Cambiar DELETE `/api/scheduling/availability/{id}` a DELETE `/api/scheduling/employees/{employeeId}/availability/{id}`
  - [ ] Actualizar `SchedulingController.cs`
  - [ ] Actualizar `frontend/src/services/api.ts`
  - [ ] Crear migration si es necesario

- [ ] **Prioridad ALTA:** Refactorizar rutas de ShiftAssignment para consistencia
  - [ ] Considerar usar query params para filtros
  - [ ] Separar acciones especiales

- [ ] **Prioridad MEDIA:** Agregar documentación XML a todos los controllers
  - [ ] UsersController
  - [ ] RolesController
  - [ ] PermissionsController
  - [ ] PositionsController
  - [ ] SchedulingController (completar)

---

## 🎯 Estándar Final Propuesto para Todos los Controllers

### Patrón Básico CRUD
```csharp
/// <summary>
/// Obtiene todos los {recursos}.
/// </summary>
[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    => Ok(await _mediator.Send(new GetAllQuery { PageNumber = pageNumber, PageSize = pageSize }));

/// <summary>
/// Obtiene un {recurso} por ID.
/// </summary>
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
    => Ok(await _mediator.Send(new GetByIdQuery { Id = id }));

/// <summary>
/// Crea un nuevo {recurso}.
/// </summary>
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateDto dto)
    => CreatedAtAction(nameof(GetById), new { id = (await _mediator.Send(...)).Id }, ...);

/// <summary>
/// Actualiza un {recurso} existente.
/// </summary>
[HttpPut("{id}")]
public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDto dto)
    => Ok(await _mediator.Send(new UpdateCommand { Id = id, ...dto }));

/// <summary>
/// Elimina un {recurso}.
/// </summary>
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(Guid id)
    => Ok(await _mediator.Send(new DeleteCommand { Id = id }));
```

---

## Referencias
- [Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines)
- [RESTful API Best Practices](https://restfulapi.net/)
- [HTTP Status Codes](https://httpwg.org/specs/rfc7231.html#status.codes)
