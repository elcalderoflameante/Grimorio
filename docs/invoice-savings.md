# Ahorro en facturas

Los nuevos cobros guardan OriginalTotal (precio bruto antes del descuento,
distribuido por cantidad pagada) y PromotionName en OrderPaymentItems. UnitPrice
y Total siguen siendo los importes finales cobrados; no cambia el pago,
inventario, rentabilidad ni el precio aplicado segun el medio de pago.

Cada nuevo detalle guarda ademas OriginalSubtotal, Discount, Subtotal y Tax:
subtotal original sin IVA, descuento sin IVA, subtotal neto sin IVA e IVA.
ElectronicDocument guarda SubtotalBeforeDiscount junto con TotalDescuento,
TotalSinImpuestos, TotalIva e ImporteTotal. La cabecera suma el detalle,
el subtotal original menos el descuento debe coincidir con el neto, y el
neto mas IVA con el total. Hay descuento cuando Discount es mayor que cero;
no se persiste un indicador redundante. XML y PDF usan el desglose guardado.
Se rechazan snapshots incompletos o inconsistentes; solo los registros
anteriores, con los nuevos campos null, usan el calculo compatible.

XML y RIDE utilizan este detalle guardado, no el precio actual del catalogo.
El descuento fiscal es la diferencia entre las bases sin IVA antes y despues
del descuento. El PDF muestra tambien un resumen comercial con IVA incluido:
precio normal, ahorro y precio final por linea, y ahorro total del comprobante.
Ejemplo con IVA 15%: $9.00 -> $6.99; ahorro bruto $2.01, descuento fiscal $1.75,
base final $6.08 e IVA $0.91. Los redondeos se hacen por linea.

Los datos anteriores permanecen null y no se reconstruyen descuentos de
facturas antiguas ni se cambian documentos autorizados. Los PDF almacenados
siguen descargandose normalmente. No se inventa ahorro cuando el precio final
es mayor que el normal (por ejemplo un precio alternativo con tarjeta).

Aplicar la migracion 20261015011546_AddInvoiceSavingsSnapshots antes de desplegar el backend.
Aplicar tambien 20261015013348_AddExplicitInvoiceAmountSnapshots para los
importes explicitos de cabecera y detalle. No rellena ni modifica historicos.
Verificacion: backend/tests/InvoiceSavings.Checks; el PDF de prueba puede
generarse indicando INVOICE_SAVINGS_PDF. No envia solicitudes al SRI ni correos.
ComboWorkflow.Checks verifica la persistencia real, los cambios posteriores de
catalogo, los reintentos de pago y que la migracion coincida con el modelo,
usando exclusivamente su base PostgreSQL aislada de pruebas.
Especificaciones de referencia: https://www.sri.gob.ec/facturacion-electronica
