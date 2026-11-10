## Preparar mesas y atender solicitudes QR

Entra en **POS → Atención QR** con `POS.TableRequests.View`. Actualizar
solicitudes requiere `POS.TableRequests.Update`; gestionar mesas requiere
`POS.Tables.Manage`. Consultar no concede esos permisos.

## Preparar una mesa

1. En la pestaña de mesas crea o edita **Número de mesa**, **Área**, **Capacidad**
   y estado según corresponda.
2. Abre **Ver QR en grande** y comprueba la mesa y la URL antes de imprimir.
3. Prueba el QR desde un teléfono en la red que usarán los clientes.
4. En **Administración → Sucursal** revisa **Mostrar pestaña de menú** y
   **Permitir pedidos desde QR** y guarda la configuración.

Un enlace local solo funciona donde el teléfono alcanza ese servidor. Generar
o imprimir el QR no publica el backend. Regenerar el token cambia el acceso:
comprueba y reemplaza las impresiones anteriores.

## Atender durante el servicio

Revisa la pestaña de solicitudes, la mesa, el mensaje y el estado. Toma la
solicitud y actualízala después de atenderla para que el resto del personal
conozca su situación. Si se solicita la cuenta, aún debes realizar el cobro.

No confundas enviar una solicitud con haber sido atendido. Si falla la conexión,
actualiza y verifica que no exista ya una solicitud antes de repetirla.

## Mesera virtual

**Mesera virtual (solo desarrollo)** es una función de pruebas de la sucursal.
No presupongas disponibilidad en producción. El micrófono depende de permisos
del dispositivo y de un contexto seguro del navegador. Mantén una alternativa
de atención humana y revisa el pedido antes de confirmarlo.

El QR de mesa no es el [QR de afiliación](/ayuda/fidelizacion-programa).
