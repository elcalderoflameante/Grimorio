# Conteos del personal: referencia técnica

Documento privado. No se empaqueta en el centro de ayuda. Los procedimientos
vigentes están en el [manual operativo](../manual/articles/inicio.md).

## Entrega y contratos

Desplegar backend, migración `20261026231829_AddStaffInventoryCounts`, frontend,
APK y manifiesto `inventory-version.json` juntos. No se asignan datos de ejemplo.
Verificar asignaciones reales y uso en teléfono antes de distribuir.

- API: `InventoryCountsController`, bajo `/api/inventory/conteos`.
- Servicio: `StaffInventoryCountService`; nunca escribe `WarehouseStock`,
  `StockMovements` ni `StockReservations`.
- Ingreso: `/api/auth/inventory/branches`, `/api/auth/inventory/users` y
  `/api/auth/inventory/login`; PIN operativo existente, usuario activo y Execute.
- Permisos `Inventory.Counts.Execute`, `View`, `Manage`, `Review`, según endpoint.
  Execute exige además pertenencia al área activa y a la sucursal.
- Fecha del servidor en la zona de la sucursal. El borrador conserva su fecha.
- Un borrador activo por área y una responsable por combinación artículo/bodega.
- Bloqueo transaccional por sucursal (`pg_advisory_xact_lock`), envío idempotente
  y control de edición obsoleta con `previousCountedAt`.
- Snapshot de saldo/hora cuando cambia la cantidad; editar solo notas conserva
  el snapshot. Ventas posteriores no recalculan las diferencias históricas.
- Enviados inmutables; reconteos como nuevos documentos enlazados.
- Consultas de hasta 366 días y 500 documentos. Cantidades de cuatro decimales.

## Android y publicación

Proyecto `mobile/inventory_app`, paquete `com.grimorio.inventory_app`.
La compilación y el script están en su [README](../../mobile/inventory_app/README.md).

Firma privada en `android/key.properties`; si falta, se usa la de
`../station_app/android/key.properties`. No publicar claves. Mantener firma y
paquete, incrementar versión y publicar APK y manifiesto juntos, con SHA-256.
Las propiedades usan `storePassword`, `keyPassword`, `keyAlias`, `storeFile`;
las rutas relativas parten de `app` del proyecto dueño de las propiedades.

El actualizador 1.0.1+2 comprueba al iniciar, cada seis horas y al volver al primer
plano si pasaron cinco minutos. Posponer mientras `CountPage` esté abierta.
Descarga a caché privado, verifica SHA-256 y paquete/versión; Android valida firma.
Instalación con confirmación y sin bucles al volver de ajustes. No se persisten
PIN/token ni cantidades sin confirmar. No existe captura offline.

## Verificación

Desde la raíz, sin conexión de producción:

```powershell
dotnet build backend/Grimorio.slnx --no-restore
dotnet run --project backend/tests/StaffInventoryCounts.Checks --no-restore
```

La prueba opcional de persistencia admite exclusivamente la base aislada
`127.0.0.1:15440/inventory_counts_checks`, usuario de pruebas `checks`.
Nunca usar la conexión del ERP.

```powershell
docker run --detach --name grimorio_inventory_counts_checks --publish 127.0.0.1:15440:5432 --env POSTGRES_DB=inventory_counts_checks --env POSTGRES_USER=checks --env POSTGRES_PASSWORD=checks --tmpfs /var/lib/postgresql/data postgres:15
dotnet run --project backend/tests/StaffInventoryCounts.Checks --no-restore -- --database
docker stop grimorio_inventory_counts_checks
```

Cubren área/sucursal, cero frente a pendiente, edición simultánea, envío
idempotente, revisión, reconteos, snapshots y ausencia de ajustes de stock.
