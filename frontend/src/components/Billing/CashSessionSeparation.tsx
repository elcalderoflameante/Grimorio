import { useEffect, useRef, useState } from 'react';
import { Alert, App, Button, DatePicker, Descriptions, Form, Input, InputNumber, Modal, Space, Switch, Table, Tag, Typography } from 'antd';
import { CheckOutlined, EyeOutlined, ReloadOutlined } from '@ant-design/icons';
import type { CashSessionDto } from '../../types';
import type { CashSessionSeparationMovementDto, CashSessionSeparationPreviewDto, CashSessionSeparationRecordDto, CashSessionSeparationResultDto, SeparateCashSessionDto } from '../../types/cashSessionSeparation';
import { cashApi } from '../../services/api';
import { branchDateTimeUtcIso, formatBranchDateTime, getStoredBranchTimeZone, toBranchDayjs } from '../../utils/branchTimeZone';
import { createIdempotencyKey } from '../../utils/idempotencyKey';
import { formatError } from '../../utils/errorHandler';

const money = (amount: number) => `$${amount.toFixed(2)}`;

export function CashSessionSeparation({ session, onCancel, onDone }: {
  session: CashSessionDto; onCancel: () => void; onDone: (result: CashSessionSeparationResultDto) => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const counted = Form.useWatch('counted', form);
  const requestId = useRef(createIdempotencyKey());
  const [preview, setPreview] = useState<CashSessionSeparationPreviewDto | null>(null);
  const [payload, setPayload] = useState<SeparateCashSessionDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [uncertain, setUncertain] = useState(false);
  const [error, setError] = useState('');

  const review = async () => {
    const values = await form.validateFields().catch(() => null);
    if (!values) return;
    const data: SeparateCashSessionDto = {
      requestId: requestId.current, cutoff: branchDateTimeUtcIso(values.cutoff)!,
      newOpeningBalance: values.newOpeningBalance, previousActualCash: values.counted ? values.previousActualCash : null,
      reason: values.reason.trim(), previewToken: '',
    };
    setBusy(true); setError(''); setPreview(null); setPayload(null);
    try {
      const result = await cashApi.previewSeparation(session.id, data);
      setPreview(result.data);
      setPayload({ ...data, previewToken: result.data.token });
    } catch (e) { setError(formatError(e)); }
    finally { setBusy(false); }
  };

  const confirm = async () => {
    if (!payload || busy) return;
    setBusy(true); setError('');
    try {
      const result = await cashApi.separateSession(session.id, payload);
      message.success('Jornadas separadas. La nueva sesion queda abierta para el mismo cajero.');
      onDone(result.data);
    } catch (e) {
      const status = (e as { response?: { status?: number } }).response?.status;
      setError(formatError(e));
      if (!status || status >= 500) setUncertain(true);
      else { setPreview(null); setPayload(null); setUncertain(false); }
    } finally { setBusy(false); }
  };

  return <Modal title="Separar jornada de caja" open width={860} maskClosable={false}
    style={{ top: 20 }} styles={{ body: { maxHeight: 'calc(100dvh - 240px)', overflowY: 'auto', paddingRight: 4 } }}
    onCancel={() => { if (!busy) onCancel(); }} closable={!busy}
    footer={<Space wrap>
      <Button disabled={busy} onClick={onCancel}>Cancelar</Button>
      {!uncertain && <Button aria-label="Revisar corte" icon={<EyeOutlined />} loading={busy} onClick={() => void review()}>Revisar corte</Button>}
      <Button type="primary" aria-label={uncertain ? 'Reintentar confirmacion' : 'Confirmar separacion'}
        icon={uncertain ? <ReloadOutlined /> : <CheckOutlined />} loading={busy}
        disabled={!preview || !payload} onClick={() => void confirm()}>
        {uncertain ? 'Reintentar confirmacion' : 'Confirmar separacion'}
      </Button>
    </Space>}>
    <Typography.Paragraph>{session.cashRegisterName} · {session.openedByName} · Apertura: {formatBranchDateTime(session.openedAt)}</Typography.Paragraph>
    <Form form={form} layout="vertical" disabled={busy || uncertain}
      initialValues={{ cutoff: toBranchDayjs(new Date())!.startOf('day'), counted: false }}
      onValuesChange={() => { setPreview(null); setPayload(null); setError(''); }}>
      <Form.Item name="cutoff" label={`Nueva jornada desde (${getStoredBranchTimeZone()})`} rules={[{ required: true, message: 'Selecciona fecha y hora.' }]}>
        <DatePicker showTime={{ format: 'HH:mm:ss' }} format="DD/MM/YYYY HH:mm:ss" style={{ width: '100%' }} />
      </Form.Item>
      <Form.Item name="newOpeningBalance" label="Fondo inicial de la nueva jornada, antes de sus cobros ($)"
        rules={[{ required: true, message: 'Indica el fondo inicial, incluso si es cero.' }]}>
        <InputNumber min={0} max={999999999.99} precision={2} prefix="$" style={{ width: '100%' }} />
      </Form.Item>
      <Form.Item name="counted" label="Se conoce el efectivo contado al cierre anterior" valuePropName="checked"><Switch /></Form.Item>
      {counted && <Form.Item name="previousActualCash" label="Efectivo contado en el cierre anterior ($)"
        rules={[{ required: true, message: 'Indica el efectivo que se conto al cierre anterior.' }]}>
        <InputNumber min={0} max={999999999.99} precision={2} prefix="$" style={{ width: '100%' }} />
      </Form.Item>}
      {!counted && <Alert type="warning" showIcon title="El cierre anterior quedara con arqueo pendiente, sin inventar un efectivo contado ni una diferencia." style={{ marginBottom: 16 }} />}
      <Form.Item name="reason" label="Motivo" rules={[{ required: true, whitespace: true, min: 5, max: 500, message: 'Escribe un motivo de 5 a 500 caracteres.' }]}>
        <Input.TextArea rows={2} maxLength={500} />
      </Form.Item>
    </Form>
    {error && <Alert type="error" showIcon title={error} style={{ marginBottom: 16 }} />}
    {uncertain && <Alert type="warning" showIcon title="No se pudo confirmar el resultado. Reintenta con el mismo intento o actualiza Caja para comprobarlo." style={{ marginBottom: 16 }} />}
    {preview && <>
      <Table size="small" pagination={false} rowKey="label" scroll={{ x: 560 }} dataSource={[
        { label: 'Jornada anterior', ...preview.previousSession }, { label: 'Nueva jornada', ...preview.newSession },
      ]} columns={[
        { title: 'Jornada', dataIndex: 'label', width: 150 },
        { title: 'Cobros', dataIndex: 'totalSales', render: money, align: 'right' },
        { title: 'Gastos', dataIndex: 'totalExpenses', render: money, align: 'right' },
        { title: 'Fondo', dataIndex: 'openingBalance', render: money, align: 'right' },
        { title: 'Efectivo esperado', dataIndex: 'expectedCash', render: money, align: 'right' },
      ]} expandable={{ expandedRowRender: row => <Space wrap>{row.totals.map(t => <Tag key={t.methodId}>{t.methodName}: {money(t.total)}</Tag>)}</Space> }} />
      <Table<CashSessionSeparationMovementDto> size="small" rowKey={row => `${row.kind}:${row.id}`} dataSource={preview.movements} scroll={{ x: 650 }}
        style={{ marginTop: 16 }} pagination={{ pageSize: 8, showSizeChanger: false }} columns={[
          { title: 'Fecha', dataIndex: 'date', render: (date: string) => formatBranchDateTime(date), width: 140 },
          { title: 'Movimiento', render: (_, row) => <>{row.kind === 'Payment' ? 'Cobro' : 'Gasto'} · {row.reference}{row.isCancelled && <Tag>Anulado</Tag>}</> },
          { title: 'Importe', dataIndex: 'amount', render: money, align: 'right', width: 100 },
          { title: 'Jornada', render: (_, row) => row.movesToNewSession ? <Tag color="blue">Nueva</Tag> : <Tag>Anterior</Tag>, width: 100 },
        ]} />
      <Alert type="info" showIcon style={{ marginTop: 12 }} title="Los cobros usan su fecha de cobro; los gastos, su fecha de registro. Desde la hora del corte pertenecen a la nueva jornada. No se vuelven a cobrar ventas ni a descontar inventario." />
    </>}
  </Modal>;
}

export function CashSessionSeparationHistory({ sessionId }: { sessionId: string }) {
  const [records, setRecords] = useState<CashSessionSeparationRecordDto[]>([]);
  const [error, setError] = useState(false);
  useEffect(() => {
    let active = true;
    cashApi.getSessionSeparations(sessionId).then(r => { if (active) setRecords(r.data); })
      .catch(() => { if (active) setError(true); });
    return () => { active = false; };
  }, [sessionId]);
  if (error) return <Alert type="warning" title="No se pudo consultar la auditoria de separaciones." />;
  return <>{records.map(r => <Descriptions key={r.id} title="Separacion de jornada" size="small" column={{ xs: 1, sm: 2 }} style={{ padding: '8px 16px' }}>
    <Descriptions.Item label="Corte efectivo">{formatBranchDateTime(r.cutoffUtc)}</Descriptions.Item>
    <Descriptions.Item label="Operacion registrada">{formatBranchDateTime(r.recordedAt)}</Descriptions.Item>
    <Descriptions.Item label="Realizada por">{r.actorName}</Descriptions.Item>
    <Descriptions.Item label="Motivo">{r.reason}</Descriptions.Item>
    <Descriptions.Item label="Sesion anterior">{r.previousSessionId}</Descriptions.Item>
    <Descriptions.Item label="Sesion nueva">{r.newSessionId}</Descriptions.Item>
  </Descriptions>)}</>;
}
