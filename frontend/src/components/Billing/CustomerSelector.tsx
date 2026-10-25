import { useState, useEffect, useRef } from 'react';
import { App as AntApp, Select, Button, Modal, Form, Input, Space, Row, Col } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import type { CustomerDto, CreateCustomerDto } from '../../types';
import { customersApi } from '../../services/api';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import CustomerIdentificationFields from './CustomerIdentificationFields';
import { formatError } from '../../utils/errorHandler';

interface Props {
  branchId: string;
  value: CustomerDto | null;
  onChange: (customer: CustomerDto | null) => void;
  allowCreate?: boolean;
  excludeFinalConsumer?: boolean;
  ariaLabel?: string;
}

export default function CustomerSelector({ value, onChange, allowCreate = true, excludeFinalConsumer = false, ariaLabel }: Props) {
  const { message } = AntApp.useApp();

  const { hasPermission } = useAuth();
  const [customers, setCustomers] = useState<CustomerDto[]>([]);
  const [search, setSearch] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [createForm] = Form.useForm();
  const [saving, setSaving] = useState(false);
  const savingRequest = useRef(false);
  const canManageCustomers = allowCreate && hasPermission(PERMISSIONS.billing.customersManage);
  const selectableCustomers = [...(value && !customers.some(c => c.id === value.id) ? [value] : []), ...customers]
    .filter(c => !excludeFinalConsumer || c.taxIdType !== 'FinalConsumer');

  useEffect(() => {
    customersApi.getAll({ activeOnly: true, search: search || undefined })
      .then(r => setCustomers(r.data))
      .catch(() => {});
  }, [search]);

  const handleCreate = async () => {
    if (savingRequest.current) return;
    savingRequest.current = true;
    setSaving(true);
    let values;
    try { values = await createForm.validateFields(); } catch { savingRequest.current = false; setSaving(false); return; }
    try {
      const dto: CreateCustomerDto = { ...values };
      const r = await customersApi.create(dto);
      setCustomers(prev => [r.data, ...prev]);
      onChange(r.data);
      setShowCreate(false);
      createForm.resetFields();
      message.success('Cliente creado');
    } catch (error) {
      message.error(formatError(error));
    } finally {
      savingRequest.current = false;
      setSaving(false);
    }
  };

  return (
    <>
      <Space.Compact style={{ width: '100%' }}>
        <Select
          aria-label={ariaLabel}
          allowClear
          showSearch
          style={{ flex: 1, minWidth: 0 }}
          placeholder="Buscar por nombre o RUC/cédula..."
          value={value?.id ?? null}
          filterOption={false}
          onSearch={setSearch}
          onChange={id => onChange(selectableCustomers.find(c => c.id === id) ?? null)}
          options={selectableCustomers.map(c => ({
            value: c.id,
            label: `${c.name}${c.taxId ? ` — ${c.taxId}` : ''}`,
          }))}
          notFoundContent={canManageCustomers ? (
            <Button type="link" icon={<PlusOutlined />} onClick={() => setShowCreate(true)}>
              Crear cliente
            </Button>
          ) : null}
        />
        {canManageCustomers && <Button icon={<PlusOutlined />} onClick={() => setShowCreate(true)} title="Nuevo cliente" />}
      </Space.Compact>

      <Modal
        title="Nuevo cliente"
        open={showCreate}
        onCancel={() => { if (!saving) setShowCreate(false); }}
        onOk={handleCreate}
        confirmLoading={saving}
        closable={!saving}
        maskClosable={!saving}
        okText="Guardar"
        cancelText="Cancelar"
        width={720}
        style={{ top: 24 }}
      >
        <Form form={createForm} layout="vertical" disabled={saving} initialValues={{ taxIdType: 'FinalConsumer' }}>
          <Row gutter={16}>
            <CustomerIdentificationFields />
            <Col xs={24} sm={12}>
              <Form.Item name="name" label="Nombre / Razón social" rules={[{ required: true }]}>
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="address" label="Dirección">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="phone" label="Teléfono">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="email" label="Email">
                <Input type="email" />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Modal>
    </>
  );
}
