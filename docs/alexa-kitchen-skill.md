# Grimorio Alexa Kitchen Skill

Primera version para tablets Fire OS: Alexa escucha el comando y llama al API de
Grimorio para actualizar el estado de items de cocina.

## Flujo esperado

1. Usuario: `Alexa, abre Grimorio`
2. Alexa: `Te escucho chef`
3. Usuario: `preparando salchipapa mesa 1`
4. Alexa llama al API y responde con `message`.
5. Si la sesion se cierra, el usuario vuelve a decir `Alexa, abre Grimorio`.

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
  "rawText": "preparando salchipapa mesa 1"
}
```

Body recomendado cuando Lambda ya parsea slots:

```json
{
  "branchId": "00000000-0000-0000-0000-000000000000",
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

Invocation name: `grimorio`

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
mantiene el comportamiento normal y repite todo el pedido.

Endpoint de lectura:

`POST /api/alexa/order-repeat`

Body:

```json
{
  "branchId": "00000000-0000-0000-0000-000000000000",
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
- `integrations/alexa/kitchen-skill/interaction-model.es-US.json`

La Lambda debe:

1. Leer `branchId` y `apiBaseUrl` desde variables de entorno.
2. Leer el secret `GRIMORIO_ALEXA_KEY`.
3. Enviar el comando al endpoint anterior.
4. Responder a Alexa con el campo `message`.
5. Mantener la sesion abierta solo despues de respuestas exitosas o errores recuperables.

Variables sugeridas:

```text
GRIMORIO_API_BASE_URL=https://erp.elcalderoflameante.com/api
GRIMORIO_BRANCH_ID=<branch-id>
GRIMORIO_ALEXA_KEY=<secret>
```
