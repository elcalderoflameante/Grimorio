import type { TableServiceRequestDto } from '../types';
import { REQUEST_TYPE_LABELS } from '../constants/tableService';

export const requestTableLabel = (request: TableServiceRequestDto) => {
  const code = request.tableCode?.trim() || 'sin numero';
  return `${/^mesa\b/i.test(code) ? code : `Mesa ${code}`}${request.tableArea ? ` · ${request.tableArea}` : ''}`;
};

export const requestAnnouncement = (requests: TableServiceRequestDto[]) => {
  if (requests.length !== 1) return `Hay ${requests.length} nuevas solicitudes de mesas.`;
  const request = requests[0];
  const table = requestTableLabel(request).replace(/·/g, ',').slice(0, 90);
  if (request.type === 99) return `${table} tiene una nueva solicitud.`;
  if (request.type === 8) return `${table} solicita un mesero.`;
  return `${table} solicita ${(REQUEST_TYPE_LABELS[request.type] || 'atencion').toLowerCase()}.`;
};

export class TableRequestAudio {
  private context: AudioContext | null = null;
  private enabled = false;
  private disposed = false;
  private queue: string[] = [];
  private current: SpeechSynthesisUtterance | null = null;
  voiceEnabled = true;

  private onBlocked: () => void;
  constructor(onBlocked: () => void = () => {}) { this.onBlocked = onBlocked; }

  async enable() {
    if (this.disposed || !window.AudioContext) throw new Error('El navegador no admite audio de avisos.');
    this.context ??= new window.AudioContext();
    let timer: ReturnType<typeof setTimeout> | undefined;
    try {
      await Promise.race([this.context.resume(), new Promise<never>((_, reject) => {
        timer = setTimeout(() => reject(new Error('No se pudo activar el parlante. Intenta nuevamente.')), 3000);
      })]);
      if (this.disposed) return;
      if (this.context.state !== 'running') throw new Error('El audio sigue bloqueado. Activa los avisos nuevamente.');
      this.enabled = true;
      this.test();
    } catch (error) { this.disable(); throw error; }
    finally { clearTimeout(timer); }
  }

  test() {
    if (!this.ready()) return;
    this.chime(); this.say('Avisos de mesas activados.');
  }

  announce(requests: TableServiceRequestDto[]) {
    if (!requests.length || !this.ready()) return;
    this.chime(); this.say(requestAnnouncement(requests));
  }

  setVoice(enabled: boolean) {
    this.voiceEnabled = enabled;
    if (!enabled) this.clearSpeech();
  }

  disable() {
    this.enabled = false; this.clearSpeech();
    if (this.context) void this.context.close().catch(() => {});
    this.context = null;
  }

  dispose() { this.disposed = true; this.disable(); }

  private ready() {
    if (!this.enabled || this.disposed) return false;
    if (this.context?.state !== 'running') { this.disable(); this.onBlocked(); return false; }
    return true;
  }

  private chime() {
    const context = this.context!;
    const time = context.currentTime;
    for (const [offset, frequency] of [[0, 660], [0.16, 880]]) {
      const oscillator = context.createOscillator();
      const gain = context.createGain();
      oscillator.frequency.value = frequency;
      gain.gain.setValueAtTime(0, time + offset);
      gain.gain.linearRampToValueAtTime(0.12, time + offset + 0.015);
      gain.gain.exponentialRampToValueAtTime(0.001, time + offset + 0.18);
      oscillator.connect(gain); gain.connect(context.destination);
      oscillator.onended = () => { oscillator.disconnect(); gain.disconnect(); };
      oscillator.start(time + offset); oscillator.stop(time + offset + 0.2);
    }
  }

  private say(text: string) {
    if (!this.voiceEnabled || !window.speechSynthesis || !window.SpeechSynthesisUtterance) return;
    if (this.queue.length >= 5) this.queue = ['Hay varias solicitudes nuevas de mesas.'];
    else this.queue.push(text);
    this.pump();
  }

  private pump() {
    if (this.current || !this.enabled || this.disposed || !this.voiceEnabled) return;
    const text = this.queue.shift(); if (!text) return;
    const utterance = new window.SpeechSynthesisUtterance(text);
    const voices = window.speechSynthesis.getVoices();
    const languages = ['es-ec', 'es-419', 'es-mx', 'es-co', 'es-ar', 'es-us'];
    utterance.voice = languages.map(lang => voices.find(v => v.lang.toLowerCase() === lang)).find(Boolean)
      ?? voices.find(v => v.lang.toLowerCase().startsWith('es')) ?? null;
    utterance.lang = utterance.voice?.lang ?? 'es-EC';
    utterance.rate = 1; utterance.volume = 1;
    const done = () => { if (this.current === utterance) { this.current = null; this.pump(); } };
    utterance.onend = done; utterance.onerror = done;
    this.current = utterance;
    try { window.speechSynthesis.speak(utterance); } catch { done(); }
  }

  private clearSpeech() {
    this.queue = [];
    if (this.current) { this.current = null; window.speechSynthesis?.cancel(); }
  }
}
