import { useEffect, useState } from 'react';
import { Table, Button, Space, Tag, Modal, Form, Input, Select, Popconfirm, message, Typography } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { CustomerDto, CreateCustomerDto, UpdateCustomerDto } from '../../types';
import { customersApi } from '../../services/api';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Title } = Typography;

const TAX_ID_TYPES = [
  { value: 'Cedula', label: 'Cédula' },
  { value: 'Ruc', label: 'RUC' },
  { value: 'Passport', label: 'Pasaporte' },
  { value: 'FinalConsumer', label: 'Consumidor final' },
];

const TAX_LABELS: Record<string, string> = {
  Cedula: 'Cédula', Ruc: 'RUC', Passport: 'Pasaporte', FinalConsumer: 'Cons. final',
};

export default function CustomersList() {
  const { hasPermission } = useAuth();
  const [customers, setCustomers] = useState<CustomerDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<CustomerDto | null>(null);
  const [form] = Form.useForm();
  const [saving, setSaving] = useState(false);
  const canManage = hasPermission(PERMISSIONS.billing.customersManage);

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
    const values = await form.validateFields();
    setSaving(true);
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
    } catch {
      message.error('Error al guardar');
    } finally {
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

      <Table
        size="small"
        loading={loading}
        dataSource={customers}
        rowKey="id"
        pagination={{ defaultPageSize: 20, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
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
          ...(canManage ? [{
            title: '', width: 80,
            render: (_: unknown, r: CustomerDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
                <Popconfirm title="¿Eliminar cliente?" onConfirm={() => handleDelete(r.id)}>
                  <Button size="small" icon={<DeleteOutlined />} danger />
                </Popconfirm>
              </Space>
            ),
          }] : []),
        ]}
      />

      <Modal
        title={editing ? 'Editar cliente' : 'Nuevo cliente'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={handleSave}
        confirmLoading={saving}
        okText="Guardar"
        cancelText="Cancelar"
        width={480}
      >
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="Nombre / Razón social" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="taxIdType" label="Tipo de identificación">
            <Select options={TAX_ID_TYPES} />
          </Form.Item>
          <Form.Item name="taxId" label="RUC / Cédula">
            <Input />
          </Form.Item>
          <Form.Item name="address" label="Dirección">
            <Input />
          </Form.Item>
          <Form.Item name="phone" label="Teléfono">
            <Input />
          </Form.Item>
          <Form.Item name="email" label="Email">
            <Input type="email" />
          </Form.Item>
          {editing && (
            <Form.Item name="isActive" label="Estado">
              <Select options={[{ value: true, label: 'Activo' }, { value: false, label: 'Inactivo' }]} />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  );
}
