# Arquitectura biométrica de asistencia

## Decisión de modelo (2026-07-30)

- Reconocimiento: OpenCV SFace `face_recognition_sface_2021dec.onnx` (Apache 2.0).
- Detección/alineación en backend: OpenCV YuNet `face_detection_yunet_2023mar.onnx` (Apache 2.0).
- Runtime: OpenCvSharp DNN en el backend .NET; ML Kit continúa realizando calidad y prueba de vida en la tablet.
- SFace SHA-256: `0BA9FBFA01B5270C96627C4EF784DA859931E02F04419C829E83484087C34E79`.
- YuNet SHA-256: `8F2383E4DD3CFBB4553EA8718107FC0423210DC964F9F4280604804ED2552FA4`.
- Los pesos se distribuyen junto al backend y sus avisos se conservan en `Biometrics/LICENSE`.

## Principios

- La detección de rostro y el reconocimiento de identidad son procesos diferentes.
- Ninguna marcación se autoriza únicamente porque ML Kit encontró un rostro.
- No se permiten vectores aleatorios, simulados ni generados desde otra plantilla.
- Las imágenes temporales se eliminan después del procesamiento local.
- Los timestamps se almacenan en UTC y el día laboral se determina con `America/Guayaquil`.

## Pipeline local

1. Cámara frontal.
2. Detección de exactamente un rostro con ML Kit.
3. Validación de tamaño, centrado, orientación y ojos visibles.
4. Prueba de vida activa: giro aleatorio, retorno al centro y parpadeo.
5. Alineación y recorte con los landmarks del mismo detector.
6. Preprocesamiento definido por el manifiesto del modelo.
7. Inferencia LiteRT para obtener el embedding.
8. Normalización L2.
9. Comparación únicamente con plantillas de la misma versión de modelo.
10. Confirmación de la acción contra el backend autenticado del kiosco.

## Contrato obligatorio del modelo

Antes de agregar un archivo `.tflite`, deben registrarse y revisarse:

- nombre y versión inmutables;
- URL y proveedor de origen;
- licencia del código, pesos y dataset de entrenamiento;
- autorización para uso comercial;
- SHA-256 del archivo;
- tamaño y orden del tensor de entrada;
- espacio de color;
- fórmula de normalización de píxeles;
- dimensión y tipo del embedding de salida;
- necesidad de alineación facial;
- benchmarks publicados;
- dispositivos Android usados para medir latencia;
- umbral inicial y protocolo de calibración local.

El modelo se empaqueta como asset de la APK. La aplicación debe negarse a enrolar o reconocer si el hash o los tensores no coinciden con su manifiesto.

## Enrolamiento

- Solo puede iniciarlo un usuario con `RRHH.Attendance.Enroll`.
- Debe ejecutarse desde un kiosco activo de la misma sucursal.
- Requiere prueba de vida.
- Debe aceptar al menos cinco muestras válidas con variación leve de pose.
- Cada muestra genera un embedding independiente.
- La plantilla final se obtiene mediante promedio y normalización L2.
- El backend cifra la plantilla antes de persistirla.
- Reenrolar revoca la plantilla anterior y deja auditoría.

## Reconocimiento

- Se comparan embeddings mediante similitud coseno.
- Deben coincidir versión y dimensiones del modelo.
- Se exige un umbral absoluto y una separación mínima frente al segundo candidato.
- El umbral definitivo se obtiene de un piloto con empleados reales y condiciones reales de iluminación; no se define arbitrariamente.
- Después de una coincidencia se aplica un periodo de enfriamiento para impedir marcaciones duplicadas.

## Alcance de la prueba de vida

El giro y parpadeo constituyen una prueba de vida activa básica. Reduce ataques con fotografías estáticas, pero no garantiza resistencia contra reproducción de video, máscaras o ataques de presentación avanzados. Antes de producción se debe evaluar un modelo dedicado de anti-spoofing o un servicio certificado, según el riesgo aceptado.
