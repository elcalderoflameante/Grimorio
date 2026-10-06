const https = require('https');

const apiBaseUrl = process.env.GRIMORIO_API_BASE_URL;
const branchId = process.env.GRIMORIO_BRANCH_ID;
const integrationKey = process.env.GRIMORIO_ALEXA_KEY;
const stationNames = (process.env.GRIMORIO_STATION_NAMES || '')
  .split(',')
  .map((name) => name.trim())
  .filter(Boolean);

exports.handler = async function handler(event) {
  const request = event.request || {};

  if (request.type === 'SessionEndedRequest') {
    return { version: '1.0', response: {} };
  }

  if (request.type === 'LaunchRequest') {
    return isConfigured()
      ? speak('Oido chef.', false)
      : speak(null, true);
  }

  if (request.type !== 'IntentRequest') {
    return speak(null, false);
  }

  if (
    request.intent?.name === 'AMAZON.CancelIntent' ||
    request.intent?.name === 'AMAZON.StopIntent'
  ) {
    return speak('Hasta luego chef.', true);
  }

  if (request.intent?.name !== 'KitchenCommandIntent') {
    if (request.intent?.name === 'RepeatOrderIntent') {
      return repeatOrder(request.intent.slots || {});
    }

    return speak(null, false);
  }

  if (!isConfigured()) {
    return speak(null, true);
  }

  const slots = request.intent.slots || {};
  const action = slotValue(slots.action);
  const tableCode = slotValue(slots.tableCode);
  const orderNumber = Number.parseInt(slotValue(slots.orderNumber) || '', 10);
  const itemText = slotValue(slots.itemText);
  const allItemsText = slotValue(slots.allItems);
  const isWholeOrder = isWholeOrderText(allItemsText);

  try {
    const result = await postJson(
      `${apiBaseUrl.replace(/\/$/, '')}/alexa/kitchen-command`,
      {
        branchId,
        stationNames,
        action,
        tableCode,
        orderNumber: Number.isNaN(orderNumber) ? null : orderNumber,
        itemText: itemText || (isWholeOrder ? undefined : allItemsText),
        allItems: isWholeOrder,
      },
    );

    return speak(result?.success === true ? 'Oido chef.' : null, false);
  } catch {
    console.warn('Grimorio kitchen-command request failed.');
    return speak(null, false);
  }
};

async function repeatOrder(slots) {
  if (!isConfigured()) {
    return speak(null, true);
  }

  const tableCode = slotValue(slots.tableCode);
  const orderNumber = Number.parseInt(slotValue(slots.orderNumber) || '', 10);
  const stationText = slotValue(slots.stationText);
  const excludeStationText = slotValue(slots.excludeStationText);

  try {
    const result = await postJson(
      `${apiBaseUrl.replace(/\/$/, '')}/alexa/order-repeat`,
      {
        branchId,
        stationNames,
        tableCode,
        orderNumber: Number.isNaN(orderNumber) ? null : orderNumber,
        stationText,
        excludeStationText,
      },
    );

    const message = result?.success === true && typeof result.message === 'string'
      ? result.message.trim() : null;
    return speak(message, false);
  } catch {
    console.warn('Grimorio order-repeat request failed.');
    return speak(null, false);
  }
}

function isConfigured() {
  const configured = Boolean(apiBaseUrl && branchId && integrationKey && stationNames.length);
  if (!configured) console.warn('Grimorio Alexa configuration is incomplete.');
  return configured;
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
    let timeout;
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
        res.on('error', (error) => {
          clearTimeout(timeout);
          reject(error);
        });
        res.on('end', () => {
          clearTimeout(timeout);
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

    // Leave time for a silent response before Alexa's request deadline.
    timeout = setTimeout(() => req.destroy(new Error('Grimorio request timeout')), 4500);
    req.on('error', (error) => {
      clearTimeout(timeout);
      reject(error);
    });
    req.write(payload);
    req.end();
  });
}

function speak(outputSpeech, shouldEndSession) {
  return {
    version: '1.0',
    response: {
      ...(outputSpeech ? { outputSpeech: {
        type: 'PlainText',
        text: outputSpeech,
      } } : {}),
      reprompt: shouldEndSession
        ? undefined
        : {
            outputSpeech: {
              type: 'PlainText',
              text: 'Hasta luego chef.',
            },
          },
      shouldEndSession,
    },
  };
}
