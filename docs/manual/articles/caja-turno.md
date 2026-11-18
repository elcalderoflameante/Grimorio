## Abrir y cerrar tu caja

Entra en **Facturación → Caja** con tu propio usuario y la sucursal correcta.
Necesitas `Billing.Cash.View` para consultar, `Billing.CashRegisters.View` para
listar cajas, `Billing.Cash.Open` para abrir y `Billing.Cash.Close` para cerrar.
El nombre del rol no sustituye estos permisos.

## Abrir el turno

1. Cuenta el efectivo que recibes como fondo inicial.
2. Pulsa **Abrir caja**, selecciona **Caja / estación** e ingresa el
   **Fondo inicial en caja ($)**. No incluyas transferencias ni tarjetas.
3. Confirma y comprueba el cajero, caja, hora de apertura y fondo mostrados.

La caja debe estar activa y sin otra sesión abierta. Tu usuario tampoco puede
tener otra sesión abierta en esa sucursal. Si no hay cajas disponibles, solicita
a administración revisar **Configurar cajas**; no abras una estación distinta
solo para evitar el bloqueo.

## Antes de cerrar

- Revisa los [pedidos pendientes](/ayuda/caja-consultar) y los cobros del turno.
- Compara efectivo, transferencias y tarjetas con los comprobantes reales.
- Corrige los [medios registrados por error](/ayuda/ventas-corregir-pagos)
  mientras la caja siga abierta.
- Revisa los gastos registrados y cuenta físicamente los billetes y monedas.

## Registrar el cierre

1. Pulsa **Cerrar caja** desde tu sesión.
2. Compara **Efectivo esperado en caja** con el dinero contado.
3. Escribe el **Efectivo contado al cierre ($)** real y las **Notas de cierre**.
4. Confirma y consulta el detalle en **Historial de sesiones**.

El esperado corresponde al fondo inicial más cobros netos en efectivo menos
gastos registrados en efectivo. Tarjetas y transferencias no son efectivo físico.
La diferencia es **contado menos esperado**: negativa indica faltante, positiva
indica sobrante. Por ejemplo, fondo $50 + efectivo $100 - gastos $5 = esperado
$145; contar $140 registra una diferencia de -$5.

Solo puedes cerrar la sesión abierta por tu usuario. Consultar otra caja no
permite cerrarla en nombre de su cajero. El cierre no cobra ni cancela los
pedidos pendientes y no corrige automáticamente diferencias. Revisa esos pedidos
antes de confirmar; no supongas que el sistema impedirá cerrar por ellos.

## Separar jornadas cuando se olvido el cierre

Si ya hubo cobros del nuevo turno en la sesion anterior, usa **Separar jornada**.
Requiere el permiso especial `Billing.Cash.Separate`; no basta con poder cerrar.
Administracion puede seleccionar otra sesion abierta en **Consultar caja**.

1. Indica desde que fecha y hora empieza la nueva jornada, en la zona horaria
   mostrada de la sucursal. No tiene que ser medianoche.
2. Ingresa el fondo inicial de la nueva jornada **antes de sus cobros**, no el
   efectivo que hay actualmente. No se copia automaticamente el esperado anterior.
3. Si se conoce el efectivo realmente contado al terminar el turno anterior,
   activa esa opcion e ingresalo. Si no se conto, queda **Pendiente de verificar**,
   sin registrar un contado cero ni una diferencia inventada.
4. Escribe el motivo y pulsa **Revisar corte**. Revisa ambas jornadas, sus medios,
   gastos, fondos y efectivo esperado, y el destino de cada movimiento.
5. Pulsa **Confirmar separacion**. Si entro un cobro, un gasto o una correccion
   despues de revisar, vuelve a revisar antes de confirmar.

Los cobros anteriores al corte permanecen en la sesion anterior; los de la hora
exacta del corte en adelante pasan a la nueva. Los gastos se separan por su hora
de **registro**, no por la fecha del documento. Los gastos anulados conservan su
historia y no suman al efectivo esperado.

La sesion anterior se cierra al corte y la nueva queda abierta en la misma caja,
para el **mismo cajero**. La persona autorizada que hizo la correccion queda en
la auditoria, junto con el motivo, los movimientos revisados y la hora real de
la operacion. Puedes ver esos datos al expandir el historial de sesiones.

No se recrean cobros, no cambian sus importes o fechas, no se regeneran facturas
del SRI y no se repiten descuentos de inventario. Los pedidos pendientes siguen
su curso. Si se pierde la respuesta al confirmar, usa **Reintentar confirmacion**
o actualiza Caja para comprobar el resultado; no vuelvas a registrar las ventas.

## Si algo no coincide

No reemplaces el contado real por el esperado para ocultar una diferencia.
Registra la observación y comunica el caso a administración. Una caja cerrada
ya no admite la corrección ordinaria de medios de pago.
