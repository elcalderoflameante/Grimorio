## Consultar la caja y los pedidos pendientes

Abre **Facturación → Caja**. Requiere `Billing.Cash.View`;
el resumen de pedidos también depende del permiso `POS.Orders.View`.
Si falla su consulta, no interpretes el mensaje de error como ausencia de pedidos.

## Distinguir los importes

| Dato | Qué representa |
| --- | --- |
| Total recaudado y cobros | Cobros registrados en la sesión mostrada. |
| Recaudación por medio de pago | Distribución de esos cobros, no dinero físico en su totalidad. |
| Efectivo en caja | Fondo inicial más efectivo cobrado menos gastos en efectivo. |
| Pedidos en curso y pendiente de cobro | Pedidos activos de la sucursal, no exclusivamente de la caja seleccionada. |

Los pedidos confirmados, en preparación, listos y entregados pueden continuar
pendientes de pago. Los borradores y cancelados no forman parte de ese resumen.
Un pedido entregado no equivale a un pedido cobrado.

## Revisar otra caja

El usuario con rol **Administrador** dispone de **Consultar caja** para elegir
una sesión abierta. La etiqueta **Solo consulta** indica que se está revisando
otra sesión, no asumiendo su identidad ni su turno. Los pendientes siguen siendo
de la sucursal. Si la sesión dejó de estar abierta, elige otra o revisa el historial.

## Comprobar datos actuales

La pantalla intenta actualizarse cada 20 segundos. Usa el botón de actualización
y revisa la hora de última actualización antes de tomar una decisión. Si aparece
un error, verifica conexión y permisos; no cierres basándote en datos incompletos.

Expande una fila de **Historial de sesiones** para consultar fondo, medios,
gastos, esperado, contado, diferencia y notas disponibles. El historial de sesiones
no sustituye el [detalle de ventas](/ayuda/ventas-consultar).

Para finalizar tu turno sigue [Abrir y cerrar tu caja](/ayuda/caja-turno).
