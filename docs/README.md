# Documentación de Grimorio

Entrada central de documentación vigente, privada e histórica.

## Manual operativo

El centro de ayuda del ERP se abre en **`/ayuda/inicio`** con sesión iniciada.
La ayuda está disponible en desarrollo; producción permanece deshabilitada.

La fuente única está en [manual/articles](manual/articles/inicio.md):

- [Instalar y actualizar Grimorio Inventario](manual/articles/inventario-instalar.md).
- [Realizar y enviar un conteo](manual/articles/inventario-contar.md).
- [Configurar áreas y frecuencias](manual/articles/inventario-configurar.md).
- [Revisar diferencias y pedir reconteo](manual/articles/inventario-revisar.md).
- [Permisos y responsabilidades](manual/articles/inventario-permisos.md).
- [Problemas frecuentes](manual/articles/inventario-problemas.md).
- [Apertura y cierre de caja](manual/articles/caja-turno.md).
- [Consulta de caja y pedidos pendientes](manual/articles/caja-consultar.md).
- [Cobro y cuentas divididas](manual/articles/ventas-cobrar.md).
- [Ventas realizadas](manual/articles/ventas-consultar.md).
- [Corrección interna de medios de pago](manual/articles/ventas-corregir-pagos.md).

## Módulos incorporados

- [Pedidos](manual/articles/pedidos-operar.md) y [atención QR](manual/articles/pedidos-qr.md).
- [Menú y recetas](manual/articles/menu-recetas.md) y [promociones](manual/articles/menu-promociones.md).
- [Artículos y stock](manual/articles/inventario-catalogos.md), [movimientos](manual/articles/inventario-movimientos.md) y [producción](manual/articles/inventario-produccion.md).
- [Compras y XML](manual/articles/compras-registrar.md).
- [Clientes](manual/articles/clientes-gestionar.md), [documentos electrónicos](manual/articles/facturacion-documentos.md) y [configuración fiscal](manual/articles/facturacion-configurar.md).
- [Gastos y resultados](manual/articles/finanzas-gastos.md).
- [Empleados y horarios](manual/articles/personal-horarios.md), [asistencia](manual/articles/personal-asistencia.md) y [nómina](manual/articles/personal-nomina.md).
- [Usuarios y sucursal](manual/articles/admin-accesos.md) y [responsabilidades](manual/articles/operaciones-responsabilidades.md).
- [Fidelización](manual/articles/fidelizacion-programa.md).
- [Meseros](manual/articles/apps-meseros.md), [Estaciones](manual/articles/apps-estaciones.md) y [walkie](manual/articles/comunicacion-walkie.md).

Las guías se entregan desde la API con autenticación y permisos; no son archivos
públicos. Solo el [catálogo aprobado](manual/catalog.json) entra en el centro de
ayuda. Los documentos técnicos no se incluyen en su buscador ni sus recursos.

## Referencias privadas

- [Cobertura, fuentes y aceptación pendiente del manual](technical/manual-coverage.md).

- [Inventario documental y migraciones pendientes](documentation-audit.md).
- [Cómo mantener la documentación](CONTRIBUTING.md).
- [Centro de ayuda: arquitectura y entrega](technical/help-center.md).
- [Conteos: contratos y pruebas](technical/inventory-counts.md).
- [Matriz de permisos](permission-matrix.md).
- [Proyecto y arranque rápido](../README.md).
- [Historial de cambios](../CHANGELOG.md).
- [Estándares de API](../backend/docs/API_STANDARDS.md).
- [Frontend](../frontend/README.md).
- [Aplicaciones móviles](../mobile/README.md).

El [inventario documental](documentation-audit.md) clasifica los documentos
anteriores. No se consideran manuales públicos ni certificados por esta primera
revisión. El archivo histórico está en `archive/`, fuera del centro de ayuda.
