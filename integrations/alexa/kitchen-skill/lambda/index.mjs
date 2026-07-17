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
    return speak('Dime preparando o listo, la mesa y el plato.', false);
  }

  if (!apiBaseUrl || !branchId || !integrationKey) {
    return speak('Grimorio no esta configurado para Alexa.', true);
  }

  const slots = request.intent.slots ?? {};
  const action = slotValue(slots.action);
  const tableCode = slotValue(slots.tableCode);
  const orderNumber = Number.parseInt(slotValue(slots.orderNumber) ?? '', 10);
  const itemText = slotValue(slots.itemText);
  const allItemsText = slotValue(slots.allItems);

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
        itemText,
        allItems: Boolean(allItemsText),
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

function slotValue(slot) {
  return slot?.value?.trim() || undefined;
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
