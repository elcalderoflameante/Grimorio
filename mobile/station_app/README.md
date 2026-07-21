# Grimorio Estaciones

Aplicacion Flutter para estaciones de cocina/barra. Muestra items pendientes por estacion, recibe eventos en tiempo real del hub de cocina y permite actualizar estados.

## Build de produccion Android

La APK no se publica en Play Store. Se genera firmada y se sirve desde el frontend del ERP en:

```text
/downloads/grimorio-estaciones.apk
```

### 1. Crear la llave de firma

Ejecutar una sola vez y guardar la llave en un lugar seguro:

```powershell
cd mobile\station_app
keytool -genkey -v -keystore android\app\grimorio-estaciones-release.jks -keyalg RSA -keysize 2048 -validity 10000 -alias grimorio-estaciones
```

La llave `android\app\grimorio-estaciones-release.jks` no debe subirse al repositorio. Android usa esta misma llave para permitir actualizaciones futuras sobre la APK instalada.

### 2. Configurar `key.properties`

Copiar el ejemplo:

```powershell
Copy-Item android\key.properties.example android\key.properties
```

Editar `android\key.properties` con las claves reales:

```properties
storePassword=...
keyPassword=...
keyAlias=grimorio-estaciones
storeFile=../app/grimorio-estaciones-release.jks
```

### 3. Generar la APK

Usar la URL publica real del API. Debe terminar en `/api`, igual que la app de meseros. La app deriva la URL de hubs desde esa base.

```powershell
flutter build apk --release --dart-define=API_BASE_URL=https://erp.elcalderoflameante.com/api
```

Salida esperada:

```text
build\app\outputs\flutter-apk\app-release.apk
```

### 4. Publicarla en el ERP

Copiar la APK al frontend con el nombre estable que usa el boton de descarga:

```powershell
New-Item -ItemType Directory -Force ..\..\frontend\public\downloads
Copy-Item build\app\outputs\flutter-apk\app-release.apk ..\..\frontend\public\downloads\grimorio-estaciones.apk
```

Despues de desplegar el frontend, la APK queda disponible en:

```text
https://erp.elcalderoflameante.com/downloads/grimorio-estaciones.apk
```

## Actualizaciones

Para actualizar la app instalada en tablets o pantallas de estacion:

1. Mantener la misma llave `.jks`.
2. Subir `version` en `pubspec.yaml`, por ejemplo `1.0.1+2`.
3. Volver a ejecutar `flutter build apk --release --dart-define=API_BASE_URL=https://erp.elcalderoflameante.com/api`.
4. Reemplazar `frontend\public\downloads\grimorio-estaciones.apk`.
5. Desplegar el frontend.

Android solo aceptara la actualizacion si la nueva APK esta firmada con la misma llave y tiene un `versionCode` mayor.

## Instalacion en dispositivos

En cada dispositivo Android:

1. Abrir el ERP desde el navegador.
2. Entrar a `POS > Estaciones`.
3. Descargar la APK desde `Descargar APK estaciones`.
4. Permitir instalacion desde el navegador si Android lo solicita.
5. Instalar la aplicacion.
