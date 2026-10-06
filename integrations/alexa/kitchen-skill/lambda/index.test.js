const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const vm = require('node:vm');

function loadSkill(stations, { result = { success: true, message: 'Oido chef.' }, status = 200,
  networkError = false, responseError = false, invalidJson = false, hang = false } = {}) {
  const calls = [];
  let onTimeout;
  const context = {
    exports: {}, URL, Buffer,
    console: { warn() {} },
    setTimeout(callback, duration) {
      assert.equal(duration, 4500);
      onTimeout = callback;
      return 1;
    },
    clearTimeout() { onTimeout = undefined; },
    process: { env: {
      GRIMORIO_API_BASE_URL: 'https://example.test/api',
      GRIMORIO_BRANCH_ID: 'test-branch',
      GRIMORIO_ALEXA_KEY: 'test-key',
      GRIMORIO_STATION_NAMES: stations,
    } },
    require(name) {
      assert.equal(name, 'https');
      return { request(options, callback) {
        const req = new EventEmitter();
        req.destroy = (error) => req.emit('error', error);
        req.write = (payload) => calls.push({ path: options.path, body: JSON.parse(payload) });
        req.end = () => {
          if (networkError) { req.emit('error', new Error('Offline')); return; }
          if (hang) return;
          const res = new EventEmitter();
          res.statusCode = status;
          res.setEncoding = () => {};
          callback(res);
          if (responseError) { res.emit('error', new Error('Connection reset')); return; }
          res.emit('data', invalidJson ? '<html>Bad gateway</html>' : JSON.stringify(result));
          res.emit('end');
        };
        return req;
      } };
    },
  };
  vm.runInNewContext(fs.readFileSync(path.join(__dirname, 'index.js'), 'utf8'), context);
  return { handler: context.exports.handler, calls, timeout: () => onTimeout() };
}

function intent(name, values) {
  return { request: { type: 'IntentRequest', intent: { name,
    slots: Object.fromEntries(Object.entries(values).map(([key, value]) => [key, { value }])),
  } } };
}

for (const [configuration, expected] of [['parrilla,fritos', ['parrilla', 'fritos']], ['bar', ['bar']]]) {
  test(`whole order command stays scoped to ${configuration}`, async () => {
    const skill = loadSkill(configuration);
    await skill.handler(intent('KitchenCommandIntent', {
      action: 'listo', tableCode: '1', allItems: 'todo el pedido', stationNames: 'otra estacion',
    }));
    assert.equal(skill.calls[0].path, '/api/alexa/kitchen-command');
    assert.deepEqual(skill.calls[0].body.stationNames, expected);
    assert.equal(skill.calls[0].body.allItems, true);
  });
  test(`repeat with a spoken filter stays scoped to ${configuration}`, async () => {
    const skill = loadSkill(configuration);
    await skill.handler(intent('RepeatOrderIntent', { tableCode: '1', stationText: 'bar' }));
    assert.equal(skill.calls[0].path, '/api/alexa/order-repeat');
    assert.deepEqual(skill.calls[0].body.stationNames, expected);
    assert.equal(skill.calls[0].body.stationText, 'bar');
  });
}

test('missing station configuration never sends unrestricted requests', async () => {
  for (const stations of [undefined, '', ' , ']) {
    const skill = loadSkill(stations);
    for (const event of [
      { request: { type: 'LaunchRequest' } },
      intent('KitchenCommandIntent', { action: 'listo', tableCode: '1', allItems: 'todo' }),
      intent('RepeatOrderIntent', { tableCode: '1' }),
    ]) {
      const result = await skill.handler(event);
      assert.equal(result.response.shouldEndSession, true);
    }
    assert.equal(skill.calls.length, 0);
  }
});

test('plural item and exclusion handling remain available', async () => {
  const skill = loadSkill(' parrilla , fritos ');
  await skill.handler(intent('KitchenCommandIntent', { action: 'listas', tableCode: '6', allItems: 'salchipapas' }));
  assert.equal(skill.calls[0].body.itemText, 'salchipapas');
  assert.equal(skill.calls[0].body.allItems, false);
  await skill.handler(intent('RepeatOrderIntent', { tableCode: '6', excludeStationText: 'bar' }));
  assert.deepEqual(skill.calls[1].body.stationNames, ['parrilla', 'fritos']);
  assert.equal(skill.calls[1].body.excludeStationText, 'bar');
});

function assertListeningSilently(result) {
  assert.equal(result.response.shouldEndSession, false);
  assert.equal(result.response.outputSpeech, undefined);
  assert.equal(result.response.reprompt.outputSpeech.type, 'PlainText');
  assert.equal(result.response.reprompt.outputSpeech.text, 'Hasta luego chef.');
}

test('successful state change confirms only Oido chef without reading the backend message', async () => {
  const skill = loadSkill('parrilla,fritos', { result: { success: true, message: 'Pedido mesa uno en preparacion.' } });
  const response = await skill.handler(intent('KitchenCommandIntent', { action: 'preparando', tableCode: '1', allItems: 'todo' }));
  assert.equal(response.response.outputSpeech.text, 'Oido chef.');
  assert.equal(response.response.shouldEndSession, false);
  assert.equal(response.response.reprompt.outputSpeech.text, 'Hasta luego chef.');
});

test('failed or incomplete results stay silent, even with explanatory messages', async () => {
  for (const result of [
    { success: false, message: 'No entendi si quieres marcar preparando o listo.' },
    { success: false, message: 'No escuche la mesa o el numero de pedido.' },
    { success: false, message: 'Hay varios platos parecidos.' },
    { message: 'Oido chef.' }, null,
  ]) {
    const skill = loadSkill('bar', { result });
    for (const name of ['KitchenCommandIntent', 'RepeatOrderIntent']) {
      assertListeningSilently(await skill.handler(intent(name, { tableCode: '1' })));
    }
  }
});

test('launch greets while fallback and unknown intents stay silent', async () => {
  const skill = loadSkill('bar');
  const launch = await skill.handler({ request: { type: 'LaunchRequest' } });
  assert.equal(launch.response.outputSpeech.text, 'Oido chef.');
  assert.equal(launch.response.shouldEndSession, false);
  assert.equal(launch.response.reprompt.outputSpeech.text, 'Hasta luego chef.');
  for (const event of [intent('AMAZON.FallbackIntent', {}), intent('UnknownIntent', {})]) {
    assertListeningSilently(await skill.handler(event));
  }
  assert.equal(skill.calls.length, 0);
});

test('explicit repeat request still reads a successful order', async () => {
  const message = 'Pedido mesa 1, Bar: 1 limonada, pendiente.';
  const skill = loadSkill('bar', { result: { success: true, message } });
  const response = await skill.handler(intent('RepeatOrderIntent', { tableCode: '1' }));
  assert.equal(response.response.outputSpeech.text, message);
});

test('HTTP, network, invalid JSON and timeout failures remain silent', async () => {
  for (const name of ['KitchenCommandIntent', 'RepeatOrderIntent']) {
    for (const options of [{ status: 502 }, { networkError: true }, { responseError: true }, { invalidJson: true }]) {
      const skill = loadSkill('bar', options);
      assertListeningSilently(await skill.handler(intent(name, { tableCode: '1' })));
    }
    const skill = loadSkill('bar', { hang: true });
    const response = skill.handler(intent(name, { tableCode: '1' }));
    skill.timeout();
    assertListeningSilently(await response);
  }
});

test('stop says goodbye while session end returns no invalid speech', async () => {
  const skill = loadSkill('bar');
  for (const name of ['AMAZON.CancelIntent', 'AMAZON.StopIntent']) {
    const result = await skill.handler(intent(name, {}));
    assert.equal(result.response.shouldEndSession, true);
    assert.equal(result.response.outputSpeech.text, 'Hasta luego chef.');
    assert.equal(result.response.reprompt, undefined);
  }
  const ended = await skill.handler({ request: { type: 'SessionEndedRequest' } });
  assert.equal(JSON.stringify(ended.response), '{}');
});
