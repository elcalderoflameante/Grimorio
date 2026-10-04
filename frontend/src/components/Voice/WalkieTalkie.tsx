import { useEffect, useRef, useState } from 'react';
import { Button, Popover, Space, Typography } from 'antd';
import { AudioOutlined } from '@ant-design/icons';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { Room, RoomEvent, Track } from 'livekit-client';
import { useAuth } from '../../context/useAuth';

type Channel = { revision: number; participants: { identity: string; name: string }[]; speaker: { identity: string; name: string } | null; leaseId: string | null };
type Session = { url: string; token: string; identity: string; state: Channel };
type View = { status: string; connected: boolean; speaking: boolean; count: number; speaker: string | null; enabled: boolean };
const initial: View = { status: 'Walkie-talkie apagado', connected: false, speaking: false, count: 0, speaker: null, enabled: false };

class VoiceConnection {
  private hub?: HubConnection;
  private room?: Room;
  private lease?: string;
  private identity = '';
  private revision = -1;
  private held = false;
  private acquiring = false;
  private starting = false;
  private stopping = false;
  private cleanup: Promise<void> = Promise.resolve();
  private generation = 0;
  private retry?: ReturnType<typeof setTimeout>;
  private heartbeat?: ReturnType<typeof setInterval>;
  private maximum?: ReturnType<typeof setTimeout>;
  private renewing = false;
  private audio = new Set<HTMLMediaElement>();
  view = { ...initial };
  private token: string;
  private changed: (view: View) => void;
  constructor(token: string, changed: (view: View) => void) { this.token = token; this.changed = changed; }
  private update(patch: Partial<View>) { this.view = { ...this.view, ...patch }; this.changed(this.view); }
  async start() {
    if (this.view.enabled || this.starting || this.stopping) return;
    this.starting = true;
    try { this.update({ enabled: true }); await this.connect(); }
    finally { this.starting = false; }
  }
  private async connect(recovering = false) {
    const generation = ++this.generation;
    this.update({ status: 'Conectando voz…', connected: false });
    const apiUrl = (import.meta.env.VITE_API_URL as string | undefined) ?? '/api';
    const hub = new HubConnectionBuilder().withUrl(`${apiUrl.replace(/\/api\/?$/, '')}/hubs/voice`, { accessTokenFactory: () => this.token }).build();
    this.hub = hub; this.revision = -1;
    hub.on('voice:state', (state: Channel) => {
      if (generation !== this.generation || state.revision < this.revision) return;
      this.revision = state.revision;
      this.update({ count: state.participants.length, speaker: state.speaker?.name ?? null });
      if (this.lease && (state.leaseId !== this.lease || state.speaker?.identity !== this.identity)) void this.stopTalking();
    });
    hub.onclose(() => { if (generation === this.generation) void this.recover(); });
    try {
      await hub.start();
      const session = await hub.invoke<Session>('Join');
      if (generation !== this.generation) { await hub.stop(); return; }
      this.identity = session.identity;
      const room = new Room({ audioCaptureDefaults: { echoCancellation: true, noiseSuppression: true, autoGainControl: true } });
      this.room = room;
      room.on(RoomEvent.TrackSubscribed, track => {
        if (track.kind !== Track.Kind.Audio) return;
        const element = track.attach(); this.audio.add(element); document.body.appendChild(element);
        void element.play().catch(() => this.update({ status: 'Pulsa Activar audio para escuchar' }));
      });
      room.on(RoomEvent.TrackUnsubscribed, track => track.detach().forEach(element => { this.audio.delete(element); element.remove(); }));
      room.on(RoomEvent.Disconnected, () => { if (generation === this.generation) void this.recover(); });
      room.on(RoomEvent.Reconnecting, () => { if (generation === this.generation) void this.recover(); });
      await room.connect(session.url, session.token);
      if (generation !== this.generation) { await room.disconnect(); return; }
      this.update({ connected: true, status: 'Escuchando · canal general' }); await this.enableAudio();
    } catch (error) {
      if (generation !== this.generation) return;
      if (recovering && this.view.enabled) { await this.recover(); return; }
      await this.stop(); this.update({ status: error instanceof Error ? error.message : 'No se pudo conectar la voz' });
    }
  }
  async enableAudio() {
    try { await this.room?.startAudio(); this.update({ status: this.room?.canPlaybackAudio ? 'Escuchando · canal general' : 'Pulsa Activar audio para escuchar' }); }
    catch { this.update({ status: 'No se pudo activar el audio' }); }
  }
  async talk() {
    if (!this.view.enabled || !this.view.connected || this.acquiring || this.lease) return;
    this.held = true; this.acquiring = true;
    const hub = this.hub!, room = this.room!, generation = this.generation;
    try {
      const lease = await hub.invoke<string | null>('Acquire');
      if (!lease) { this.update({ status: 'Canal ocupado. Espera tu turno.' }); return; }
      if (!this.held || generation !== this.generation) { await hub.invoke('Release', lease); return; }
      this.lease = lease;
      for (let i = 0; i < 20 && !room.localParticipant.permissions?.canPublish; i++) await new Promise(resolve => setTimeout(resolve, 100));
      if (!this.held || this.lease !== lease || generation !== this.generation) return;
      if (!room.localParticipant.permissions?.canPublish) throw new Error('Micrófono no habilitado');
      await room.localParticipant.setMicrophoneEnabled(true);
      if (!this.held || this.lease !== lease || generation !== this.generation) { await room.localParticipant.setMicrophoneEnabled(false); return; }
      this.update({ speaking: true, status: 'Hablando · suelta para terminar' });
      this.heartbeat = setInterval(() => void this.renew(), 2500);
      this.maximum = setTimeout(() => void this.stopTalking(), 28000);
    } catch { await this.stopTalking(); this.update({ status: 'No se pudo transmitir. Revisa el micrófono y la conexión.' }); }
    finally { this.acquiring = false; }
  }
  private async renew() {
    if (this.renewing || !this.lease) return;
    this.renewing = true;
    try { if (!await this.hub?.invoke<boolean>('Renew', this.lease)) await this.stopTalking(); }
    catch { await this.stopTalking(); }
    finally { this.renewing = false; }
  }
  async stopTalking() {
    this.held = false; clearInterval(this.heartbeat); clearTimeout(this.maximum);
    const lease = this.lease; this.lease = undefined; this.update({ speaking: false });
    try { await this.room?.localParticipant.setMicrophoneEnabled(false); } catch { await this.room?.disconnect().catch(() => {}); }
    if (lease) { try { await this.hub?.invoke('Release', lease); } catch { /* Server lease expires. */ } }
    if (this.view.connected) this.update({ status: 'Escuchando · canal general' });
  }
  private teardown() {
    ++this.generation; this.update({ connected: false });
    this.cleanup = this.cleanup.then(() => this.cleanupSession());
    return this.cleanup;
  }
  private async cleanupSession() {
    await this.stopTalking();
    const room = this.room, hub = this.hub; this.room = undefined; this.hub = undefined;
    await room?.disconnect().catch(() => {}); await hub?.stop().catch(() => {});
    this.audio.forEach(element => element.remove()); this.audio.clear();
    this.update({ connected: false, count: 0, speaker: null });
  }
  private async recover() {
    if (!this.view.enabled) return;
    await this.teardown();
    if (!this.view.enabled) return;
    this.update({ status: 'Reconectando voz…' }); clearTimeout(this.retry);
    this.retry = setTimeout(() => { if (this.view.enabled) void this.connect(true); }, 5000);
  }
  async stop() {
    if (this.stopping) return;
    this.stopping = true;
    try { this.update({ enabled: false }); clearTimeout(this.retry); await this.teardown(); this.update({ ...initial }); }
    finally { this.stopping = false; }
  }
}

export default function WalkieTalkie() {
  const { token, branchId, hasPermission } = useAuth();
  const [view, setView] = useState<View>(initial);
  const connection = useRef<VoiceConnection | null>(null);
  const allowed = hasPermission('POS.Orders.View') || hasPermission('POS.Kitchen.View');
  useEffect(() => {
    if (!token || !branchId || !allowed) return;
    let disposed = false;
    const voice = new VoiceConnection(token, value => { if (!disposed) setView(value); }); connection.current = voice;
    const release = () => void voice.stopTalking();
    const visibility = () => { if (document.hidden) release(); };
    window.addEventListener('blur', release); document.addEventListener('visibilitychange', visibility);
    return () => { disposed = true; connection.current = null; window.removeEventListener('blur', release); document.removeEventListener('visibilitychange', visibility); void voice.stop(); };
  }, [token, branchId, allowed]);
  if (!allowed) return null;
  return <Popover title="Walkie-talkie · sucursal" trigger="click" onOpenChange={open => { if (!open) void connection.current?.stopTalking(); }} content={
    <Space orientation="vertical" style={{ maxWidth: 300 }}>
      <Typography.Text role="status">{view.status}</Typography.Text>
      {view.connected && <Typography.Text>{view.count} conectados{view.speaker ? ` · Habla ${view.speaker}` : ''}</Typography.Text>}
      <Button onClick={() => view.enabled ? void connection.current?.stop() : void connection.current?.start()}>{view.enabled ? 'Salir del canal' : 'Activar walkie-talkie'}</Button>
      {view.connected && <>
        <Button onClick={() => void connection.current?.enableAudio()}>Activar audio</Button>
        <button type="button" style={{ padding: 16, touchAction: 'none', userSelect: 'none', width: '100%', background: view.speaking ? '#b91c1c' : '#164e63', color: 'white', border: 0, borderRadius: 8 }}
          onPointerDown={event => { event.currentTarget.setPointerCapture(event.pointerId); void connection.current?.talk(); }}
          onPointerUp={() => void connection.current?.stopTalking()} onPointerCancel={() => void connection.current?.stopTalking()}
          onLostPointerCapture={() => void connection.current?.stopTalking()}
          onKeyDown={event => { if ((event.key === ' ' || event.key === 'Enter') && !event.repeat) { event.preventDefault(); void connection.current?.talk(); } }}
          onKeyUp={event => { if (event.key === ' ' || event.key === 'Enter') void connection.current?.stopTalking(); }} onBlur={() => void connection.current?.stopTalking()}>
          {view.speaking ? 'Hablando…' : 'Mantener para hablar'}
        </button>
      </>}
    </Space>
  }><Button icon={<AudioOutlined />} aria-label="Walkie-talkie" type={view.connected ? 'primary' : 'default'} /></Popover>;
}
