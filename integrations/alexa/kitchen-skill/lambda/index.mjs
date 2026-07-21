const apiBaseUrl = process.env.GRIMORIO_API_BASE_URL;
const branchId = process.env.GRIMORIO_BRANCH_ID;
const integrationKey = process.env.GRIMORIO_ALEXA_KEY;

export const handler = async (event) => {
  const request = event.request ?? {};

  if (request.type === 'LaunchRequest') {
    return speak('Te escucho chef.', false);
  }

  if (request.type === 'SessionEndedRequest') {
    return speak('Listo.', true);
  }

  if (request.type !== 'IntentRequest') {
    return speak('No entendi el comando.', false);
  }

  if (request.intent?.name === 'AMAZON.CancelIntent' ||
      request.intent?.name === 'AMAZON.StopIntent') {
    return speak('Listo chef.', true);
  }

  if (request.intent?.name !== 'KitchenCommandIntent') {
    if (request.intent?.name === 'RepeatOrderIntent') {
      return repeatOrder(request.intent.slots ?? {});
    }

    return speak('Dime preparando, listo, o repite pedido mesa y el numero.', false);
  }

  if (!apiBaseUrl || !branchId || !integrationKey) {
    return speak('Grimorio no esta configurado para Alexa.', true);
  }

  const slots = request.intent.slots ?? {};
  const action = slotValue(slots.action);
  const tableCode = slotValue(slots.tableCode);
  const orderNumber = Number.parseInt(slotValue(slots.orderNumber) ?? '', 10);
  const stationText = slotValue(slots.stationText);
  const excludeStationText = slotValue(slots.excludeStationText);
  const itemText = slotValue(slots.itemText);
  const allItemsText = slotValue(slots.allItems);
  const isWholeOrder = isWholeOrderText(allItemsText);

  try {
    const response = await fetch(`${apiBaseUrl.replace(/\/$/, '')}/alexa/kitchen-command`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Grimorio-Alexa-Key': integrationKey,
      },
      body: JSON.stringify({
        branchId,
        action,
        tableCode,
        orderNumber: Number.isNaN(orderNumber) ? null : orderNumber,
        itemText: itemText || (isWholeOrder ? undefined : allItemsText),
        allItems: isWholeOrder,
      }),
    });

    if (!response.ok) {
      return speak('No pude comunicarme con Grimorio.', false);
    }

    const result = await response.json();
    return speak(result.message ?? 'Listo chef.', false);
  } catch {
    return speak('No pude comunicarme con Grimorio.', false);
  }
};

async function repeatOrder(slots) {
  if (!apiBaseUrl || !branchId || !integrationKey) {
    return speak('Grimorio no esta configurado para Alexa.', true);
  }

  const tableCode = slotValue(slots.tableCode);
  const orderNumber = Number.parseInt(slotValue(slots.orderNumber) ?? '', 10);

  try {
    const response = await fetch(`${apiBaseUrl.replace(/\/$/, '')}/alexa/order-repeat`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Grimorio-Alexa-Key': integrationKey,
      },
      body: JSON.stringify({
        branchId,
        tableCode,
        orderNumber: Number.isNaN(orderNumber) ? null : orderNumber,
        stationText,
        excludeStationText,
      }),
    });

    if (!response.ok) {
      return speak('No pude consultar el pedido en Grimorio.', false);
    }

    const result = await response.json();
    return speak(result.message ?? 'No encontre ese pedido.', false);
  } catch {
    return speak('No pude consultar el pedido en Grimorio.', false);
  }
}

function slotValue(slot) {
  return slot?.value?.trim() || undefined;
}

function isWholeOrderText(value) {
  if (!value) return false;
  const normalized = value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .trim();

  return /\b(todo|toda|todos|todas|pedido completo|todo el pedido|toda la mesa)\b/.test(normalized);
}

function speak(outputSpeech, shouldEndSession) {
  return {
    version: '1.0',
    response: {
      outputSpeech: {
        type: 'PlainText',
        text: outputSpeech,
      },
      reprompt: shouldEndSession
        ? undefined
        : {
            outputSpeech: {
              type: 'PlainText',
              text: 'Te escucho chef.',
            },
          },
      shouldEndSession,
    },
  };
}
