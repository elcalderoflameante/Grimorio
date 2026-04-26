import { useEffect, useState } from 'react';
import {
  Modal, Button, Space, Typography, Divider, InputNumber, Select, Tag,
  Alert, Spin, List, Tooltip, message,
} from 'antd';
import {
  PlusOutlined, DeleteOutlined, DollarOutlined, CreditCardOutlined,
  WifiOutlined, QrcodeOutlined, CheckCircleOutlined,
} from '@ant-design/icons';
import type { OrderDto, OrderPaymentDto, AddOrderPaymentDto, AddPaymentLineDto, CustomerDto } from '../../types';
import { cashApi } from '../../services/api';
import CustomerSelector from './CustomerSelector';

const { Text, Title } = Typography;

const METHOD_OPTIONS = [
  { value: 'Cash',     label: 'Efectivo',      icon: <DollarOutlined /> },
  { value: 'Card',     label: 'Tarjeta',        icon: <CreditCardOutlined /> },
  { value: 'Transfer', label: 'Transferencia',  icon: <WifiOutlined /> },
  { value: 'QR',       label: 'QR',             icon: <QrcodeOutlined /> },
];
const METHOD_COLORS: Record<string, string> = {
  Cash: 'green', Card: 'blue', Transfer: 'purple', QR: 'cyan',
};
const METHOD_LABELS: Record<string, string> = {
  Cash: 'Efectivo', Card: 'Tarjeta', Transfer: 'Transferencia', QR: 'QR',
};
const DOC_OPTIONS = [
  { value: 'NotaDeVenta', label: 'Nota de Venta' },
  { value: 'Factura',     label: 'Factura (SRI)' },
];

interface PaymentLine { method: string; amountTendered: number }

interface Props {
  order: OrderDto;
  open: boolean;
  onClose: () => void;
  onPaid: () => void;
  branchId: string;
}

export default function PayOrderModal({ order, open, onClose, onPaid, branchId }: Props) {
  const [existingPayments, setExistingPayments] = useState<OrderPaymentDto[]>([]);
  const [loadingPayments, setLoadingPayments] = useState(false);
  const [saving, setSaving] = useState(false);

  const [orderAmount, setOrderAmount] = useState<number>(0);
  const [docType, setDocType] = useState<string>('NotaDeVenta');
  const [customer, setCustomer] = useState<CustomerDto | null>(null);
  const [lines, setLines] = useState<PaymentLine[]>([{ method: 'Cash', amountTendered: 0 }]);

  const alreadyPaid = existingPayments.reduce((s, p) => s + p.orderAmount, 0);
  const remaining = Math.max(0, order.total - alreadyPaid);
  const isFullyPaid = remaining <= 0.01;

  useEffect(() => {
    if (!open) return;
    loadPayments();
  }, [open, order.id]);

  useEffect(() => {
    if (remaining > 0) {
      setOrderAmount(parseFloat(remaining.toFixed(2)));
      setDocType('NotaDeVenta');
      setCustomer(null);
      setLines([{ method: 'Cash', amountTendered: 0 }]);
    }
  }, [remaining]);

  const loadPayments = async () => {
    setLoadingPayments(true);
    try {
      const r = await cashApi.getOrderPayments(order.id);
      setExistingPayments(r.data);
    } catch {
      message.error('Error al cargar pagos existentes');
    } finally {
      setLoadingPayments(false);
    }
  };

  const addLine = () => setLines(prev => [...prev, { method: 'Cash', amountTendered: 0 }]);

  const removeLine = (idx: number) =>
    setLines(prev => prev.filter((_, i) => i !== idx));

  const updateLine = (idx: number, field: keyof PaymentLine, value: string | number) =>
    setLines(prev => prev.map((l, i) => i === idx ? { ...l, [field]: value } : l));

  const totalTendered = lines.reduce((s, l) => s + (l.amountTendered || 0), 0);
  const totalChange = Math.max(0, totalTendered - (orderAmount || 0));
  const hasCashLine = lines.some(l => l.method === 'Cash');
  const tenderCoversAmount = totalTendered >= (orderAmount || 0);
  const amountExceedsRemaining = (orderAmount || 0) > remaining + 0.01;
  const needsCustomer = docType === 'Factura' && !customer;
  const canSubmit = !amountExceedsRemaining && !needsCustomer && tenderCoversAmount && (orderAmount || 0) > 0;

  const handlePay = async () => {
    if (!canSubmit) return;
    if (totalChange > 0 && !hasCashLine) {
      message.warning('Hay excedente pero no hay línea de efectivo para dar vuelto.');
      return;
    }
    setSaving(true);
    try {
      const dto: AddOrderPaymentDto = {
        orderAmount,
        documentType: docType,
        customerId: customer?.id,
        lines: lines.map(l => ({ method: l.method, amountTendered: l.amountTendered } as AddPaymentLineDto)),
      };
      await cashApi.payOrder(order.id, dto);
      message.success('Cobro registrado');
      onPaid();
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'Error al registrar el cobro');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      title={
        <Space>
          <Text strong>Cobrar orden #{order.number}</Text>
          {order.tableCode && <Tag>{order.tableCode}</Tag>}
        </Space>
      }
      open={open}
      onCancel={onClose}
      footer={null}
      width={520}
      destroyOnHidden
    >
      {/* Resumen */}
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
        <Text type="secondary">Total de la orden</Text>
        <Text strong style={{ fontSize: 16 }}>${order.total.toFixed(2)}</Text>
      </div>

      {/* Cobros ya registrados */}
      {loadingPayments ? (
        <Spin size="small" style={{ display: 'block', margin: '8px 0' }} />
      ) : existingPayments.length > 0 && (
        <>
          <List
            size="small"
            dataSource={existingPayments}
            renderItem={(p, idx) => (
              <List.Item style={{ padding: '4px 0' }}>
                <Space style={{ width: '100%', justifyContent: 'space-between' }}>
                  <Space wrap>
                    <Text type="secondary">Cobro {idx + 1}</Text>
                    <Tag color={p.documentType === 'Factura' ? 'gold' : 'default'}>
                      {p.documentType === 'Factura' ? 'Factura' : 'Nota de Venta'}
                    </Tag>
                    {p.lines.map((l, li) => (
                      <Tag key={li} color={METHOD_COLORS[l.method]}>
                        {METHOD_LABELS[l.method]} ${l.netAmount.toFixed(2)}
                      </Tag>
                    ))}
                    {p.customerName && <Text type="secondary">{p.customerName}</Text>}
                  </Space>
                  <Text strong>${p.orderAmount.toFixed(2)}</Text>
                </Space>
              </List.Item>
            )}
          />
          <Divider style={{ margin: '8px 0' }} />
        </>
      )}

      {/* Saldo pendiente */}
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Text type={isFullyPaid ? 'success' : 'warning'}>
          {isFullyPaid ? 'Pagada completamente' : 'Saldo pendiente'}
        </Text>
        <Text strong style={{ fontSize: 15, color: isFullyPaid ? '#52c41a' : '#faad14' }}>
          ${remaining.toFixed(2)}
        </Text>
      </div>

      {isFullyPaid ? (
        <Alert
          type="success"
          icon={<CheckCircleOutlined />}
          message="Esta orden está completamente pagada."
          showIcon
        />
      ) : (
        <>
          <Title level={5} style={{ marginBottom: 12 }}>Nuevo cobro</Title>

          {/* Monto */}
          <div style={{ marginBottom: 12 }}>
            <Text type="secondary" style={{ display: 'block', marginBottom: 4 }}>Monto a cobrar</Text>
            <InputNumber
              style={{ width: '100%' }}
              prefix="$"
              min={0.01}
              max={remaining}
              precision={2}
              value={orderAmount}
              onChange={v => setOrderAmount(v ?? 0)}
              status={amountExceedsRemaining ? 'error' : undefined}
            />
            {amountExceedsRemaining && (
              <Text type="danger" style={{ fontSize: 12 }}>
                No puede superar el saldo pendiente (${remaining.toFixed(2)})
              </Text>
            )}
            <Space style={{ marginTop: 6 }}>
              <Button size="small" onClick={() => setOrderAmount(parseFloat(remaining.toFixed(2)))}>
                Todo el saldo
              </Button>
              {remaining > 1 && (
                <Button size="small" onClick={() => setOrderAmount(parseFloat((remaining / 2).toFixed(2)))}>
                  Mitad
                </Button>
              )}
            </Space>
          </div>

          {/* Tipo de documento */}
          <div style={{ marginBottom: 12 }}>
            <Text type="secondary" style={{ display: 'block', marginBottom: 4 }}>Documento</Text>
            <Select
              style={{ width: '100%' }}
              options={DOC_OPTIONS}
              value={docType}
              onChange={v => { setDocType(v); if (v === 'NotaDeVenta') setCustomer(null); }}
            />
          </div>

          {/* Cliente */}
          <div style={{ marginBottom: 16 }}>
            <Text type="secondary" style={{ display: 'block', marginBottom: 4 }}>
              Cliente{docType === 'Factura' ? <Text type="danger"> *</Text> : ' (opcional)'}
            </Text>
            <CustomerSelector branchId={branchId} value={customer} onChange={setCustomer} />
            {needsCustomer && (
              <Text type="danger" style={{ fontSize: 12 }}>La factura requiere un cliente con RUC o cédula</Text>
            )}
          </div>

          {/* Medios de pago */}
          <div style={{ marginBottom: 8 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
              <Text type="secondary">Medios de pago</Text>
              <Button size="small" icon={<PlusOutlined />} onClick={addLine}>Agregar método</Button>
            </div>

            {lines.map((line, idx) => {
              // El vuelto se asigna a la última línea de efectivo
              const cashLines = lines.map((l, i) => ({ ...l, i })).filter(l => l.method === 'Cash');
              const isLastCash = cashLines.at(-1)?.i === idx;
              const lineChange = isLastCash ? totalChange : 0;

              return (
                <div key={idx} style={{ display: 'flex', gap: 8, marginBottom: 8, alignItems: 'center' }}>
                  <Select
                    style={{ width: 150 }}
                    value={line.method}
                    onChange={v => updateLine(idx, 'method', v)}
                    options={METHOD_OPTIONS.map(o => ({
                      value: o.value,
                      label: <Space>{o.icon}{o.label}</Space>,
                    }))}
                  />
                  <InputNumber
                    style={{ flex: 1 }}
                    prefix="$"
                    min={0}
                    precision={2}
                    placeholder="Monto entregado"
                    value={line.amountTendered || undefined}
                    onChange={v => updateLine(idx, 'amountTendered', v ?? 0)}
                  />
                  {lineChange > 0 && (
                    <Tooltip title="Vuelto estimado">
                      <Tag color="orange">vuelto ${lineChange.toFixed(2)}</Tag>
                    </Tooltip>
                  )}
                  {lines.length > 1 && (
                    <Button size="small" danger icon={<DeleteOutlined />} onClick={() => removeLine(idx)} />
                  )}
                </div>
              );
            })}

            {tenderCoversAmount && totalChange > 0 && !hasCashLine && (
              <Alert type="warning" showIcon message="Agrega una línea de efectivo para dar el vuelto" style={{ marginTop: 8 }} />
            )}
            {!tenderCoversAmount && (orderAmount || 0) > 0 && (
              <Text type="danger" style={{ fontSize: 12 }}>
                Faltan ${((orderAmount || 0) - totalTendered).toFixed(2)} por cubrir
              </Text>
            )}
          </div>

          <Divider style={{ margin: '12px 0' }} />

          <Button
            type="primary"
            block
            size="large"
            loading={saving}
            disabled={!canSubmit}
            onClick={handlePay}
          >
            Confirmar cobro ${(orderAmount || 0).toFixed(2)}
          </Button>
        </>
      )}
    </Modal>
  );
}
