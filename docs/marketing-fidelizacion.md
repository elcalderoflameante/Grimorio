# Marketing y fidelizacion: fase 1

## Configuracion inicial

1. Aplicar la migracion de fidelizacion y reiniciar el backend para registrar los nuevos permisos.
2. En Marketing y fidelizacion > Recompensas, crear los productos que se entregaran como premios. Estos usan las mismas imagenes, recetas y opciones que los productos existentes, pero su visibilidad es Solo recompensas: no aparecen en el menu publico, POS ni app de meseros y no pueden venderse por el flujo ordinario.
3. En Programa, asignar los productos a cada hito, habilitar los hitos y finalmente activar el programa. La escalera inicial es un borrador: visitas 5, 10, 15, 20 y 25; desde 25 se repite cada 5. El premio de la visita 20 queda por definir.
4. En Miembros y tarjetas, inscribir con nombre, celular y aceptacion del programa. No se pide cedula/RUC ni se crea una ficha fiscal ficticia. Correo y dia/mes del cumpleanos son opcionales. La autorizacion de comunicaciones comerciales es independiente y opcional.
5. Asociar la tarjeta al cobro. Cada cliente de una cuenta dividida conserva su propia tarjeta, independiente del cliente de facturacion. Se puede buscar por telefono, nombre o codigo CF, o leer el QR con un lector que escriba su contenido. Aplica tambien a domicilios registrados en el ERP; no hay integracion automatica con WhatsApp.

## Ficha del miembro

- El telefono principal es unico por sucursal. Se normalizan los formatos locales de celular ecuatoriano (09...), +593..., 593... y 00593... al mismo valor internacional. Otros paises deben ingresarse con + y codigo de pais. Es validacion de formato, no verificacion de titularidad ni de disponibilidad en WhatsApp.
- La ficha de fidelizacion tiene identidad propia. Al actualizar telefono, correo, cumpleanos o autorizacion comercial, no cambian el codigo de tarjeta, enlace privado, visitas ni premios.
- La ficha fiscal es opcional y se vincula expresamente desde el selector. No se vinculan ni fusionan clientes automaticamente por coincidencias de telefono o nombre. Cada ficha fiscal puede estar vinculada a una sola tarjeta por sucursal.
- Para vincular una ficha fiscal se necesita Billing.Customers.View. Seleccionar una ficha existente completa los campos de contacto vacios; no sobrescribe los ya ingresados. Los datos fiscales se siguen administrando en Clientes.
- Cumpleanos guarda solo dia y mes, sin anio; acepta el 29 de febrero. No se expone en la tarjeta publica. No se envian saludos ni promociones automaticamente en esta fase.
- La migracion conserva las tarjetas antiguas y copia el nombre y correo de su ficha fiscal. El telefono queda pendiente de completar explicitamente para no asignar numeros compartidos o duplicados. El registro rapido de nuevos miembros y la actualizacion de fichas requieren un celular valido.
- Una ficha fiscal inactiva no desactiva por si sola la tarjeta de fidelizacion. Son registros independientes.

## Reglas

- Configuracion por sucursal; no se modifica el historial de cobros anteriores.
- Minimo inicial de USD 10 despues de descuentos, IVA incluido. Minimo, inclusion de IVA, productos excluidos y vigencia son configurables.
- Los pagos parciales del mismo miembro, pedido y dia local suman para alcanzar el minimo. No se combinan pedidos diferentes ni dias distintos.
- Maximo una visita por miembro por dia, usando la zona horaria de la sucursal. El excedente no genera visitas adicionales.
- Solo se acumulan cobros asociados expresamente a una tarjeta; no se utiliza automaticamente el cliente de la factura.
- El progreso no se reinicia al emitir premios. El premio de una visita se emite una sola vez y conserva su regla, productos y vencimiento originales aunque se edite el programa.
- Vigencia inicial: 30 dias desde la emision. Disponible desde el siguiente dia local, no en el consumo que lo genero.
- Productos e IVA se calculan con los importes efectivamente cobrados. Propinas, cambio y dinero recibido de mas no acumulan visitas.
- Cobro, inventario, contribucion, visita y premio usan la misma transaccion. La restriccion unica del pago y el bloqueo del miembro evitan duplicados por reintentos o cobros concurrentes.

## Acceso

- Marketing.Loyalty.View: resumen, miembros, tarjetas e historial.
- Marketing.Loyalty.Enroll: inscribir y actualizar fichas de miembros, sin necesidad de permisos de facturacion para el registro rapido.
- Marketing.Loyalty.Use: asociar tarjetas a cobros.
- Marketing.Loyalty.Manage: configurar el programa; necesita Menu.Items.View para seleccionar productos.
- El catalogo Recompensas reutiliza Menu.Items.View y Menu.Items.Manage. Los roles Cajero reciben los permisos operativos del programa mediante el seeder habitual. Tras cambiar permisos, iniciar sesion nuevamente.
- La tarjeta web usa un enlace aleatorio de 256 bits; muestra solo el primer nombre, codigo, progreso y premios. No expone identificacion, correo o telefono. El QR contiene el codigo de tarjeta, no el enlace privado. No compartir el enlace privado en sitios publicos.

## Afiliacion publica en pruebas

La web incorpora `/fidelizacion` con afiliacion y recuperacion por codigo de WhatsApp. Esta deshabilitada por defecto y limitada a telefonos autorizados del entorno de pruebas; no conecta el numero real ni sustituye el registro desde caja. Ver [configuracion, limites y requisitos de Meta](whatsapp-afiliacion-pruebas.md).

## Limites de esta fase

Incluye configuracion, catalogo privado, inscripcion, actualizacion de datos, cumpleanos opcional, tarjeta web con QR, acumulacion, emision de premios e historial. **No incluye canje ni envio a preparacion de premios**, Google Wallet, Apple Wallet, notificaciones comerciales ni campanas (incluidas las de cumpleanos). No se deben entregar premios usando ventas comunes a cero: el canje necesita su propio flujo transaccional para registrar entrega e inventario sin duplicados.

No se aplican ajustes retroactivos de visitas ni reversiones automaticas por devoluciones en esta fase. No activar el programa hasta revisar estas condiciones y preparar el flujo de canje.
