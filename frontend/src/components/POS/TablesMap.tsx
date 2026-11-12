import { useEffect, useMemo, useState } from 'react';
import { Alert, Badge, Button, Card, Empty, Spin, Tag, Tooltip } from 'antd';
import { PlusOutlined, ReloadOutlined, UserOutlined } from '@ant-design/icons';
import { tableServiceApi } from '../../services/api';
import type { RestaurantTableDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { compareTablesByNumber } from '../../utils/tableOrdering';
import { useAuth } from '../../context/useAuth';
import { subscribeTableMap } from '../../services/tableMapRealtime';

interface Props {
  branchId: string;
  onSelectTable: (table: RestaurantTableDto) => void;
  refreshKey?: number;
}

interface AreaGroup {
  area: string;
  tables: RestaurantTableDto[];
}

const getAreaLabel = (area?: string | null) => area?.trim() || 'General';

export default function TablesMap({ branchId, onSelectTable, refreshKey }: Props) {
  const { token } = useAuth();
  const [snapshot, setSnapshot] = useState<{ branchId: string; tables: RestaurantTableDto[] }>();
  const tables = useMemo(() => snapshot?.branchId === branchId ? snapshot.tables : [], [snapshot, branchId]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    let active = true;
    let inFlight = false;
    let refreshPending = false;
    let eventTimer: ReturnType<typeof setTimeout> | undefined;
    setError(null);
    const load = async (silent = false) => {
      if (inFlight) { refreshPending = true; return; }
      inFlight = true;
      if (!silent) setLoading(true);
      try {
        const { data } = await tableServiceApi.getTables(branchId);
        if (active) {
          setSnapshot({ branchId, tables: data.filter(m => m.isActive).sort(compareTablesByNumber) });
          setNow(new Date());
          setError(null);
        }
      } catch (e) {
        if (active) setError(formatError(e));
      } finally {
        inFlight = false;
        if (active) setLoading(false);
        if (active && refreshPending) {
          refreshPending = false;
          if (document.visibilityState === 'visible') void load(true);
        }
      }
    };
    const resume = () => {
      setNow(new Date());
      if (document.visibilityState === 'visible') void load(true);
    };
    const changed = () => {
      if (!active || document.visibilityState !== 'visible' || eventTimer !== undefined) return;
      eventTimer = setTimeout(() => { eventTimer = undefined; if (active) void load(true); }, 200);
    };
    void load();
    const unsubscribe = token ? subscribeTableMap({
      apiUrl: import.meta.env.VITE_API_URL, token, branchId, onChange: changed,
    }) : undefined;
    const timer = window.setInterval(() => {
      if (document.visibilityState === 'visible') void load(true);
    }, 120_000);
    window.addEventListener('focus', resume);
    window.addEventListener('online', resume);
    document.addEventListener('visibilitychange', resume);
    return () => {
      active = false;
      window.clearInterval(timer);
      clearTimeout(eventTimer);
      unsubscribe?.();
      window.removeEventListener('focus', resume);
      window.removeEventListener('online', resume);
      document.removeEventListener('visibilitychange', resume);
    };
  }, [branchId, token, refreshKey, reloadKey]);

  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), 15_000);
    return () => window.clearInterval(timer);
  }, []);

  const areaGroups = useMemo<AreaGroup[]>(() => {
    const groups = new Map<string, AreaGroup>();

    tables.forEach(table => {
      const area = getAreaLabel(table.area);
      const group = groups.get(area);

      if (group) {
        group.tables.push(table);
        return;
      }

      groups.set(area, { area, tables: [table] });
    });

    return Array.from(groups.values())
      .map(group => ({
        ...group,
        tables: [...group.tables].sort(compareTablesByNumber),
      }))
      .sort((a, b) => compareTablesByNumber(a.tables[0], b.tables[0]));
  }, [tables]);

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 12 }}>
        <Button icon={<ReloadOutlined />} loading={loading} onClick={() => setReloadKey(key => key + 1)} size="small">Actualizar</Button>
      </div>
      {error && <Alert type="warning" showIcon title="No se pudo actualizar el mapa de mesas"
        description={error} style={{ marginBottom: 12 }} />}
      {loading && tables.length === 0 ? <Spin style={{ display: 'block', margin: '40px auto' }} /> :
        tables.length === 0 && !error ? <Empty description="No hay mesas configuradas" image={Empty.PRESENTED_IMAGE_SIMPLE}>
          <Button type="primary" icon={<PlusOutlined />}>Ir a configurar mesas</Button>
        </Empty> : null}

      {areaGroups.map(group => (
        <div key={group.area} style={{ marginBottom: 24 }}>
          <div style={{ fontSize: 13, fontWeight: 700, color: '#666', marginBottom: 8, textTransform: 'uppercase', letterSpacing: 0 }}>
            Área: {group.area}
          </div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
            {group.tables.map(table => {
              const occupied = table.currentStatus === 'Occupied';
              const draft = table.currentStatus === 'Draft';
              const borderColor = occupied ? '#ff4d4f' : draft ? '#faad14' : '#52c41a';
              const background = occupied ? '#fff1f0' : draft ? '#fffbe6' : '#f6ffed';
              const tagColor = occupied ? 'error' : draft ? 'warning' : 'success';
              const statusLabel = occupied ? 'Ocupada' : draft ? 'Borrador' : 'Libre';
              const orderStartedAt = table.currentOrderStartedAt;
              const showOrderInfo = (occupied || draft) && !!orderStartedAt;
              const elapsed = showOrderInfo ? formatElapsed(orderStartedAt, now) : null;
              const pending = formatCurrency(table.pendingPaymentTotal ?? 0);
              return (
                <Tooltip key={table.id} title={`${group.area} · ${table.capacity} personas`}>
                  <Card
                    hoverable
                    size="small"
                    onClick={() => onSelectTable(table)}
                    style={{
                      width: 150,
                      textAlign: 'center',
                      cursor: 'pointer',
                      borderColor,
                      background,
                    }}
                    styles={{ body: { padding: '12px 8px' } }}
                  >
                    <div style={{ fontSize: 18, fontWeight: 700 }}>Mesa {table.code}</div>
                    {(occupied || draft) && table.currentOrderWaiterName && (
                      <div style={{ fontSize: 12, color: '#595959', marginTop: 4, overflowWrap: 'anywhere' }}>
                        <UserOutlined aria-hidden style={{ marginRight: 4 }} />
                        {table.currentOrderWaiterName}
                      </div>
                    )}
                    {!occupied && !draft && (
                      <div style={{ fontSize: 12, color: '#666', marginBottom: 6 }}>{table.capacity} personas</div>
                    )}
                    {!occupied && (
                      <Tag color={tagColor} style={{ fontSize: 11 }}>
                        {statusLabel}
                      </Tag>
                    )}
                    {showOrderInfo && (
                      <div style={{ marginTop: 8, display: 'grid', gap: 4 }}>
                        <div style={{ fontSize: 12, color: '#595959', lineHeight: 1.2 }}>
                          Tiempo: <strong>{elapsed}</strong>
                        </div>
                        <div style={{ fontSize: 12, color: '#262626', lineHeight: 1.2 }}>
                          Pendiente: <strong>{pending}</strong>
                        </div>
                      </div>
                    )}
                    {(occupied || draft) && (
                      <Badge
                        count="●"
                        style={{ background: 'transparent', color: borderColor, boxShadow: 'none', fontSize: 10 }}
                      />
                    )}
                  </Card>
                </Tooltip>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}

function formatElapsed(startedAt: string, now: Date) {
  const start = new Date(startedAt);
  if (Number.isNaN(start.getTime())) return '--';

  const totalMinutes = Math.max(0, Math.floor((now.getTime() - start.getTime()) / 60_000));
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;

  if (hours <= 0) return `${minutes} min`;
  return `${hours}h ${minutes.toString().padStart(2, '0')}m`;
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('es-EC', {
    style: 'currency',
    currency: 'USD',
  }).format(value);
}
