import { useCallback, useEffect, useMemo, useState } from 'react';
import { App as AntApp, Badge, Button, Card, Empty, Spin, Tag, Tooltip } from 'antd';
import { PlusOutlined, ReloadOutlined } from '@ant-design/icons';
import { tableServiceApi } from '../../services/api';
import type { RestaurantTableDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { compareTablesByNumber } from '../../utils/tableOrdering';

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
  const { message } = AntApp.useApp();

  const [tables, setTables] = useState<RestaurantTableDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [now, setNow] = useState(() => new Date());

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await tableServiceApi.getTables(branchId);
      setTables(res.data.filter(m => m.isActive).sort(compareTablesByNumber));
    } catch (e) { message.error(formatError(e)); }
    finally { setLoading(false); }
  }, [branchId]);

  useEffect(() => { load(); }, [load, refreshKey]);

  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), 60_000);
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

  if (loading) return <Spin style={{ display: 'block', margin: '40px auto' }} />;

  if (tables.length === 0) {
    return (
      <Empty
        description="No hay mesas configuradas"
        image={Empty.PRESENTED_IMAGE_SIMPLE}
      >
        <Button type="primary" icon={<PlusOutlined />}>Ir a configurar mesas</Button>
      </Empty>
    );
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 12 }}>
        <Button icon={<ReloadOutlined />} onClick={load} size="small">Actualizar</Button>
      </div>

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
