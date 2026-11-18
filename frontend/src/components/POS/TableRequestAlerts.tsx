import { useEffect, useRef, useState } from 'react';
import { Alert, App, Badge, Button, Drawer, Empty, Space, Switch, Tag, Tooltip, Typography } from 'antd';
import { BellOutlined, CheckOutlined, PlayCircleOutlined, ReloadOutlined, SoundOutlined, UserAddOutlined } from '@ant-design/icons';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import { REQUEST_STATUS_LABELS, REQUEST_STATUS_COLORS, REQUEST_TYPE_LABELS } from '../../constants/tableService';
import { tableServiceApi } from '../../services/api';
import { subscribeTableRequests } from '../../services/tableRequestRealtime';
import type { TableServiceRequestDto, TableServiceRequestStatus } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { formatBranchTime } from '../../utils/branchTimeZone';
import { requestTableLabel, TableRequestAudio } from '../../utils/tableRequestAudio';

export default function TableRequestAlerts() {
  const { token, branchId, hasPermission } = useAuth();
  if (!token || !branchId || !hasPermission(PERMISSIONS.pos.tableRequestsView)) return null;
  return <ActiveAlerts key={`${branchId}:${token}`} branchId={branchId} token={token}
    canUpdate={hasPermission(PERMISSIONS.pos.tableRequestsUpdate)} />;
}

function ActiveAlerts({ branchId, token, canUpdate }: { branchId: string; token: string; canUpdate: boolean }) {
  const { notification, message } = App.useApp();
  const [open, setOpen] = useState(false);
  const openRef = useRef(open);
  openRef.current = open;
  const [requests, setRequests] = useState<TableServiceRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [connected, setConnected] = useState(false);
  const [sound, setSound] = useState(false);
  const [voice, setVoice] = useState(true);
  const [enabling, setEnabling] = useState(false);
  const [busy, setBusy] = useState<string | null>(null);
  const [now, setNow] = useState(Date.now());
  const reload = useRef<() => void>(() => {});
  const audio = useRef<TableRequestAudio | null>(null);

  useEffect(() => {
    let active = true, inFlight = false, pending = false, initialized = false;
    let eventTimer: ReturnType<typeof setTimeout> | undefined;
    const seen = new Set<string>();
    const player = new TableRequestAudio(() => {
      if (active) { setSound(false); setError('El navegador detuvo el audio. Activa el sonido nuevamente.'); }
    });
    audio.current = player;
    const load = async () => {
      if (!active) return;
      if (inFlight) { pending = true; return; }
      inFlight = true;
      try {
        const response = await tableServiceApi.getActiveRequests();
        if (!active || pending) return;
        const rows = response.data.filter(r => r.branchId.toLowerCase() === branchId.toLowerCase()
          && [1, 2, 3].includes(r.status)).sort((a, b) => a.requestedAt.localeCompare(b.requestedAt));
        const fresh = initialized ? rows.filter(r => r.status === 1 && !seen.has(r.id)) : [];
        for (const row of rows) seen.add(row.id);
        initialized = true;
        setRequests(rows); setError(''); setNow(Date.now());
        if (fresh.length) {
          player.announce(fresh);
          if (!openRef.current) notification.info({ key: 'table-requests-new', title: fresh.length === 1 ? requestTableLabel(fresh[0]) : `${fresh.length} nuevas solicitudes de mesas`,
            description: fresh.length === 1 ? fresh[0].customMessage || REQUEST_TYPE_LABELS[fresh[0].type] : undefined,
            duration: 8, onClick: () => setOpen(true), placement: 'topRight' });
        }
      } catch (e) { if (active) setError(formatError(e)); }
      finally {
        inFlight = false;
        if (active) {
          setLoading(false);
          if (pending) { pending = false; void load(); }
        }
      }
    };
    reload.current = () => { void load(); };
    const changed = () => {
      if (eventTimer !== undefined) return;
      eventTimer = setTimeout(() => { eventTimer = undefined; void load(); }, 150);
    };
    const resume = () => { if (document.visibilityState !== 'hidden') { setNow(Date.now()); void load(); } };
    void load();
    const unsubscribe = subscribeTableRequests({ apiUrl: import.meta.env.VITE_API_URL, token, branchId,
      onChange: changed, onSync: () => void load(), onConnection: value => { if (active) setConnected(value); } });
    const backup = setInterval(resume, 120000);
    const clock = setInterval(() => setNow(Date.now()), 30000);
    window.addEventListener('focus', resume); window.addEventListener('online', resume);
    document.addEventListener('visibilitychange', resume);
    return () => {
      active = false; reload.current = () => {}; audio.current = null; player.dispose(); unsubscribe();
      notification.destroy('table-requests-new');
      clearInterval(backup); clearInterval(clock); clearTimeout(eventTimer);
      window.removeEventListener('focus', resume); window.removeEventListener('online', resume);
      document.removeEventListener('visibilitychange', resume);
    };
  }, [branchId, token, notification]);

  const toggleSound = async (enabled: boolean) => {
    if (!enabled) { audio.current?.disable(); setSound(false); return; }
    const player = audio.current; if (!player) return;
    setEnabling(true);
    try { await player.enable(); if (audio.current === player) { setSound(true); setError(''); } }
    catch (e) { if (audio.current === player) { setSound(false); setError(formatError(e)); } }
    finally { if (audio.current === player) setEnabling(false); }
  };
  const act = async (id: string, status?: TableServiceRequestStatus) => {
    setBusy(id);
    try {
      if (status) await tableServiceApi.setRequestStatus(id, { status });
      else await tableServiceApi.takeRequest(id);
    } catch (e) { message.error(formatError(e)); }
    finally { setBusy(null); reload.current(); }
  };

  return <>
    <Tooltip title={`Solicitudes de mesas · sonido ${sound ? 'activado' : 'desactivado'}`}>
      <Badge count={requests.length} overflowCount={99}>
        <Button type="text" aria-label="Solicitudes de mesas" icon={<BellOutlined />} onClick={() => setOpen(true)} />
      </Badge>
    </Tooltip>
    <Drawer title={`Solicitudes de mesas (${requests.length})`} open={open} onClose={() => setOpen(false)} size={520}
      extra={<Tooltip title="Actualizar solicitudes"><Button aria-label="Actualizar solicitudes" icon={<ReloadOutlined />} loading={loading} onClick={() => reload.current()} /></Tooltip>}>
      <Space wrap size={16} style={{ marginBottom: 16 }}>
        <Space><Typography.Text>Sonido</Typography.Text><Switch aria-label="Sonido de solicitudes" checked={sound} loading={enabling} onChange={enabled => void toggleSound(enabled)} /></Space>
        <Space><Typography.Text>Voz</Typography.Text><Switch aria-label="Voz de solicitudes" checked={voice} disabled={!window.speechSynthesis}
          onChange={enabled => { setVoice(enabled); audio.current?.setVoice(enabled); }} /></Space>
        <Tooltip title="Probar parlante"><Button aria-label="Probar parlante" icon={<SoundOutlined />} disabled={!sound} onClick={() => audio.current?.test()} /></Tooltip>
        {!connected && <Tag color="warning">Sin conexion en tiempo real</Tag>}
      </Space>
      {error && <Alert type="warning" showIcon title={error} style={{ marginBottom: 16 }} />}
      {!requests.length && !loading && !error && <Empty description="Sin solicitudes pendientes" />}
      <div aria-live="polite">{requests.map(row => <section key={row.id} aria-label={`Solicitud ${row.id}`}
        style={{ borderBottom: '1px solid #e5e5e5', padding: '16px 0', overflowWrap: 'anywhere' }}>
        <div style={{ display: 'flex', gap: 8, justifyContent: 'space-between', alignItems: 'start' }}>
          <Typography.Text strong>{requestTableLabel(row)}</Typography.Text>
          <Tag color={REQUEST_STATUS_COLORS[row.status]}>{row.status === 1 ? 'Pendiente' : REQUEST_STATUS_LABELS[row.status]}</Tag>
        </div>
        <Typography.Paragraph style={{ margin: '8px 0' }}>{REQUEST_TYPE_LABELS[row.type] || 'Solicitud'}{row.customMessage ? ` · ${row.customMessage}` : ''}</Typography.Paragraph>
        <Typography.Text type="secondary">{formatBranchTime(row.requestedAt)} · {Math.max(0, Math.floor((now - Date.parse(row.requestedAt)) / 60000))} min</Typography.Text>
        {row.takenByName && <div><Typography.Text type="secondary">Responsable: {row.takenByName}</Typography.Text></div>}
        {canUpdate && <Space wrap style={{ marginTop: 12, display: 'flex' }}>
          {row.status === 1 && <Button icon={<UserAddOutlined />} disabled={busy !== null} loading={busy === row.id} onClick={() => void act(row.id)}>Tomar</Button>}
          {row.status === 2 && <Button icon={<PlayCircleOutlined />} disabled={busy !== null} loading={busy === row.id} onClick={() => void act(row.id, 3)}>En proceso</Button>}
          {(row.status === 2 || row.status === 3) && <Button type="primary" icon={<CheckOutlined />} disabled={busy !== null} loading={busy === row.id} onClick={() => void act(row.id, 4)}>Completar</Button>}
        </Space>}
      </section>)}</div>
    </Drawer>
  </>;
}
