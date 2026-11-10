# Cobertura y aceptación del manual

Revisión de la ampliación: **2026-11-10**. Registro privado, fuera del catálogo
operativo. Las fuentes de cada nueva guía figuran en `catalog.json`; la API no
expone ese campo al lector.

## Cobertura de módulos

La primera base tiene 33 artículos: introducción, seis guías de conteos, cinco
de caja y ventas y 21 de los grupos restantes. No equivale a documentar cada
variante, excepción, integración o requisito normativo.

| Guía | Referencia principal revisada | Aceptación |
| --- | --- | --- |
| [Tomar y seguir pedidos](../manual/articles/pedidos-operar.md) | `frontend/src/components/POS/PosOrderModule.tsx` | Código y pantalla; operación real pendiente. |
| [Atender mesas por QR](../manual/articles/pedidos-qr.md) | `frontend/src/components/POS/TableServiceModule.tsx` | Código y pantalla; operación real pendiente. |
| [Platos, recetas y combos](../manual/articles/menu-recetas.md) | `frontend/src/components/Menu/MenuItemsList.tsx` | Código y pantalla; operación real pendiente. |
| [Configurar promociones](../manual/articles/menu-promociones.md) | `frontend/src/components/POS/PromotionsList.tsx` | Código y pantalla; operación real pendiente. |
| [Artículos, unidades y stock](../manual/articles/inventario-catalogos.md) | `frontend/src/components/Inventory/ArticlesList.tsx` | Código y pantalla; operación real pendiente. |
| [Movimientos y conciliación](../manual/articles/inventario-movimientos.md) | `frontend/src/components/Inventory/StockMovements.tsx` | Código y pantalla; operación real pendiente. |
| [Registrar producción](../manual/articles/inventario-produccion.md) | `frontend/src/components/Inventory/Production.tsx` | Código y pantalla; operación real pendiente. |
| [Registrar compras e importar XML](../manual/articles/compras-registrar.md) | `frontend/src/components/Purchases/PurchaseForm.tsx` | Código y pantalla; operación real pendiente. |
| [Buscar y mantener clientes](../manual/articles/clientes-gestionar.md) | `frontend/src/components/Billing/CustomerIdentificationFields.tsx` | Código y pantalla; operación real pendiente. |
| [Revisar documentos electrónicos](../manual/articles/facturacion-documentos.md) | `frontend/src/components/Billing/ElectronicInvoices.tsx` | Código y pantalla; operación real pendiente. |
| [Configuración fiscal y medios](../manual/articles/facturacion-configurar.md) | `frontend/src/components/Billing/TaxConfig.tsx` | Código y pantalla; operación real pendiente. |
| [Gastos y resultados](../manual/articles/finanzas-gastos.md) | `frontend/src/components/Finance/ExpensesList.tsx` | Código y pantalla; operación real pendiente. |
| [Empleados y horarios](../manual/articles/personal-horarios.md) | `frontend/src/components/Scheduling/MonthlySchedule.tsx` | Código y pantalla; operación real pendiente. |
| [Asistencia y marcaciones](../manual/articles/personal-asistencia.md) | `frontend/src/components/Attendance/AttendanceDashboard.tsx` | Código y pantalla; operación real pendiente. |
| [Revisar el rol de pagos](../manual/articles/personal-nomina.md) | `frontend/src/components/Payroll/PayrollSummary.tsx` | Código y pantalla; operación real pendiente. |
| [Usuarios, permisos y sucursal](../manual/articles/admin-accesos.md) | `frontend/src/components/Users/UserList.tsx` | Código y pantalla; operación real pendiente. |
| [Responsabilidades operativas](../manual/articles/operaciones-responsabilidades.md) | `frontend/src/components/Operations/ResponsibilitiesModule.tsx` | Código y pantalla; operación real pendiente. |
| [Miembros y tarjetas de fidelización](../manual/articles/fidelizacion-programa.md) | `frontend/src/components/Marketing/LoyaltyModule.tsx` | Código y pantalla; operación real pendiente. |
| [Instalar y usar Meseros](../manual/articles/apps-meseros.md) | `mobile/waitstaff_app/README.md` | Código y pantalla; operación real pendiente. |
| [Cocina y barra en Estaciones](../manual/articles/apps-estaciones.md) | `mobile/station_app/README.md` | Código y pantalla; operación real pendiente. |
| [Usar el walkie de la sucursal](../manual/articles/comunicacion-walkie.md) | `frontend/src/components/Voice/WalkieTalkie.tsx` | Código y pantalla; operación real pendiente. |

También se contrastaron advertencias de compras con `PurchaseCostCalculator`
y `PurchasesCommandHandlers`, permisos con controladores y constantes, y
apps y fidelización con referencias existentes. Los documentos antiguos se
conservan como referencias técnicas o de transición, no como otro manual
operativo que deba mantenerse en paralelo.

## Comprobaciones automatizadas

- `helpTopics.test.mjs` exige una guía catalogada por cada caso funcional del
  Dashboard, salvo bienvenida y perfil, y verifica referencias de fuente.
- `Help.Checks` verifica accesos positivos y negativos de nuevos módulos,
  búsqueda, recursos incluidos, HTTP autenticado y ausencia de archivos técnicos.
- El navegador usa el catálogo real, intercepta APIs y prohíbe escrituras.
- Ninguna prueba del manual necesita escribir en PostgreSQL, enviar al SRI,
  usar OpenAI, realizar pagos o transmitir audio real.

## Aceptación pendiente

1. Validar procedimientos con responsables de cada módulo y datos de prueba.
2. Instalar APK autorizadas y comprobar red, cámara, audio y permisos en equipos reales.
3. Revisar guías con contabilidad y nómina sin tratarlas como normativa.
4. Confirmar funcionalidades y versiones desplegadas antes de anunciarlas.
5. Autorizar publicación, compilar API y frontend correspondientes y habilitar
   ambas variables de ayuda según la guía técnica de entrega.

No habilitar producción ni borrar documentación anterior automáticamente por
pasar estas pruebas. No se ha ejecutado despliegue ni aceptación física de
dispositivos como parte de esta ampliación.
