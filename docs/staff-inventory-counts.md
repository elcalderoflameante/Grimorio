# Conteos del personal

La app Android registra observaciones antes del cierre. Administración revisa en **Inventario > Conteos del personal** por la mañana. El módulo nunca escribe en WarehouseStock, StockMovements ni StockReservations. Cualquier ajuste se registra por separado en Movimientos con los permisos correspondientes.

## Configuración inicial

1. Desplegar backend, migración `AddStaffInventoryCounts`, frontend y APK juntos según el procedimiento habitual.
2. Configurar el PIN operativo de cada persona en Usuarios.
3. Asignar `Inventory.Counts.Execute` al personal. Revisores: `Inventory.Counts.View` y `Inventory.Counts.Review`. Configuración: `Inventory.Counts.Manage`.
4. En **Áreas y frecuencias**, crear Cocina, Bar, Mesas u otras áreas y seleccionar usuarios autorizados. Varias personas pueden compartir Bar; una persona puede tener varias áreas.
5. Seleccionar artículos, bodega y frecuencia diaria/semanal. Cantidades en unidad base del artículo; no se presuponen equivalencias entre paquetes, botellas, porciones y kilos.
6. Instalar la APK desde **Descargar app** e ingresar con sucursal, nombre y PIN.

No se asignan productos/personas de ejemplo. Deben configurarse desde el ERP con datos reales.

## Reglas

- Actualizaciones Android: detección automática, descarga verificada e instalación con confirmación. El aviso espera mientras el conteo esté abierto. Desde `1.0.1+2`; la instalación inicial se hace desde el ERP. Ver README de la app para publicación de versiones.

- Fecha del servidor y zona horaria de la sucursal. Flujo pensado para tarde/noche antes de medianoche.
- Un artículo/bodega tiene una sola área responsable. Para reasignarlo se quita del área anterior.
- Diarios: un conteo enviado por fecha. Semanales: lunes a domingo; pendientes desde el día elegido hasta completarse esa semana. No se acumulan semanas anteriores.
- Un borrador activo por área, continuable por los usuarios asignados. Autor por línea y responsable de envío. Un borrador anterior conserva su fecha.
- Cero es válido y vacío sigue pendiente; se requieren todas las cantidades antes de enviar. Hasta cuatro decimales y observaciones de 1000 caracteres.
- Cada cantidad confirmada guarda hora del servidor y saldo del sistema, oculto al personal. Modificar cantidad toma otro saldo; editar solo observación conserva el anterior. Ventas posteriores no recalculan diferencias históricas.
- Se registra al contar, con conexión; no se reconstruye el saldo a una hora declarada por el teléfono. Pedidos/movimientos simultáneos pueden requerir revisión.
- Escrituras serializadas por sucursal, envío idempotente y rechazo de ediciones obsoletas en líneas compartidas. Cerrar el diálogo y actualizar ante un conflicto.
- Cantidad conservada en el diálogo si falla la red; borradores confirmados en servidor. Sin captura offline ni persistencia de cantidades sin confirmar al cerrar la app.
- Enviados inmutables. Revisar/descartar/pedir reconteo exige motivo y no sobrescribe revisiones previas.
- El reconteo crea otro documento enlazado, preserva el original e incluye sus artículos y nuevos pendientes. El original pasa a «Con reconteo enviado» tras el nuevo envío.
- Historial: rango de hasta un año y 500 conteos por consulta; 30 días inicialmente.

## Verificación

```powershell
dotnet build backend/Grimorio.slnx --no-restore
dotnet run --project backend/tests/StaffInventoryCounts.Checks --no-restore
```

Pruebas de persistencia exclusivamente en `127.0.0.1:15440/inventory_counts_checks`, usuario `checks`, sin leer configuración del ERP:

```powershell
docker run --detach --name grimorio_inventory_counts_checks --publish 127.0.0.1:15440:5432 --env POSTGRES_DB=inventory_counts_checks --env POSTGRES_USER=checks --env POSTGRES_PASSWORD=checks --tmpfs /var/lib/postgresql/data postgres:15
dotnet run --project backend/tests/StaffInventoryCounts.Checks --no-restore -- --database
docker stop grimorio_inventory_counts_checks
```

Cubren acceso por área/sucursal, cero frente a pendiente, edición simultánea, envío idempotente, revisión, reconteos, snapshots y ausencia de cambios en existencias/movimientos/reservas.
