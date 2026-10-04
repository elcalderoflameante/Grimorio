# Activar walkie-talkie en el VPS

Esta función requiere desplegar API, frontend y las dos apps actualizadas.
El código no activa por sí mismo el servidor de voz. No hay migraciones nuevas.
El despliegue base continúa funcionando sin el overlay de voz.

## Publicación de esta versión

- Meseros: `1.0.21+22`, `/downloads/grimorio-meseros.apk`.
- Estaciones: `1.0.8+9`, `/downloads/grimorio-estaciones.apk`.
- Ambas APK usan `https://erp.elcalderoflameante.com/api` y sus firmas existentes.
- Incluye la portada pública pendiente del restaurante en `/`; el ERP continúa
  en `/login` y `/dashboard`.
- Publicación autorizada para probar en producción. La compilación y los checks
  locales no certifican todavía audio real, recepción con pantalla bloqueada ni
  conectividad del VPS. El walkie arranca apagado y se activa manualmente.

El stack tiene cuatro contenedores: `db`, `api`, `web` (frontend y Caddy) y
`livekit` (audio). Las APK siguen instalándose en los dispositivos; Docker no
ejecuta las apps Android. DNS y firewall se configuran fuera de los contenedores.

## Configuración de producción

1. Crear un registro DNS A para `voz.elcalderoflameante.com` apuntando a la IP
   del VPS. No usar un proxy que solo soporte tráfico HTTP para los medios.
2. Abrir en el firewall del proveedor y del VPS TCP 7881, UDP 7882 y UDP 3478.
   Mantener TCP 80/443 para Caddy. El puerto administrativo 7880 no se publica.
3. En `deploy/cloudcone/.env`, añadir:

   ```dotenv
   VOICE_DOMAIN=voz.elcalderoflameante.com
   VOICE_API_KEY=una_clave_nueva_para_voz
   VOICE_API_SECRET=un_secreto_aleatorio_de_al_menos_32_bytes
   VOICE_MEMORY_LIMIT=512m
   ```

   Generar un secreto, por ejemplo con `openssl rand -hex 32`. Mantenerlo solo
   en el servidor; nunca en APK, frontend o repositorio.

4. Actualizar el repositorio con `git pull --ff-only`. Conservar el `.env`, los
   secretos y el nombre de proyecto Compose usados en el despliegue existente;
   no crear otro stack paralelo con volúmenes vacíos. Tener un respaldo reciente
   de la base antes de actualizar. Desde `deploy/cloudcone`, comprobar y levantar
   el stack (builds secuenciales para reducir el pico de memoria en el VPS de 4 GB):

   ```bash
   docker compose --env-file .env -f docker-compose.yml -f docker-compose.voice.yml config --quiet
   docker compose --env-file .env -f docker-compose.yml -f docker-compose.voice.yml pull livekit
   docker compose --env-file .env -f docker-compose.yml -f docker-compose.voice.yml build api
   docker compose --env-file .env -f docker-compose.yml -f docker-compose.voice.yml build web
   docker compose --env-file .env -f docker-compose.yml -f docker-compose.voice.yml up -d --no-build
   docker compose --env-file .env -f docker-compose.yml -f docker-compose.voice.yml ps
   docker compose --env-file .env -f docker-compose.yml -f docker-compose.voice.yml logs --tail=100 livekit api web
   docker stats --no-stream
   ```

   Usar ambos archivos también en futuras actualizaciones para conservar la
   configuración de voz. El límite de memoria inicial es provisional: medirlo
   con los seis dispositivos y ajustar sin dejar al ERP sin memoria.

5. Comprobar que el ERP permite ingresar y ver pedidos; descargar e instalar
   ambas APK actualizadas. Activar el canal en dos equipos y probar en ambos
   sentidos antes de incorporar los demás. Verificar el checklist inferior.

Nunca ejecutar `docker compose down -v`: elimina los datos persistentes.

## Diagnóstico y desactivación de voz

- No conecta: revisar DNS, certificado de Caddy y logs de `api`/`livekit`.
- Conecta pero no se escucha: comprobar volumen, permisos, botón «Activar audio»
  del navegador, firewall y puertos de medios. WSS conectado no prueba el audio.
- Contenedor reiniciándose: revisar logs y `docker stats`; si aparece `OOMKilled`,
  medir consumo y revisar el límite de memoria antes de aumentarlo.
- Silencio al bloquear Android: revisar permisos de segundo plano, notificación
  persistente y restricciones de batería del fabricante.

Si la voz causa problemas, salir del canal en los equipos y desactivarla sin
tocar la base de datos. Desde `deploy/cloudcone`, recrear API y web con el Compose
base (sin overlay) y después detener únicamente LiveKit:

```bash
docker compose --env-file .env -f docker-compose.yml up -d --no-build api web
docker compose --env-file .env -f docker-compose.yml -f docker-compose.voice.yml stop livekit
```

Esto desactiva la voz en el servidor; no revierte el código del ERP ni las APK.

## Operación

- Activar el walkie desde la app visible. Solicita micrófono, notificación y
  permiso de ejecución en segundo plano en Android. Sale apagado al iniciar.
- En web, activar audio si el navegador bloquea la reproducción automática.
- Mantener pulsado para hablar. Máximo servidor 30 segundos, renovación del
  turno cada 2,5 segundos y caducidad a los 8 segundos sin renovación.
- Al soltar, salir, bloquear la pantalla o perder conexión se detiene el envío.
  El servicio Android mantiene la recepción durante una sesión activada; debe
  verificarse en los equipos del restaurante. No funciona tras forzar el cierre.
- Los anuncios TTS de pedidos esperan al canal y se reanudan al finalizar.
- Una sola instancia de API: el control de turnos está en memoria. No escalar
  réplicas sin incorporar un coordinador distribuido. Cada arranque de API usa
  salas nuevas para separar las sesiones antiguas y forzar reconexión.
- Los participantes mostrados representan sesiones del canal, no confirmación
  de que alguien haya oído el audio ni de que tenga el volumen adecuado.
- No hay grabación de audio. La API emite credenciales temporales por sucursal
  y solo autoriza publicar micrófono al participante que tiene el turno.

El overlay usa ICE/TCP y TURN/UDP como alternativas de conectividad. No incluye
TURN/TLS en 443: si una red bloquea UDP y TCP 7881, hace falta configurar una
alternativa TURN/TLS con su certificado y resolver el uso de 443 junto a Caddy.

## Pruebas de aceptación en producción

- Mesero habla, cocina/barra/caja escuchan y pueden responder.
- Dos pulsaciones simultáneas: un solo emisor; soltar antes de recibir el turno
  no debe abrir el micrófono posteriormente.
- Pérdida de Wi-Fi mientras transmite: se libera el turno y se vuelve a escuchar
  al reconectar sin reanudar transmisión automáticamente.
- API o LiveKit reiniciados: la UI informa desconexión y permite recuperar.
- Dos sucursales no comparten participantes ni audio.
- Pedido nuevo durante conversación: el aviso TTS se conserva y se reproduce
  al terminar; notificaciones visuales siguen funcionando.
- Pantalla bloqueada 15 minutos: recepción y reconexión; comprobar batería.
- Auriculares, altavoz, eco entre dispositivos cercanos e interrupción por llamada.
- No grabar pedidos reales ni modificar su estado durante estas pruebas de voz.

## Desarrollo

Se puede ejecutar LiveKit local con credenciales exclusivas de desarrollo.
Configurar `Voice__Enabled=true`, `Voice__PublicUrl=ws://IP_LAN:7880`,
`Voice__InternalUrl=http://localhost:7880`, `Voice__ApiKey` y `Voice__ApiSecret`
en el proceso API. Un teléfono no puede alcanzar `localhost` del computador:
la URL pública debe usar una IP accesible desde su Wi-Fi. Producción usa WSS.

El módulo Flutter compartido está en `mobile/packages/grimorio_voice`.
Resolver dependencias en ambas apps antes de ejecutar `flutter run`.

### Verificaciones locales

```powershell
dotnet run --project backend/tests/VoiceChannel.Checks
dotnet build backend/Grimorio.API
```

En `frontend`: `npm run build`. En ambas apps: `flutter analyze --no-pub`.
Las pruebas de estaciones requieren la configuración de API, aunque sus
servicios estén simulados:

```powershell
flutter test --no-pub --dart-define=API_BASE_URL=http://localhost:5186/api
```

Los checks del backend usan un cliente de medios simulado: validan turnos,
caducidad, errores de permisos y separación de sucursales, no el transporte de
audio. Compilar o pasar estos checks no reemplaza las pruebas entre dispositivos.

## Referencias oficiales

- https://docs.livekit.io/transport/self-hosting/ports-firewall/
- https://docs.livekit.io/reference/other/roomservice-api/
- https://docs.livekit.io/transport/sdk-platforms/flutter/
