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
5. Actualizar `frontend/public/downloads/waitstaff-version.json` con la misma
   version y el SHA-256 exacto del APK publicado.
6. Desplegar el frontend.

Android solo aceptara la actualizacion si la nueva APK esta firmada con la misma llave y tiene un `versionCode` mayor.

### Aviso y descarga dentro de la app

Desde `1.0.26+27`, Meseros consulta `/downloads/waitstaff-version.json` al iniciar,
cada seis horas y al volver al primer plano si pasaron al menos cinco minutos
desde la ultima consulta. No requiere una sesion iniciada. Si no hay conexion
o no existe el manifiesto, la app sigue funcionando.

La primera instalacion de esta version se hace descargando el APK desde el ERP.
Las versiones anteriores no tienen actualizador y no pueden detectar esta mejora.
Las siguientes actualizaciones se anuncian dentro de Meseros. El usuario pulsa
Actualizar ahora, ve el progreso y confirma la instalacion del sistema Android.
La primera vez debe permitir instalar aplicaciones desde Meseros. No es una
instalacion silenciosa y no requiere desinstalar ni borrar los datos.

El aviso se aplaza mientras este abierta la toma de pedidos, incluso si el
pedido esta esperando verificar un envio. No se reinicia ni cierra el editor.
Si el manifiesto tiene `required: false`, tambien permite posponer el aviso.

El APK se descarga al cache privado de actualizaciones y se verifica su SHA-256.
Solo se aceptan enlaces del mismo servidor; Android comprueba ademas que sea
el paquete de Meseros, una version mayor y una firma compatible con la instalada.
Publicar siempre APK y manifiesto en el mismo despliegue; no reutilizar la
misma version para binarios diferentes.

```json
{
  "versionCode": 28,
  "versionName": "1.0.27",
  "required": false,
  "apkUrl": "/downloads/grimorio-meseros.apk",
  "sha256": "REEMPLAZAR_POR_EL_SHA256_REAL_DEL_APK",
  "message": "Descripcion breve de los cambios."
}
```

Obtener el hash despues de copiar el APK a `frontend/public/downloads`:

```powershell
Get-FileHash ../../frontend/public/downloads/grimorio-meseros.apk -Algorithm SHA256
```

## Instalacion en telefonos

En cada telefono Android:

1. Abrir el ERP desde el navegador.
2. Entrar a `POS > Atencion QR`.
3. Descargar la APK desde `Descargar APK meseros`.
4. Permitir instalacion desde el navegador si Android lo solicita.
5. Instalar la aplicacion.
