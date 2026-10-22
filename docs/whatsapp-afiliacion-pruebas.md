# Afiliacion publica por WhatsApp: pruebas

Esta entrega NO conecta, registra ni migra el numero del restaurante. Tampoco publica la app de Meta ni configura facturacion. Solo permite el modo Test; cambiar Mode a Production mantiene el servicio bloqueado. La coexistencia del numero real requiere una revision independiente antes de cualquier alta.

## Alcance

- Ruta publica `/fidelizacion`: afiliarse y recuperar la tarjeta verificando el celular; no requiere usuario del ERP ni datos fiscales.
- Web y QR de mesas muestran un enlace cuando la afiliacion esta habilitada. Se abre en otra pestana para no perder el pedido. El QR de mesa sigue identificando esa mesa; no se reemplaza.
- Para redes se puede compartir el enlace `/fidelizacion` del sitio; cualquier QR promocional debe apuntar a ese mismo enlace, nunca al enlace privado de una tarjeta.
- Nombre y telefono para nuevos miembros; correo y dia/mes de cumpleanos opcionales. Aceptacion del programa y publicidad son consentimientos separados. Publicidad desmarcada por defecto.
- No se revela si el telefono existe antes de probar su titularidad. Verificar un telefono existente devuelve la misma tarjeta, sin sobrescribir nombre, ficha fiscal, autorizaciones, visitas o premios. No reactiva miembros inactivos.
- No crea clientes fiscales, no vincula fichas por coincidencia y no genera visitas por registrarse.

## Configuracion del backend

Aplicar la migracion `AddPublicLoyaltyEnrollment` junto con las migraciones previas. Configurar por variables de entorno o secretos del servidor, no en archivos versionados ni en el frontend:

```dotenv
LoyaltyWhatsApp__Enabled=false
LoyaltyWhatsApp__Mode=Test
LoyaltyWhatsApp__PublicBranchId=ID_DE_LA_SUCURSAL_DE_PRUEBAS
LoyaltyWhatsApp__TestPhoneNumberId=ID_DEL_NUMERO_DE_PRUEBA_DE_META
LoyaltyWhatsApp__AccessToken=TOKEN_PRIVADO_DE_META
LoyaltyWhatsApp__AuthenticationTemplate=NOMBRE_DE_PLANTILLA_DE_AUTENTICACION
LoyaltyWhatsApp__TemplateLanguage=es
LoyaltyWhatsApp__GraphVersion=v25.0
LoyaltyWhatsApp__AllowedTestPhones__0=+593NUMERO_AUTORIZADO
LoyaltyWhatsApp__CodeHashKey=SECRETO_ALEATORIO_DE_AL_MENOS_32_CARACTERES
LoyaltyWhatsApp__WebhookVerifyToken=OTRO_SECRETO_ALEATORIO_DE_AL_MENOS_32_CARACTERES
LoyaltyWhatsApp__AppSecret=SECRETO_DE_LA_APP_DE_META
```

Usar secretos diferentes y generados aleatoriamente, no los literales de ejemplo. El token de acceso, el secreto de la app, la clave de los codigos y el token del webhook cumplen funciones distintas. Nunca compartirlos por chat ni capturas. Si uno se expone, revocarlo/rotarlo.

`Enabled` solo debe pasar a `true` despues de configurar TODOS los valores necesarios para el envio y una sucursal con programa activo en un entorno de pruebas. Si faltan, la pagina muestra no disponible y los enlaces del menu se ocultan. No activar el programa real para probar esta integracion.

Los destinatarios deben estar tanto en la lista de prueba de Meta como en AllowedTestPhones. El adaptador vuelve a comprobar esa lista antes de cada envio. No se admite un proveedor simulado en produccion ni se devuelve el codigo en respuestas HTTP o logs.

### Plantilla requerida

Se necesita una plantilla de categoria **Authentication** aprobada/disponible para la cuenta remitente, con codigo en el cuerpo y boton **Copy code** (indice 0). El idioma configurado debe coincidir con el aprobado. El mensaje de ejemplo `Confirmacion del pedido` NO es una plantilla OTP y no verifica telefonos: no se reutiliza ni se simula su contenido.

Si la cuenta de prueba de Meta solo ofrece mensajes de ejemplo y no permite esa plantilla de autenticacion, el envio OTP real queda pendiente hasta disponer de una cuenta/remitente compatible. Las pruebas automatizadas usan un doble del transporte, no afirman que Meta haya aprobado una plantilla. El token temporal del panel debe renovarse cuando venza.

### Webhook

Despues de desplegar el backend, la URL publica sera:

`https://erp.elcalderoflameante.com/api/integrations/whatsapp/webhook`

Antes del despliegue esa URL NO esta disponible. En Meta se introduce el mismo valor de WebhookVerifyToken en Token de verificacion, se verifica y se suscribe el campo `messages`. La recepcion de datos de produccion depende tambien de las condiciones de publicacion/permisos que muestre Meta; esta entrega no las cambia.

GET verifica el token y responde al challenge. POST valida `X-Hub-Signature-256` con el AppSecret sobre los bytes originales. Se conservan solo estado e identificador de entrega para el numero de prueba configurado; se ignoran mensajes entrantes. Un webhook no verifica al cliente ni suma visitas. Duplicados y notificaciones fuera de orden no retroceden estados de entrega.

El proxy HTTPS debe preservar el cuerpo y los encabezados de firma. Debe sobrescribir los encabezados reenviados desde internet para que los limites por IP no puedan eludirse enviando X-Forwarded-For arbitrario.

## Seguridad y comprobacion

- Codigo aleatorio de 6 digitos, vigencia 5 minutos, 5 intentos por codigo, 60 segundos entre solicitudes, maximo 5 solicitudes por telefono en 24 horas. Los errores de envio tambien cuentan.
- Las solicitudes publicas se limitan ademas a 12 por IP/minuto; respuestas sin cache. Configurar limites adicionales en el proxy si se expone ampliamente.
- Solo se guarda HMAC del codigo ligado al identificador de solicitud, no el codigo en claro. Los reenvios invalidan codigos anteriores. PostgreSQL serializa solicitudes y consumo de codigos concurrentes; resisten reinicios y varias instancias.
- No se devuelve el enlace privado sin verificar. La recuperacion no mezcla ni duplica tarjetas. Si cambia el telefono desde caja, se elimina la marca de verificacion anterior.
- Una tarea elimina las solicitudes de mas de 7 dias cada 12 horas, incluso si se desactiva el envio. No elimina miembros, visitas ni premios.
- La tarjeta es un enlace privado: no publicarlo ni usarlo en QR de publicidad.
- El cliente HTTP publico no adjunta tokens de empleados ni registra codigos en consola.
- Antes de abrir al publico, revisar el texto de condiciones/privacidad y el flujo pendiente de canje. No incluye campanas, Wallet ni entrega de premios.

## Pruebas locales

Reutilizar `grimorio_postgres` en puerto 5432; no levantar otro contenedor. Usar exclusivamente la base `grimorio_checks` y el usuario `grimorio_checks`, separados de los datos del ERP. La suite tambien conserva compatibilidad con el antiguo contenedor desechable 15432/checks, pero no se necesita iniciarlo.

Configurar CORRECTION_CHECKS_CONNECTION apuntando a esa base y ejecutar `dotnet run --project backend/tests/OrderCorrections.Checks`. No activar CORRECTION_CHECKS_ALLOW_RESET salvo que se quiera borrar expresamente solo esa base de pruebas. La suite valida la combinacion host/puerto/base/usuario antes de operar.

Referencias: [Cloud API](https://developers.facebook.com/documentation/business-messaging/whatsapp/overview), [plantillas de autenticacion](https://developers.facebook.com/docs/whatsapp/business-management-api/authentication-templates/), [coexistencia](https://developers.facebook.com/documentation/business-messaging/whatsapp/embedded-signup/onboarding-business-app-users).
