import { useCallback, useEffect, useState } from 'react';
import { App, Button, Col, Empty, Form, Input, InputNumber, Modal, QRCode, Row, Select, Space, Spin, Statistic, Switch, Table, Tag, Tooltip, Typography } from 'antd';
import { CopyOutlined, DeleteOutlined, EditOutlined, PlusOutlined, QrcodeOutlined, ReloadOutlined, SaveOutlined } from '@ant-design/icons';
import { loyaltyApi } from '../../services/loyaltyApi';
import type { LoyaltyDetail, LoyaltyMember, LoyaltyProgram, LoyaltySummary, LoyaltyTier } from '../../services/loyaltyApi';
import { menuApi } from '../../services/api';
import type { MenuItemDto } from '../../types';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import { formatError } from '../../utils/errorHandler';
import { formatBranchDateTime } from '../../utils/branchTimeZone';
import LoyaltyMemberEditor from './LoyaltyMemberEditor';

const status = { Available: ['green', 'Disponible'], Upcoming: ['gold', 'Proxima visita'], Expired: ['default', 'Vencido'] } as const;
const money = (n: number) => `$${n.toFixed(2)}`;

export default function LoyaltyModule({ section }: { section: 'summary' | 'members' | 'config' }) {
  const { message } = App.useApp();
  const { hasPermission } = useAuth();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [search, setSearch] = useState('');
  const [members, setMembers] = useState<LoyaltyMember[]>([]);
  const [summary, setSummary] = useState<LoyaltySummary>();
  const [program, setProgram] = useState<LoyaltyProgram>();
  const [items, setItems] = useState<MenuItemDto[]>([]);
  const [detail, setDetail] = useState<LoyaltyDetail>();
  const [profileEditor, setProfileEditor] = useState<{ member?: LoyaltyMember } | null>(null);
  const [tierIndex, setTierIndex] = useState<number | null>(null);
  const [tierForm] = Form.useForm<LoyaltyTier>();
  const canManage = hasPermission(PERMISSIONS.marketing.loyaltyManage);
  const load = useCallback(async () => {
    setLoading(true);
    try {
      if (section === 'summary') setSummary((await loyaltyApi.summary()).data);
      if (section === 'members') setMembers((await loyaltyApi.members(search)).data);
      if (section === 'config') {
        const [p, i] = await Promise.all([loyaltyApi.program(), menuApi.getItems()]);
        setProgram(p.data); setItems(i.data);
      }
    } catch (error) { message.error(formatError(error)); }
    finally { setLoading(false); }
  }, [section, search, message]);
  useEffect(() => { void load(); }, [load]);
  const openTier = (index: number) => {
    tierForm.resetFields();
    tierForm.setFieldsValue(program?.tiers[index] ?? { visitNumber: (program?.tiers.at(-1)?.visitNumber ?? 0) + 5,
      name: '', kind: 'Bundle', isEnabled: false, products: [] });
    setTierIndex(index);
  };
  const rewardItems = items.filter(i => i.saleVisibility === 'RewardsOnly' && i.isActive);
  const update = (patch: Partial<LoyaltyProgram>) => setProgram(current => current ? { ...current, ...patch } : current);
  const save = async () => {
    if (!program) return;
    setSaving(true);
    try { setProgram((await loyaltyApi.saveProgram(program)).data); message.success('Programa guardado'); }
    catch (error) { message.error(formatError(error)); }
    finally { setSaving(false); }
  };

  return <div>
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12, flexWrap: 'wrap', marginBottom: 20 }}>
      <Typography.Title level={4} style={{ margin: 0 }}>{section === 'summary' ? 'Marketing y fidelizacion' : section === 'members' ? 'Miembros y tarjetas' : 'Programa de fidelizacion'}</Typography.Title>
      <Space>
        <Tooltip title="Actualizar"><Button icon={<ReloadOutlined />} aria-label="Actualizar" loading={loading} onClick={() => void load()} /></Tooltip>
        {section === 'members' && hasPermission(PERMISSIONS.marketing.loyaltyEnroll) && <Button type="primary" icon={<PlusOutlined />}
          onClick={() => setProfileEditor({})}>Inscribir miembro</Button>}
        {section === 'config' && canManage && <Button type="primary" icon={<SaveOutlined />} loading={saving} onClick={() => void save()}>Guardar</Button>}
      </Space>
    </div>
    <Spin spinning={loading}>
      {section === 'summary' && summary && <Row gutter={[24, 24]}>
        {([['Miembros', summary.members], ['Miembros activos', summary.activeMembers], ['Visitas registradas', summary.visits], ['Premios disponibles', summary.availableRewards], ['Premios vencidos', summary.expiredRewards]] as const)
          .map(([title, value]) => <Col xs={12} md={8} lg={4} key={title}><Statistic title={title} value={value} /></Col>)}
      </Row>}
      {section === 'members' && <>
        <Input.Search placeholder="Telefono, nombre o codigo de tarjeta" allowClear onSearch={setSearch} style={{ width: 360, maxWidth: '100%', marginBottom: 16 }} />
        <Table rowKey="id" dataSource={members} pagination={{ pageSize: 10 }} scroll={{ x: 900 }} columns={[
          { title: 'Cliente', dataIndex: 'customerName' }, { title: 'Tarjeta', dataIndex: 'cardCode' },
          { title: 'Telefono', dataIndex: 'phone', render: phone => phone || <Tag>Sin telefono</Tag> },
          { title: 'Facturacion', render: (_, m) => <Tag>{m.customerId ? 'Vinculada' : 'Sin vincular'}</Tag> },
          { title: 'Visitas', dataIndex: 'visits', width: 80 }, { title: 'Premios disponibles', dataIndex: 'rewards', width: 150 },
          { title: 'Estado', render: (_, m) => <Tag color={m.isActive ? 'green' : 'default'}>{m.isActive ? 'Activo' : 'Inactivo'}</Tag> },
          { title: 'Acciones', width: 100, render: (_, m) => <Space size={4}>
            <Tooltip title="Ver tarjeta e historial"><Button icon={<QrcodeOutlined />} aria-label={`Ver tarjeta de ${m.customerName}`}
              onClick={() => { void loyaltyApi.member(m.id).then(r => setDetail(r.data)).catch(e => message.error(formatError(e))); }} /></Tooltip>
            {hasPermission(PERMISSIONS.marketing.loyaltyEnroll) && <Tooltip title="Actualizar ficha"><Button icon={<EditOutlined />} aria-label={`Actualizar ficha de ${m.customerName}`}
              onClick={() => setProfileEditor({ member: m })} /></Tooltip>}
          </Space> },
        ]} />
      </>}
      {section === 'config' && program && <Form layout="vertical" disabled={!canManage || saving}>
        <Row gutter={16}>
          <Col xs={24} md={12}><Form.Item label="Nombre del programa"><Input value={program.name} maxLength={120} onChange={e => update({ name: e.target.value })} /></Form.Item></Col>
          <Col xs={12} md={6}><Form.Item label="Programa activo"><Switch checked={program.isActive} onChange={isActive => update({ isActive })} /></Form.Item></Col>
          <Col xs={12} md={6}><Form.Item label="Vigencia del premio (dias)"><InputNumber min={1} max={365} value={program.rewardValidityDays} onChange={v => update({ rewardValidityDays: v ?? 30 })} /></Form.Item></Col>
          <Col xs={12} md={6}><Form.Item label="Consumo minimo por pedido"><InputNumber prefix="$" min={.01} precision={2} value={program.minimumSpend} onChange={v => update({ minimumSpend: v ?? 10 })} /></Form.Item></Col>
          <Col xs={12} md={6}><Form.Item label="Incluir IVA en el minimo"><Switch checked={program.includeTax} onChange={includeTax => update({ includeTax })} /></Form.Item></Col>
          <Col xs={24} md={12}><Form.Item label="Productos que no acumulan"><Select mode="multiple" showSearch optionFilterProp="label" value={program.excludedItemIds}
            options={items.filter(i => i.saleVisibility !== 'RewardsOnly').map(i => ({ value: i.id, label: i.name }))} onChange={excludedItemIds => update({ excludedItemIds })} /></Form.Item></Col>
        </Row>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
          <Typography.Title level={5} style={{ margin: 0 }}>Escalera de recompensas</Typography.Title>
          <Button icon={<PlusOutlined />} onClick={() => openTier(program.tiers.length)}>Agregar hito</Button>
        </div>
        <Table rowKey="visitNumber" dataSource={program.tiers} pagination={false} scroll={{ x: 720 }} columns={[
          { title: 'Visitas', render: (_, t) => <span>{t.visitNumber}{t.repeatEvery ? ` y cada ${t.repeatEvery}` : ''}</span>, width: 130 },
          { title: 'Premio', dataIndex: 'name' },
          { title: 'Productos', render: (_, t) => t.products.length ? t.products.map(p => `${p.quantity} x ${p.name}`).join(', ') : <Tag>Sin productos</Tag> },
          { title: 'Tipo', render: (_, t) => t.kind === 'Bundle' ? 'Todos los productos' : 'Uno a elegir' },
          { title: 'Habilitado', render: (_, t, index) => <Switch checked={t.isEnabled} disabled={!canManage || !t.products.length}
            onChange={isEnabled => update({ tiers: program.tiers.map((x, n) => n === index ? { ...x, isEnabled } : x) })} /> },
          { title: 'Acciones', width: 100, render: (_, t, index) => <Space size={4}>
            <Tooltip title="Editar hito"><Button size="small" icon={<EditOutlined />} aria-label={`Editar hito ${t.visitNumber}`} onClick={() => openTier(index)} /></Tooltip>
            <Tooltip title="Eliminar hito"><Button size="small" danger icon={<DeleteOutlined />} aria-label={`Eliminar hito ${t.visitNumber}`}
              onClick={() => update({ tiers: program.tiers.filter((_, n) => n !== index) })} /></Tooltip>
          </Space> },
        ]} />
      </Form>}
    </Spin>
    {profileEditor && <LoyaltyMemberEditor member={profileEditor.member} onClose={() => setProfileEditor(null)}
      onSaved={saved => { setProfileEditor(null); setDetail(saved); void load(); }} />}
    <Modal title="Hito de recompensas" forceRender open={tierIndex !== null} onCancel={() => setTierIndex(null)} okText="Aplicar" cancelText="Cancelar"
      onOk={() => { void tierForm.validateFields().then(() => {
        if (!program || tierIndex === null) return;
        const value = tierForm.getFieldsValue(true) as LoyaltyTier;
        const tiers = [...program.tiers]; tiers[tierIndex] = value; update({ tiers: tiers.sort((a, b) => a.visitNumber - b.visitNumber) }); setTierIndex(null);
      }).catch(() => undefined); }}>
      <Form name="loyalty-tier" form={tierForm} layout="vertical">
        <Row gutter={12}>
          <Col span={12}><Form.Item name="visitNumber" label="Numero de visita" rules={[{ required: true }]}><InputNumber min={1} max={10000} style={{ width: '100%' }} /></Form.Item></Col>
          <Col span={12}><Form.Item name="repeatEvery" label="Repetir cada (visitas)"><InputNumber min={1} max={1000} style={{ width: '100%' }} /></Form.Item></Col>
        </Row>
        <Form.Item name="name" label="Nombre del premio" rules={[{ required: true }, { max: 120 }]}><Input maxLength={120} /></Form.Item>
        <Form.Item name="kind" label="Entrega" rules={[{ required: true }]}><Select options={[{ value: 'Bundle', label: 'Todos los productos' }, { value: 'Choice', label: 'Un producto a elegir' }]} /></Form.Item>
        <Form.Item name="maximumPrice" label="Precio maximo del producto ($)"><InputNumber min={.01} precision={2} /></Form.Item>
        <Form.Item label="Productos de Recompensas">
          <Form.Item noStyle shouldUpdate>{({ getFieldValue, setFieldValue }) => <Select mode="multiple" showSearch optionFilterProp="label"
            style={{ width: '100%' }} value={(getFieldValue('products') ?? []).map((p: { menuItemId: string }) => p.menuItemId)}
            options={rewardItems.map(i => ({ value: i.id, label: `${i.name} · ${money(i.price)}` }))}
            onChange={(ids: string[]) => setFieldValue('products', ids.map(id => ({ menuItemId: id, name: rewardItems.find(i => i.id === id)?.name ?? '',
              quantity: (getFieldValue('products') ?? []).find((p: { menuItemId: string }) => p.menuItemId === id)?.quantity ?? 1 })))} />}</Form.Item>
        </Form.Item>
        <Form.Item noStyle shouldUpdate>{({ getFieldValue, setFieldValue }) => (getFieldValue('products') ?? []).map((p: { menuItemId: string; name: string; quantity: number }, index: number) =>
          <div key={p.menuItemId} style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 8 }}><span style={{ flex: 1 }}>{p.name}</span>
            <InputNumber aria-label={`Cantidad de ${p.name}`} min={1} max={20} precision={0} value={p.quantity} onChange={value => setFieldValue('products',
              getFieldValue('products').map((x: typeof p, n: number) => n === index ? { ...x, quantity: value ?? 1 } : x))} /></div>)}</Form.Item>
        <Form.Item name="isEnabled" label="Habilitado" valuePropName="checked"><Switch /></Form.Item>
      </Form>
    </Modal>
    <Modal title={detail?.member.customerName ?? 'Tarjeta'} open={!!detail} onCancel={() => setDetail(undefined)} footer={null} width={640}>
      {detail && <>
        <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 20, marginBottom: 20 }}>
          <QRCode value={detail.member.cardCode} size={140} />
          <div><Typography.Title level={4}>{detail.member.visits} visitas</Typography.Title><div>{detail.member.cardCode}</div>
            <Input aria-label="Enlace privado de la tarjeta" readOnly style={{ marginTop: 12, maxWidth: 340 }}
              value={`${window.location.origin}/fidelizacion/${detail.publicToken}`} onFocus={e => e.target.select()} />
            <Button icon={<CopyOutlined />} style={{ marginTop: 12 }} onClick={() => {
              if (!navigator.clipboard) { message.info('Selecciona el enlace de la tarjeta para copiarlo'); return; }
              void navigator.clipboard.writeText(`${window.location.origin}/fidelizacion/${detail.publicToken}`)
              .then(() => message.success('Enlace copiado')).catch(() => message.error('No se pudo copiar el enlace')); }}>Copiar enlace de tarjeta</Button></div>
        </div>
        <Row gutter={[16, 8]} style={{ marginBottom: 16 }}>
          <Col xs={24} sm={12}><strong>Telefono:</strong> {detail.member.phone || 'Pendiente'}</Col>
          <Col xs={24} sm={12}><strong>Correo:</strong> <span style={{ overflowWrap: 'anywhere' }}>{detail.member.email || 'No registrado'}</span></Col>
          <Col xs={24} sm={12}><strong>Cumpleanos:</strong> {detail.member.birthDay && detail.member.birthMonth ? `${detail.member.birthDay}/${detail.member.birthMonth}` : 'No registrado'}</Col>
          <Col xs={24} sm={12}><strong>Facturacion:</strong> {detail.member.customerId ? 'Vinculada' : 'Sin vincular'}</Col>
          <Col span={24}><strong>Comunicaciones comerciales:</strong> {detail.member.marketingConsent ? 'Autorizadas' : 'No autorizadas'}</Col>
        </Row>
        <Typography.Title level={5}>Premios</Typography.Title>
        {detail.rewards.length === 0 ? <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Sin premios emitidos" /> : detail.rewards.map(r =>
          <div key={r.id} style={{ padding: '10px 0', borderBottom: '1px solid #eee' }}><strong>{r.name}</strong> <Tag color={status[r.status][0]}>{status[r.status][1]}</Tag>
            <div>Visita {r.visitNumber} · Vence {formatBranchDateTime(r.expiresAt)}</div></div>)}
        <Typography.Title level={5}>Visitas</Typography.Title>
        <Table size="small" rowKey="id" dataSource={detail.visits} pagination={{ pageSize: 5 }} columns={[
          { title: 'Fecha', dataIndex: 'businessDate' }, { title: 'Consumo valido', render: (_, v) => money(v.qualifiedSpend) },
          { title: 'Minimo aplicado', render: (_, v) => money(v.minimumSpend) },
        ]} />
      </>}
    </Modal>
  </div>;
}
