import { useEffect, useRef, useState } from 'react';
import { App, Alert, Col, Form, Input, Modal, Row, Select, Spin, Typography } from 'antd';
import type { CardBankDto, OrderPaymentDto, PaymentMethodConfigDto } from '../../types';
import type { PaymentMethodCorrectionDto } from '../../types/paymentMethodCorrection';
import { cashApi, paymentMethodsApi } from '../../services/api';
import { createIdempotencyKey } from '../../utils/idempotencyKey';
import { formatError } from '../../utils/errorHandler';
import { formatBranchDateTime } from '../../utils/branchTimeZone';

interface Values {
  lineId: string;
  methodId: string;
  cardPaymentType?: string;
  cardBankId?: string;
  cardBrand?: string;
  authorizationNumber?: string;
  reason: string;
}

export default function CorrectPaymentMethodModal({ payment, onClose, onSuccess }: {
  payment: OrderPaymentDto | null;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm<Values>();
  const [methods, setMethods] = useState<PaymentMethodConfigDto[]>([]);
  const [banks, setBanks] = useState<CardBankDto[]>([]);
  const [history, setHistory] = useState<PaymentMethodCorrectionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadFailed, setLoadFailed] = useState(false);
  const [saving, setSaving] = useState(false);
  const inFlight = useRef(false);
  const requestId = useRef('');
  const methodId = Form.useWatch('methodId', form);
  const lineId = Form.useWatch('lineId', form);
  const selectedMethod = methods.find(m => m.id === methodId);
  const selectedLine = payment?.lines.find(l => l.id === lineId);
  const editable = payment?.cashSessionStatus === 'Open' && payment.documentType === 'NotaDeVenta' && !payment.electronicDocumentId
    && payment.lines.every(line => !line.employeeId);

  useEffect(() => {
    if (!payment) return;
    let cancelled = false;
    requestId.current = createIdempotencyKey();
    form.resetFields(); setHistory([]); setMethods([]); setBanks([]);
    form.setFieldsValue({ lineId: payment.lines[0]?.id });
    setLoading(true); setLoadFailed(false);
    Promise.all([paymentMethodsApi.getAll(), paymentMethodsApi.getCardBanks(), cashApi.getPaymentMethodCorrections(payment.id)])
      .then(([methodsRes, banksRes, historyRes]) => {
        if (cancelled) return;
        setMethods(methodsRes.data.filter(m => m.isActive && m.purpose !== 'EmployeePayrollDeduction'));
        setBanks(banksRes.data); setHistory(historyRes.data);
      }).catch(error => { if (!cancelled) { setLoadFailed(true); message.error(formatError(error)); } })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [payment, form, message]);

  const submit = async () => {
    if (!payment || inFlight.current || !editable || loading || loadFailed) return;
    inFlight.current = true; setSaving(true);
    try {
      let values;
      try { values = await form.validateFields(); } catch { return; }
      const line = payment.lines.find(l => l.id === values.lineId);
      if (!line) return;
      const card = methods.find(m => m.id === values.methodId)?.isCard;
      await cashApi.correctPaymentMethod(payment.id, {
        requestId: requestId.current, paymentLineId: line.id, expectedMethodId: line.methodId,
        expectedUpdatedAt: line.updatedAt ?? null, methodId: values.methodId, reason: values.reason.trim(),
        ...(card ? { cardPaymentType: values.cardPaymentType, cardBankId: values.cardBankId,
          cardBrand: values.cardBrand, authorizationNumber: values.authorizationNumber?.trim() } : {}),
      });
      message.success('Medio de pago corregido'); onSuccess(); onClose();
    } catch (error) { message.error(formatError(error)); }
    finally { inFlight.current = false; setSaving(false); }
  };

  return <Modal open={!!payment} title="Corregir medio de pago" width={640} forceRender
    okText="Guardar correccion" cancelText="Cerrar" onOk={submit} onCancel={() => { if (!inFlight.current) onClose(); }}
    confirmLoading={saving} okButtonProps={{ disabled: !editable || loading || loadFailed }}
    cancelButtonProps={{ disabled: saving }} closable={!saving} maskClosable={!saving} keyboard={!saving}>
    <Spin spinning={loading}>
      <Typography.Paragraph style={{ marginTop: 12 }}>
        Orden #{payment?.orderNumber} · Nota de venta · ${payment?.orderAmount.toFixed(2)}
      </Typography.Paragraph>
      {!editable && <Alert type="info" showIcon title="Solo se pueden corregir notas de venta con caja abierta y sin consumos de empleados." style={{ marginBottom: 12 }} />}
      <Form form={form} layout="vertical" disabled={saving || loading || loadFailed || !editable}>
        <Form.Item name="lineId" label="Pago registrado" rules={[{ required: true, message: 'Selecciona el pago' }]}>
          <Select options={payment?.lines.map(l => ({ value: l.id, label: `${l.methodName} - $${l.netAmount.toFixed(2)}` }))}
            onChange={() => form.setFieldsValue({ methodId: undefined, cardPaymentType: undefined, cardBankId: undefined,
              cardBrand: undefined, authorizationNumber: undefined })} />
        </Form.Item>
        {selectedLine && <Typography.Paragraph>Importe a reclasificar: <strong>${selectedLine.netAmount.toFixed(2)}</strong></Typography.Paragraph>}
        <Form.Item name="methodId" label="Medio de pago correcto" rules={[{ required: true, message: 'Selecciona el medio correcto' }]}>
          <Select options={methods.map(m => ({ value: m.id, label: m.name }))} />
        </Form.Item>
        {selectedMethod?.isCard && <Row gutter={12}>
          <Col xs={24} sm={12}><Form.Item name="cardPaymentType" label="Tipo de tarjeta" rules={[{ required: true, message: 'Selecciona credito o debito' }]}>
            <Select options={[{ value: 'Credit', label: 'Credito' }, { value: 'Debit', label: 'Debito' }]} />
          </Form.Item></Col>
          <Col xs={24} sm={12}><Form.Item name="cardBankId" label="Banco" rules={[{ required: true, message: 'Selecciona el banco' }]}>
            <Select showSearch optionFilterProp="label" options={banks.map(b => ({ value: b.id, label: b.name }))} />
          </Form.Item></Col>
          <Col xs={24} sm={12}><Form.Item name="cardBrand" label="Marca" rules={[{ required: true, message: 'Selecciona la marca' }]}>
            <Select options={['Visa', 'Mastercard', 'Diners', 'American Express', 'Discover', 'Otro'].map(value => ({ value, label: value }))} />
          </Form.Item></Col>
          <Col xs={24} sm={12}><Form.Item name="authorizationNumber" label="Autorizacion" rules={[{ required: true, whitespace: true, message: 'Ingresa la autorizacion' }]}>
            <Input maxLength={50} />
          </Form.Item></Col>
        </Row>}
        <Form.Item name="reason" label="Motivo de la correccion" rules={[{ required: true, whitespace: true, message: 'Indica el motivo' },
          { min: 5, max: 500, message: 'Usa entre 5 y 500 caracteres' }]}>
          <Input.TextArea rows={2} maxLength={500} />
        </Form.Item>
      </Form>
      {history.length > 0 && <section>
        <Typography.Title level={5}>Historial de correcciones</Typography.Title>
        {history.map(c => <div key={c.id} style={{ borderTop: '1px solid #e5e7eb', padding: '10px 0', overflowWrap: 'anywhere' }}>
          <div>{formatBranchDateTime(c.correctedAt)} · {c.actorName}</div>
          <div>{c.before.methodName} → {c.after.methodName} · ${c.after.netAmount.toFixed(2)}</div>
          {c.before.change > 0 && <div>Antes: recibido ${c.before.amountTendered.toFixed(2)}, cambio ${c.before.change.toFixed(2)}</div>}
          {c.before.isCard && <div>Antes: {c.before.cardPaymentType === 'Credit' ? 'Credito' : 'Debito'} · {c.before.cardBankName} · {c.before.cardBrand} · {c.before.authorizationNumber}</div>}
          {c.after.isCard && <div>Ahora: {c.after.cardPaymentType === 'Credit' ? 'Credito' : 'Debito'} · {c.after.cardBankName} · {c.after.cardBrand} · {c.after.authorizationNumber}</div>}
          <Typography.Text type="secondary">{c.reason}</Typography.Text>
        </div>)}
      </section>}
    </Spin>
  </Modal>;
}
