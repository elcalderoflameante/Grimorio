# Matriz de permisos

Esta matriz define los permisos funcionales del ERP antes de aplicarlos en backend, frontend y seed de roles.

Convencion de nombres:

- `Modulo.Recurso.Accion` para operaciones normales.
- `View`, `Create`, `Update`, `Delete` para CRUD.
- `Manage` cuando una pantalla mezcla varias operaciones administrativas.
- Verbos especificos solo cuando la accion tiene riesgo operativo propio, por ejemplo `Close`, `Cancel`, `Generate`, `Retry`.

## Administracion

| Permiso | Descripcion | Menu / pantalla | Endpoints principales |
| --- | --- | --- | --- |
| `Admin.Users.View` | Ver usuarios | Administracion > Usuarios | `GET /api/users` |
| `Admin.Users.Create` | Crear usuarios | Administracion > Usuarios | `POST /api/users` |
| `Admin.Users.Update` | Editar usuarios y asignar roles | Administracion > Usuarios | `PUT /api/users/{id}`, `POST /api/users/{id}/roles` |
| `Admin.Users.Delete` | Eliminar usuarios | Administracion > Usuarios | `DELETE /api/users/{id}` |
| `Admin.Roles.View` | Ver roles | Administracion > Roles | `GET /api/roles` |
| `Admin.Roles.Create` | Crear roles | Administracion > Roles | `POST /api/roles` |
| `Admin.Roles.Update` | Editar roles y asignar permisos | Administracion > Roles | `PUT /api/roles/{id}`, `POST /api/roles/{id}/permissions` |
| `Admin.Roles.Delete` | Eliminar roles | Administracion > Roles | `DELETE /api/roles/{id}` |
| `Admin.Permissions.View` | Ver permisos | Administracion > Permisos | `GET /api/permissions` |
| `Admin.Permissions.Manage` | Crear, editar o eliminar permisos | Administracion > Permisos | `POST/PUT/DELETE /api/permissions` |
| `Admin.Branch.View` | Ver datos de sucursal | Administracion > Sucursal | `GET /api/branches/current` |
| `Admin.Branch.Update` | Editar datos de sucursal | Administracion > Sucursal | `PUT /api/branches/current` |

## RRHH

| Permiso | Descripcion | Menu / pantalla | Endpoints principales |
| --- | --- | --- | --- |
| `RRHH.Employees.View` | Ver empleados | RRHH > Empleados | `GET /api/employees` |
| `RRHH.Employees.Create` | Crear empleados | RRHH > Empleados | `POST /api/employees` |
| `RRHH.Employees.Update` | Editar empleados | RRHH > Empleados | `PUT /api/employees/{id}` |
| `RRHH.Employees.Delete` | Eliminar empleados | RRHH > Empleados | `DELETE /api/employees/{id}` |
| `RRHH.Positions.View` | Ver posiciones | RRHH > Posiciones | `GET /api/positions` |
| `RRHH.Positions.Manage` | Crear, editar o eliminar posiciones | RRHH > Posiciones | `POST/PUT/DELETE /api/positions` |
| `RRHH.Scheduling.View` | Ver turnos, disponibilidad y configuraciones | RRHH > Horarios | `GET /api/scheduling/*` |
| `RRHH.Scheduling.Manage` | Crear, editar, eliminar o generar turnos | RRHH > Horarios | `POST/PUT/DELETE /api/scheduling/*` |
| `RRHH.Payroll.View` | Ver nomina, anticipos, consumos, ajustes y roles | RRHH > Nomina | `GET /api/payroll/*` |
| `RRHH.Payroll.Manage` | Configurar nomina y registrar movimientos | RRHH > Nomina | `POST/PUT/PATCH/DELETE /api/payroll/*` |

## POS y atencion de mesa

| Permiso | Descripcion | Menu / pantalla | Endpoints principales |
| --- | --- | --- | --- |
| `POS.Orders.View` | Ver pedidos y resumen de pedidos activos | POS > Pedidos | `GET /api/pos/ordenes*` |
| `POS.Orders.Create` | Crear pedidos | POS > Pedidos | `POST /api/pos/ordenes` |
| `POS.Orders.Update` | Editar items, confirmar o entregar pedidos | POS > Pedidos | `PUT /api/pos/ordenes/{id}/items`, `POST /api/pos/ordenes/{id}/confirmar`, `POST /api/pos/ordenes/{id}/entregar` |
| `POS.Orders.Cancel` | Cancelar pedidos o items pendientes | POS > Pedidos | `POST /api/pos/ordenes/{id}/cancelar`, `POST /api/pos/ordenes/items/{id}/cancelar` |
| `POS.DirectSale.Create` | Crear ventas directas de mostrador | POS > Pedidos | `POST /api/pos/ventas-directas` |
| `POS.Kitchen.View` | Ver items de estaciones/KDS | POS > Estaciones | `GET /api/pos/estaciones/{id}/items`, `GET /api/pos/estaciones/{id}/completados` |
| `POS.Kitchen.Update` | Cambiar estado de items de cocina | POS > Estaciones | `PATCH /api/pos/ordenes/items/{id}/estado` |
| `POS.Stations.View` | Ver estaciones | POS > Estaciones | `GET /api/pos/estaciones` |
| `POS.Stations.Manage` | Administrar estaciones | POS > Estaciones | `POST/PUT/DELETE /api/pos/estaciones` |
| `POS.Tables.View` | Ver mesas QR | POS > Pedidos / Atencion QR | `GET /api/tableService/tables` |
| `POS.Tables.Manage` | Administrar mesas QR y posicion | POS > Atencion QR | `POST/PUT/DELETE /api/tableService/tables`, `POST /api/tableService/tables/{id}/regenerate-token`, `PATCH /api/pos/tables/{id}/position` |
| `POS.TableRequests.View` | Ver solicitudes QR | POS > Atencion QR | `GET /api/tableService/requests` |
| `POS.TableRequests.Update` | Tomar y cambiar estado de solicitudes QR | POS > Atencion QR | `POST /api/tableService/requests/{id}/take`, `POST /api/tableService/requests/{id}/status` |

Los endpoints `public/*` de `TableService` quedan sin permiso de usuario porque funcionan con token publico de mesa.

## Menu

| Permiso | Descripcion | Menu / pantalla | Endpoints principales |
| --- | --- | --- | --- |
| `Menu.Categories.View` | Ver categorias del menu | Menu > Categorias | `GET /api/menu/categorias` |
| `Menu.Categories.Manage` | Crear, editar o eliminar categorias | Menu > Categorias | `POST/PUT/DELETE /api/menu/categorias` |
| `Menu.Items.View` | Ver items y recetas | Menu > Items y recetas | `GET /api/menu/items` |
| `Menu.Items.Manage` | Crear, editar o eliminar items y recetas | Menu > Items y recetas | `POST/PUT/DELETE /api/menu/items`, `PUT/DELETE /api/menu/*receta*` |
| `Menu.StockConsume` | Descontar stock por venta | Proceso POS/facturacion | `POST /api/menu/venta/descontar-stock` |

## Inventario

| Permiso | Descripcion | Menu / pantalla | Endpoints principales |
| --- | --- | --- | --- |
| `Inventory.Config.View` | Ver unidades, conversiones, categorias y bodegas | Inventario > Configuracion | `GET /api/inventory/unidades`, `conversiones`, `categorias`, `bodegas` |
| `Inventory.Config.Manage` | Administrar configuracion de inventario | Inventario > Configuracion | `POST/PUT/DELETE /api/inventory/unidades`, `conversiones`, `categorias`, `bodegas` |
| `Inventory.Articles.View` | Ver articulos | Inventario > Articulos | `GET /api/inventory/articulos` |
| `Inventory.Articles.Manage` | Crear, editar o eliminar articulos | Inventario > Articulos | `POST/PUT/DELETE /api/inventory/articulos` |
| `Inventory.Stock.View` | Ver stock y alertas | Inventario > Stock actual | `GET /api/inventory/stock`, `GET /api/inventory/alertas` |
| `Inventory.Movements.View` | Ver movimientos | Inventario > Movimientos | `GET /api/inventory/movimientos` |
| `Inventory.Movements.Create` | Registrar movimientos e inventario inicial | Inventario > Movimientos | `POST /api/inventory/movimientos`, `POST /api/inventory/movimientos/inventario-inicial` |

## Compras

| Permiso | Descripcion | Menu / pantalla | Endpoints principales |
| --- | --- | --- | --- |
| `Purchases.Suppliers.View` | Ver proveedores | Compras > Proveedores | `GET /api/purchases/proveedores` |
| `Purchases.Suppliers.Manage` | Crear, editar o eliminar proveedores | Compras > Proveedores | `POST/PUT/DELETE /api/purchases/proveedores` |
| `Purchases.Orders.View` | Ver compras | Compras > Compras | `GET /api/purchases/compras` |
| `Purchases.Orders.Create` | Registrar compras | Compras > Compras | `POST /api/purchases/compras` |
| `Purchases.Orders.Update` | Editar compras | Compras > Compras | `PUT /api/purchases/compras/{id}` |
| `Purchases.Orders.Cancel` | Anular compras | Compras > Compras | `POST /api/purchases/compras/{id}/anular` |
| `Purchases.Orders.Delete` | Eliminar compras | Compras > Compras | `DELETE /api/purchases/compras/{id}` |

## Facturacion, caja y SRI

| Permiso | Descripcion | Menu / pantalla | Endpoints principales |
| --- | --- | --- | --- |
| `Billing.Customers.View` | Ver clientes | Facturacion > Clientes | `GET /api/customers` |
| `Billing.Customers.Manage` | Crear, editar o eliminar clientes | Facturacion > Clientes | `POST/PUT/DELETE /api/customers` |
| `Billing.Cash.View` | Ver caja, sesiones, ventas y pagos | Facturacion > Caja / Ventas | `GET /api/cash/*` |
| `Billing.Cash.Open` | Abrir caja | Facturacion > Caja | `POST /api/cash/abrir` |
| `Billing.Cash.Close` | Cerrar caja | Facturacion > Caja | `POST /api/cash/sesiones/{id}/cerrar` |
| `Billing.Cash.Charge` | Cobrar ordenes | Facturacion > Caja | `POST /api/cash/cobrar/{orderId}` |
| `Billing.CashRegisters.View` | Ver cajas y estaciones de cobro | Facturacion > Caja | `GET /api/cash/cajas` |
| `Billing.CashRegisters.Manage` | Administrar cajas y estaciones de cobro | Facturacion > Caja | `POST/PUT/DELETE /api/cash/cajas` |
| `Billing.PaymentMethods.View` | Ver medios de pago y bancos | Facturacion > Medios de pago | `GET /api/cash/metodos-pago`, `GET /api/cash/bancos-tarjeta` |
| `Billing.PaymentMethods.Manage` | Administrar medios de pago y bancos | Facturacion > Medios de pago | `POST/PUT/DELETE /api/cash/metodos-pago`, `bancos-tarjeta` |
| `Billing.Tax.View` | Ver tarifas fiscales | Facturacion > Configuracion fiscal | `GET /api/tax/tarifas` |
| `Billing.Tax.Manage` | Administrar tarifas fiscales | Facturacion > Configuracion fiscal | `POST/PUT/DELETE /api/tax/tarifas` |
| `Billing.Sri.View` | Ver configuracion, documentos, XML, RIDE y respuestas SRI | Facturacion > Documentos electronicos | `GET /api/sri/*` |
| `Billing.Sri.Manage` | Configurar SRI, certificado, SMTP y plantilla | Facturacion > Documentos electronicos / Plantilla | `PUT/POST/DELETE /api/sri/config`, `certificado`, `smtp`, `invoice-template` |
| `Billing.Sri.Generate` | Generar o reintentar documentos electronicos | Facturacion > Documentos electronicos | `POST /api/sri/documentos/generar/{orderPaymentId}`, `POST /api/sri/documentos/{id}/reintentar` |

## Roles base sugeridos

| Rol | Permisos sugeridos |
| --- | --- |
| `Administrador` | Todos los permisos |
| `Supervisor` | Ver todo, operar POS/caja, compras e inventario, sin administrar usuarios/permisos ni configuraciones fiscales sensibles |
| `Cajero` | `POS.Orders.View`, `POS.DirectSale.Create`, `Billing.Customers.*`, `Billing.Cash.*`, `Billing.PaymentMethods.View`, `Billing.Sri.View`, `Billing.Sri.Generate` |
| `Mesero` | `POS.Orders.View`, `POS.Orders.Create`, `POS.Orders.Update`, `POS.Tables.View`, `POS.TableRequests.View`, `POS.TableRequests.Update`, `Menu.Categories.View`, `Menu.Items.View` |
| `Cocina` | `POS.Kitchen.View`, `POS.Kitchen.Update`, `POS.Stations.View` |
| `Bodega` | `Inventory.*`, `Purchases.Suppliers.View`, `Purchases.Orders.View` |
| `Compras` | `Purchases.*`, `Inventory.Articles.View`, `Inventory.Stock.View`, `Inventory.Movements.View` |
| `Contabilidad` | `Billing.*`, `Purchases.Orders.View`, `Purchases.Suppliers.View`, `Inventory.Stock.View` |
| `RRHH` | `RRHH.*` |

## Permisos existentes a migrar

El seed actual usa algunos nombres iniciales que conviene reemplazar o mapear:

| Permiso actual | Nuevo permiso sugerido |
| --- | --- |
| `POS.Sell` | `POS.Orders.Create`, `POS.Orders.Update`, `Billing.Cash.Charge` segun rol |
| `POS.ViewReports` | `Billing.Cash.View` o un futuro `Reports.POS.View` |
| `Inventory.Adjust` | `Inventory.Movements.Create` |
| `Inventory.View` | `Inventory.Stock.View`, `Inventory.Articles.View`, `Inventory.Movements.View` |
| `Cash.Close` | `Billing.Cash.Close` |
| `Admin.ManageUsers` | `Admin.Users.*` |
| `Admin.ManageRoles` | `Admin.Roles.*`, `Admin.Permissions.*` |
| `RRHH.ViewEmployees` | `RRHH.Employees.View` |
| `RRHH.CreateEmployees` | `RRHH.Employees.Create` |
| `RRHH.UpdateEmployees` | `RRHH.Employees.Update` |
| `RRHH.DeleteEmployees` | `RRHH.Employees.Delete` |
