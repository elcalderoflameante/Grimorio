import { useEffect, useState } from 'react';
import { Badge, Button, Card, Empty, Spin, Tag, Tooltip, message } from 'antd';
import { PlusOutlined, ReloadOutlined } from '@ant-design/icons';
import { tableServiceApi } from '../../services/api';
import type { RestaurantTableDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

interface Props {
  branchId: string;
  onSelectTable: (table: RestaurantTableDto) => void;
}

export default function TablesMap({ branchId, onSelectTable }: Props) {
  const [tables, setTables] = useState<RestaurantTableDto[]>([]);
  const [loading, setLoading] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const res = await tableServiceApi.getTables(branchId);
      setTables(res.data.filter(m => m.isActive));
    } catch (e) { message.error(formatError(e)); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [branchId]);

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

  // Group by area
  const areas = [...new Set(tables.map(t => t.area ?? 'General'))];

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 12 }}>
        <Button icon={<ReloadOutlined />} onClick={load} size="small">Actualizar</Button>
      </div>

      {areas.map(area => (
        <div key={area} style={{ marginBottom: 24 }}>
          <div style={{ fontSize: 13, fontWeight: 600, color: '#888', marginBottom: 8, textTransform: 'uppercase', letterSpacing: 1 }}>
            {area}
          </div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
            {tables.filter(t => (t.area ?? 'General') === area).map(table => {
              const occupied = table.currentStatus === 'Occupied';
              return (
                <Tooltip key={table.id} title={`${table.name} · ${table.capacity} personas`}>
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
                    <div style={{ fontSize: 18, fontWeight: 700 }}>{table.code}</div>
                    <div style={{ fontSize: 12, color: '#666', marginBottom: 6 }}>{table.name}</div>
                    <Tag color={occupied ? 'error' : 'success'} style={{ fontSize: 11 }}>
                      {occupied ? 'Occupied' : 'Free'}
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
