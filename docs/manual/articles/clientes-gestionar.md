## Buscar y mantener clientes sin duplicarlos

Entra en **Facturación → Clientes** con `Billing.Customers.View`.
Crear o editar requiere `Billing.Customers.Manage`; ver sus ventas también
requiere el permiso de consulta de caja.

1. Busca por nombre, identificación, teléfono o correo antes de crear.
2. Si existe, revisa y actualiza ese registro; no crees otro porque el nombre
   tenga una tilde o una variante de escritura.
3. Para uno nuevo completa identificación, nombre o razón social y datos de
   contacto. Revisa especialmente el correo si se enviarán facturas.
4. Comprueba el tipo detectado y guarda. Cédula y RUC se validan; pasaporte
   debe seleccionarse expresamente cuando corresponda.

Si se indica **Ya existe**, utiliza o actualiza el registro identificado,
incluido uno inactivo cuando aplique. Escribir una identificación existente
no garantiza que el formulario cargue automáticamente los otros campos.
La validación del formato no consulta ni certifica identidad ante una entidad externa.

## Revisar el historial

Usa **Ver ventas del cliente** para consultar sus cobros. El historial corresponde
al registro seleccionado; dos fichas duplicadas pueden tener ventas distintas.

No elimines una ficha para intentar mover sus ventas a otra: la pantalla no es
un asistente de fusión. La consolidación de duplicados antiguos requiere una
revisión técnica, respaldo y comprobación de sus referencias.

Actualizar al cliente no equivale a modificar una factura autorizada. Si hay un
documento rechazado, revisa el XML, su versión y el error siguiendo
[Documentos electrónicos](/ayuda/facturacion-documentos).
