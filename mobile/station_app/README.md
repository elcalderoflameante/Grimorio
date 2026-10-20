# Grimorio Estaciones

Aplicacion Flutter para estaciones de cocina/barra. Muestra items pendientes por estacion, recibe eventos en tiempo real del hub de cocina y permite actualizar estados.

## Verificacion

Desde `mobile/station_app`:

```powershell
flutter analyze
flutter test --dart-define=API_BASE_URL=http://localhost/api
```

Las pruebas usan servicios simulados; no requieren un backend en localhost.
Cubren pedidos nuevos y adicionales, correcciones de modificadores, cambios de
mesa, observaciones, cancelaciones, recuperacion de pedidos al reconectar,
actualizaciones concurrentes y pantallas
de tablet de 1024x600 y 1280x800.

La interfaz usa un modo compacto para pantallas de cocina: tarjetas de 170 px,
barra lateral reducida y controles densos, sin truncar nombres de componentes
de combos, modificadores ni observaciones.

En el menú de la pantalla KDS, `Tamaño de presentación` permite elegir
Automático, Compacto, Normal o Grande. La preferencia se guarda únicamente en
el dispositivo; el modo Automático selecciona el perfil según el espacio útil
reportado por Android.

Si se pierde la conexión con el servidor, la app conserva las comandas visibles
y reintenta automáticamente con esperas de 2, 5, 10 y 30 segundos. Al recuperar
la conexión vuelve a consultar pedidos activos y completados, sin intervención
del usuario. Los cambios de estado que fallen por una interrupción temporal se
mantienen pendientes y se reintentan con la misma espera progresiva; el plato
permanece bloqueado hasta que el servidor confirme el cambio.

Antes de distribuir una APK, comprobar en una tablet real:

1. Login por sucursal, usuario y PIN; seleccion de varias estaciones.
2. Pedido con modificadores, nota general y observaciones por plato.
3. Preparacion parcial desde Alexa y desde la pantalla, incluyendo cancelacion.
4. Desconectar Wi-Fi, modificar un pedido y reconectar: verificar pantalla e historial.
5. Avisos de voz para nuevos pedidos, adicionales, correcciones de opciones,
   cambios de mesa y cambios de observaciones.
6. Deteccion de una version superior, descarga, validacion SHA-256 y apertura
   del instalador de Android.

La voz de la app reproduce avisos; los comandos de voz se gestionan con Alexa.
El canal general funciona en modo de solo recepción: se conecta automáticamente
después del login y no solicita micrófono ni muestra controles para transmitir.
La recepción se mantiene en segundo plano sin mostrar una barra de estado, para
no quitar espacio ni provocar desbordes en televisores y TV Box.

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

La app consulta `/downloads/station-version.json` al abrir y cada seis horas.
Despues de copiar la APK, actualizar en ese archivo `versionCode`,
`versionName` y el hash obtenido con:

```powershell
Get-FileHash ..\..\frontend\public\downloads\grimorio-estaciones.apk -Algorithm SHA256
```

La primera version que incorpora el actualizador debe instalarse manualmente.
Las siguientes se descargan desde la app y solo requieren confirmar el
instalador de Android. Si el dispositivo lo solicita, habilitar una sola vez
`Instalar aplicaciones desconocidas` para Grimorio KDS.

Android solo aceptara la actualizacion si la nueva APK esta firmada con la misma llave y tiene un `versionCode` mayor.

## Instalacion en dispositivos

En cada dispositivo Android:

1. Abrir el ERP desde el navegador.
2. Entrar a `POS > Estaciones`.
3. Descargar la APK desde `Descargar APK estaciones`.
4. Permitir instalacion desde el navegador si Android lo solicita.
5. Instalar la aplicacion.
