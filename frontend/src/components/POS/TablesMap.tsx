import { useCallback, useEffect, useMemo, useState } from 'react';
import { Badge, Button, Card, Empty, Spin, Tag, Tooltip, message } from 'antd';
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
  const [tables, setTables] = useState<RestaurantTableDto[]>([]);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await tableServiceApi.getTables(branchId);
      setTables(res.data.filter(m => m.isActive).sort(compareTablesByNumber));
    } catch (e) { message.error(formatError(e)); }
    finally { setLoading(false); }
  }, [branchId]);

  useEffect(() => { load(); }, [load, refreshKey]);

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
              return (
                <Tooltip key={table.id} title={`${group.area} · ${table.capacity} personas`}>
                  <Card
                    hoverable
                    size="small"
                    onClick={() => onSelectTable(table)}
                    style={{
                      width: 110,
                      textAlign: 'center',
                      cursor: 'pointer',
                      borderColor: occupied ? '#ff4d4f' : '#52c41a',
                      background: occupied ? '#fff1f0' : '#f6ffed',
                    }}
                    styles={{ body: { padding: '12px 8px' } }}
                  >
                    <div style={{ fontSize: 18, fontWeight: 700 }}>Mesa {table.code}</div>
                    <div style={{ fontSize: 12, color: '#666', marginBottom: 6 }}>{table.capacity} personas</div>
                    <Tag color={occupied ? 'error' : 'success'} style={{ fontSize: 11 }}>
                      {occupied ? 'Ocupada' : 'Libre'}
                    </Tag>
                    {occupied && (
                      <Badge
                        count="●"
                        style={{ background: 'transparent', color: '#ff4d4f', boxShadow: 'none', fontSize: 10 }}
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
