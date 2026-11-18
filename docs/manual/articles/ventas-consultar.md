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

## Cantidades y rentabilidad por plato o promocion

En **Facturacion → Rentabilidad**, selecciona fechas y, si corresponde, caja.
Las fechas corresponden al cobro, en la zona horaria de la sucursal.

- **Detalle** separa el mismo plato por promocion registrada.
  Un descuento sin identificacion promocional aparece separado, no se atribuye
  a una oferta por su precio o por el dia de la semana.
- **Promociones** consolida las unidades o combos cobrados de cada oferta,
  descuento, venta neta sin IVA, costos, utilidad y margen.
- **Por plato** conserva el consolidado original, sin separar promociones.

El buscador encuentra platos y promociones; los filtros de detalle no cambian
los indicadores generales del periodo. Estas vistas son alternativas del mismo
reporte: no sumes sus totales entre si.

Un combo completo cuenta como una unidad, no como sus componentes. Sus costos
incluyen las recetas guardadas de todos los componentes cuando existen.
En segunda unidad con descuento, **Pares equiv.** distribuye las parejas del grupo
entre las unidades cobradas; puede mostrar fracciones si se cobra parcialmente
o en distintas fechas. Una unidad sobrante no crea otra pareja. Sin grupo
historico no se inventa una cantidad de pares.

Los cobros antiguos sin detalle por plato aparecen como **Estimado** porque se
distribuyen proporcionalmente. Sin promocion registrada no es posible reconstruir
con certeza una oferta antigua. Los costos usan el promedio de compras del reporte,
no una valorizacion contable historica; revisa las alertas de costos o unidades.

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
