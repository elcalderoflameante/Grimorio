## Administrar usuarios, roles y sucursal

En **Administración**, cada pantalla tiene permisos propios de consulta y
gestión. Ver Usuarios no autoriza crear o eliminar cuentas. Usa cuentas personales
y asigna solo los permisos necesarios para la tarea.

## Configurar acceso

1. Revisa **Roles** y **Permisos** y separa consulta, registro, corrección y
   administración. El nombre Cajero o Mesero no garantiza una acción concreta.
2. En **Usuarios** busca la cuenta existente y crea o edita con el permiso
   correspondiente. Revisa estado, credenciales, roles y sucursal.
3. Vincula **Empleado vinculado** cuando sea una cuenta del personal.
4. Tras cambios de permisos, inicia sesión nuevamente y prueba con el usuario
   afectado, sin compartir la cuenta administradora.

La ficha de empleado y la cuenta son distintas. Un empleado admite una cuenta
no eliminada, incluso si esa cuenta está inactiva. Cambiar datos laborales no
actualiza automáticamente las credenciales. Desactivar o eliminar el empleado
desactiva su cuenta vinculada; reactivarlo no reactiva por sí solo al usuario.

## Sucursal

Revisa datos de contacto, zona horaria, imagen y controles de atención QR en
**Sucursal**. Cambiar la zona horaria puede cambiar cómo se agrupan y muestran
las fechas locales; no lo uses para corregir una factura individual.

## Soporte y seguridad

No compartas PIN, contraseñas, claves API ni certificados en capturas. Si una
pantalla no aparece, comprueba permisos y sesión antes de solicitar un cambio
de datos. Un botón oculto no debe resolverse usando el usuario de otra persona.

La ayuda utiliza los permisos de la sesión y no los amplía. Su habilitación en
producción es una entrega técnica separada; guardar una opción de sucursal no
publica por sí solo el centro de ayuda.
