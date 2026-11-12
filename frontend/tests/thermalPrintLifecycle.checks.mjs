import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';
import ts from 'typescript';

const source = await readFile(new URL('../src/utils/thermalReceiptPrinter.ts', import.meta.url), 'utf8');
const { outputText } = ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
});
const api = {};
let html;
const popup = {
  closed: false,
  document: { open() {}, write(value) { html = value; }, close() {} },
};
vm.runInNewContext(outputText, {
  exports: api,
  require(name) {
    assert.equal(name, './branchTimeZone');
    return { formatBranchDateTime: value => value };
  },
  window: { open: () => popup },
});

function checkLifecycle(label) {
  const script = html.match(/<script>([\s\S]*?)<\/script>/)[1];
  for (const outcome of ['printed', 'cancelled']) {
    const events = new EventTarget();
    const calls = [];
    let pendingPrint;
    vm.runInNewContext(script, {
      window: {
        addEventListener: events.addEventListener.bind(events),
        focus: () => calls.push('focus'),
        print: () => calls.push('print'),
        close: () => calls.push('close'),
      },
      setTimeout(callback) { pendingPrint = callback; },
    });
    events.dispatchEvent(new Event('load'));
    assert.deepEqual(calls, [], `${label}: must wait for rendering`);
    pendingPrint();
    assert.deepEqual(calls, ['focus', 'print'], `${label}: must not close before afterprint`);
    // Browsers emit the same event for printing and cancelling the dialog.
    events.dispatchEvent(new Event('afterprint'));
    events.dispatchEvent(new Event('afterprint'));
    assert.deepEqual(calls, ['focus', 'print', 'close'], `${label}: ${outcome} closes only once`);
  }
}

api.printThermalReceipt({
  documentType: 'NotaVenta', orderNumber: 123, paidAt: '2026-11-12',
  issuer: { businessName: 'Restaurant', ruc: '123', address: 'Address' },
  customer: { name: 'Customer' }, items: [], payments: [],
  totals: { subtotal: 10, total: 10, tendered: 10, change: 0 },
});
checkLifecycle('Receipt');

api.printThermalPrebill({
  issuer: { tradeName: 'Restaurant' }, printedAt: '2026-11-12', items: [],
  subtotalWithoutTax: 10, discountWithoutTax: 0, tax: 0, total: 10,
}, popup);
checkLifecycle('Prebill');
console.log('Thermal print lifecycle checks passed: receipts and prebills close only after printing/cancelling.');
