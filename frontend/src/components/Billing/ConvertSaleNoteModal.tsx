import { useEffect, useRef, useState } from 'react';
import { App, Form, Input, Modal, Typography } from 'antd';
import type { CustomerDto, OrderPaymentDto } from '../../types';
import { sriApi } from '../../services/api';
import { useAuth } from '../../context/useAuth';
import { formatError } from '../../utils/errorHandler';
import CustomerSelector from './CustomerSelector';

export default function ConvertSaleNoteModal({ payment, onClose, onSuccess }: {
  payment: OrderPaymentDto | null;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const { message } = App.useApp();
  const { branchId } = useAuth();
  const [customer, setCustomer] = useState<CustomerDto | null>(null);
  const [saving, setSaving] = useState(false);
  const inFlight = useRef(false);
  const [form] = Form.useForm<{ reason: string }>();
  useEffect(() => { if (payment) { setCustomer(null); form.resetFields(); } }, [payment, form]);

  const submit = async () => {
    if (!payment || inFlight.current) return;
    inFlight.current = true;
    setSaving(true);
    try {
      let values;
      try { values = await form.validateFields(); } catch { return; }
      if (!customer) { message.warning('Selecciona el cliente de la factura'); return; }
      const { data } = await sriApi.convertSaleNote(payment.id, { customerId: customer.id, reason: values.reason.trim() });
      message.success(`Factura ${data.numeroFactura} registrada para envio al SRI`);
      onSuccess();
      onClose();
    } catch (error) { message.error(formatError(error)); }
    finally { inFlight.current = false; setSaving(false); }
  };

  return <Modal title={<span style={{ display: 'block', paddingRight: 24, overflowWrap: 'anywhere' }}>Emitir factura desde nota de venta</span>} open={!!payment} width={640}
    okText="Emitir factura" cancelText="Cancelar" confirmLoading={saving}
    cancelButtonProps={{ disabled: saving }} closable={!saving} maskClosable={!saving}
    onOk={submit} onCancel={() => { if (!inFlight.current) onClose(); }}>
    <Typography.Paragraph style={{ marginTop: 16 }}>
      Orden #{payment?.orderNumber} · Cobro ${payment?.orderAmount.toFixed(2)}
    </Typography.Paragraph>
    <Form form={form} layout="vertical" disabled={saving}>
      <Form.Item label="Cliente de la factura" required>
        <div style={{ pointerEvents: saving ? 'none' : undefined }}>
          <CustomerSelector branchId={branchId ?? ''} value={customer} onChange={setCustomer} />
        </div>
      </Form.Item>
      <Form.Item name="reason" label="Motivo de la correccion" rules={[
        { required: true, whitespace: true, message: 'Indica el motivo' },
        { min: 5, max: 500, message: 'Usa entre 5 y 500 caracteres' },
      ]}>
        <Input.TextArea rows={3} maxLength={500} />
      </Form.Item>
    </Form>
  </Modal>;
}
