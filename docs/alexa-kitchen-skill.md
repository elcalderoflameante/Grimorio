# Grimorio Alexa Kitchen Skill

Primera version para tablets Fire OS: Alexa escucha el comando y llama al API de
Grimorio para actualizar el estado de items de cocina.

## Flujo esperado

1. Usuario: `Alexa, abre cocina caldero` o `Alexa, abre bar caldero`
2. Alexa responde `Oido chef` y abre la escucha.
3. Usuario: `preparando salchipapa mesa 1`
4. Alexa llama al API y dice solo `Oido chef` si el cambio fue exitoso.
5. Si la sesion se cierra, el usuario vuelve a abrir la skill correspondiente.

## Respuestas discretas en cocina

- Cambio de estado con `success: true`: solo `Oido chef.`.
- Frase no reconocida, mesa ausente, plato ambiguo, error de red o rechazo del API:
  sin respuesta hablada; se solicita mantener la escucha abierta para repetir.
- `Repite el pedido`: lee el pedido cuando la consulta es exitosa; si falla, silencio.
- Abrir la skill: `Oido chef`.
- Decir `para` o `cancela`: `Hasta luego chef` y cierra la sesion.
- Por inactividad, el reprompt dice `Hasta luego chef`. Alexa abre una ultima ventana
  breve de escucha y despues cierra si no recibe otra instruccion.

La skill no mantiene el microfono abierto indefinidamente. Alexa controla el
tiempo de respuesta. El backend no puede hablar cuando recibe `SessionEndedRequest`,
por lo que la despedida por inactividad se reproduce como reprompt justo antes del
cierre; cuando deje de escuchar, volver a decir `Alexa, abre cocina caldero` o
`Alexa, abre bar caldero`.
La consola web de Alexa no reproduce el reprompt ni vence la sesion como un
dispositivo real. Amazon indica que la despedida por inactividad debe probarse
en un Echo o dispositivo Alexa. En el simulador, probar la despedida explicita
escribiendo `para` o `cancela`.
Estos cambios silencian las respuestas de nuestra skill, no los mensajes propios
de Alexa ante problemas de invocacion, red del dispositivo o servicio de Amazon.
El tiempo de espera de las llamadas al ERP se limita a 4,5 segundos para poder
devolver una respuesta silenciosa antes del limite de Alexa. Un timeout no
garantiza que el servidor no haya aplicado el cambio: comprobar KDS si hay duda.

Al repetir un pedido, el backend lee solamente platos pendientes o en preparacion.
No incluye platos `Ready`, y un pedido completamente listo deja de considerarse
activo para esta consulta. Esto aplica antes de los filtros Cocina/Bar y evita
volver a anunciar trabajo ya finalizado.

Referencia de sesiones:
https://developer.amazon.com/en-US/docs/alexa/custom-skills/manage-skill-session-and-session-attributes.html

Para activar este comportamiento, actualizar `lambda/index.js` y reconstruir el
modelo correspondiente: ahora incluye `AMAZON.FallbackIntent`. Conservar la
configuracion de estaciones de cada skill. Probar silencio, ruido, comando
incompleto, comando exitoso y repeticion del pedido en el Echo/Fire real.

## Separacion de cocina y bar

Se usan dos skills Custom en espanol Estados Unidos (`es-US`). Cada una tiene
su propio nombre de invocacion y su propia configuracion de Lambda:

| Skill | Invocation name | Modelo para JSON Editor | GRIMORIO_STATION_NAMES |
| --- | --- | --- | --- |
| Cocina | `cocina caldero` | `interaction-model.cocina.es-US.json` | `parrilla,fritos` |
| Bar | `bar caldero` | `interaction-model.bar.es-US.json` | `bar` |

Los modelos estan en `integrations/alexa/kitchen-skill/`. Ambas skills usan el
mismo archivo `lambda/index.js`, desplegado por separado. No configurar ambas
skills contra una sola Lambda con las mismas variables: responderian por las
mismas estaciones.

Los nombres de estacion deben coincidir con los del ERP, ignorando mayusculas,
acentos y espacios al inicio/final. `Cocina` es el nombre del grupo de voz; no
requiere crear una estacion nueva en el ERP.

Ejemplo: mesa 1 tiene limonada (bar), alitas (fritos) y ribeye (parrilla).

- `Abre cocina caldero` + `repite todo el pedido de la mesa 1`: alitas y ribeye.
- `Abre cocina caldero` + `listo todo el pedido mesa 1`: solo alitas y ribeye.
- `Abre bar caldero` + `repite pedido mesa 1`: solo limonada.
- `Abre bar caldero` + `preparando todo el pedido mesa 1`: solo limonada.

Los filtros hablados de estacion restringen aun mas el grupo configurado, nunca
lo amplian. Cocina puede pedir `repite parrilla mesa 1`. Pedir `repite bar mesa 1`
desde cocina no devuelve los platos del bar. `Sin bar` desde cocina conserva
parrilla y fritos si bar existe en el pedido.

### Orden de puesta en marcha

1. Desplegar primero el backend actualizado. El backend anterior no conoce
   `stationNames` y no limita los comandos por grupo.
2. En Alexa Developer Console, reutilizar la skill actual para Cocina y crear
   otra skill Custom para Bar, ambas con locale `es-US`.
3. En Build > Interaction Model > JSON Editor, importar el modelo correspondiente,
   guardar y ejecutar Build Skill.
4. En Code, usar el mismo `lambda/index.js` en ambas, con las variables indicadas
   abajo y el `GRIMORIO_STATION_NAMES` de cada fila. Guardar y desplegar cada Lambda.
   Si la instalacion actual usa constantes en `index.js` en lugar de variables de
   entorno, definir los valores en cada copia por separado; no versionar secretos.
5. En Test, activar las pruebas y comprobar ambos nombres con la misma cuenta y
   el idioma espanol Estados Unidos del dispositivo.
6. Probar una mesa mixta y verificar en KDS que cambiar toda cocina deja bar
   pendiente, y viceversa. Verificar tambien adicionales, modificadores y notas.

No se necesita migracion ni una nueva APK. Las estaciones reciben los mismos
eventos de estado que antes. La Lambda nueva rechaza comandos si falta configurar
sus estaciones; no cae silenciosamente al pedido completo.

Amazon pide normalmente dos palabras para nombres genericos; por eso se usan
`cocina caldero` y `bar caldero`, sujetos a la validacion de Amazon. Se evitan
`mi cocina` y `mi bar` porque Alexa puede confundirlos con grupos, dispositivos,
rutinas u otras funciones del hogar:
https://developer.amazon.com/en-US/docs/alexa/custom-skills/choose-the-invocation-name-for-a-custom-skill.html

## Endpoint Grimorio

`POST /api/alexa/kitchen-command`

Headers:

```http
Content-Type: application/json
X-Grimorio-Alexa-Key: <secret>
```

El secret se configura en el backend con:

```text
ALEXA_KITCHEN_COMMAND_KEY=<secret>
```

En Docker Compose se expone al API como `Alexa__KitchenCommandKey`.

Body minimo para pruebas con frase completa:

```json
{
  "branchId": "00000000-0000-0000-0000-000000000000",
  "stationNames": ["parrilla", "fritos"],
  "rawText": "preparando salchipapa mesa 1"
}
```

Body recomendado cuando Lambda ya parsea slots:

```json
{
  "branchId": "00000000-0000-0000-0000-000000000000",
  "stationNames": ["parrilla", "fritos"],
  "action": "preparando",
  "tableCode": "1",
  "itemText": "salchipapa",
  "allItems": false
}
```

Respuesta:

```json
{
  "success": true,
  "message": "Oido chef, salchipapa en preparacion.",
  "status": "InPreparation",
  "updatedCount": 1,
  "items": []
}
```

## Intents iniciales

Invocation names: `cocina caldero` y `bar caldero`.

Intent: `KitchenCommandIntent`

Slots:

- `action`: `preparando`, `listo`, `lista`, `listos`, `listas`, `terminado`, `completo`
- `itemText`: `salchipapa`/`salchipapas`, `combo uno`, `alitas`, `hamburguesa`, texto libre si el locale lo permite
- `tableCode`: numero o codigo de mesa
- `orderNumber`: numero de pedido
- `allItems`: palabras como `todo`, `toda`, `pedido`

Utterances:

```text
{action} pedido {orderNumber}
{action} {itemText} mesa {tableCode}
{action} mesa {tableCode} {itemText}
{itemText} {action} mesa {tableCode}
{itemText} mesa {tableCode} {action}
mesa {tableCode} {itemText} {action}
{action} todo mesa {tableCode}
{action} todo el pedido mesa {tableCode}
{action} pedido mesa {tableCode}
mesa {tableCode} {action} {itemText}
mesa {tableCode} {action} todo el pedido
```

Regla: `{action} mesa {tableCode}` no debe cambiar todos los platos. Para marcar toda la mesa,
el usuario debe decir explicitamente `todo`, `toda la mesa` o `todo el pedido`.
Las coincidencias parciales son validas cuando hay un solo plato probable: `combo 6 listo mesa 3`
puede marcar `Combo 6 de alitas`. Si en la misma mesa hay dos platos parecidos, por ejemplo
`Combo 6 de alitas` y `Combo 6 de salchichas`, Alexa debe pedir que se especifique cual.
La Lambda solo envia `allItems: true` cuando el slot realmente contiene `todo`, `toda`,
`todo el pedido` o equivalente; si Alexa coloca por error un plato plural como `salchipapas`
en ese slot, se reenvia como texto de plato.

Intent: `RepeatOrderIntent`

Slots:

- `tableCode`: numero de mesa
- `orderNumber`: numero de pedido
- `stationText`: estacion a repetir, por ejemplo `bar`, `fritos`, `parrilla`
- `excludeStationText`: estacion a excluir, por ejemplo `sin bar`

Utterances:

```text
repite pedido mesa {tableCode}
repite todo el pedido de la mesa {tableCode}
repite el pedido de la mesa {tableCode}
dime el pedido de la mesa {tableCode}
que tiene la mesa {tableCode}
que pidio la mesa {tableCode}
lee pedido mesa {tableCode}
repite pedido {orderNumber}
repite {stationText} mesa {tableCode}
repite pedido {stationText} mesa {tableCode}
que hay para {stationText} mesa {tableCode}
que tiene {stationText} mesa {tableCode}
repite pedido mesa {tableCode} sin {excludeStationText}
repite mesa {tableCode} sin {excludeStationText}
repite pedido mesa {tableCode} excepto {excludeStationText}
que hay en mesa {tableCode} sin {excludeStationText}
```

Si se indica una estacion, Grimorio responde solo los items de esa estacion. Si se usa
`sin` o `excepto`, responde todo el pedido menos esa estacion. Sin filtro de estacion,
repite el pedido de las estaciones configuradas en esa skill.

Endpoint de lectura:

`POST /api/alexa/order-repeat`

Body:

```json
{
  "branchId": "00000000-0000-0000-0000-000000000000",
  "stationNames": ["bar"],
  "tableCode": "1"
}
```

Respuesta:

```json
{
  "success": true,
  "message": "Pedido de Mesa 1: 1 salchipapa, pendiente; 1 combo uno, con BBQ, en preparacion."
}
```

## Lambda

Scaffold incluido:

- `integrations/alexa/kitchen-skill/lambda/index.js`
- `integrations/alexa/kitchen-skill/lambda/index.test.js`
- `integrations/alexa/kitchen-skill/interaction-model.cocina.es-US.json`
- `integrations/alexa/kitchen-skill/interaction-model.bar.es-US.json`

Los dos modelos contienen los mismos intents, slots y frases; solo cambia el
nombre de invocacion. Si se modifica el modelo de voz, aplicar el mismo cambio en
ambos JSON y conservar `cocina caldero` o `bar caldero` segun corresponda. El
modelo anterior de `grimorio` fue eliminado porque ya no se despliega.

La Lambda debe:

1. Leer `branchId` y `apiBaseUrl` desde variables de entorno.
2. Leer el secret `GRIMORIO_ALEXA_KEY`.
3. Enviar el comando al endpoint anterior.
4. Confirmar cambios exitosos con `Oido chef`; usar `message` solo al leer un pedido exitosamente.
5. Mantener la sesion abierta solo despues de respuestas exitosas o errores recuperables.

Variables requeridas por cada Lambda:

```text
GRIMORIO_API_BASE_URL=https://erp.elcalderoflameante.com/api
GRIMORIO_BRANCH_ID=<branch-id>
GRIMORIO_ALEXA_KEY=<secret>
GRIMORIO_STATION_NAMES=parrilla,fritos
```

Para Bar, cambiar solo `GRIMORIO_STATION_NAMES=bar`. La sucursal, URL y clave
de integracion pueden ser las mismas.

Si se edita la configuracion directamente en Code (sin acceso a variables de
entorno), reemplazar la declaracion completa de `stationNames` en cada copia:

```javascript
// Solo en la Lambda de Cocina Caldero:
const stationNames = ['parrilla', 'fritos'];
// O solo en la Lambda de Bar Caldero (no declarar ambas en el mismo archivo):
const stationNames = ['bar'];
```

Conservar en cada Lambda los valores reales ya usados para `apiBaseUrl`,
`branchId` e `integrationKey`. No pegar estas credenciales en el repositorio.

`stationNames` es opcional en el API para compatibilidad con clientes anteriores:
omitido o null conserva el alcance completo; una lista vacia o sin coincidencias
no devuelve ni actualiza platos. Es un filtro operativo de una integracion de
confianza, no una credencial ni un permiso de seguridad independiente por skill.

## Pruebas locales

```powershell
node --test integrations/alexa/kitchen-skill/lambda/index.test.js
dotnet run --project backend/tests/KitchenOrderState.Checks/KitchenOrderState.Checks.csproj
```
