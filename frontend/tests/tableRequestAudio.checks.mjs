import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';
import ts from 'typescript';

const source = await readFile(new URL('../src/utils/tableRequestAudio.ts', import.meta.url), 'utf8');
const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } });
const output = {};
const priority = {};
const prioritySource = await readFile(new URL('../src/utils/walkieAudioPriority.ts', import.meta.url), 'utf8');
vm.runInNewContext(ts.transpileModule(prioritySource, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText, { exports: priority });
let tones = 0, closes = 0, cancels = 0, blocked = 0;
const spoken = [], contexts = [];
class Audio {
  state = 'suspended'; currentTime = 0; destination = {};
  constructor() { contexts.push(this); }
  async resume() { this.state = 'running'; }
  async close() { closes++; this.state = 'closed'; }
  createOscillator() { return { frequency: {}, connect() {}, disconnect() {}, start() { tones++; }, stop() {} }; }
  createGain() { return { gain: { setValueAtTime() {}, linearRampToValueAtTime() {}, exponentialRampToValueAtTime() {} }, connect() {}, disconnect() {} }; }
}
const voices = [{ lang: 'es-ES', name: 'Spanish' }, { lang: 'es-MX', name: 'Latin American' }];
const window = { AudioContext: Audio, SpeechSynthesisUtterance: class { constructor(text) { this.text = text; } },
  speechSynthesis: { getVoices: () => voices, speak(u) { spoken.push(u); u.onend?.(); }, cancel() { cancels++; } } };
vm.runInNewContext(outputText, { exports: output, window, setTimeout, clearTimeout,
  require: name => {
    if (name === './walkieAudioPriority') return priority;
    assert.equal(name, '../constants/tableService'); return { REQUEST_TYPE_LABELS: { 1: 'Servilletas', 8: 'Llamar mesero' } };
  } });
const { TableRequestAudio, requestAnnouncement } = output;
const row = { id: 'request', tableCode: '5', tableArea: 'Principal', type: 1, customMessage: '<script>untrusted</script>' };
const audio = new TableRequestAudio(() => blocked++);
audio.announce([row]); assert.equal(tones, 0, 'No automatic playback before activation');
await audio.enable(); assert.equal(tones, 2); assert.equal(spoken.length, 1);
audio.announce([row]); assert.equal(tones, 4); assert.equal(spoken.at(-1).voice.lang, 'es-MX');
assert.match(spoken.at(-1).text, /Mesa 5.*servilletas/);
assert.equal(requestAnnouncement([{ ...row, type: 99 }]).includes('untrusted'), false, 'Do not read arbitrary customer messages aloud');
assert.match(requestAnnouncement([{ ...row, type: 8 }]), /un mesero/);
assert.match(requestAnnouncement([row, row, row]), /3 nuevas solicitudes/);
audio.setVoice(false); const count = spoken.length; audio.announce([row]);
assert.equal(spoken.length, count); assert.equal(tones, 6, 'Timbre works with voice disabled');
audio.setVoice(true); contexts.at(-1).state = 'suspended'; audio.announce([row]);
assert.equal(blocked, 1, 'Suspended audio asks for explicit reactivation');
await audio.enable(); assert.equal(contexts.length, 2);
audio.disable(); const stoppedAt = tones; audio.announce([row]); assert.equal(tones, stoppedAt);
await audio.enable();
window.speechSynthesis.speak = u => spoken.push(u);
audio.announce([row]);
for (let i = 0; i < 20; i++) audio.announce([row]);
const beforeDrain = spoken.length;
let steps = 0;
while (steps++ < 10) { const u = spoken.at(-1); const n = spoken.length; u.onend?.(); if (n === spoken.length) break; }
assert.ok(spoken.length - beforeDrain <= 5, 'Speech backlog is bounded and summarized');
audio.announce([row]);
audio.dispose(); assert.ok(cancels > 0); assert.ok(closes > 0);
const finalTones = tones; audio.announce([row]); assert.equal(tones, finalTones);
await assert.rejects(() => audio.enable());

const firstWalkie = {}, secondWalkie = {};
priority.setWalkieAudioBusy(firstWalkie, true);
const deferred = new TableRequestAudio();
const beforeBusy = tones, spokenBeforeBusy = spoken.length;
await deferred.enable(); deferred.announce([row]);
assert.equal(tones, beforeBusy, 'Even the speaker test waits for the walkie');
assert.equal(spoken.length, spokenBeforeBusy);
priority.setWalkieAudioBusy(secondWalkie, true);
priority.setWalkieAudioBusy(firstWalkie, false);
await new Promise(resolve => setTimeout(resolve, 350));
assert.equal(tones, beforeBusy, 'Do not resume while another walkie still holds priority');
priority.setWalkieAudioBusy(secondWalkie, false);
await new Promise(resolve => setTimeout(resolve, 350));
assert.equal(tones, beforeBusy + 2, 'Waiting notifications share a single catch-up chime');
assert.equal(spoken.length, spokenBeforeBusy + 1);
const interrupted = spoken.at(-1), cancelledBefore = cancels;
priority.setWalkieAudioBusy(firstWalkie, true);
assert.equal(cancels, cancelledBefore + 1, 'An incoming walkie immediately cancels our current speech');
interrupted.onend?.();
const beforeResume = spoken.length;
priority.setWalkieAudioBusy(firstWalkie, false);
assert.equal(spoken.length, beforeResume, 'Allow the end of the walkie audio to finish');
await new Promise(resolve => setTimeout(resolve, 350));
assert.equal(spoken.at(-1).text, interrupted.text, 'Interrupted notice restarts instead of being lost');
priority.setWalkieAudioBusy(firstWalkie, true);
deferred.setVoice(false); deferred.announce([row]);
const beforeMuted = tones, mutedSpeech = spoken.length;
priority.setWalkieAudioBusy(firstWalkie, false);
await new Promise(resolve => setTimeout(resolve, 350));
assert.equal(tones, beforeMuted + 2); assert.equal(spoken.length, mutedSpeech, 'Voice-off still queues the chime behind the walkie');
priority.setWalkieAudioBusy(firstWalkie, true); deferred.announce([row]); deferred.disable();
const beforeDisabled = tones;
priority.setWalkieAudioBusy(firstWalkie, false);
await new Promise(resolve => setTimeout(resolve, 350));
assert.equal(tones, beforeDisabled, 'Muting discards waiting notices');
deferred.dispose();
console.log('Table request audio checks passed: activation, voice, mute, bounded queue, walkie interruption, deferred playback, multiple priority owners and cleanup.');
