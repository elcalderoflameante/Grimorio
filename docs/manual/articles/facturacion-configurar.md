## Revisar configuración fiscal y medios de pago

Esta tarea corresponde a administración y contabilidad. **Facturación →
Configuración fiscal** separa consulta (`Billing.Tax.View`) y gestión
(`Billing.Tax.Manage`); firma, integración y documentos pueden requerir permisos
SRI adicionales. No cambies valores de producción para experimentar.

## Antes de emitir

1. Revisa RUC, razón social, dirección, establecimiento y punto de emisión.
2. Confirma **Ambiente SRI**: pruebas y producción no son intercambiables.
3. Revisa secuenciales con la persona responsable; no los reinicies para resolver
   un error de autorización.
4. Carga el certificado `.p12` y su contraseña desde el formulario autorizado y
   comprueba su validación. No adjuntes certificado o contraseña a incidencias.
5. Configura SMTP, remitente y envío activo y realiza la prueba de correo
   únicamente con un destinatario autorizado.

## Tarifas y precios

Las tarifas tienen nombre, código SRI, porcentaje, estado y valor por defecto.
Los platos también pueden tener tarifa asignada. Crear una tarifa no demuestra
que todos los platos la estén usando ni que cambie el precio final.

Esta pantalla no es un asistente de IVA temporal por feriados: no presupongas
activación por fecha, reversión automática o elección entre conservar y reducir
el precio final. Ese flujo necesita implementación y validación específicas.
Antes de cambiar impuestos consulta el criterio y las fechas aplicables con
contabilidad; aquí no se fija normativa vigente.

## Medios y presentación

En **Medios de pago**, consulta con `Billing.PaymentMethods.View` y administra
con `Billing.PaymentMethods.Manage`. Revisa qué medio es efectivo, tarjeta o
consumo de empleados antes de usarlo. Los bancos de tarjeta se administran en
ese catálogo. El nombre visible no sustituye su configuración.

La **Plantilla de factura** modifica presentación; no autoriza una factura ni
corrige su XML. Comprueba un documento de prueba antes de distribuir un formato.
Consulta [Documentos electrónicos](/ayuda/facturacion-documentos).
