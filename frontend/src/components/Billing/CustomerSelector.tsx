import { useState, useEffect } from 'react';
import { Select, Button, Modal, Form, Input, Space, message } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import type { CustomerDto, CreateCustomerDto } from '../../types';
import { customersApi } from '../../services/api';

const TAX_ID_TYPES = [
  { value: 'Cedula', label: 'Cédula' },
  { value: 'Ruc', label: 'RUC' },
  { value: 'Passport', label: 'Pasaporte' },
  { value: 'FinalConsumer', label: 'Consumidor final' },
];

interface Props {
  branchId: string;
  value: CustomerDto | null;
  onChange: (customer: CustomerDto | null) => void;
}

export default function CustomerSelector({ value, onChange }: Props) {
  const [customers, setCustomers] = useState<CustomerDto[]>([]);
  const [search, setSearch] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [createForm] = Form.useForm();
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    customersApi.getAll({ activeOnly: true, search: search || undefined })
      .then(r => setCustomers(r.data))
      .catch(() => {});
  }, [search]);

  const handleCreate = async () => {
    const values = await createForm.validateFields();
    setSaving(true);
    try {
      const dto: CreateCustomerDto = { ...values };
      const r = await customersApi.create(dto);
      setCustomers(prev => [r.data, ...prev]);
      onChange(r.data);
      setShowCreate(false);
      createForm.resetFields();
      message.success('Cliente creado');
    } catch {
      message.error('Error al crear el cliente');
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <Space.Compact style={{ width: '100%' }}>
        <Select
          allowClear
          showSearch
          style={{ flex: 1 }}
          placeholder="Buscar por nombre o RUC/cédula..."
          value={value?.id ?? null}
          filterOption={false}
          onSearch={setSearch}
          onChange={id => onChange(customers.find(c => c.id === id) ?? null)}
          options={customers.map(c => ({
            value: c.id,
            label: `${c.name}${c.taxId ? ` — ${c.taxId}` : ''}`,
          }))}
          notFoundContent={
            <Button type="link" icon={<PlusOutlined />} onClick={() => setShowCreate(true)}>
              Crear cliente
            </Button>
          }
        />
        <Button icon={<PlusOutlined />} onClick={() => setShowCreate(true)} title="Nuevo cliente" />
      </Space.Compact>

      <Modal
        title="Nuevo cliente"
        open={showCreate}
        onCancel={() => setShowCreate(false)}
        onOk={handleCreate}
        confirmLoading={saving}
        okText="Guardar"
        cancelText="Cancelar"
        width={480}
      >
        <Form form={createForm} layout="vertical" initialValues={{ taxIdType: 'FinalConsumer' }}>
          <Form.Item name="name" label="Nombre / Razón social" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="taxIdType" label="Tipo de identificación">
            <Select options={TAX_ID_TYPES} />
          </Form.Item>
          <Form.Item name="taxId" label="RUC / Cédula">
            <Input placeholder="0000000000" />
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
        </Form>
      </Modal>
    </>
  );
}
