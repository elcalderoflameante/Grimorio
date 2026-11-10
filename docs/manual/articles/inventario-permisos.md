## Permisos del módulo

| Permiso | Uso |
| --- | --- |
| `Inventory.Counts.Execute` | Realizar conteos del personal con PIN. Solo permite trabajar en áreas activas asignadas. |
| `Inventory.Counts.View` | Consultar conteos, cantidades esperadas y diferencias de la sucursal. |
| `Inventory.Counts.Manage` | Configurar áreas, responsables, artículos, bodegas y frecuencias. |
| `Inventory.Counts.Review` | Guardar una revisión, descartar o solicitar reconteo con motivo. Para consultar el documento en la web también se necesita View. |

El rol Administrador sigue el acceso administrativo existente. Los demás roles deben tener los permisos asignados expresamente.

## Preparar al empleado

1. Comprueba que existe un usuario activo de la sucursal. No crees otro usuario para el mismo empleado solo por usar esta app.
2. Con los permisos de Administración necesarios, configura su PIN operativo de cuatro dígitos en **Usuarios**.
3. Asigna a su rol **Realizar conteos del personal con PIN**.
4. Agrégalo a las personas autorizadas de al menos un área activa.
5. Pídele que ingrese con su nombre y PIN. Si la sesión ya estaba abierta antes del cambio, vuelve a ingresar.

El PIN es personal. No compartas cuentas o contraseñas administrativas para contar.

## Separación de responsabilidades

- **Personal:** observa y registra cantidades físicas sin ver las cantidades esperadas.
- **Supervisión:** compara, investiga y deja una decisión con motivo.
- **Administración de inventario:** configura áreas y, con permisos separados, registra movimientos autorizados.

Ninguno de los permisos de conteos permite por sí mismo registrar un movimiento de stock, editar una venta o cambiar una factura.

## Acceso a estos manuales

La API filtra las guías según los permisos de la sesión, también en las búsquedas y las capturas. Ver una guía no concede el permiso de ejecutar la operación. La documentación técnica de servidores y despliegues no forma parte de este centro de ayuda.
