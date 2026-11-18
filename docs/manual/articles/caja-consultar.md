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

## Solicitudes de mesas desde la computadora

La campana **Solicitudes de mesas** de la barra superior permanece disponible
en Caja y en las otras pantallas del ERP. Necesitas `POS.TableRequests.View`;
para **Tomar**, **En proceso** y **Completar**, tambien `POS.TableRequests.Update`.
No basta con el permiso de consultar caja. No se asignan permisos automaticamente.

Abre la campana y activa **Sonido** con un clic en esa computadora. **Probar
parlante** reproduce un timbre y, si **Voz** esta activa, una frase de prueba.
Puedes dejar solo el timbre. La voz usa las voces disponibles del navegador o
Windows, priorizando espanol latinoamericano; no usa OpenAI ni pide microfono.

Cada solicitud nueva pendiente se anuncia una sola vez durante esa sesion de
la pagina. Las que ya estaban al entrar se muestran sin reproducir una cola de
avisos antiguos. El texto personalizado del cliente se muestra en pantalla, pero
no se lee en voz alta. Al cerrar sesion, recargar o cambiar de sucursal debes
activar el sonido otra vez. En varias pestanas, activalo solo en una para evitar
avisos duplicados entre ellas.

El listado incluye solicitudes pendientes, tomadas y en proceso, incluso si
tienen mas de 24 horas. Los cambios de otros equipos se recuperan por SignalR,
al reconectar o volver a la ventana, con un respaldo visible cada dos minutos.
Un fallo de conexion conserva el ultimo listado y muestra una advertencia.

**Tomar** registra como responsable al usuario que esta operando el ERP; no
asigna automaticamente una mesera distinta. Puedes avisar a la mesera cercana
y marcar **En proceso**, y **Completar** cuando haya atendido la mesa.
Escuchar un aviso no toma ni completa nada. Completar una solicitud de pedido
no confirma el pedido para cocina y una solicitud de cuenta no registra un cobro.

Manten el ERP abierto, el parlante conectado y Windows sin suspender. El
navegador puede limitar el audio o conexiones en segundo plano: si el sonido
se bloquea, activalo nuevamente y usa la prueba. La computadora apagada, dormida
o el navegador cerrado no reciben estos avisos.
