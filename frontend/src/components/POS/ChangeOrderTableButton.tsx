import { useRef, useState } from 'react';
import { App, Button, Modal, Select, Spin, Tooltip, Typography } from 'antd';
import { SwapOutlined } from '@ant-design/icons';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import { posApi, tableServiceApi } from '../../services/api';
import type { OrderDto, RestaurantTableDto } from '../../types';
import { createIdempotencyKey } from '../../utils/idempotencyKey';
import { formatError } from '../../utils/errorHandler';

export default function ChangeOrderTableButton({ order, disabled, onChanged }: {
  order: OrderDto;
  disabled?: boolean;
  onChanged: (order: OrderDto, table: RestaurantTableDto) => void;
}) {
  const { branchId, hasPermission } = useAuth();
  const { message } = App.useApp();
  const [open, setOpen] = useState(false);
  const [tables, setTables] = useState<RestaurantTableDto[]>([]);
  const [targetId, setTargetId] = useState<string>();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const inFlight = useRef(false);
  const requestId = useRef('');
  const loadTables = async () => {
    if (!branchId) return;
    setLoading(true);
    try {
      const { data } = await tableServiceApi.getTables(branchId);
      setTables(data.filter(t => t.id !== order.tableId && t.isActive && t.currentStatus === 'Free'));
    } catch (error) { setTables([]); message.error(formatError(error)); }
    finally { setLoading(false); }
  };
  const show = () => {
    requestId.current = createIdempotencyKey();
    setTargetId(undefined);
    setTables([]);
    setOpen(true);
    void loadTables();
  };
  const submit = async () => {
    if (inFlight.current || !order.tableId || !targetId || disabled) return;
    const target = tables.find(t => t.id === targetId);
    if (!target) return;
    inFlight.current = true;
    setSaving(true);
    try {
      const { data } = await posApi.transferOrderTable(order.id, {
        requestId: requestId.current, fromTableId: order.tableId, toTableId: target.id,
      });
      onChanged(data, { ...target, id: data.tableId ?? target.id, code: data.tableCode ?? target.code });
      setOpen(false);
      message.success(`Pedido trasladado a la mesa ${data.tableCode}`);
    } catch (error) {
      message.error(formatError(error));
      // Keep the selection/request intact on an uncertain response so retry remains idempotent.
    } finally { inFlight.current = false; setSaving(false); }
  };
  if (!hasPermission(PERMISSIONS.pos.ordersUpdate) || order.type !== 'DineIn' || !order.tableId
    || order.paidAt || order.status === 'Cancelled') return null;
  return <>
    <Tooltip title="Cambiar de mesa">
      <Button size="small" type="text" icon={<SwapOutlined />} aria-label="Cambiar de mesa" disabled={disabled} onClick={show} />
    </Tooltip>
    <Modal title={<span style={{ display: 'block', paddingRight: 24 }}>Cambiar de mesa</span>} open={open} width={440}
      okText="Trasladar pedido" cancelText="Cancelar" confirmLoading={saving}
      okButtonProps={{ disabled: loading || !targetId || disabled }} cancelButtonProps={{ disabled: saving }}
      closable={!saving} maskClosable={!saving} onOk={submit} onCancel={() => { if (!inFlight.current) setOpen(false); }}>
      <Typography.Paragraph style={{ marginTop: 16 }}>Orden #{order.number} - Mesa {order.tableCode}</Typography.Paragraph>
      <Spin spinning={loading}>
        <label htmlFor="transfer-table">Mesa de destino</label>
        <Select id="transfer-table" showSearch optionFilterProp="label" value={targetId} onChange={setTargetId}
          style={{ width: '100%', marginTop: 8, marginBottom: 16 }} disabled={saving || loading}
          placeholder="Seleccionar mesa libre" notFoundContent={loading ? null : 'No hay mesas libres'}
          options={tables.map(t => ({ value: t.id, label: `Mesa ${t.code}${t.area ? ` - ${t.area}` : ''} (${t.capacity} personas)` }))} />
      </Spin>
    </Modal>
  </>;
}
