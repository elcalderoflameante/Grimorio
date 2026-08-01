# Grimorio Asistencia

Kiosco Android para marcaciones del personal mediante deteccion facial, prueba de vida y reconocimiento SFace en el backend.

## Distribucion

La APK no se publica en Play Store. Se genera firmada y se sirve desde el frontend del ERP en:

```text
/downloads/grimorio-asistencia.apk
```

En produccion:

```text
https://erp.elcalderoflameante.com/downloads/grimorio-asistencia.apk
```

## Firma release

La app busca primero `android/key.properties`. Si no existe, reutiliza
`mobile/station_app/android/key.properties`, que apunta a la llave de produccion compartida por las apps internas.
La llave y `key.properties` nunca deben subirse al repositorio.

Ejemplo para una llave independiente:

```powershell
Copy-Item android\key.properties.example android\key.properties
```

## Generar la APK

```powershell
flutter pub get
flutter analyze
flutter test
flutter build apk --release --dart-define=API_BASE_URL=https://erp.elcalderoflameante.com/api
```

Salida:

```text
build\app\outputs\flutter-apk\app-release.apk
```

Publicacion:

```powershell
Copy-Item build\app\outputs\flutter-apk\app-release.apk ..\..\frontend\public\downloads\grimorio-asistencia.apk
```

Para actualizar una instalacion se debe incrementar `version` en `pubspec.yaml`, generar nuevamente con la misma llave y reemplazar la APK publicada.
