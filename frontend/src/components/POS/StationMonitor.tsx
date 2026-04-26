import { useEffect, useRef, useState } from 'react';
import { Badge, Button, Card, Col, Empty, Row, Select, Space, Spin, Tag, Typography, message } from 'antd';
import { CheckOutlined, ReloadOutlined } from '@ant-design/icons';
import { posApi } from '../../services/api';
import type { WorkStationDto, OrderItemStatus, StationItemDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Title, Text } = Typography;

const TIPO_COLORS: Record<string, string> = {
  Mesa: 'blue',
  Llevar: 'orange',
  Domicilio: 'purple',
};

export default function StationMonitor() {
  const [estaciones, setEstaciones] = useState<WorkStationDto[]>([]);
  const [estacionId, setEstacionId] = useState<string | null>(null);
  const [items, setItems] = useState<StationItemDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadingItem, setLoadingItem] = useState<string | null>(null);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    posApi.getStations().then(r => {
      setEstaciones(r.data.filter(e => e.isActive));
      if (r.data.length > 0) setEstacionId(r.data[0].id);
    }).catch(() => {});
  }, []);

  const loadItems = async (id: string) => {
    setLoading(true);
    try {
      const res = await posApi.getStationItems(id);
      setItems(res.data);
    } catch (e) { message.error(formatError(e)); }
    finally { setLoading(false); }
  };

  useEffect(() => {
    if (!estacionId) return;
    loadItems(estacionId);

    intervalRef.current = setInterval(() => loadItems(estacionId), 15_000);
    return () => { if (intervalRef.current) clearInterval(intervalRef.current); };
  }, [estacionId]);

  const avanzarEstado = async (item: StationItemDto) => {
    const next: OrderItemStatus = item.status === 'Pending' ? 'InPreparation' : 'Ready';
    setLoadingItem(item.orderItemId);
    try {
      await posApi.setItemStatus(item.orderItemId, next);
      if (estacionId) await loadItems(estacionId);
    } catch (e) { message.error(formatError(e)); }
    finally { setLoadingItem(null); }
  };

  const pendientes = items.filter(i => i.status === 'Pending');
  const enPreparacion = items.filter(i => i.status === 'InPreparation');

  const renderCard = (item: StationItemDto) => {
    const elapsed = Math.floor((Date.now() - new Date(item.confirmedAt).getTime()) / 60000);
    const urgent = elapsed >= 10;
    return (
      <Card
        key={item.orderItemId}
        size="small"
        style={{ borderColor: urgent ? '#ff4d4f' : undefined, background: urgent ? '#fff1f0' : undefined }}
        styles={{ body: { padding: 12 } }}
        actions={[
          <Button
            key="avanzar"
            type="primary"
            size="small"
            icon={<CheckOutlined />}
            loading={loadingItem === item.orderItemId}
            onClick={() => avanzarEstado(item)}
            style={{ margin: '0 8px' }}
          >
            {item.status === 'Pending' ? 'Iniciar' : 'Marcar listo'}
          </Button>,
        ]}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <div>
            <Tag color={TIPO_COLORS[item.orderType] ?? 'default'} style={{ fontSize: 11 }}>
              #{item.orderNumber} {item.orderType}
            </Tag>
            {item.tableCode && <Tag style={{ fontSize: 11 }}>Mesa {item.tableCode}</Tag>}
            {item.customerName && <Tag style={{ fontSize: 11 }}>{item.customerName}</Tag>}
          </div>
          <Text type={urgent ? 'danger' : 'secondary'} style={{ fontSize: 11 }}>
            {elapsed}m
          </Text>
        </div>
        <div style={{ fontSize: 16, fontWeight: 700, margin: '8px 0 4px' }}>
          <Badge count={item.quantity} size="small" style={{ background: '#1677ff' }}>
            <span style={{ paddingRight: 8 }}>{item.itemName}</span>
          </Badge>
        </div>
        {item.notes && (
          <Text type="secondary" style={{ fontSize: 12 }}>📝 {item.notes}</Text>
        )}
      </Card>
    );
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Monitor de Estación</Title>
        <Space>
          <Select
            value={estacionId}
            onChange={setEstacionId}
            style={{ width: 200 }}
            options={estaciones.map(e => ({ label: e.name, value: e.id }))}
            placeholder="Seleccionar estación"
          />
          <Button
            icon={<ReloadOutlined />}
            onClick={() => estacionId && loadItems(estacionId)}
          >
            Actualizar
          </Button>
        </Space>
      </div>

      {!estacionId ? (
        <Empty description="Selecciona una estación" />
      ) : loading ? (
        <Spin style={{ display: 'block', margin: '40px auto' }} />
      ) : items.length === 0 ? (
        <Empty description="Sin ítems pendientes en esta estación" image={Empty.PRESENTED_IMAGE_SIMPLE} />
      ) : (
        <Row gutter={16}>
          <Col xs={24} md={12}>
            <div style={{ marginBottom: 8 }}>
              <Tag color="orange" style={{ fontSize: 13 }}>
                Pendientes ({pendientes.length})
              </Tag>
            </div>
            <Space direction="vertical" style={{ width: '100%' }}>
              {pendientes.length === 0
                ? <Text type="secondary">Sin ítems pendientes</Text>
                : pendientes.map(renderCard)}
            </Space>
          </Col>
          <Col xs={24} md={12}>
            <div style={{ marginBottom: 8 }}>
              <Tag color="blue" style={{ fontSize: 13 }}>
                En preparación ({enPreparacion.length})
              </Tag>
            </div>
            <Space direction="vertical" style={{ width: '100%' }}>
              {enPreparacion.length === 0
                ? <Text type="secondary">Sin ítems en preparación</Text>
                : enPreparacion.map(renderCard)}
            </Space>
          </Col>
        </Row>
      )}
    </div>
  );
}
