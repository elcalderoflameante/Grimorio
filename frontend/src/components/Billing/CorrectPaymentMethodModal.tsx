import { useEffect, useRef, useState } from 'react';
import { App, Alert, Form, Input, Modal, Select, Spin, Typography } from 'antd';
import type { CardBankDto, OrderPaymentDto, PaymentMethodConfigDto } from '../../types';
import type { PaymentMethodCorrectionDto, PaymentMethodCorrectionLineDto } from '../../types/paymentMethodCorrection';
import CorrectionPaymentLines from './CorrectionPaymentLines';
import { cashApi, paymentMethodsApi } from '../../services/api';
import { createIdempotencyKey } from '../../utils/idempotencyKey';
import { formatError } from '../../utils/errorHandler';
import { formatBranchDateTime } from '../../utils/branchTimeZone';

interface Values {
  lineId: string;
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
  const lineId = Form.useWatch('lineId', form);
  const selectedLine = payment?.lines.find(l => l.id === lineId);
  const assignedCents = (lines ?? []).reduce((sum, line) => sum + Math.round((line.amount ?? 0) * 100), 0);
  const remainingCents = Math.round((selectedLine?.netAmount ?? 0) * 100) - assignedCents;
  const editable = payment?.cashSessionStatus === 'Open' && payment.documentType === 'NotaDeVenta' && !payment.electronicDocumentId
    && payment.lines.every(line => !line.employeeId);

  useEffect(() => {
    if (!payment) return;
    let cancelled = false;
    requestId.current = createIdempotencyKey();
    form.resetFields(); setHistory([]); setMethods([]); setBanks([]);
    const first = payment.lines[0];
    setLoading(true); setLoadFailed(false);
    Promise.all([paymentMethodsApi.getAll(), paymentMethodsApi.getCardBanks(), cashApi.getPaymentMethodCorrections(payment.id)])
      .then(([methodsRes, banksRes, historyRes]) => {
        if (cancelled) return;
        setMethods(methodsRes.data.filter(m => m.isActive && m.purpose !== 'EmployeePayrollDeduction'));
        setBanks(banksRes.data); setHistory(historyRes.data);
        form.setFieldsValue({ lineId: first?.id, lines: [{ amount: first?.netAmount, methodId: first?.methodId,
          cardPaymentType: first?.cardPaymentType, cardBankId: first?.cardBankId,
          cardBrand: first?.cardBrand, authorizationNumber: first?.authorizationNumber }] });
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
      const total = values.lines.reduce((sum, part) => sum + Math.round(part.amount * 100), 0);
      if (total !== Math.round(line.netAmount * 100)) {
        message.error('La suma de los medios debe coincidir con el importe a reclasificar.'); return;
      }
      await cashApi.correctPaymentMethod(payment.id, {
        requestId: requestId.current, paymentLineId: line.id, expectedMethodId: line.methodId,
        expectedUpdatedAt: line.updatedAt ?? null, reason: values.reason.trim(),
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
        Orden #{payment?.orderNumber} · Nota de venta · ${payment?.orderAmount.toFixed(2)}
      </Typography.Paragraph>
      {!editable && <Alert type="info" showIcon title="Solo se pueden corregir notas de venta con caja abierta y sin consumos de empleados." style={{ marginBottom: 12 }} />}
      <Form form={form} layout="vertical" disabled={saving || loading || loadFailed || !editable}>
        <Form.Item name="lineId" label="Pago registrado" rules={[{ required: true, message: 'Selecciona el pago' }]}>
          <Select options={payment?.lines.map(l => ({ value: l.id, label: `${l.methodName} - $${l.netAmount.toFixed(2)}` }))}
            onChange={id => {
              const source = payment?.lines.find(l => l.id === id);
              form.setFieldValue('lines', [{ methodId: source?.methodId, amount: source?.netAmount,
                cardPaymentType: source?.cardPaymentType, cardBankId: source?.cardBankId,
                cardBrand: source?.cardBrand, authorizationNumber: source?.authorizationNumber }]);
            }} />
        </Form.Item>
        {selectedLine && <Typography.Paragraph>Importe a reclasificar: <strong>${selectedLine.netAmount.toFixed(2)}</strong></Typography.Paragraph>}
        <CorrectionPaymentLines methods={methods} banks={banks} disabled={saving || loading || loadFailed || !editable} />
        {selectedLine && <Typography.Paragraph type={remainingCents === 0 ? 'success' : 'danger'}>
          Asignado: ${(assignedCents / 100).toFixed(2)} · Diferencia: ${(remainingCents / 100).toFixed(2)}
        </Typography.Paragraph>}
        <Form.Item name="reason" label="Motivo de la correccion" rules={[{ required: true, whitespace: true, message: 'Indica el motivo' },
          { min: 5, max: 500, message: 'Usa entre 5 y 500 caracteres' }]}>
          <Input.TextArea rows={2} maxLength={500} />
        </Form.Item>
      </Form>
      {history.length > 0 && <section>
        <Typography.Title level={5}>Historial de correcciones</Typography.Title>
        {history.map(c => <div key={c.id} style={{ borderTop: '1px solid #e5e7eb', padding: '10px 0', overflowWrap: 'anywhere' }}>
          <div>{formatBranchDateTime(c.correctedAt)} · {c.actorName}</div>
          <div>Antes: {c.before.methodName} · ${c.before.netAmount.toFixed(2)}</div>
          {c.before.change > 0 && <div>Antes: recibido ${c.before.amountTendered.toFixed(2)}, cambio ${c.before.change.toFixed(2)}</div>}
          {c.before.isCard && <div>Antes: {c.before.cardPaymentType === 'Credit' ? 'Credito' : 'Debito'} · {c.before.cardBankName} · {c.before.cardBrand} · {c.before.authorizationNumber}</div>}
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
