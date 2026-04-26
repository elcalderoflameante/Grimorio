import { useEffect, useState } from 'react';
import { Modal, Form, Select, InputNumber, Space, Typography, Divider, Tag, Button, message } from 'antd';
import { DollarOutlined, CreditCardOutlined, BankOutlined, QrcodeOutlined } from '@ant-design/icons';
import type { OrderDto, CashSessionDto, CustomerDto, PayOrderDto } from '../../types';
import { cashApi } from '../../services/api';
import CustomerSelector from './CustomerSelector';

const { Text, Title } = Typography;

const METHOD_OPTIONS = [
  { value: 'Cash', label: 'Efectivo', icon: <DollarOutlined /> },
  { value: 'Card', label: 'Tarjeta', icon: <CreditCardOutlined /> },
  { value: 'Transfer', label: 'Transferencia', icon: <BankOutlined /> },
  { value: 'QR', label: 'QR / Billetera', icon: <QrcodeOutlined /> },
];

interface Props {
  order: OrderDto;
  open: boolean;
  onClose: () => void;
  onPaid: (payment: { change: number; method: string }) => void;
  branchId: string;
}

export default function PayOrderModal({ order, open, onClose, onPaid, branchId }: Props) {
  const [form] = Form.useForm();
  const [method, setMethod] = useState<string>('Cash');
  const [amountPaid, setAmountPaid] = useState<number>(order.total);
  const [customer, setCustomer] = useState<CustomerDto | null>(null);
  const [activeSession, setActiveSession] = useState<CashSessionDto | null>(null);
  const [saving, setSaving] = useState(false);

  const change = method === 'Cash' ? Math.max(0, amountPaid - order.total) : 0;

  useEffect(() => {
    if (open) {
      setMethod('Cash');
      setAmountPaid(order.total);
      setCustomer(null);
      form.resetFields();
      cashApi.getActiveSession()
        .then(r => setActiveSession(r.data))
        .catch(() => setActiveSession(null));
    }
  }, [open, order.total, form]);

  const handlePay = async () => {
    if (method === 'Cash' && amountPaid < order.total) {
      message.error('El monto recibido es menor al total de la orden');
      return;
    }
    setSaving(true);
    try {
      const dto: PayOrderDto = {
        method,
        amountPaid: method === 'Cash' ? amountPaid : order.total,
        customerId: customer?.id,
        cashSessionId: activeSession?.id,
      };
      await cashApi.payOrder(order.id, dto);
      message.success(`Orden #${order.number} cobrada${change > 0 ? ` — Vuelto: $${change.toFixed(2)}` : ''}`);
      onPaid({ change, method });
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'Error al cobrar la orden');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      title={`Cobrar Orden #${order.number}`}
      open={open}
      onCancel={onClose}
      footer={null}
      width={460}
    >
      {activeSession && (
        <Tag color="green" style={{ marginBottom: 12 }}>
          Sesión abierta por {activeSession.openedByName}
        </Tag>
      )}

      <div style={{ background: '#fafafa', borderRadius: 8, padding: '12px 16px', marginBottom: 20 }}>
        <Space style={{ width: '100%', justifyContent: 'space-between' }}>
          <Text type="secondary">Total de la orden</Text>
          <Title level={4} style={{ margin: 0 }}>${order.total.toFixed(2)}</Title>
        </Space>
      </div>

      <Form form={form} layout="vertical">
        <Form.Item label="Método de pago">
          <Space wrap>
            {METHOD_OPTIONS.map(opt => (
              <Button
                key={opt.value}
                type={method === opt.value ? 'primary' : 'default'}
                icon={opt.icon}
                onClick={() => setMethod(opt.value)}
              >
                {opt.label}
              </Button>
            ))}
          </Space>
        </Form.Item>

        {method === 'Cash' && (
          <Form.Item label="Monto recibido">
            <InputNumber
              style={{ width: '100%' }}
              size="large"
              min={0}
              precision={2}
              prefix="$"
              value={amountPaid}
              onChange={v => setAmountPaid(v ?? order.total)}
            />
          </Form.Item>
        )}

        {method === 'Cash' && amountPaid > order.total && (
          <div style={{ background: '#f6ffed', border: '1px solid #b7eb8f', borderRadius: 8, padding: '10px 16px', marginBottom: 16 }}>
            <Space style={{ width: '100%', justifyContent: 'space-between' }}>
              <Text strong>Vuelto a entregar</Text>
              <Title level={3} style={{ margin: 0, color: '#52c41a' }}>${change.toFixed(2)}</Title>
            </Space>
          </div>
        )}

        <Divider />

        <Form.Item label="Cliente (opcional — requerido para factura)">
          <CustomerSelector
            branchId={branchId}
            value={customer}
            onChange={setCustomer}
          />
        </Form.Item>
      </Form>

      <Button
        type="primary"
        size="large"
        block
        loading={saving}
        onClick={handlePay}
        disabled={method === 'Cash' && amountPaid < order.total}
      >
        Confirmar cobro
      </Button>
    </Modal>
  );
}
