## Revisar facturas, XML y correo

Abre **Facturación → Documentos electrónicos** con `Billing.Sri.View`.
Generar o reintentar requiere los permisos y condiciones del documento;
leer esta guía no los concede.

1. Filtra por fechas y estado y localiza la orden y el número de factura.
2. Expande el detalle y revisa fecha de emisión, ambiente, clave de acceso,
   estado, reintentos y mensaje completo del error.
3. Usa **Descargar XML** o **Ver respuesta del SRI** para investigar el rechazo.
4. Si está autorizada, verifica número y fecha de autorización y descarga el
   **RIDE (PDF)** cuando esté disponible.
5. Revisa por separado destinatario y estado del correo. **Enviar factura por
   correo** no es lo mismo que enviarla al SRI.

## Si el cliente fue corregido

Comprueba que el XML enviado tenga la identificación y el tipo correctos.
No basta con ver el dato actualizado en Clientes. El reintento sigue las reglas
de regeneración y de estado del ERP; una autorización o rechazo de una versión
anterior no demuestra el resultado de una versión corregida.

Si aparece un resultado pendiente o un rechazo anterior, conserva la respuesta
y consulta nuevamente conforme al mensaje. Si se informa un límite diario de
intentos, no sigas pulsando reintentar ni cambies secuenciales o claves para
sortearlo: solicita revisión y respeta la indicación recibida.

## Límites

Un cobro, una autorización fiscal y un envío de correo son hechos distintos.
No cobres nuevamente por un error del SRI o del correo. No alteres una factura
autorizada usando la [corrección interna de medios](/ayuda/ventas-corregir-pagos).

Esta guía describe pantallas y estados del ERP, no determina plazos legales ni
procedimientos de anulación fiscal. Los casos fiscales se revisan con la persona
responsable de contabilidad. Consulta [Configuración fiscal](/ayuda/facturacion-configurar).
