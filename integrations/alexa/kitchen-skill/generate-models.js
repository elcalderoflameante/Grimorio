const fs = require('node:fs');
const path = require('node:path');

// Keep intents and samples in one source; only the invocation differs per skill.
const source = JSON.parse(fs.readFileSync(path.join(__dirname, 'interaction-model.es-US.json'), 'utf8'));
for (const [area, invocation] of [['cocina', 'cocina caldero'], ['bar', 'bar caldero']]) {
  const model = structuredClone(source);
  model.interactionModel.languageModel.invocationName = invocation;
  const target = path.join(__dirname, `interaction-model.${area}.es-US.json`);
  const content = `${JSON.stringify(model, null, 2)}\n`;
  if (process.argv.includes('--check')) {
    if (fs.readFileSync(target, 'utf8').replace(/\r\n/g, '\n') !== content) {
      throw new Error(`Regenerate ${path.basename(target)} with node generate-models.js`);
    }
  } else {
    fs.writeFileSync(target, content);
  }
}
