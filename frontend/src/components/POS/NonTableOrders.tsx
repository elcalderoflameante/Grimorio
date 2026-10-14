import { useEffect, useRef, useState } from 'react';
import { App as AntApp, Alert, Button, Popconfirm, Space, Table, Tag, Tooltip } from 'antd';
import { EyeOutlined, ReloadOutlined, StopOutlined } from '@ant-design/icons';
import { posApi } from '../../services/api';
import type { ActiveOrderSummaryDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const statuses: Record<string, { label: string; color: string }> = {
  Draft: { label: 'Borrador', color: 'default' },
  Confirmed: { label: 'Confirmada', color: 'processing' },
  InPreparation: { label: 'En preparación', color: 'orange' },
  Ready: { label: 'Lista', color: 'cyan' },
  Delivered: { label: 'Entregada', color: 'green' },
};

interface Props {
  branchId: string;
  refreshKey: number;
  onOpen: (order: ActiveOrderSummaryDto) => void;
}

export default function NonTableOrders({ branchId, refreshKey, onOpen }: Props) {
  const { message } = AntApp.useApp();
  const { hasPermission } = useAuth();
  const [orders, setOrders] = useState<ActiveOrderSummaryDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [cancellingId, setCancellingId] = useState<string | null>(null);
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const cancellationInFlight = useRef(false);
  const canCancel = hasPermission(PERMISSIONS.pos.ordersCancel);

  const cancelDraft = async (order: ActiveOrderSummaryDto) => {
    if (cancellationInFlight.current || !canCancel || order.status !== 'Draft') return;
    cancellationInFlight.current = true;
    setCancellingId(order.id);
    try {
      await posApi.cancelOrder(order.id);
      message.success(`Borrador #${order.number} cancelado`);
      setReloadKey(key => key + 1);
    } catch (e) {
      message.error(formatError(e));
    } finally {
      cancellationInFlight.current = false;
      setCancellingId(null);
    }
  };

  useEffect(() => {
    let active = true;
    let inFlight = false;
    setOrders([]);
    const load = async (silent = false) => {
      if (inFlight) return;
      inFlight = true;
      if (!silent) setLoading(true);
      try {
        const { data } = await posApi.getActiveOrderSummaries();
        if (active) {
          setOrders(data.filter(order => order.type === 'Takeout' || order.type === 'Delivery'));
          setError(null);
        }
      } catch (e) {
        if (active) setError(formatError(e));
      } finally {
        inFlight = false;
        if (active) setLoading(false);
      }
    };
    void load();
    const timer = window.setInterval(() => { void load(true); }, 20_000);
    return () => { active = false; window.clearInterval(timer); };
  }, [branchId, refreshKey, reloadKey]);

  return <div>
    <Space style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 12 }}>
      <Button icon={<ReloadOutlined />} loading={loading} disabled={!!cancellingId} onClick={() => setReloadKey(key => key + 1)}>Actualizar</Button>
    </Space>
    {error && <Alert type="error" showIcon title={error} style={{ marginBottom: 12 }} />}
    <Table<ActiveOrderSummaryDto>
      rowKey="id" dataSource={orders} loading={loading} size="small"
      scroll={{ x: 660 }} pagination={{ pageSize: 15, hideOnSinglePage: true }}
      locale={{ emptyText: 'Sin pedidos pendientes para llevar o domicilio' }}
      columns={[
        { title: 'Pedido', dataIndex: 'number', width: 90, render: number => `#${number}` },
        { title: 'Tipo', dataIndex: 'type', width: 120, render: type => type === 'Takeout' ? 'Para llevar' : 'Domicilio' },
        { title: 'Cliente', dataIndex: 'customerName', render: name => name || 'Sin nombre' },
        { title: 'Estado', dataIndex: 'status', width: 140, render: status => <Tag color={statuses[status]?.color}>{statuses[status]?.label ?? status}</Tag> },
        { title: 'Total del pedido', dataIndex: 'total', width: 125, align: 'right', render: total => `$${total.toFixed(2)}` },
        { title: 'Acciones', width: 135, render: (_, order) => <Space size={4}>
          <Button icon={<EyeOutlined />} disabled={!!cancellingId} onClick={() => onOpen(order)}>Abrir</Button>
          {canCancel && order.status === 'Draft' && <Popconfirm
            title={`¿Cancelar el borrador #${order.number}?`}
            description="El pedido dejará de aparecer en pendientes."
            okText="Cancelar borrador" cancelText="Volver"
            okButtonProps={{ danger: true, loading: cancellingId === order.id }}
            onOpenChange={open => setConfirmingId(open ? order.id : null)}
            onConfirm={() => cancelDraft(order)}
          >
            <Tooltip title="Cancelar borrador" open={confirmingId === order.id ? false : undefined}>
              <Button danger icon={<StopOutlined />} aria-label={`Cancelar borrador #${order.number}`}
                loading={cancellingId === order.id} disabled={!!cancellingId} />
            </Tooltip>
          </Popconfirm>}
        </Space> },
      ]}
    />
  </div>;
}
