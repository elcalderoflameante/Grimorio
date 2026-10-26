import { useEffect, useRef, useState } from 'react';
import { App, Alert, Form, Input, Modal, Spin, Typography } from 'antd';
import type { CardBankDto, OrderPaymentDto, PaymentMethodConfigDto } from '../../types';
import type { PaymentMethodCorrectionDto, PaymentMethodCorrectionLineDto } from '../../types/paymentMethodCorrection';
import CorrectionPaymentLines from './CorrectionPaymentLines';
import { cashApi, paymentMethodsApi } from '../../services/api';
import { createIdempotencyKey } from '../../utils/idempotencyKey';
import { formatError } from '../../utils/errorHandler';
import { formatBranchDateTime } from '../../utils/branchTimeZone';

interface Values {
  lines: PaymentMethodCorrectionLineDto[];
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
  const lines = Form.useWatch('lines', form);
  const assignedCents = (lines ?? []).reduce((sum, line) => sum + Math.round((line.amount ?? 0) * 100), 0);
  const remainingCents = Math.round((payment?.orderAmount ?? 0) * 100) - assignedCents;
  const editable = payment?.cashSessionStatus === 'Open'
    && payment.lines.every(line => !line.employeeId);

  useEffect(() => {
    if (!payment) return;
    let cancelled = false;
    requestId.current = createIdempotencyKey();
    form.resetFields(); setHistory([]); setMethods([]); setBanks([]);
    setLoading(true); setLoadFailed(false);
    Promise.all([paymentMethodsApi.getAll(), paymentMethodsApi.getCardBanks(), cashApi.getPaymentMethodCorrections(payment.id)])
      .then(([methodsRes, banksRes, historyRes]) => {
        if (cancelled) return;
        setMethods(methodsRes.data.filter(m => m.isActive && m.purpose !== 'EmployeePayrollDeduction'));
        setBanks(banksRes.data); setHistory(historyRes.data);
        form.setFieldsValue({ lines: payment.lines.map(line => ({ amount: line.netAmount, methodId: line.methodId,
          cardPaymentType: line.cardPaymentType, cardBankId: line.cardBankId,
          cardBrand: line.cardBrand, authorizationNumber: line.authorizationNumber })) });
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
      const total = values.lines.reduce((sum, part) => sum + Math.round(part.amount * 100), 0);
      if (total !== Math.round(payment.orderAmount * 100)) {
        message.error('La suma de los medios debe coincidir con el total de la venta.'); return;
      }
      await cashApi.correctPaymentMethod(payment.id, {
        requestId: requestId.current, reason: values.reason.trim(),
        expectedLines: payment.lines.map(line => ({ id: line.id, methodId: line.methodId,
          updatedAt: line.updatedAt ?? null, netAmount: line.netAmount })),
        lines: values.lines.map(part => ({ methodId: part.methodId, amount: part.amount,
          ...(methods.find(m => m.id === part.methodId)?.isCard ? {
            cardPaymentType: part.cardPaymentType, cardBankId: part.cardBankId,
            cardBrand: part.cardBrand, authorizationNumber: part.authorizationNumber?.trim(),
          } : {}),
        })),
      });
      message.success('Medio de pago corregido'); onSuccess(); onClose();
    } catch (error) { message.error(formatError(error)); }
    finally { inFlight.current = false; setSaving(false); }
  };

  return <Modal open={!!payment} title="Corregir medio de pago" width={640} forceRender
    styles={{ body: { maxHeight: '70vh', overflowY: 'auto', paddingRight: 4 } }}
    okText="Guardar correccion" cancelText="Cerrar" onOk={submit} onCancel={() => { if (!inFlight.current) onClose(); }}
    confirmLoading={saving} okButtonProps={{ disabled: !editable || loading || loadFailed }}
    cancelButtonProps={{ disabled: saving }} closable={!saving} maskClosable={!saving} keyboard={!saving}>
    <Spin spinning={loading}>
      <Typography.Paragraph style={{ marginTop: 12 }}>
        Orden #{payment?.orderNumber} · {payment?.documentType === 'Factura' ? 'Factura' : 'Nota de venta'} · ${payment?.orderAmount.toFixed(2)}
      </Typography.Paragraph>
      {payment?.electronicDocumentId && <Alert type="warning" showIcon
        title="Correccion interna de caja"
        description="El XML y el RIDE existentes conservan los medios de pago registrados originalmente."
        style={{ marginBottom: 12 }} />}
      {!editable && <Alert type="info" showIcon title="Solo se pueden corregir cobros con caja abierta y sin consumos de empleados." style={{ marginBottom: 12 }} />}
      <Form form={form} layout="vertical" disabled={saving || loading || loadFailed || !editable}>
        <Typography.Paragraph>Total de la venta: <strong>${payment?.orderAmount.toFixed(2)}</strong></Typography.Paragraph>
        <CorrectionPaymentLines methods={methods} banks={banks} disabled={saving || loading || loadFailed || !editable} />
        <Typography.Paragraph type={remainingCents === 0 ? 'success' : 'danger'}>
          Asignado: ${(assignedCents / 100).toFixed(2)} · Diferencia: ${(remainingCents / 100).toFixed(2)}
        </Typography.Paragraph>
        <Form.Item name="reason" label="Motivo de la correccion" rules={[{ required: true, whitespace: true, message: 'Indica el motivo' },
          { min: 5, max: 500, message: 'Usa entre 5 y 500 caracteres' }]}>
          <Input.TextArea rows={2} maxLength={500} />
        </Form.Item>
      </Form>
      {history.length > 0 && <section>
        <Typography.Title level={5}>Historial de correcciones</Typography.Title>
        {history.map(c => <div key={c.id} style={{ borderTop: '1px solid #e5e7eb', padding: '10px 0', overflowWrap: 'anywhere' }}>
          <div>{formatBranchDateTime(c.correctedAt)} · {c.actorName}</div>
          {(c.beforeLines?.length ? c.beforeLines : [c.before]).map((part, index) => <div key={`${part.id}-${index}`}>
            Antes: {part.methodName} · ${part.netAmount.toFixed(2)}
            {part.change > 0 && <div>Recibido ${part.amountTendered.toFixed(2)}, cambio ${part.change.toFixed(2)}</div>}
            {part.isCard && <div>{part.cardPaymentType === 'Credit' ? 'Credito' : 'Debito'} · {part.cardBankName} · {part.cardBrand} · {part.authorizationNumber}</div>}
          </div>)}
          {(c.afterLines?.length ? c.afterLines : [c.after]).map((part, index) => <div key={`${part.id}-${index}`}>
            Ahora: {part.methodName} · ${part.netAmount.toFixed(2)}
            {part.isCard && <div>{part.cardPaymentType === 'Credit' ? 'Credito' : 'Debito'} · {part.cardBankName} · {part.cardBrand} · {part.authorizationNumber}</div>}
          </div>)}
          <Typography.Text type="secondary">{c.reason}</Typography.Text>
        </div>)}
      </section>}
    </Spin>
  </Modal>;
}
