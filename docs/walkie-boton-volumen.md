# Botón físico y sonidos del walkie-talkie

Implementación Android compartida por meseros y estaciones. En la barra del walkie,
abrir el icono de ajustes y activar **Hablar con volumen arriba**. Mantener el botón
para solicitar el turno; soltarlo termina o cancela la solicitud pendiente.
El botón solo se reserva cuando el canal está conectado y la opción está activa.
Volumen abajo conserva su función. Desactivar la opción permite subir el volumen.

Los tonos locales indican turno concedido (antes de abrir el micrófono), fin de
transmisión (después de cerrarlo) y canal ocupado/error. No son grabaciones ni se
envían como audio al resto del canal. Se pueden desactivar en los mismos ajustes.
Los avisos TTS esperan también durante los tonos y la solicitud de turno.

## Uso fuera de la app

Desde los ajustes del walkie, abrir Accesibilidad y habilitar
**Grimorio · botón del walkie-talkie** para la app que se use en ese teléfono.
Android exige que lo habilite la persona; la app no lo activa por sí sola.
El servicio solo filtra volumen arriba y no solicita lectura del contenido de
pantallas, gestos ni automatización de otras apps. Con ambas apps instaladas,
habilitar este servicio solamente en una: Android puede asignar el filtrado de
teclas a un único servicio de accesibilidad.

Activar primero el canal con la app visible y conceder micrófono y ejecución en
segundo plano. El servicio de accesibilidad por sí solo no inicia ni reabre el canal.

**Pantalla apagada: compatibilidad pendiente en Redmi 9 e Infinix Note 50 Pro.**
No todos los Android entregan esos eventos con pantalla apagada, incluso con
accesibilidad habilitada. El código procesa los eventos que Android entregue;
no evita el bloqueo del sistema ni mantiene encendida la pantalla. La recepción
de audio bloqueado ya probada no demuestra que el botón físico funcione bloqueado.

Si se pierde la señal del botón durante 1,5 segundos, termina la pulsación. Este
control requiere los eventos de repetición normales al mantener una tecla física.
También hay un máximo de 28 segundos y hay que soltar/volver a pulsar para un nuevo
turno. Se conserva la caducidad del permiso de publicación en el servidor.

## Prueba física requerida

En cada teléfono, anotar versión de Android y MIUI/XOS y comprobar:

1. Canal apagado u opción desactivada: volumen arriba ajusta volumen normalmente.
2. Con app visible: mantener para hablar, soltar para cerrar y pulsación muy rápida
   mientras se obtiene el turno (no debe abrir el micrófono más tarde).
3. Otro participante hablando: señal de ocupado y ningún segundo emisor.
4. Con otra app abierta, con pantalla bloqueada encendida y con pantalla apagada:
   mantener 5 segundos, soltar y comprobar desde el receptor que deja de escucharse.
5. Desconectar Wi-Fi, salir del canal o deshabilitar accesibilidad durante una
   pulsación: no debe volver a transmitir automáticamente al recuperar conexión.
6. Mantener más de 28 segundos: debe cortar, sin reiniciarse por repetición de tecla.
7. Recibir una solicitud durante el tono: su aviso hablado se conserva.
8. Comprobar tonos y primeras palabras con altavoz y auriculares.

Si Android no entrega las teclas con pantalla apagada, esta modalidad no queda
validada en ese dispositivo; habría que evaluar un accesorio PTT compatible.

## Verificación automatizada

En `mobile/packages/grimorio_voice`: `flutter analyze` y `flutter test`.
En el proyecto Gradle de una app: `:grimorio_voice:testDebugUnitTest`.
Las pruebas cubren liberación temprana, repeticiones, pérdida de eventos, corte
por duración y retorno a volumen normal. No reemplazan la prueba física del fabricante.
