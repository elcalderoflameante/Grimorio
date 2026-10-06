# Walkie-talkie por sucursal

Estado: implementación local inicial; pendiente de despliegue y prueba de audio
con dispositivos reales. Consultar `deploy-walkie-talkie.md` para activación.

## Alcance acordado

Un canal general por sucursal compartido por meseros, estaciones de cocina/barra
y caja en el POS web. Todos los participantes conectados y con recepción activa
escuchan al emisor. Los meseros mantienen presionado para hablar y sueltan para
detener; las estaciones KDS son receptores y no pueden solicitar turno.
No grabar conversaciones ni modificar pedidos mediante la voz.

## Arquitectura propuesta

- LiveKit/WebRTC transporta únicamente audio. SDK Flutter para ambas apps y SDK
  web para el POS.
- La API autentica al participante usando su sesión existente. Deriva la sucursal
  del JWT y valida sus permisos; no acepta una sala arbitraria del cliente.
- Sala `walkie-branch-{branchId}` e identidad distinta por sesión/dispositivo,
  vinculada al usuario y, cuando corresponda, a sus estaciones autorizadas.
- Credenciales LiveKit exclusivamente en el servidor. Tokens temporales limitados
  a la sala autorizada, recepción y publicación de micrófono según el turno.
- Un hub de voz separado coordina presencia y turno de palabra. No transportar
  audio por SignalR ni Firebase.
- El servidor concede un único turno por sucursal de forma atómica y comprueba
  propiedad al renovarlo/liberarlo. Un identificador de concesión evita que un
  mensaje atrasado libere el turno de otro participante.
- El turno caduca si se pierde la conexión, falta su renovación o se alcanza el
  límite de transmisión. Propuesta inicial: máximo 30 segundos, ajustable.
- Aplicar el permiso de publicación también en el servicio de voz: deshabilitar
  un botón no basta para garantizar un único emisor. No conceder el siguiente
  turno hasta retirar el anterior; ante fallo, bloquear transmisión y recuperar.
- Si se despliegan varias instancias de API, el control del turno debe usar un
  coordinador compartido; un bloqueo en memoria solo sirve con una instancia.

## Integración

### Meseros

Control persistente accesible desde solicitudes, mapa, cuenta y toma de pedidos.
Estados: desconectado, conectando, escuchando, hablando, canal ocupado y
reconectando. Mostrar emisor y participantes conectados; estar conectado no
certifica que alguien haya escuchado físicamente el mensaje.

### Estaciones

La pantalla KDS se conecta automáticamente como receptor después del login, sin
solicitar micrófono ni registrar controles PTT. El servicio TTS actual tiene una
cola de anuncios: al recibir voz,
pausar o interrumpir conservando el aviso pendiente, y reanudarlo al finalizar.
No usar directamente `TtsService.stop()` para esto: actualmente vacía la cola.
Mantener visibles los avisos de pedidos durante la conversación.

### POS web

Control accesible durante el trabajo de caja. Activación explícita del usuario
para habilitar reproducción y solicitar permisos del navegador. Mostrar errores
de reproducción: una conexión establecida no asegura que el navegador permita
emitir sonido. Requiere HTTPS y pestaña activa en el sentido de no estar cerrada
o suspendida; no prometer recepción cuando el navegador termina la pestaña.

### Android y recuperación

Activar el modo walkie desde una pantalla visible. Configurar servicio en primer
plano, notificación y permisos adecuados para la sesión de audio. Probar recepción
con pantalla bloqueada en los dispositivos reales. No prometer reactivación del
micrófono desde segundo plano ni funcionamiento después de forzar el cierre.

Ante reconexión, volver a escuchar sin reanudar automáticamente una transmisión.
Detener el micrófono al soltar, cancelar el gesto, salir del canal, cerrar sesión
o perder el turno. Probar interrupciones por llamadas, auriculares, Bluetooth y
eco entre dispositivos cercanos.

## Alojamiento

El repositorio documenta CloudCone con PostgreSQL, API y Caddy en Docker.
LiveKit puede alojarse en infraestructura propia o usarse como servicio gestionado.
Se acordó alojamiento propio: VPS de 6 vCPU, 4 GB RAM y 60 GB de disco, para
6 dispositivos (3 meseros, caja, cocina y barra). Medir la memoria libre y el
consumo del ERP antes de activar; estos datos describen capacidad, no uso real.

Para alojamiento propio: definir dominio de voz, certificados, puertos de medios
y TURN, y medir consumo antes de compartir servidor con el ERP. Caddy ya ocupa
80/443: no instalar otro proxy/TURN en esos mismos puertos sin diseñar la
distribución. No se han cambiado DNS, firewall ni servicios de producción.

## Orden de implementación y aceptación

1. Servicio de voz de desarrollo y acceso autenticado por sucursal.
2. Turno único, caducidad, revocación y reconexión; probar pulsaciones simultáneas,
   liberaciones atrasadas y rechazo de otra sucursal.
3. Comunicación bidireccional entre meseros y estaciones.
4. Recepción con pantalla bloqueada y coordinación con anuncios TTS.
5. Integración en caja web y prueba con todos los participantes del canal.
6. Prueba de carga con la concurrencia esperada, volumen, latencia, batería y
   cambios de red; elegir alojamiento y preparar despliegue y nuevas APK.

## Referencias

- https://docs.livekit.io/transport/self-hosting/
- https://docs.livekit.io/transport/self-hosting/ports-firewall/
- https://docs.livekit.io/frontends/reference/tokens-grants/
- https://developer.android.com/develop/background-work/services/fgs/restrictions-bg-start
