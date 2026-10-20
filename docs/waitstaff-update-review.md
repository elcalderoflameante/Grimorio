# Revision de Meseros y actualizaciones Android

Revision del 20 de octubre de 2026, tomando como base el commit `845c6fb`.
La integracion del actualizador de Estaciones estaba pendiente localmente y
se uso como referencia sin modificar sus archivos.

## Compatibilidad con los cambios recientes

| Cambio | Estado en Meseros |
| --- | --- |
| Nombres tematicos y generales de platos | Ya incluidos en catalogo y carrito. |
| Contrasenas temporales | La app indica completar el cambio en la web antes de ingresar. |
| Productos adicionales en cuentas listas o entregadas | Permitidos si la cuenta mantiene saldo y no esta pagada ni cancelada. |
| Promociones exclusivas, revalidacion de precios e inventario | Ya incluidos; el backend conserva la validacion definitiva. |
| Combos y opciones segun tamano | La app envia las opciones; el backend resuelve consumos y preparacion por estacion. |
| Saldo pendiente | La cuenta usa `pendingPaymentTotal` devuelto por el servidor. |
| Nombre del mesero asignado en el mapa | Pendiente visual: el backend devuelve `currentOrderWaiterName`, pero la app aun no lo muestra. Todas las mesas siguen visibles. |
| Correcciones de items autorizadas, cobros por cliente y precuenta impresa | Son funciones del POS web; no se agregaron controles de cobro ni cancelacion a Meseros. |
| Detalle de preparacion de cada componente de combo | Meseros muestra el estado del item padre; el desglose por componente continua en Estaciones. |

No se detecto un cambio de contrato que requiera modificar los endpoints de
toma de pedidos de Meseros. La verificacion funcional con dispositivos sigue
siendo necesaria para validar el comportamiento de instalacion del fabricante.

## Actualizador incorporado en 1.0.26+27

Consulta su propio manifiesto `waitstaff-version.json`; usa el mismo mecanismo
de consulta, descarga con progreso y apertura del instalador que Estaciones.
La publicacion incluye el APK firmado y el SHA-256 de ese archivo en el manifiesto.

Se consulta al iniciar, cada seis horas y al volver al primer plano cuando
pasaron cinco minutos desde la ultima consulta. Una falla de conexion no bloquea
la app. El aviso espera mientras este abierto el editor de pedidos para evitar
interrumpir borradores o envios pendientes.

Android exige permiso para instalar desde Meseros y confirmacion del usuario.
La primera instalacion que incorpora el actualizador es manual desde el ERP.
Las versiones posteriores pueden anunciarse y descargarse desde la app.

Pruebas: `flutter test --no-pub` y `dart analyze lib test` en
`mobile/waitstaff_app`. Cubren versiones mayores, rechazo de manifiestos
invalidos, hash incorrecto, instalacion y permiso, pedido en curso y modo sin red.
