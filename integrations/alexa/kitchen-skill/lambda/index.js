const https = require('https');

const apiBaseUrl = process.env.GRIMORIO_API_BASE_URL;
const branchId = process.env.GRIMORIO_BRANCH_ID;
const integrationKey = process.env.GRIMORIO_ALEXA_KEY;

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
    if (request.intent?.name === 'RepeatOrderIntent') {
      return repeatOrder(request.intent.slots || {});
    }

    return speak('Dime preparando, listo, o repite pedido mesa y el numero.', false);
  }

  if (!apiBaseUrl || !branchId || !integrationKey) {
    return speak('Grimorio no esta configurado para Alexa.', true);
  }

  const slots = request.intent.slots || {};
  const action = slotValue(slots.action);
  const tableCode = slotValue(slots.tableCode);
  const orderNumber = Number.parseInt(slotValue(slots.orderNumber) || '', 10);
  const stationText = slotValue(slots.stationText);
  const excludeStationText = slotValue(slots.excludeStationText);
  const itemText = slotValue(slots.itemText);
  const allItemsText = slotValue(slots.allItems);
  const isWholeOrder = isWholeOrderText(allItemsText);

  try {
    const result = await postJson(
      `${apiBaseUrl.replace(/\/$/, '')}/alexa/kitchen-command`,
      {
        branchId,
        action,
        tableCode,
        orderNumber: Number.isNaN(orderNumber) ? null : orderNumber,
        itemText: itemText || (isWholeOrder ? undefined : allItemsText),
        allItems: isWholeOrder,
      },
    );

    return speak(result.message || 'Listo chef.', false);
  } catch {
    return speak('No pude comunicarme con Grimorio.', false);
  }
};

async function repeatOrder(slots) {
  if (!apiBaseUrl || !branchId || !integrationKey) {
    return speak('Grimorio no esta configurado para Alexa.', true);
  }

  const tableCode = slotValue(slots.tableCode);
  const orderNumber = Number.parseInt(slotValue(slots.orderNumber) || '', 10);

  try {
    const result = await postJson(
      `${apiBaseUrl.replace(/\/$/, '')}/alexa/order-repeat`,
      {
        branchId,
        tableCode,
        orderNumber: Number.isNaN(orderNumber) ? null : orderNumber,
        stationText,
        excludeStationText,
      },
    );

    return speak(result.message || 'No encontre ese pedido.', false);
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

function postJson(url, body) {
  const parsed = new URL(url);
  const payload = JSON.stringify(body);

  return new Promise((resolve, reject) => {
    const req = https.request(
      {
        hostname: parsed.hostname,
        path: `${parsed.pathname}${parsed.search}`,
        port: parsed.port || 443,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Content-Length': Buffer.byteLength(payload),
          'X-Grimorio-Alexa-Key': integrationKey,
        },
      },
      (res) => {
        let data = '';
        res.setEncoding('utf8');
        res.on('data', (chunk) => {
          data += chunk;
        });
        res.on('end', () => {
          if (res.statusCode < 200 || res.statusCode >= 300) {
            reject(new Error(`HTTP ${res.statusCode}: ${data}`));
            return;
          }

          try {
            resolve(JSON.parse(data));
          } catch (error) {
            reject(error);
          }
        });
      },
    );

    req.on('error', reject);
    req.write(payload);
    req.end();
  });
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
