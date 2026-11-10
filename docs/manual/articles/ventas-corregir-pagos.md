## Corregir los medios de una venta ya cobrada

Esta operación reclasifica internamente el dinero de un cobro para cuadrar caja.
No vuelve a cobrar al cliente, no cambia platos, cantidades, inventario ni el
total vendido. Tampoco realiza un movimiento real en un banco o datáfono.

> **Corrección interna de caja:** el XML y el RIDE existentes conservan los medios
> originales. No se regeneran ni se reenvían al SRI por guardar esta corrección.

## Requisitos

- Permisos `Billing.Cash.View`, `Billing.Payments.CorrectMethod` y
  `Billing.PaymentMethods.View`, con un rol activo autorizado.
- El cobro pertenece a la sucursal actual y tiene una caja asociada **abierta**.
- No contiene consumos de empleados ni medios especiales de descuento por nómina.

Puede aplicarse a notas de venta y facturas, incluso autorizadas, respetando esos
límites. Esto describe el comportamiento interno del ERP; no constituye un
procedimiento de rectificación fiscal.

## Reemplazar el desglose

1. En **Facturación → Ventas**, localiza el cobro y abre
   **Corregir medio de pago / consultar historial**.
2. Revisa las líneas cargadas. Puedes quitar líneas y agregar otras, aunque el
   número de medios sea diferente del original.
3. Distribuye el importe en entre 1 y 10 líneas con valores positivos y hasta
   dos decimales. Usa medios activos normales.
4. Para cada tarjeta completa crédito o débito, banco válido, marca y autorización.
5. Comprueba que **Asignado** sea el total de la venta y **Diferencia** sea $0.00.
6. Escribe un **Motivo de la correccion** de entre 5 y 500 caracteres y pulsa
   **Guardar correccion**.
7. Revisa el resultado en ventas y la distribución por medios de la caja.

Por ejemplo, un cobro de $30 registrado todo en tarjeta puede reemplazarse por
$10 en efectivo, $12 en transferencia y $8 en tarjeta. No escribas $50 en efectivo
para indicar dinero recibido con vuelto: aquí se reclasifica el **importe neto**.

El historial conserva antes, después, responsable, fecha y motivo. Si otra persona
modificó los pagos mientras el formulario estaba abierto, actualiza la venta y
revisa sus valores antes de intentarlo nuevamente.

## Casos que requieren revisión

Una caja cerrada, consumos de empleados, líneas que no suman el cobro o cambios
sin diferencia real no se resuelven forzando el formulario. Solicita revisión a
administración. Tampoco uses esta acción para cambiar el cliente, los impuestos,
el total, una promoción o el estado fiscal.

Consulta también [Cierre de caja](/ayuda/caja-turno) y
[Ventas realizadas](/ayuda/ventas-consultar).
