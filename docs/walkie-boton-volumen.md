# Botón físico y sonidos del walkie-talkie

Implementación Android compartida por meseros y estaciones. En la barra del walkie,
abrir el icono de ajustes y activar **Hablar con volumen arriba**. Mantener el botón
para solicitar el turno; soltarlo termina o cancela la solicitud pendiente.
El botón solo se reserva cuando el canal está conectado y la opción está activa.
Volumen abajo conserva su función. Desactivar la opción permite subir el volumen.

El sonido local suministrado para el walkie indica turno concedido (antes de abrir
el micrófono) y fin de transmisión (después de cerrarlo). Canal ocupado/error usa
un aviso corto distinto. No se envían como audio al resto del canal y pueden
desactivarse en los mismos ajustes. Los avisos TTS esperan también durante el
sonido y la solicitud de turno.

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

El botón funciona con la app visible o con otra app abierta, siempre que la
pantalla permanezca encendida. Con la pantalla apagada vuelve a su operación
normal y no activa el micrófono. Esto evita depender de restricciones de batería
y entrega de teclas distintas entre fabricantes.

La pulsación no depende de eventos de repetición: se mantiene desde el evento de
presionar hasta el de soltar. Hay un máximo de seguridad de 28 segundos y luego
hay que soltar/volver a pulsar para un nuevo turno. Se conserva la caducidad del
permiso de publicación en el servidor.

## Prueba física requerida

En cada teléfono, anotar versión de Android y MIUI/XOS y comprobar:

1. Canal apagado u opción desactivada: volumen arriba ajusta volumen normalmente.
2. Con app visible: mantener para hablar, soltar para cerrar y pulsación muy rápida
   mientras se obtiene el turno (no debe abrir el micrófono más tarde).
3. Otro participante hablando: señal de ocupado y ningún segundo emisor.
4. Con otra app abierta y la pantalla encendida: mantener 5 segundos, soltar y
   comprobar desde el receptor que deja de escucharse. Al apagar la pantalla no
   debe iniciar una transmisión nueva.
5. Desconectar Wi-Fi, salir del canal o deshabilitar accesibilidad durante una
   pulsación: no debe volver a transmitir automáticamente al recuperar conexión.
6. Mantener más de 28 segundos: debe cortar, sin reiniciarse por repetición de tecla.
7. Recibir una solicitud durante el tono: su aviso hablado se conserva.
8. Comprobar tonos y primeras palabras con altavoz y auriculares.

## Verificación automatizada

En `mobile/packages/grimorio_voice`: `flutter analyze` y `flutter test`.
En el proyecto Gradle de una app: `:grimorio_voice:testDebugUnitTest`.
Las pruebas cubren liberación temprana, ausencia de repeticiones del fabricante,
corte por duración y retorno a volumen normal. No reemplazan la prueba física.
