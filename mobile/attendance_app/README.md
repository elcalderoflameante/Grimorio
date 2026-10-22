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

## Actualización 1.0.4

- La cámara Android procesa cuadros NV21 en memoria y captura JPEG al identificar.
  Si el dispositivo no admite el formato, utiliza fotografías como alternativa.
- Las marcaciones llevan un identificador de operación. El servidor guarda el
  resultado y la marcación en una misma transacción PostgreSQL; los reintentos
  recuperan ese resultado, incluso después de reiniciar la API.
- Una operación sin confirmar se guarda cifrada en la tablet. Al volver a abrir
  la app se intenta recuperar; si no hay conexión se muestra Reintentar conexión
  y se bloquean nuevas marcaciones hasta resolver la pendiente.
- Una jornada manual o corregida no puede crear otra jornada abierta ni
  superponerse con las horas de una jornada existente. Las operaciones de un
  empleado se serializan en PostgreSQL, también entre solicitudes concurrentes.

El despliegue debe actualizar primero la API (incluye la migración
`AddAttendanceKioskOperationReceipts`) y después instalar la APK en las tablets.
La API conserva compatibilidad con la APK 1.0.3, pero sus reintentos no tienen
la recuperación duradera de 1.0.4.

Almacenamiento seguro se mantiene en **10.x** para migrar las claves antiguas de
9.x. Cada tablet debe instalar y abrir esta versión antes de una futura migración
a 11.x. No se borran claves automáticamente si ocurre un error de migración.
La conservación del enrolamiento y credenciales debe probarse instalando encima
de la APK anterior, sin desinstalarla.

Validación local:

```powershell
flutter analyze --no-pub
flutter test --no-pub
dotnet run --project ../../backend/tests/AttendanceSecurity.Checks
dotnet run --project ../../backend/tests/AttendancePersistence.Checks
```

La última prueba usa exclusivamente PostgreSQL en `127.0.0.1:15440`, base
`attendance_checks`, usuario/clave `checks`. Requiere un contenedor aislado:

```powershell
docker run -d --name grimorio_attendance_checks -p 127.0.0.1:15440:5432 -e POSTGRES_DB=attendance_checks -e POSTGRES_USER=checks -e POSTGRES_PASSWORD=checks postgres:15-alpine
```
