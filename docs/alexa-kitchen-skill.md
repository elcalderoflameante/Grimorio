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

- `action`: `preparando`, `listo`, `lista`, `terminado`, `completo`
- `itemText`: `salchipapa`, `combo uno`, `alitas`, `hamburguesa`, texto libre si el locale lo permite
- `tableCode`: numero o codigo de mesa
- `orderNumber`: numero de pedido
- `allItems`: palabras como `todo`, `toda`, `pedido`

Utterances:

```text
{action} mesa {tableCode}
{action} pedido {orderNumber}
{action} {itemText} mesa {tableCode}
{action} mesa {tableCode} {itemText}
{action} todo mesa {tableCode}
{action} todo el pedido mesa {tableCode}
{action} pedido mesa {tableCode}
mesa {tableCode} {action}
mesa {tableCode} {action} {itemText}
mesa {tableCode} {action} todo el pedido
```

## Lambda

Scaffold incluido:

- `integrations/alexa/kitchen-skill/lambda/index.mjs`
- `integrations/alexa/kitchen-skill/interaction-model.es-ES.json`

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
