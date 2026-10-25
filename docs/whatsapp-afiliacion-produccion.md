# WhatsApp y afiliacion: preparacion de produccion

El codigo admite Production y esta preparado para el numero esperado +593964135806 (0964135806). Aparecer en Business Suite no demuestra que este conectado a Cloud API ni que la coexistencia este habilitada. Esta entrega no registra ni migra el numero y no modifica la cuenta de Meta.

## Enlaces que quedaran disponibles tras desplegar

| Uso | URL |
| --- | --- |
| Afiliacion y recuperacion | https://www.elcalderoflameante.com/fidelizacion |
| Politica de privacidad | https://www.elcalderoflameante.com/fidelizacion/privacidad |
| Instrucciones de eliminacion de datos | https://www.elcalderoflameante.com/fidelizacion/eliminar-datos |
| Webhook (GET de verificacion y POST firmado) | https://erp.elcalderoflameante.com/api/integrations/whatsapp/webhook |
| App de Meta | https://developers.facebook.com/apps/2240696883514460/dashboard/ |
| Administrador de WhatsApp | https://business.facebook.com/wa/manage/home/ |

Los enlaces son los destinos previstos, NO una afirmacion de que el despliegue ya se haya realizado. El ERP los calcula desde VITE_PUBLIC_APP_URL y VITE_ERP_APP_URL al arrancar el contenedor; deben apuntar a los dominios reales con HTTPS, no localhost ni example.com.

En Marketing y fidelizacion > Programa > Conexion de WhatsApp se muestran enlaces copiables, parametros pendientes, numero esperado y limite diario. Solo usuarios con Marketing.Loyalty.Manage de la sucursal configurada pueden consultar esta pantalla; las respuestas nunca contienen secretos ni tokens. No hay formulario para guardarlos en el navegador.

## 1. Desplegar sin activar envios

Incorporar las variables nuevas de deploy/cloudcone/.env.example al .env privado del servidor. No reemplazar el archivo existente ni sus claves de base de datos, JWT, SMTP o SRI. Docker Compose pasa las variables de WhatsApp al servicio api; una variable suelta en .env no llega al contenedor si no esta declarada en Compose.

Mantener:

```dotenv
LOYALTY_WHATSAPP_ENABLED=false
LOYALTY_WHATSAPP_MODE=Production
LOYALTY_WHATSAPP_RESTRICT_RECIPIENTS=true
LOYALTY_WHATSAPP_COEXISTENCE_CONFIRMED=false
LOYALTY_WHATSAPP_DAILY_SEND_LIMIT=100
```

El backend aplica sus migraciones habituales al arrancar. Este cambio de produccion no requiere otra migracion de esquema ademas de las anteriores de fidelizacion. No reiniciar ni borrar la base del ERP para configurarlo.

Desplegar api y web siguiendo el procedimiento habitual. Comprobar que los enlaces publicos carguen desde un telefono externo y que la pantalla Programa muestre los dominios correctos. La afiliacion debe continuar desactivada.

La politica de privacidad y las instrucciones son textos operativos del programa, no una certificacion juridica. Revisar su exactitud, la identidad del responsable, la conservacion de datos y el procedimiento interno de atencion antes de publicarlos para clientes. Las solicitudes se atienden por el restaurante: este enlace no borra automaticamente datos ni documentos fiscales.

## 2. Completar la configuracion privada

| Variable del servidor | Valor |
| --- | --- |
| LOYALTY_WHATSAPP_BRANCH_ID | UUID real de la sucursal del programa; nunca el UUID de ejemplo |
| LOYALTY_WHATSAPP_META_APP_ID | 2240696883514460, confirmar que sea la app propia del restaurante |
| LOYALTY_WHATSAPP_BUSINESS_PHONE | +593964135806 |
| LOYALTY_WHATSAPP_PHONE_NUMBER_ID | Phone Number ID real de ese numero en Cloud API; no el numero de telefono, no el ID de prueba |
| LOYALTY_WHATSAPP_BUSINESS_ACCOUNT_ID | WABA real donde estan el numero y su plantilla |
| LOYALTY_WHATSAPP_ACCESS_TOKEN | Token privado de produccion con acceso a esa WABA y permisos de mensajeria y administracion necesarios |
| LOYALTY_WHATSAPP_APP_SECRET | Secreto de esta app de Meta, para validar firmas de webhooks |
| LOYALTY_WHATSAPP_AUTHENTICATION_TEMPLATE | caldero_codigo_fidelizacion, o el nombre aprobado exacto |
| LOYALTY_WHATSAPP_TEMPLATE_LANGUAGE | es, o el idioma aprobado exacto |
| LOYALTY_WHATSAPP_CODE_HASH_KEY | Secreto aleatorio propio, al menos 32 caracteres |
| LOYALTY_WHATSAPP_WEBHOOK_VERIFY_TOKEN | Otro secreto aleatorio propio, al menos 32 caracteres |
| LOYALTY_WHATSAPP_ALLOWED_PHONE_1 | Celular autorizado del operador para la primera prueba, formato +593... |
| LOYALTY_WHATSAPP_ALLOWED_PHONE_2 | Otro destinatario autorizado, opcional |

Generar las claves propias con un generador criptografico; por ejemplo `openssl rand -hex 32` en un terminal privado. Guardarlas en el .env protegido del servidor o su gestor de secretos, no en el repositorio, capturas, chat o configuracion frontend. No reutilizar el secreto JWT, el token de Meta ni el secreto de la app como claves propias.

El token temporal usado en la prueba anterior no es una credencial de produccion duradera. Configurar el acceso apropiado de la app/usuario del sistema al activo de WhatsApp segun lo que Meta exija para esa cuenta. No quitar el numero de la aplicacion del telefono ni ejecutar /register para intentar arreglar un error de permisos.

## 3. En Meta

1. Confirmar la conexion por **coexistencia** para seguir usando WhatsApp Business en el telefono. Si Meta no ofrece una ruta compatible, detener el alta y resolverla con soporte/proveedor; no migrar ni borrar el numero. No marcar COEXISTENCE_CONFIRMED por el mero hecho de verlo en Business Suite.
2. Verificar el ID real del numero y la WABA, y conceder a la app los accesos necesarios. La autorizacion anterior que selecciono solo Test Messaging Account no habilita por si sola la cuenta real.
3. Crear/aprobar una plantilla **Authentication** con boton **Copy code**, idioma exacto y expiracion recomendada de cinco minutos. No reutilizar Confirmacion del pedido ni una plantilla de marketing. No enviar publicidad dentro del codigo de verificacion.
4. Configurar el webhook con la URL de la tabla y el mismo valor privado de LOYALTY_WHATSAPP_WEBHOOK_VERIFY_TOKEN; verificar y guardar. Suscribir `messages` para el activo real y confirmar la vinculacion de la app a la WABA.
5. Completar la informacion de pago, publicacion de la app, verificacion empresarial y permisos que Meta solicite para ese caso. En Configuracion basica de la app usar las URLs publicas de privacidad e instrucciones de eliminacion cuando correspondan. La app debe poder recibir webhooks de produccion segun las condiciones que muestre Meta.

El webhook puede verificarse mientras ENABLED=false. El ERP nunca muestra el token: copiarlo desde el gestor de secretos al panel de Meta, no al chat.

## 4. Comprobar sin enviar mensajes

Con las variables completas y el backend reiniciado, usar **Comprobar Meta** en Programa. Esta accion hace solo lecturas HTTPS a Graph API: busca el numero esperado en la WABA y exige codigo de numero VERIFIED, plantilla APPROVED de categoria AUTHENTICATION, idioma coincidente y boton COPY_CODE. No modifica Meta ni envia mensajes.

La comprobacion esta disponible aunque ENABLED=false y antes de confirmar coexistencia. Exito significa que esas consultas fueron satisfactorias, no que se hayan validado facturacion, publicacion, entrega de mensajes o coexistencia. Si el numero no esta entre los primeros 100 de la WABA, la comprobacion falla de forma segura; revisar la cuenta configurada.

## 5. Primera prueba en el servidor de produccion

Solo despues de confirmar coexistencia en el telefono, marcar COEXISTENCE_CONFIRMED=true. Mantener RESTRICT_RECIPIENTS=true y la lista de destinatarios autorizados. Con el programa revisado/activo, cambiar ENABLED=true y recrear solamente api con el procedimiento de despliegue. No distribuir aun el QR general.

Probar desde un telefono autorizado: afiliacion, codigo correcto/incorrecto, reenvio despues de 60 segundos, recuperacion de la misma tarjeta y un consumo real autorizado si se quiere comprobar la acumulacion. El propio registro no genera visitas. Estas afiliaciones quedan en la base real: no crear facturas o cobros ficticios en produccion.

Cada solicitud nueva verifica otra vez numero y plantilla antes de enviar; una configuracion que apunte al numero equivocado o una plantilla pendiente impide el envio. Un fallo de Meta se devuelve sin cuerpos de respuesta ni credenciales. El webhook solo actualiza estados de entrega, nunca confirma el codigo del cliente ni otorga visitas.

## 6. Apertura y limites

Cuando las pruebas sean correctas y el procedimiento de entrega/canje de premios este resuelto, cambiar RESTRICT_RECIPIENTS=false y reiniciar api. Con programa activo y configuracion completa, la web y el menu QR mostraran la afiliacion publica. Descargar/publicar el QR general desde Marketing y fidelizacion; no compartir un enlace privado de tarjeta.

Conservar el tope inicial de 100 solicitudes de codigo por sucursal en 24 horas (configurable de 1 a 10000), ademas de cinco por telefono/dia, 60 segundos entre envios, cinco intentos y cinco minutos de vigencia. Las solicitudes fallidas tambien consumen presupuesto de intentos. No es una estimacion monetaria: los precios y limites de Meta son independientes.

Para detener los envios: ENABLED=false y recrear api. Esto no elimina tarjetas, calderos, visitas ni premios y no modifica el WhatsApp del telefono. Para volver a una prueba limitada: RESTRICT_RECIPIENTS=true y configurar los telefonos.

El proxy HTTPS debe preservar cuerpos/firma y sobrescribir encabezados reenviados desde internet. No registrar codigos, tokens, cuerpos de autorizacion ni el query de verificacion del webhook en logs publicos. Mantener las claves de Data Protection y todos los volumenes de produccion.

## Referencias de Meta

- https://whatsappbusiness.com/products/business-platform/
- https://developers.facebook.com/documentation/business-messaging/whatsapp/embedded-signup/onboarding-business-app-users
- https://developers.facebook.com/docs/whatsapp/business-management-api/authentication-templates/
- https://developers.facebook.com/documentation/business-messaging/whatsapp/webhooks/overview
