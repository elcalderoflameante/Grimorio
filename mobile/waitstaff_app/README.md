# Grimorio Meseros

Aplicacion Flutter para que el personal de meseros vea y atienda solicitudes de mesas en tiempo real.

## Build de produccion Android

La APK no se publica en Play Store. Se genera firmada y se sirve desde el frontend del ERP en:

```text
/downloads/grimorio-meseros.apk
```

### 1. Crear la llave de firma

Ejecutar una sola vez y guardar la llave en un lugar seguro:

```powershell
cd mobile\waitstaff_app
keytool -genkey -v -keystore android\app\grimorio-meseros-release.jks -keyalg RSA -keysize 2048 -validity 10000 -alias grimorio-meseros
```

La llave `android\app\grimorio-meseros-release.jks` no debe subirse al repositorio. Android usa esta misma llave para permitir actualizaciones futuras sobre la APK instalada.

### 2. Configurar `key.properties`

Copiar el ejemplo:

```powershell
Copy-Item android\key.properties.example android\key.properties
```

Editar `android\key.properties` con las claves reales:

```properties
storePassword=...
keyPassword=...
keyAlias=grimorio-meseros
storeFile=../app/grimorio-meseros-release.jks
```

### 3. Generar la APK

Antes del build, confirmar que existe el archivo de Firebase:

```text
android\app\google-services.json
```

Ese archivo debe corresponder al proyecto Firebase usado para notificaciones push y no debe publicarse en documentacion.

Usar la URL publica real del backend. Debe terminar en `/api`.

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
Copy-Item build\app\outputs\flutter-apk\app-release.apk ..\..\frontend\public\downloads\grimorio-meseros.apk
```

Despues de desplegar el frontend, la APK queda disponible en:

```text
https://erp.elcalderoflameante.com/downloads/grimorio-meseros.apk
```

## Actualizaciones

Para actualizar la app instalada en los telefonos:

1. Mantener la misma llave `.jks`.
2. Subir `version` en `pubspec.yaml`, por ejemplo `1.0.1+2`.
3. Volver a ejecutar `flutter build apk --release --dart-define=API_BASE_URL=https://erp.elcalderoflameante.com/api`.
4. Reemplazar `frontend\public\downloads\grimorio-meseros.apk`.
5. Desplegar el frontend.

Android solo aceptara la actualizacion si la nueva APK esta firmada con la misma llave y tiene un `versionCode` mayor.

## Instalacion en telefonos

En cada telefono Android:

1. Abrir el ERP desde el navegador.
2. Entrar a `POS > Atencion QR`.
3. Descargar la APK desde `Descargar APK meseros`.
4. Permitir instalacion desde el navegador si Android lo solicita.
5. Instalar la aplicacion.
