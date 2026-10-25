import { useEffect, useMemo, useRef, useState } from 'react';
import { App as AntApp, Table, Button, Space, Tag, Modal, Form, Input, Select, Popconfirm, Typography, Row, Col, Tooltip } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, ShoppingOutlined } from '@ant-design/icons';
import type { CustomerDto, CreateCustomerDto, UpdateCustomerDto } from '../../types';
import { customersApi } from '../../services/api';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import CustomerIdentificationFields from './CustomerIdentificationFields';
import { formatError } from '../../utils/errorHandler';
import CustomerSalesModal from './CustomerSalesModal';

const { Title } = Typography;

const TAX_LABELS: Record<string, string> = {
  Cedula: 'Cédula', Ruc: 'RUC', Passport: 'Pasaporte', FinalConsumer: 'Cons. final',
};

export default function CustomersList() {
  const { message } = AntApp.useApp();

  const { hasPermission } = useAuth();
  const [customers, setCustomers] = useState<CustomerDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<CustomerDto | null>(null);
  const [form] = Form.useForm();
  const [saving, setSaving] = useState(false);
  const savingRequest = useRef(false);
  const canManage = hasPermission(PERMISSIONS.billing.customersManage);
  const canViewSales = hasPermission(PERMISSIONS.billing.cashView);
  const [search, setSearch] = useState('');
  const [customerPage, setCustomerPage] = useState(1);
  const [salesCustomer, setSalesCustomer] = useState<CustomerDto | null>(null);
  const filteredCustomers = useMemo(() => {
    const normalize = (value: string) => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
    const term = normalize(search.trim());
    return customers.filter(c => !term || [c.name, c.taxId, c.phone, c.email].some(value => value && normalize(value).includes(term)));
  }, [customers, search]);

  const load = async () => {
    setLoading(true);
    try {
      const r = await customersApi.getAll();
      setCustomers(r.data);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    form.setFieldValue('taxIdType', 'FinalConsumer');
    setModalOpen(true);
  };

  const openEdit = (c: CustomerDto) => {
    setEditing(c);
    form.setFieldsValue({ ...c });
    setModalOpen(true);
  };

  const handleSave = async () => {
    if (savingRequest.current) return;
    savingRequest.current = true;
    setSaving(true);
    let values;
    try { values = await form.validateFields(); } catch { savingRequest.current = false; setSaving(false); return; }
    try {
      if (editing) {
        const dto: UpdateCustomerDto = { ...values };
        const r = await customersApi.update(editing.id, dto);
        setCustomers(prev => prev.map(c => c.id === editing.id ? r.data : c));
        message.success('Cliente actualizado');
      } else {
        const dto: CreateCustomerDto = { ...values };
        const r = await customersApi.create(dto);
        setCustomers(prev => [r.data, ...prev]);
        message.success('Cliente creado');
      }
      setModalOpen(false);
    } catch (error) {
      message.error(formatError(error));
    } finally {
      savingRequest.current = false;
      setSaving(false);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await customersApi.delete(id);
      setCustomers(prev => prev.filter(c => c.id !== id));
      message.success('Cliente eliminado');
    } catch {
      message.error('Error al eliminar');
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Clientes</Title>
        {canManage && <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Nuevo</Button>}
      </div>

      <Input.Search allowClear aria-label="Buscar clientes" placeholder="Nombre, identificacion, telefono o correo"
        value={search} onChange={e => { setSearch(e.target.value); setCustomerPage(1); }} style={{ width: 420, maxWidth: '100%', marginBottom: 16 }} />

      <Table
        size="small"
        scroll={{ x: 950 }}
        loading={loading}
        dataSource={filteredCustomers}
        rowKey="id"
        pagination={{ current: customerPage, onChange: setCustomerPage, defaultPageSize: 20, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
        columns={[
          { title: 'Nombre', dataIndex: 'name', sorter: (a, b) => a.name.localeCompare(b.name) },
          { title: 'Tipo ID', dataIndex: 'taxIdType', width: 120, render: t => TAX_LABELS[t] ?? t },
          { title: 'RUC / Cédula', dataIndex: 'taxId', width: 150 },
          { title: 'Teléfono', dataIndex: 'phone', width: 130 },
          { title: 'Email', dataIndex: 'email' },
          {
            title: 'Estado', dataIndex: 'isActive', width: 90,
            render: v => <Tag color={v ? 'green' : 'default'}>{v ? 'Activo' : 'Inactivo'}</Tag>,
          },
          ...(canManage || canViewSales ? [{
            title: 'Acciones', width: canManage && canViewSales ? 120 : 85, fixed: 'right' as const,
            render: (_: unknown, r: CustomerDto) => (
              <Space>
                {canViewSales && <Tooltip title="Ver ventas del cliente"><Button size="small" icon={<ShoppingOutlined />}
                  aria-label={`Ver ventas de ${r.name}`} onClick={() => setSalesCustomer(r)} /></Tooltip>}
                {canManage && <><Tooltip title="Editar cliente"><Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} /></Tooltip>
                <Popconfirm title="¿Eliminar cliente?" onConfirm={() => handleDelete(r.id)}>
                  <Button size="small" icon={<DeleteOutlined />} danger />
                </Popconfirm>
                </>}
              </Space>
            ),
          }] : []),
        ]}
      />

      {salesCustomer && <CustomerSalesModal key={salesCustomer.id} customer={salesCustomer} onClose={() => setSalesCustomer(null)} />}

      <Modal
        title={editing ? 'Editar cliente' : 'Nuevo cliente'}
        open={modalOpen}
        onCancel={() => { if (!saving) setModalOpen(false); }}
        onOk={handleSave}
        confirmLoading={saving}
        closable={!saving}
        maskClosable={!saving}
        okText="Guardar"
        cancelText="Cancelar"
        width={760}
        style={{ top: 32 }}
      >
        <Form form={form} layout="vertical" disabled={saving}>
          <Form.Item name="name" label="Nombre / Razón social" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Row gutter={16}>
            <CustomerIdentificationFields customerId={editing?.id} />
            <Col xs={24} md={12}>
              <Form.Item name="address" label="Dirección">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="phone" label="Teléfono">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} md={editing ? 12 : 24}>
              <Form.Item name="email" label="Email">
                <Input type="email" />
              </Form.Item>
            </Col>
            {editing && (
              <Col xs={24} md={12}>
                <Form.Item name="isActive" label="Estado">
                  <Select options={[{ value: true, label: 'Activo' }, { value: false, label: 'Inactivo' }]} />
                </Form.Item>
              </Col>
            )}
          </Row>
        </Form>
      </Modal>
    </div>
  );
}
