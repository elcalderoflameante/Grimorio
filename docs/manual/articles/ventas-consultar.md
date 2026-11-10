## Consultar las ventas realizadas

Entra en **Facturación → Ventas**. Necesitas `Billing.Cash.View`.

1. Selecciona el rango de fechas; inicialmente muestra el día actual.
2. Filtra por **Caja**, **Cajero** o **Documento** según lo que buscas.
3. Pulsa **Actualizar** cuando necesites volver a consultar.
4. Expande una venta para ver su detalle y sus líneas de pago.

La fecha de esta lista corresponde al **cobro**, presentada en la zona horaria
de la sucursal. No es la fecha de importación de una compra ni necesariamente
la de autorización de una factura.

Los indicadores y totales por medio corresponden a las filas cargadas que pasan
los filtros. La consulta solicita hasta **200 cobros**: para períodos con mayor
volumen reduce el rango y no uses esta vista como un consolidado ilimitado.
La paginación de la tabla distribuye las filas cargadas, no solicita todo el histórico.

En una cuenta dividida, una orden puede aparecer en varios cobros. Por eso
**Transacciones** cuenta cobros y no necesariamente mesas u órdenes distintas.

## Acciones disponibles

- **Imprimir comprobante 80mm**: obtiene el comprobante de ese cobro.
- **Registrar o editar propina**: requiere permiso de cobrar.
- **Corregir medio de pago / consultar historial**: requiere el permiso específico
  de corrección; consulta [sus límites](/ayuda/ventas-corregir-pagos).
- Las acciones de factura dependen de permisos y estado del documento. La guía
  de caja no sustituye el procedimiento fiscal ni autoriza anular comprobantes.

El estado electrónico y el estado del correo son independientes del cobro.
Una factura rechazada no demuestra que el cliente no haya pagado: verifica el
registro antes de intentar cobrar nuevamente.
