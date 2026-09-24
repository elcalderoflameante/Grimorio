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
.\verify-release.ps1
Copy-Item build\app\outputs\flutter-apk\app-release.apk ..\..\frontend\public\downloads\grimorio-asistencia.apk
```

Para actualizar una instalacion se debe incrementar `version` en `pubspec.yaml`, generar nuevamente con la misma llave y reemplazar la APK publicada.

## Actualización 1.0.3

La API exige ahora un token de reconocimiento de un solo uso (30 segundos),
ligado al empleado y al kiosco. Actualizar la API y las tablets en la misma
ventana de mantenimiento: las APK anteriores no pueden marcar con la API nueva.
Los tokens están en memoria del único proceso API del despliegue actual;
un reinicio requiere reconocer el rostro nuevamente. Antes de usar varias réplicas,
se debe implementar almacenamiento compartido con consumo atómico.

La dirección predeterminada de la app es producción. Para desarrollo se debe
proporcionar `API_BASE_URL` explícitamente; Android release solo permite HTTPS.
Verificar también la URL incluida en el binario, no solamente la firma y versión.

Antes del despliegue, probar en la tablet: entrada, descanso, regreso, salida
confirmada, selección abandonada, pérdida/restablecimiento de Internet, suspensión
y reanudación, rostro desconocido y cierre de una jornada después de medianoche.
El parpadeo es una comprobación básica local, no una garantía contra reproducción
de videos. La contingencia sin Internet sigue siendo el registro manual en el ERP.
