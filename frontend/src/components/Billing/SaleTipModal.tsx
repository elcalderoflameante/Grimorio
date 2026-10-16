import { useEffect, useRef, useState } from 'react';
import { App, Button, Col, Form, InputNumber, Modal, Popconfirm, Row, Select, Spin } from 'antd';
import { DeleteOutlined } from '@ant-design/icons';
import { saleTipsApi } from '../../services/api';
import type { SaleTipDto, TipMethod } from '../../types/saleTips';
import { TIP_METHODS } from '../../types/saleTips';
import { formatError } from '../../utils/errorHandler';

export default function SaleTipModal({ sale, onClose, onSaved }: {
  sale: { id: string; orderNumber: number } | null;
  onClose: () => void;
  onSaved?: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm<{ amount: number; method: TipMethod }>();
  const [tip, setTip] = useState<SaleTipDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [loadFailed, setLoadFailed] = useState(false);
  const [saving, setSaving] = useState(false);
  const inFlight = useRef(false);
  const paymentId = sale?.id;

  useEffect(() => {
    if (!paymentId) return;
    let cancelled = false;
    setLoading(true);
    setLoadFailed(false);
    setTip(null);
    form.resetFields();
    saleTipsApi.get(paymentId).then(({ data }) => {
      if (cancelled) return;
      setTip(data);
      form.setFieldsValue(data ? { amount: data.amount, method: data.method } : { method: 'Cash' });
    }).catch(error => {
      if (cancelled) return;
      setLoadFailed(true);
      message.error(formatError(error));
    }).finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [paymentId, form, message]);

  const submit = async () => {
    if (!paymentId || inFlight.current || loading || loadFailed) return;
    inFlight.current = true;
    setSaving(true);
    try {
      let values;
      try { values = await form.validateFields(); } catch { return; }
      await saleTipsApi.save(paymentId, values);
      message.success('Propina guardada');
      onSaved?.();
      onClose();
    } catch (error) {
      message.error(formatError(error));
    } finally {
      inFlight.current = false;
      setSaving(false);
    }
  };

  const remove = async () => {
    if (!paymentId || inFlight.current) return;
    inFlight.current = true;
    setSaving(true);
    try {
      await saleTipsApi.remove(paymentId);
      message.success('Propina eliminada');
      onSaved?.();
      onClose();
    } catch (error) { message.error(formatError(error)); }
    finally { inFlight.current = false; setSaving(false); }
  };

  return <Modal open={!!sale} title={`Propina - Orden #${sale?.orderNumber ?? ''}`} width={480}
    onCancel={() => { if (!inFlight.current) onClose(); }} closable={!saving} maskClosable={!saving}
    footer={<div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end', flexWrap: 'wrap' }}>
      {tip && <Popconfirm title="Eliminar esta propina?" onConfirm={remove} disabled={saving}>
        <Button danger icon={<DeleteOutlined />} disabled={saving} style={{ marginRight: 'auto' }}>Eliminar</Button>
      </Popconfirm>}
      <Button disabled={saving} onClick={onClose}>Cancelar</Button>
      <Button type="primary" loading={saving} disabled={loading || loadFailed} onClick={submit}>Guardar</Button>
    </div>}>
    <Spin spinning={loading}>
      <Form form={form} layout="vertical" disabled={saving || loading || loadFailed} onFinish={submit}>
        <Row gutter={12} style={{ marginTop: 16 }}>
          <Col xs={24} sm={12}>
            <Form.Item name="amount" label="Importe" rules={[{ required: true, message: 'Ingresa el importe' },
              { type: 'number', min: 0.01, max: 99999999.99, message: 'Ingresa un importe mayor a cero' }]}>
              <InputNumber prefix="$" min={0.01} max={99999999.99} precision={2} step={0.5} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col xs={24} sm={12}>
            <Form.Item name="method" label="Metodo" rules={[{ required: true, message: 'Selecciona el metodo' }]}>
              <Select options={TIP_METHODS} />
            </Form.Item>
          </Col>
        </Row>
      </Form>
    </Spin>
  </Modal>;
}
