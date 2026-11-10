## Cobrar un pedido

Abre el pedido desde POS y pulsa **Cobrar**. Necesitas `Billing.Cash.Charge`,
acceso al pedido y a los catálogos de medios de pago. Revisa que haya una sesión
de caja abierta válida para registrar el cobro.

## Cobro único con uno o varios medios

1. Comprueba platos, cantidades, promociones, cobros previos y saldo pendiente.
2. En **Cobro único**, selecciona el documento y el cliente. Para **Factura**
   el cliente es obligatorio; verifica su identificación antes de confirmar.
3. En **Medios de pago**, agrega las líneas necesarias y sus importes.
4. En tarjeta completa crédito o débito, banco, marca y autorización. No ingreses
   el número completo de tarjeta ni su código de seguridad.
5. Comprueba el importe del botón **Confirmar cobro** y confirma una sola vez.

Ejemplo sin recálculo de promociones: para una cuenta de $30 puedes registrar
$10 en efectivo, $10 en transferencia y $10 en tarjeta. Dar vuelto solo corresponde
a un medio configurado como efectivo; no registres una transferencia mayor para
simular vuelto.

Una promoción puede tener un descuento diferente con tarjeta. Al agregar tarjeta
comprueba **Pendiente con tarjeta** y el importe final antes de confirmar: no
asumas que conserva el total calculado para efectivo o transferencia.

## Dividir la cuenta por platos

En **Cuenta dividida**, selecciona los platos y cantidades del cliente activo,
completa su documento y medios, y confirma su cobro. Agrega otros clientes para
los platos restantes. Cada cliente puede tener varios medios de pago: dividir
por clientes no es lo mismo que repartir un cobro entre efectivo y tarjeta.

Un cliente ya cobrado conserva su comprobante; no lo elimines para cobrarlo de
nuevo. Revisa el saldo y los platos pendientes después de cada operación. Si hay
pagos antiguos sin detalle de platos, revisa la advertencia antes de dividir.

## Si se interrumpe la conexión

Si aparece **Cobro pendiente de verificar**, usa **Verificar / reintentar cobro**.
El sistema intenta recuperar el resultado o reutilizar la misma solicitud.
**No registres otro cobro por ese importe** ni borres los datos del navegador para
evitar la advertencia. Si no se actualiza el saldo, usa **Actualizar** y verifica
las [ventas registradas](/ayuda/ventas-consultar) antes de continuar.

Registrar el cobro no confirma por sí solo la autorización del SRI ni el envío
del correo. Esos estados se revisan separadamente. Un error posterior de medios
se atiende con [Corrección interna de pagos](/ayuda/ventas-corregir-pagos), no
volviendo a cobrar la venta.
