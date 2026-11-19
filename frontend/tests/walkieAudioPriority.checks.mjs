import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';
import ts from 'typescript';
const compile = source => ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText;
const priority = {};
vm.runInNewContext(compile(await readFile(new URL('../src/utils/walkieAudioPriority.ts', import.meta.url), 'utf8')), { exports: priority });
let hub, room, joinSpeaker = null, resolveAcquire;
class Hub {
  events = {};
  on(name, callback) { this.events[name] = callback; }
  onclose() {}
  async start() {}
  async stop() {}
  async invoke(method) {
    if (method === 'Join') return { identity: 'cashier', url: 'fixture', token: 'fixture', state: { revision: 1, participants: [], speaker: joinSpeaker, leaseId: null } };
    if (method === 'Acquire') return new Promise(resolve => { resolveAcquire = resolve; });
    return true;
  }
}
class Room {
  canPlaybackAudio = true;
  localParticipant = { permissions: { canPublish: true }, async setMicrophoneEnabled(enabled) { room.mic = enabled; } };
  constructor() { room = this; }
  on() {}
  async connect() {}
  async disconnect() {}
  async startAudio() {}
}
const source = await readFile(new URL('../src/components/Voice/WalkieTalkie.tsx', import.meta.url), 'utf8');
const output = {};
const script = source.slice(0, source.indexOf('export default function WalkieTalkie')).replace('import.meta.env.VITE_API_URL', "'/api'") + '\nexport { VoiceConnection };';
vm.runInNewContext(compile(script), { exports: output, setTimeout, clearTimeout, setInterval, clearInterval,
  require: name => {
    if (name.endsWith('walkieAudioPriority')) return priority;
    if (name === '@microsoft/signalr') return { HubConnectionBuilder: class { withUrl() { return this; } build() { hub = new Hub(); return hub; } } };
    if (name === 'livekit-client') return { Room, RoomEvent: {}, Track: { Kind: { Audio: 'audio' } } };
    throw new Error(`Unexpected import ${name}`);
  } });
const walkie = new output.VoiceConnection('fixture', () => {});
try {
  await walkie.start(); assert.equal(priority.isWalkieAudioBusy(), false, 'An idle connected walkie does not mute notices');
  hub.events['voice:state']({ revision: 2, participants: [], speaker: { identity: 'waiter', name: 'Waiter' }, leaseId: 'remote' });
  assert.equal(priority.isWalkieAudioBusy(), true, 'Incoming speech has priority');
  hub.events['voice:state']({ revision: 1, participants: [], speaker: null, leaseId: null });
  assert.equal(priority.isWalkieAudioBusy(), true, 'Outdated state cannot release priority');
  hub.events['voice:state']({ revision: 3, participants: [], speaker: null, leaseId: null });
  assert.equal(priority.isWalkieAudioBusy(), false);
  const talking = walkie.talk();
  assert.equal(priority.isWalkieAudioBusy(), true, 'Local push-to-talk gets priority before acquiring the microphone');
  resolveAcquire('local'); await talking; assert.equal(room.mic, true);
  await walkie.stopTalking(); assert.equal(room.mic, false); assert.equal(priority.isWalkieAudioBusy(), false);
  hub.events['voice:state']({ revision: 4, participants: [], speaker: { identity: 'waiter' }, leaseId: 'remote' });
  await walkie.stop(); assert.equal(priority.isWalkieAudioBusy(), false, 'Disconnect releases priority');
  joinSpeaker = { identity: 'already-speaking', name: 'Waiter' };
  await walkie.start(); assert.equal(priority.isWalkieAudioBusy(), true, 'Joining an occupied channel already blocks notices');
} finally { await walkie.stop(); }
assert.equal(priority.isWalkieAudioBusy(), false);
console.log('Walkie priority checks passed: idle, incoming, stale state, outgoing, microphone release, occupied join and disconnect.');
