import { useCallback, useEffect, useState } from 'react';
import { Alert, App, Button, Card, DatePicker, Drawer, Form, Input, Modal, Select, Space, Switch, Table, Tabs, Tag, Typography } from 'antd';
import { DownloadOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import { staffInventoryCountsApi as api } from '../../services/staffInventoryCounts';
import type { CountArea, CountCatalog, StaffCount } from '../../types/staffInventoryCounts';
import { formatError } from '../../utils/errorHandler';
import { formatBranchDateTime } from '../../utils/branchTimeZone';

const statuses: Record<string, string> = { Draft: 'Borrador', Submitted: 'Pendiente de revisión', Reviewed: 'Revisado', Dismissed: 'Descartado', RecountRequested: 'Reconteo solicitado', Recounted: 'Con reconteo enviado' };
const days = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
const qty = (v: number | null) => v == null ? '—' : Number(v.toFixed(4)).toLocaleString('es-EC', { maximumFractionDigits: 4 });

export default function StaffInventoryCounts() {
  const { hasPermission } = useAuth();
  const { message } = App.useApp();
  const canView = hasPermission(PERMISSIONS.inventory.countsView);
  const canManage = hasPermission(PERMISSIONS.inventory.countsManage);
  const canReview = hasPermission(PERMISSIONS.inventory.countsReview);
  const [tab, setTab] = useState(canView ? 'pending' : 'config');
  const [counts, setCounts] = useState<StaffCount[]>([]);
  const [catalog, setCatalog] = useState<CountCatalog>();
  const [range, setRange] = useState<[string, string]>([dayjs().subtract(30, 'day').format('YYYY-MM-DD'), dayjs().format('YYYY-MM-DD')]);
  const [status, setStatus] = useState<string>();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>();
  const [selected, setSelected] = useState<StaffCount>();
  const [editing, setEditing] = useState<CountArea>();
  const [saving, setSaving] = useState(false);
  const [decision, setDecision] = useState('Reviewed');
  const [notes, setNotes] = useState('');
  const [version, setVersion] = useState<string>();
  const [form] = Form.useForm<CountArea>();
  const load = useCallback(async () => {
    setLoading(true); setError(undefined);
    try {
      if (tab === 'pending' || tab === 'history') {
        const response = await api.list({ from: range[0], to: range[1], status: tab === 'pending' ? 'Submitted' : status });
        setCounts(response.data);
      } else if (tab === 'config' && canManage) setCatalog((await api.catalog()).data);
    } catch (e) { setError(formatError(e)); } finally { setLoading(false); }
  }, [tab, range, status, canManage]);
  useEffect(() => { void load(); }, [load]);
  useEffect(() => {
    let active = true;
    fetch('/downloads/inventory-version.json').then(r => r.ok ? r.json() : null)
      .then((v: { versionName?: string; apkUrl?: string } | null) => { if (active && v?.apkUrl === '/downloads/grimorio-inventario.apk') setVersion(v.versionName); })
      .catch(() => undefined);
    return () => { active = false; };
  }, []);
  const edit = (area?: CountArea) => {
    const value = area ?? { id: '', name: '', isActive: true, userIds: [], rules: [] };
    setEditing(value); form.setFieldsValue(value);
  };
  const save = async () => {
    let value: CountArea;
    try { value = await form.validateFields(); } catch { return; }
    setSaving(true);
    try { await api.saveArea({ ...value, id: editing?.id ?? '' }); setEditing(undefined); message.success('Área guardada'); await load(); }
    catch (e) { message.error(formatError(e)); } finally { setSaving(false); }
  };
  const review = async () => {
    if (!selected || !notes.trim()) return;
    setSaving(true);
    try { setSelected((await api.review(selected.id, decision, notes)).data); setNotes(''); message.success('Revisión guardada. Las existencias no se modificaron.'); await load(); }
    catch (e) { message.error(formatError(e)); } finally { setSaving(false); }
  };
  const showDetail = async (row: StaffCount) => {
    try { setSelected((await api.detail(row.id)).data); setNotes(''); setDecision('Reviewed'); }
    catch (e) { message.error(formatError(e)); }
  };
  const countTable = <>
    <Space wrap style={{ marginBottom: 16 }}>
      <DatePicker.RangePicker value={[dayjs(range[0]), dayjs(range[1])]} allowClear={false} onChange={values => {
        if (values?.[0] && values[1]) setRange([values[0].format('YYYY-MM-DD'), values[1].format('YYYY-MM-DD')]);
      }} />
      {tab === 'history' && <Select allowClear placeholder="Todos los estados" value={status} onChange={setStatus} style={{ width: 230 }} options={Object.entries(statuses).map(([value, label]) => ({ value, label }))} />}
    </Space>
    <Table<StaffCount> rowKey="id" loading={loading} dataSource={counts} scroll={{ x: 900 }} pagination={{ pageSize: 20 }}
      columns={[
        { title: 'Fecha del conteo', dataIndex: 'businessDate' }, { title: 'Área', dataIndex: 'areaName' },
        { title: 'Responsable del envío', dataIndex: 'counterName' },
        { title: 'Estado', dataIndex: 'status', render: v => <Tag color={v === 'Submitted' ? 'orange' : undefined}>{statuses[v] ?? v}</Tag> },
        { title: 'Diferencias', render: (_, x) => x.status === 'Draft' ? '—' : x.lines.filter(l => l.difference != null && l.difference !== 0).length },
        { title: 'Enviado', render: (_, x) => x.submittedAt ? formatBranchDateTime(x.submittedAt) : '—' },
        { title: '', render: (_, x) => <Button onClick={() => void showDetail(x)}>Revisar detalle</Button> },
      ]} />
    <Typography.Text type="secondary">Hasta 500 conteos por consulta. Usa el rango de fechas para consultar periodos anteriores.</Typography.Text>
  </>;
  const config = <>
    <Typography.Paragraph>Asigna cada artículo y bodega a un área. Varias personas pueden contar la misma área con su propio PIN. Las cantidades se ingresan en la unidad base del artículo.</Typography.Paragraph>
    <Alert type="info" showIcon title="Acceso por PIN" description="Asigna el permiso «Realizar conteos del personal con PIN» al rol correspondiente y configura su PIN en Usuarios. Luego selecciona aquí las personas autorizadas para cada área." style={{ marginBottom: 16 }} />
    <Button type="primary" icon={<PlusOutlined />} onClick={() => edit()} style={{ marginBottom: 16 }}>Nueva área</Button>
    <Table<CountArea> rowKey="id" dataSource={catalog?.areas} loading={loading} columns={[
      { title: 'Área', dataIndex: 'name' }, { title: 'Responsables', render: (_, x) => x.userIds.map(id => catalog?.users.find(u => u.id === id)?.name ?? 'Usuario inactivo').join(', ') },
      { title: 'Artículos', render: (_, x) => x.rules.length }, { title: 'Estado', render: (_, x) => x.isActive ? 'Activa' : 'Inactiva' },
      { title: '', render: (_, x) => <Button onClick={() => edit(x)}>Editar</Button> },
    ]} />
  </>;
  return <div>
    <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }}>
      <Typography.Title level={4} style={{ margin: 0 }}>Conteos del personal</Typography.Title>
      <Button icon={<ReloadOutlined />} loading={loading} onClick={() => void load()}>Actualizar</Button>
    </Space>
    <Alert type="info" showIcon title="Los conteos no modifican el inventario" description="El personal cuenta antes del cierre y administración revisa por la mañana. Cada saldo de comparación queda guardado al registrar la cantidad. Si se necesita un ajuste, se registra por separado en Movimientos con la autorización correspondiente." style={{ marginBottom: 16 }} />
    {error && <Alert type="error" showIcon title={error} style={{ marginBottom: 16 }} />}
    <Tabs activeKey={tab} onChange={setTab} items={[
      ...(canView ? [{ key: 'pending', label: 'Pendientes de revisión', children: countTable }, { key: 'history', label: 'Historial', children: countTable }] : []),
      ...(canManage ? [{ key: 'config', label: 'Áreas y frecuencias', children: config }] : []),
      { key: 'download', label: 'Descargar app', children: <Card title="Grimorio Inventario · Android">
        <Typography.Paragraph>Ingresa con tu sucursal, usuario y PIN habitual. Verás solamente las áreas que tienes asignadas.</Typography.Paragraph>
        {version ? <Button type="primary" icon={<DownloadOutlined />} href="/downloads/grimorio-inventario.apk" download>Descargar APK · {version}</Button>
          : <Alert type="warning" title="La APK todavía no está publicada en este servidor." />}
        <Typography.Paragraph style={{ marginTop: 16 }}>Abre esta sección desde el teléfono, descarga el archivo e instálalo. Si Android lo solicita, permite la instalación desde el navegador.</Typography.Paragraph>
      </Card> },
    ]} />
    <Drawer open={!!selected} onClose={() => setSelected(undefined)} size="large" title={selected ? `${selected.areaName} · ${selected.businessDate}` : ''}>
      {selected && <>
        <Typography.Paragraph>{selected.counterName} · {statuses[selected.status]}</Typography.Paragraph>
        {selected.recountOfId && <Typography.Paragraph>Reconteo de <Typography.Text copyable>{selected.recountOfId}</Typography.Text></Typography.Paragraph>}
        <Table rowKey="id" dataSource={selected.lines} size="small" scroll={{ x: 800 }} pagination={false} columns={[
          { title: 'Artículo', dataIndex: 'articleName' }, { title: 'Bodega', dataIndex: 'warehouseName' }, { title: 'Unidad', dataIndex: 'unitSymbol' },
          { title: 'Sistema al contar', dataIndex: 'expectedQuantity', render: qty }, { title: 'Contado', dataIndex: 'quantity', render: qty },
          { title: 'Diferencia', dataIndex: 'difference', render: (v: number | null) => <Typography.Text type={v ? 'danger' : undefined}>{qty(v)}</Typography.Text> },
          { title: 'Registrado', dataIndex: 'countedAt', render: v => v ? formatBranchDateTime(v) : '—' }, { title: 'Observación', dataIndex: 'notes' },
        ]} />
        {selected.reviewNotes && <Alert style={{ marginTop: 16 }} type="info" title={selected.reviewNotes} description={`${selected.reviewerName ?? ''} · ${selected.reviewedAt ? formatBranchDateTime(selected.reviewedAt) : ''}`} />}
        {canReview && selected.status === 'Submitted' && <Space orientation="vertical" style={{ width: '100%', marginTop: 24 }}>
          <Select value={decision} onChange={setDecision} style={{ width: '100%' }} options={[
            { value: 'Reviewed', label: 'Marcar como revisado' }, { value: 'Dismissed', label: 'Descartar con motivo' }, { value: 'RecountRequested', label: 'Solicitar nuevo conteo' },
          ]} />
          <Input.TextArea value={notes} onChange={e => setNotes(e.target.value)} maxLength={2000} rows={3} placeholder="Motivo u observación de la revisión (obligatorio)" />
          <Button type="primary" disabled={!notes.trim()} loading={saving} onClick={() => void review()}>Guardar revisión</Button>
        </Space>}
      </>}
    </Drawer>
    <Modal open={!!editing} title={editing?.id ? 'Editar área de conteo' : 'Nueva área de conteo'} width={1000} onCancel={() => setEditing(undefined)} onOk={() => void save()} confirmLoading={saving} destroyOnHidden>
      <Form form={form} layout="vertical" preserve={false}>
        <Form.Item name="name" label="Nombre" rules={[{ required: true, whitespace: true, max: 120 }]}><Input placeholder="Cocina, bar, mesas…" /></Form.Item>
        <Form.Item name="isActive" label="Área activa" valuePropName="checked"><Switch /></Form.Item>
        <Form.Item name="userIds" label="Personas autorizadas" rules={[{ required: true, type: 'array', min: 1 }]}><Select mode="multiple" showSearch optionFilterProp="label" options={catalog?.users.map(u => ({ value: u.id, label: u.name }))} /></Form.Item>
        <Form.List name="rules">{(fields, { add, remove }) => <>
          {fields.map(field => <Card key={field.key} size="small" style={{ marginBottom: 8 }}>
            <Space wrap align="start">
              <Form.Item name={[field.name, 'articleId']} label="Artículo y unidad" rules={[{ required: true }]}><Select showSearch optionFilterProp="label" style={{ width: 260 }} options={catalog?.articles.map(a => ({ value: a.id, label: `${a.name} (${a.unitSymbol})` }))} /></Form.Item>
              <Form.Item name={[field.name, 'warehouseId']} label="Bodega" rules={[{ required: true }]}><Select style={{ width: 180 }} options={catalog?.warehouses.map(w => ({ value: w.id, label: w.name }))} /></Form.Item>
              <Form.Item name={[field.name, 'frequency']} label="Frecuencia" rules={[{ required: true }]}><Select style={{ width: 120 }} options={[{ value: 'Daily', label: 'Diaria' }, { value: 'Weekly', label: 'Semanal' }]} /></Form.Item>
              <Form.Item noStyle shouldUpdate>{() => form.getFieldValue(['rules', field.name, 'frequency']) === 'Weekly'
                ? <Form.Item name={[field.name, 'weekDay']} label="Día semanal" rules={[{ required: true }]}><Select style={{ width: 130 }} options={days.map((label, value) => ({ label, value }))} /></Form.Item>
                : <Form.Item name={[field.name, 'weekDay']} hidden><Input /></Form.Item>}</Form.Item>
              <Button danger onClick={() => remove(field.name)} style={{ marginTop: 30 }}>Quitar</Button>
            </Space>
          </Card>)}
          <Button onClick={() => add({ frequency: 'Daily', weekDay: 1 })} icon={<PlusOutlined />}>Agregar artículo</Button>
        </>}</Form.List>
        <Typography.Paragraph type="secondary" style={{ marginTop: 12 }}>Los artículos semanales siguen pendientes desde el día elegido hasta que se cuenten esa semana. Un artículo de una misma bodega tiene una sola área responsable.</Typography.Paragraph>
      </Form>
    </Modal>
  </div>;
}
