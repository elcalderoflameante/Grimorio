import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';
import ts from 'typescript';

const source = await readFile(new URL('../src/utils/tableRequestDesktop.ts', import.meta.url), 'utf8');
const output = {}, notices = [], errors = [];
let permission = 'default', decision = 'granted', prompts = 0, focuses = 0, opens = 0, fail = false, resolvePrompt;
class Notification {
  static get permission() { return permission; }
  static async requestPermission() { prompts++; permission = decision; return permission; }
  constructor(title, options) { if (fail) throw new TypeError('Unsupported platform'); this.title = title; this.options = options; notices.push(this); }
  close() { this.closed = true; }
}
const window = { isSecureContext: true, Notification, focus() { focuses++; } };
const document = { visibilityState: 'visible', hasFocus: () => true };
vm.runInNewContext(ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText,
  { exports: output, window, document, Error, require: name => name === '../constants/tableService'
    ? { REQUEST_TYPE_LABELS: { 1: 'Servilletas', 99: 'Otra solicitud' } }
    : { requestTableLabel: row => `Mesa ${row.tableCode}` } });
const { TableRequestDesktop, supportsTableRequestDesktop } = output;
const create = () => new TableRequestDesktop(() => opens++, message => errors.push(message));
const request = { id: 'one', tableCode: '5', type: 1, status: 1, customMessage: 'Private customer message' };
const desktop = create();
desktop.announce([request]); assert.equal(notices.length, 0); assert.equal(prompts, 0);
assert.equal(await desktop.enable(), true); assert.equal(prompts, 1); assert.equal(notices.length, 1);
assert.equal(notices[0].options.silent, true, 'No OS chime over the walkie');
desktop.announce([request]); assert.equal(notices.length, 1, 'Focused ERP needs no desktop interruption');
document.hasFocus = () => false;
desktop.announce([request]); assert.equal(notices.length, 2); assert.ok(notices[0].closed);
assert.equal(notices[1].options.body, 'Servilletas'); assert.ok(!JSON.stringify(notices[1].options).includes('Private'));
notices[1].onclick(); assert.equal(focuses, 1); assert.equal(opens, 1); assert.ok(notices[1].closed);
document.hasFocus = () => true; document.visibilityState = 'hidden';
desktop.announce([request, { ...request, id: 'two' }]); assert.match(notices.at(-1).title, /2 nuevas/);
desktop.retain([request]); assert.ok(notices.at(-1).closed, 'Remove stale desktop notices when a request leaves Pending');
desktop.announce([request]); desktop.retain([{ ...request, status: 2 }]); assert.ok(notices.at(-1).closed);
desktop.announce([request]); const lateClick = notices.at(-1).onclick;
desktop.dispose(); lateClick(); assert.equal(opens, 1, 'Old branch/logout callbacks cannot reopen the panel');
const count = notices.length; desktop.announce([request]); await desktop.enable(); assert.equal(notices.length, count);

permission = 'denied'; const denied = create();
await assert.rejects(() => denied.enable(), /bloqueadas/); assert.equal(prompts, 1, 'Never reprompt a denied permission'); denied.dispose();
permission = 'default'; decision = 'default'; const dismissed = create();
await assert.rejects(() => dismissed.enable(), /No se concedio/); dismissed.dispose();
window.isSecureContext = false; assert.equal(supportsTableRequestDesktop(), false);
await assert.rejects(() => create().enable(), /HTTPS/); window.isSecureContext = true;
permission = 'granted'; const revoked = create(); await revoked.enable();
permission = 'denied'; revoked.announce([request]); assert.match(errors.at(-1), /revocado/);
revoked.dispose();
permission = 'granted'; fail = true; const unsupported = create();
assert.equal(await unsupported.enable(), false); assert.match(errors.at(-1), /Unsupported/); unsupported.dispose(); fail = false;
permission = 'default'; Notification.requestPermission = () => new Promise(resolve => { resolvePrompt = resolve; });
const pending = create(), enabling = pending.enable(); pending.dispose();
const beforeLatePermission = notices.length; resolvePrompt('granted');
assert.equal(await enabling, false); assert.equal(notices.length, beforeLatePermission, 'Late permission resolution cannot notify after logout');
console.log('Desktop checks passed: opt-in, focus/visibility, silent notifications, privacy, click, cleanup, denial, unsupported HTTP/platform, revocation and late permission.');
