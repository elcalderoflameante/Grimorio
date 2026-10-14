# Vinculacion de empleados y usuarios

Los empleados conservan sus datos laborales y los usuarios sus credenciales,
roles y permisos. La cuenta puede vincularse opcionalmente a un empleado de la
misma sucursal. Cada empleado admite una sola cuenta que no haya sido eliminada,
incluidas las cuentas inactivas. Las cuentas tecnicas pueden seguir sin empleado.

Desde Administracion > Usuarios, al crear o editar una cuenta se selecciona
Empleado vinculado. Al crear, se precargan nombre, apellido y correo del empleado;
la contrasena y los roles se configuran de forma explicita. Los datos de acceso
no se modifican automaticamente al cambiar los datos laborales.

Para relacionar una mesera ya registrada, edita su usuario y selecciona su ficha
de empleado. La lista de usuarios muestra el empleado vinculado, y la ficha y
lista de empleados muestran la cuenta asociada. La seleccion usa IDs y excluye
empleados asignados a otras cuentas o inactivos.

Desactivar o eliminar un empleado desactiva su cuenta vinculada dentro de la
misma transaccion. Tambien se bloquean login por contrasena, login por PIN y
nuevas peticiones autenticadas con JWT existentes. Reactivar al empleado no
reactiva su usuario: un administrador debe habilitar expresamente la cuenta.
Las conexiones SignalR existentes se vuelven a validar al reconectarse.

El vinculo historico se conserva cuando se elimina una cuenta o un empleado.
Una cuenta eliminada no impide asignar otra al mismo empleado. Las restricciones
de unicidad se aplican tambien en PostgreSQL y la baja se serializa con la
creacion o cambio del vinculo mediante bloqueo de la fila del empleado.

El despliegue requiere aplicar la migracion `20261014163633_LinkUsersToEmployees`.
Las cuentas existentes quedan sin asociacion hasta vincularlas manualmente.

Las integraciones se verifican con `backend/tests/UserEmployeeLink.Checks` y una
base PostgreSQL aislada en `127.0.0.1:15432`, base `checks`, usuario `checks`.
El proyecto exige `EMPLOYEE_LINK_CHECKS_CONNECTION` y rechaza otros destinos.
