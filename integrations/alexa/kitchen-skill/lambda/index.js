const apiBaseUrl = 'https://erp.elcalderoflameante.com/api'
const branchId = 'ae364a95-2f6d-43a6-bdc0-1b17e5d9746d';
const integrationKey = 'DhqqxIPpvDKxd6++1xRzjxy63PXSXc7pRnbPj/tASKQ=';

exports.handler = async function handler(event) {
  const request = event.request || {};

  if (request.type === 'LaunchRequest') {
    return speak('Oido chef.', false);
  }

  if (request.type === 'SessionEndedRequest') {
    return speak('Listo.', true);
  }

  if (request.type !== 'IntentRequest') {
    return speak('No entendi el comando.', false);
  }

  if (
    request.intent?.name === 'AMAZON.CancelIntent' ||
    request.intent?.name === 'AMAZON.StopIntent'
  ) {
    return speak('Listo chef.', true);
  }

  if (request.intent?.name !== 'KitchenCommandIntent') {
    return speak('Dime preparando o listo, la mesa y el plato.', false);
  }

  if (!apiBaseUrl || !branchId || !integrationKey) {
    return speak('Grimorio no esta configurado para Alexa.', true);
  }

  const slots = request.intent.slots || {};
  const action = slotValue(slots.action);
  const tableCode = slotValue(slots.tableCode);
  const orderNumber = Number.parseInt(slotValue(slots.orderNumber) || '', 10);
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
    return speak(result.message || 'Listo chef.', false);
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
              text: 'Oido chef.',
            },
          },
      shouldEndSession,
    },
  };
}
