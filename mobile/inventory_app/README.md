# Grimorio Inventario

App Flutter para cocina, bar y mesas. PIN operativo existente y acceso a áreas asignadas. Conteos para revisión, sin modificar existencias.

## Compilar y publicar

```powershell
flutter pub get
dart format lib test
flutter analyze --no-pub
flutter test --no-pub
flutter build apk --release --no-pub --dart-define=API_BASE_URL=https://erp.elcalderoflameante.com/api
./publish-apk.ps1
```

Firma: `android/key.properties`, o configuración privada de `../station_app/android/key.properties` como respaldo. No se copian ni publican claves. Una firma propia usa `storePassword`, `keyPassword`, `keyAlias`, `storeFile`; rutas relativas a la carpeta `app` del proyecto dueño de las propiedades.

Paquete: `com.grimorio.inventory_app`. Mantener firma y aumentar versión para actualizaciones. Publicación en `/downloads/grimorio-inventario.apk` y `inventory-version.json` con versión y SHA-256. Descarga en **Inventario > Conteos del personal > Descargar app**.

## Uso

Desde `1.0.1+2`, la app consulta `/downloads/inventory-version.json` al iniciar, cada seis horas y al volver al primer plano si pasaron cinco minutos. No necesita login para comprobar versiones y sigue funcionando si falla la consulta. Permite posponer avisos opcionales (`required: false`).

Mientras un conteo está abierto, se posponen aviso e instalación; también se conserva cualquier cantidad escrita sin confirmar en el diálogo. Al salir del conteo se permite actualizar. Se descarga al caché privado, verifica SHA-256 y abre el instalador Android con confirmación del usuario. Si Android solicita permiso de instalación, regresa al instalador al volver sin abrir ajustes en bucle. Se valida paquete y versión superior; Android valida la firma. No requiere desinstalar ni borra borradores guardados en el servidor.

Esta primera versión con actualizador se instala desde el ERP; `1.0.0+1` no puede detectarla automáticamente. Las siguientes versiones se anunciarán dentro de la app. Publicar siempre APK y manifiesto juntos, con la misma firma y un `versionCode` mayor. El script publica actualizaciones opcionales por defecto.

Elegir sucursal y nombre, ingresar PIN, elegir área y confirmar cantidades. Borradores guardados en servidor. No se guardan PIN ni token en el teléfono; al abrir otra sesión se pide PIN. El borrador sigue disponible si vence la sesión.

Requiere conexión. Ante falla de red, las cantidades sin confirmar siguen en el diálogo para reintentar, pero no se conservan al cerrar la aplicación. Actualizar recupera cambios de otras personas en áreas compartidas.

[Operación y permisos](../../docs/staff-inventory-counts.md).
