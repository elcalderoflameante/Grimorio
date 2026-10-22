import { useCallback, useEffect, useState } from 'react';
import { Alert, App, Button, Checkbox, Col, DatePicker, Descriptions, Form, Input, InputNumber, Modal, Row, Select, Space, Statistic, Switch, Table, Tabs, Tag, TimePicker, Typography } from 'antd';
import { CameraOutlined, EditOutlined, PlusOutlined, ReloadOutlined, SwapOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { responsibilityApi } from '../../services/responsibilityApi';
import type { ResponsibilityActivity, ResponsibilityAssignment, ResponsibilityCatalog, ResponsibilityDay, ResponsibilityNamed, ResponsibilityTask } from '../../services/responsibilityApi';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import { formatError } from '../../utils/errorHandler';
import { formatBranchDateTime } from '../../utils/branchTimeZone';

const frequencies = [{ value: 'Daily', label: 'Diaria' }, { value: 'Weekly', label: 'Semanal (días seleccionados)' },
  { value: 'Fortnightly', label: 'Quincenal (cada 14 días desde la fecha inicial)' }, { value: 'Monthly', label: 'Mensual' }, { value: 'EveryNDays', label: 'Cada N días' }];
const stages = [{ value: 'Opening', label: 'Inicio del turno' }, { value: 'DuringShift', label: 'Durante el turno' }, { value: 'Closing', label: 'Cierre del turno' }];
const photos = [{ value: 'None', label: 'Sin foto obligatoria' }, { value: 'After', label: 'Foto al terminar' }, { value: 'BeforeAndAfter', label: 'Fotos antes y después' }];
const states: Record<string, [string, string]> = { Pending: ['gold', 'Pendiente'], Overdue: ['red', 'Vencida'], Completed: ['green', 'Realizada'], CompletedWithIssue: ['orange', 'Revisada con novedad'] };
type Kind = 'areas' | 'positions' | 'tasks' | 'assignments';
type Editable = ResponsibilityNamed | ResponsibilityTask | ResponsibilityAssignment;
type EditorValues = Partial<ResponsibilityTask & ResponsibilityAssignment> & { period?: [Dayjs, Dayjs]; anchor?: Dayjs; shiftStart?: Dayjs };
const emptyCatalog: ResponsibilityCatalog = { areas: [], positions: [], tasks: [], assignments: [], employees: [] };

export default function ResponsibilitiesModule() {
  const { message } = App.useApp();
  const { hasPermission } = useAuth();
  const canManage = hasPermission(PERMISSIONS.operations.responsibilitiesManage);
  const [catalog, setCatalog] = useState<ResponsibilityCatalog>(emptyCatalog);
  const [date, setDate] = useState(dayjs().format('YYYY-MM-DD'));
  const [day, setDay] = useState<ResponsibilityDay>();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editor, setEditor] = useState<{ kind: Kind; value?: Editable }>();
  const [detail, setDetail] = useState<ResponsibilityActivity>();
  const [transfer, setTransfer] = useState<ResponsibilityActivity>();
  const [image, setImage] = useState<string>();
  const [form] = Form.useForm<EditorValues>();
  const [transferForm] = Form.useForm<{ employeeId: string; reason: string }>();
  const stage = Form.useWatch('stage', form);
  const frequency = Form.useWatch('frequency', form);
  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [config, sheet] = await Promise.all([responsibilityApi.catalog(), responsibilityApi.day(date)]);
      setCatalog(config.data); setDay(sheet.data);
    } catch (error) { message.error(formatError(error)); }
    finally { setLoading(false); }
  }, [date, message]);
  useEffect(() => { void load(); }, [load]);
  useEffect(() => () => { if (image) URL.revokeObjectURL(image); }, [image]);
  const names = (kind: 'areas' | 'positions', id: string) => catalog[kind].find(x => x.id === id)?.name ?? id;
  const employeeName = (id: string) => catalog.employees.find(x => x.id === id)?.name ?? id;
  const openEditor = (kind: Kind, value?: Editable) => {
    form.resetFields();
    const task = value as ResponsibilityTask | undefined;
    const assignment = value as ResponsibilityAssignment | undefined;
    form.setFieldsValue({ frequency: 'Daily', stage: 'Opening', photoMode: 'After', daysOfWeekMask: 127,
      everyNDays: 1, windowMinutes: 30, isActive: true, checklist: [], ...value,
      anchor: dayjs(task?.anchorDate ?? date),
      period: [dayjs(assignment?.fromDate ?? date), dayjs(assignment?.toDate ?? date).add(assignment ? 0 : 6, 'day')],
      shiftStart: dayjs(`2000-01-01T${assignment?.shiftStartTime ?? '10:00:00'}`) });
    setEditor({ kind, value });
  };
  const save = async () => {
    if (!editor) return;
    const values = await form.validateFields();
    setSaving(true);
    try {
      const id = editor.value?.id;
      if (editor.kind === 'areas' || editor.kind === 'positions') await responsibilityApi.saveNamed(editor.kind, { id, name: values.name, isActive: values.isActive });
      if (editor.kind === 'tasks') await responsibilityApi.saveTask({ ...values, id, anchorDate: values.anchor!.format('YYYY-MM-DD'),
        intervalMinutes: values.stage === 'DuringShift' ? values.intervalMinutes : null });
      if (editor.kind === 'assignments') await responsibilityApi.saveAssignment({ id, positionId: values.positionId, employeeId: values.employeeId,
        fromDate: values.period![0].format('YYYY-MM-DD'), toDate: values.period![1].format('YYYY-MM-DD'), shiftStartTime: values.shiftStart!.format('HH:mm:ss'), isActive: values.isActive });
      setEditor(undefined); message.success('Configuración guardada. Las actividades ya generadas conservan su historial.'); await load();
    } catch (error) { message.error(formatError(error)); }
    finally { setSaving(false); }
  };
  const showImage = async (id: string) => {
    try { const response = await responsibilityApi.evidence(id); setImage(URL.createObjectURL(response.data)); }
    catch (error) { message.error(formatError(error)); }
  };
  const transferActivity = async () => {
    if (!transfer) return;
    const values = await transferForm.validateFields(); setSaving(true);
    try { await responsibilityApi.transfer(transfer.id, values.employeeId, values.reason); setTransfer(undefined); await load(); message.success('Responsable reemplazada; cambio registrado en el historial.'); }
    catch (error) { message.error(formatError(error)); }
    finally { setSaving(false); }
  };
  const namedTable = (kind: 'areas' | 'positions') => <Space orientation="vertical" style={{ width: '100%' }}>
    {canManage && <Button icon={<PlusOutlined />} onClick={() => openEditor(kind)}>Agregar {kind === 'areas' ? 'área' : 'puesto'}</Button>}
    <Table rowKey="id" dataSource={catalog[kind]} columns={[
      { title: 'Nombre', dataIndex: 'name' }, { title: 'Estado', render: (_, row: ResponsibilityNamed) => <Tag color={row.isActive ? 'green' : 'default'}>{row.isActive ? 'Activo' : 'Inactivo'}</Tag> },
      { title: '', render: (_, row: ResponsibilityNamed) => canManage && <Button icon={<EditOutlined />} onClick={() => openEditor(kind, row)}>Editar</Button> },
    ]} />
  </Space>;
  return <div>
    <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 18 }} wrap>
      <Typography.Title level={4} style={{ margin: 0 }}>Responsabilidades operativas</Typography.Title>
      <Button icon={<ReloadOutlined />} loading={loading} onClick={() => void load()}>Actualizar</Button>
    </Space>
    <Tabs items={[
      { key: 'day', label: 'Jornadas y evidencias', children: <>
        <Space wrap style={{ marginBottom: 18 }}><DatePicker allowClear={false} value={dayjs(date)} onChange={value => { if (value) setDate(value.format('YYYY-MM-DD')); }} /><Typography.Text type="secondary">Una responsable por actividad. Reemplazos con motivo registrado.</Typography.Text></Space>
        <Row gutter={[16, 16]} style={{ marginBottom: 18 }}>{[['Actividades', day?.activities.length ?? 0], ['Pendientes', day?.activities.filter(x => x.status === 'Pending').length ?? 0], ['Vencidas', day?.activities.filter(x => x.status === 'Overdue').length ?? 0], ['Con novedades', day?.activities.filter(x => x.status === 'CompletedWithIssue').length ?? 0]].map(([title, value]) => <Col xs={12} md={6} key={title}><Statistic title={title} value={value} /></Col>)}</Row>
        {day?.warnings.map((warning, i) => <Alert key={i} showIcon type="warning" style={{ marginBottom: 8 }} title={warning.positionName} description={warning.message} />)}
        <Table rowKey="id" loading={loading} dataSource={day?.activities ?? []} scroll={{ x: 850 }} columns={[
          { title: 'Actividad', render: (_, row: ResponsibilityActivity) => <><strong>{row.taskName}</strong><br />{row.areaName} · {row.positionName}</> },
          { title: 'Responsable', dataIndex: 'employeeName' },
          { title: 'Revisión', render: (_, row: ResponsibilityActivity) => formatBranchDateTime(row.availableAtUtc) },
          { title: 'Límite', render: (_, row: ResponsibilityActivity) => formatBranchDateTime(row.dueAtUtc) },
          { title: 'Estado', render: (_, row: ResponsibilityActivity) => <Tag color={states[row.status]?.[0]}>{states[row.status]?.[1] ?? row.status}</Tag> },
          { title: 'Acciones', render: (_, row: ResponsibilityActivity) => <Space wrap><Button onClick={() => setDetail(row)}>Detalle ({row.evidence.length} fotos)</Button>{canManage && ['Pending', 'Overdue'].includes(row.status) && <Button icon={<SwapOutlined />} onClick={() => { transferForm.resetFields(); setTransfer(row); }}>Reemplazar</Button>}</Space> },
        ]} />
      </> },
      { key: 'areas', label: 'Áreas', children: namedTable('areas') },
      { key: 'positions', label: 'Puestos operativos', children: <><Alert showIcon type="info" style={{ marginBottom: 16 }} title="Ejemplos: Mesera 1, Mesera 2, Cocina 1. Estos puestos no cambian permisos ni cargos de nómina." />{namedTable('positions')}</> },
      { key: 'tasks', label: 'Tareas y frecuencias', children: <>
        {canManage && <Button style={{ marginBottom: 16 }} icon={<PlusOutlined />} onClick={() => openEditor('tasks')}>Crear tarea</Button>}
        <Table rowKey="id" dataSource={catalog.tasks} scroll={{ x: 800 }} columns={[
          { title: 'Tarea', dataIndex: 'name' }, { title: 'Área', render: (_, t: ResponsibilityTask) => names('areas', t.areaId) }, { title: 'Puesto', render: (_, t: ResponsibilityTask) => names('positions', t.positionId) },
          { title: 'Frecuencia', render: (_, t: ResponsibilityTask) => frequencies.find(x => x.value === t.frequency)?.label }, { title: 'Momento', render: (_, t: ResponsibilityTask) => stages.find(x => x.value === t.stage)?.label },
          { title: 'Estado', render: (_, t: ResponsibilityTask) => t.isActive ? 'Activa' : 'Inactiva' }, { title: '', render: (_, t: ResponsibilityTask) => canManage && <Button onClick={() => openEditor('tasks', t)}>Editar</Button> },
        ]} />
      </> },
      { key: 'assignments', label: 'Rotaciones por semana y turno', children: <>
        <Alert showIcon type="info" style={{ marginBottom: 16 }} title="Asigna cada puesto a una persona durante un periodo. La hora inicial debe coincidir con su turno guardado en Personal → Horarios. Los días libres no generan actividades." description="Cambiar una rotación no modifica actividades ya generadas. Usa Reemplazar en la jornada para cubrir una ausencia." />
        {canManage && <Button style={{ marginBottom: 16 }} icon={<PlusOutlined />} onClick={() => openEditor('assignments')}>Asignar puesto</Button>}
        <Table rowKey="id" dataSource={catalog.assignments} scroll={{ x: 750 }} columns={[
          { title: 'Puesto', render: (_, a: ResponsibilityAssignment) => names('positions', a.positionId) }, { title: 'Empleado', render: (_, a: ResponsibilityAssignment) => employeeName(a.employeeId) },
          { title: 'Desde', dataIndex: 'fromDate' }, { title: 'Hasta', dataIndex: 'toDate' }, { title: 'Inicio de turno', render: (_, a: ResponsibilityAssignment) => a.shiftStartTime.slice(0, 5) },
          { title: 'Estado', render: (_, a: ResponsibilityAssignment) => a.isActive ? 'Activa' : 'Inactiva' }, { title: '', render: (_, a: ResponsibilityAssignment) => canManage && <Button onClick={() => openEditor('assignments', a)}>Editar</Button> },
        ]} />
      </> },
    ]} />
    <Modal title={editor?.value ? 'Editar configuración' : 'Nueva configuración'} open={!!editor} onCancel={() => { if (!saving) setEditor(undefined); }} onOk={() => void save()} confirmLoading={saving} width={720} destroyOnHidden>
      <Form form={form} layout="vertical">
        {editor?.kind !== 'assignments' && <Form.Item name="name" label="Nombre" rules={[{ required: true, whitespace: true, max: editor?.kind === 'tasks' ? 160 : 120 }]}><Input /></Form.Item>}
        {editor?.kind === 'tasks' && <>
          <Row gutter={16}><Col span={12}><Form.Item name="areaId" label="Área" rules={[{ required: true }]}><Select options={catalog.areas.filter(x => x.isActive).map(x => ({ value: x.id, label: x.name }))} /></Form.Item></Col><Col span={12}><Form.Item name="positionId" label="Puesto responsable" rules={[{ required: true }]}><Select options={catalog.positions.filter(x => x.isActive).map(x => ({ value: x.id, label: x.name }))} /></Form.Item></Col></Row>
          <Form.Item name="instructions" label="Instrucciones"><Input.TextArea maxLength={2000} rows={3} /></Form.Item>
          <Row gutter={16}><Col span={12}><Form.Item name="frequency" label="Frecuencia" rules={[{ required: true }]}><Select options={frequencies} /></Form.Item></Col><Col span={12}><Form.Item name="anchor" label="Fecha inicial de la recurrencia" rules={[{ required: true }]}><DatePicker /></Form.Item></Col></Row>
          {frequency === 'EveryNDays' && <Form.Item name="everyNDays" label="Cada cuántos días" rules={[{ required: true }]}><InputNumber min={1} max={366} /></Form.Item>}
          <Form.Item name="daysOfWeekMask" label="Días habilitados" getValueProps={(mask: number) => ({ value: [1, 2, 4, 8, 16, 32, 64].filter(bit => (mask & bit) !== 0) })} getValueFromEvent={(values: number[]) => values.reduce((sum, bit) => sum | bit, 0)}><Checkbox.Group options={['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'].map((label, i) => ({ label, value: 1 << i }))} /></Form.Item>
          <Row gutter={16}><Col span={12}><Form.Item name="stage" label="Momento del turno"><Select options={stages} /></Form.Item></Col><Col span={12}><Form.Item name="windowMinutes" label="Minutos para realizarla"><InputNumber min={5} max={240} /></Form.Item></Col></Row>
          {stage === 'DuringShift' && <Form.Item name="intervalMinutes" label="Repetir durante el turno cada (minutos)" extra="Dejar vacío para una sola revisión al inicio del turno."><InputNumber min={15} max={720} /></Form.Item>}
          <Form.Item name="photoMode" label="Evidencia desde la cámara"><Select options={photos} /></Form.Item>
          <Typography.Text strong>Controles de la actividad</Typography.Text>
          <Form.List name="checklist">{(fields, { add, remove }) => <>{fields.map(field => <Space key={field.key} align="start" wrap>
            <Form.Item name={[field.name, 'key']} hidden><Input /></Form.Item>
            <Form.Item name={[field.name, 'label']} rules={[{ required: true, max: 150 }]}><Input placeholder="Ej. Jabón de manos" /></Form.Item>
            <Form.Item name={[field.name, 'options']} rules={[{ required: true, type: 'array', min: 2, max: 10 }]}><Select mode="tags" style={{ minWidth: 260 }} placeholder="Opciones de respuesta" tokenSeparators={[';']} /></Form.Item>
            <Form.Item noStyle shouldUpdate>{() => <Form.Item name={[field.name, 'attentionOptions']} label="Respuestas que requieren novedad"><Select mode="multiple" style={{ minWidth: 260 }} options={(form.getFieldValue(['checklist', field.name, 'options']) as string[] ?? []).map(value => ({ value, label: value }))} /></Form.Item>}</Form.Item>
            <Button onClick={() => remove(field.name)}>Quitar</Button>
          </Space>)}<Button onClick={() => add({ key: `check-${Date.now()}`, label: '', options: ['Conforme', 'Requiere atención'], attentionOptions: ['Requiere atención'] })}>Agregar control</Button></>}</Form.List>
        </>}
        {editor?.kind === 'assignments' && <>
          <Form.Item name="positionId" label="Puesto operativo" rules={[{ required: true }]}><Select options={catalog.positions.filter(x => x.isActive).map(x => ({ value: x.id, label: x.name }))} /></Form.Item>
          <Form.Item name="employeeId" label="Empleado" rules={[{ required: true }]}><Select showSearch optionFilterProp="label" options={catalog.employees.map(x => ({ value: x.id, label: `${x.name}${x.hasUser ? '' : ' (sin usuario vinculado)'}` }))} /></Form.Item>
          <Form.Item name="period" label="Vigencia de la rotación" rules={[{ required: true }]}><DatePicker.RangePicker /></Form.Item>
          <Form.Item name="shiftStart" label="Hora inicial del turno en Horarios" rules={[{ required: true }]}><TimePicker format="HH:mm" minuteStep={5} /></Form.Item>
        </>}
        <Form.Item name="isActive" label="Activa" valuePropName="checked"><Switch /></Form.Item>
      </Form>
    </Modal>
    <Modal open={!!detail} title={detail?.taskName} footer={null} onCancel={() => setDetail(undefined)}>
      {detail && <><Descriptions column={1} items={[
        { key: 'owner', label: 'Responsable', children: `${detail.employeeName} · ${detail.positionName}` }, { key: 'area', label: 'Área', children: detail.areaName },
        { key: 'instructions', label: 'Instrucciones', children: detail.instructions }, { key: 'completed', label: 'Registrada', children: detail.completedAtUtc ? formatBranchDateTime(detail.completedAtUtc) : 'Pendiente' },
        ...detail.checklist.map(check => ({ key: check.key, label: check.label, children: detail.answers.find(a => a.key === check.key)?.value ?? 'Sin registrar' })),
        { key: 'notes', label: 'Novedades', children: detail.notes ?? 'Sin observaciones' },
      ]} /><Space wrap>{detail.evidence.map(e => <Button key={e.id} icon={<CameraOutlined />} onClick={() => void showImage(e.id)}>{e.phase === 'Before' ? 'Antes' : 'Después'} · {e.capturedAtUtc ? formatBranchDateTime(e.capturedAtUtc) : ''}</Button>)}</Space></>}
    </Modal>
    <Modal open={!!image} title="Evidencia privada" footer={null} onCancel={() => setImage(undefined)}>{image && <img src={image} alt="Evidencia de la actividad" style={{ width: '100%' }} />}</Modal>
    <Modal open={!!transfer} title="Reemplazar responsable" confirmLoading={saving} onCancel={() => { if (!saving) setTransfer(undefined); }} onOk={() => void transferActivity()}>
      <Form form={transferForm} layout="vertical"><Form.Item name="employeeId" label="Persona que cubrirá esta actividad" rules={[{ required: true }]}><Select options={catalog.employees.filter(x => x.id !== transfer?.employeeId).map(x => ({ value: x.id, label: x.name }))} /></Form.Item><Form.Item name="reason" label="Motivo del reemplazo" rules={[{ required: true, whitespace: true, max: 500 }]}><Input.TextArea /></Form.Item></Form>
    </Modal>
  </div>;
}
