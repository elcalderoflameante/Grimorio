import { useEffect, useState } from 'react';
import { Alert, Button, Descriptions, Spin, Tag, Typography } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
import { inventoryApi } from '../../services/api';
import type { StockMovementTraceDto } from '../../types/inventoryReconciliation';
import { formatBranchDateTime } from '../../utils/branchTimeZone';
import { formatError } from '../../utils/errorHandler';

const labels = { Sale: 'Cobro', Purchase: 'Compra', Production: 'Producción', Manual: 'Sin documento vinculado', Incomplete: 'Trazabilidad incompleta' };
const idValue = (value?: string) => value ? <Typography.Text copyable style={{ overflowWrap: 'anywhere' }}>{value}</Typography.Text> : '-';

export default function StockMovementTrace({ movementId }: { movementId: string }) {
  const [trace, setTrace] = useState<StockMovementTraceDto>();
  const [error, setError] = useState<string>();
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    let active = true;
    setTrace(undefined);
    setError(undefined);
    inventoryApi.getMovementTrace(movementId)
      .then(response => { if (active) setTrace(response.data); })
      .catch(e => { if (active) setError(formatError(e)); });
    return () => { active = false; };
  }, [movementId, attempt]);
  if (error) return <Alert type="error" title={error} action={<Button icon={<ReloadOutlined />} onClick={() => setAttempt(x => x + 1)}>Reintentar</Button>} />;
  if (!trace) return <Spin />;
  return <Descriptions size="small" column={{ xs: 1, sm: 1, md: 2 }} style={{ overflowWrap: 'anywhere' }} items={[
    { key: 'origin', label: 'Origen', children: <Tag color={trace.origin === 'Incomplete' ? 'warning' : 'blue'}>{labels[trace.origin]}</Tag> },
    { key: 'movement', label: 'Movimiento', children: idValue(trace.movementId) },
    { key: 'date', label: 'Registrado', children: formatBranchDateTime(trace.createdAt) },
    { key: 'reference', label: 'Referencia', children: trace.reference || '-' },
    ...(trace.orderId ? [
      { key: 'order', label: `Orden #${trace.orderNumber ?? '-'}`, children: idValue(trace.orderId) },
      { key: 'orderItem', label: 'Item del pedido', children: idValue(trace.orderItemId) },
    ] : []),
    ...(trace.orderPaymentId ? [
      { key: 'payment', label: 'Cobro', children: idValue(trace.orderPaymentId) },
      { key: 'paymentItem', label: 'Linea del cobro', children: idValue(trace.orderPaymentItemId) },
      { key: 'paidQuantity', label: 'Cantidad cobrada del plato', children: trace.paidItemQuantity },
      { key: 'paidAt', label: 'Fecha del cobro', children: trace.paidAt ? formatBranchDateTime(trace.paidAt) : '-' },
    ] : []),
    ...(trace.stockReservationId ? [{ key: 'reservation', label: 'Reserva', children: idValue(trace.stockReservationId) }] : []),
    ...(trace.purchaseId ? [
      { key: 'purchase', label: `Compra ${trace.documentNumber || ''}`, children: idValue(trace.purchaseId) },
      { key: 'purchaseItem', label: 'Linea de compra', children: idValue(trace.purchaseItemId) },
    ] : []),
    ...(trace.productionOrderId ? [{ key: 'production', label: trace.productionNumber || 'Producción', children: idValue(trace.productionOrderId) }] : []),
    ...(trace.notes ? [{ key: 'notes', label: 'Observación', children: trace.notes }] : []),
  ]} />;
}
