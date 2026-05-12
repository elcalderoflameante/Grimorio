import { useEffect, useState } from 'react';
import {
  Table, Button, Modal, Form, Input, Switch, InputNumber,
  Space, Tag, Popconfirm, Typography, message, ColorPicker,
} from 'antd';
import type { AggregationColor } from 'antd/es/color-picker/color';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { PaymentMethodConfigDto, CreatePaymentMethodConfigDto, UpdatePaymentMethodConfigDto } from '../../types';
import { paymentMethodsApi } from '../../services/api';

const { Title, Text } = Typography;

const DEFAULT_COLORS = ['#52c41a', '#1677ff', '#722ed1', '#13c2c2', '#fa8c16', '#eb2f96', '#f5222d', '#faad14'];

export default function PaymentMethodsSettings() {
  const [methods, setMethods] = useState<PaymentMethodConfigDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<PaymentMethodConfigDto | null>(null);
  const [saving, setSaving] = useState(false);
  const [form] = Form.useForm();

  const load = async () => {
    setLoading(true);
    try {
      const r = await paymentMethodsApi.getAll(false);
      setMethods(r.data);
    } catch {
      message.error('Error al cargar medios de pago');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({ color: '#1677ff', isCash: false, isActive: true, sortOrder: methods.length + 1 });
    setModalOpen(true);
  };

  const openEdit = (m: PaymentMethodConfigDto) => {
    setEditing(m);
    form.setFieldsValue({ name: m.name, color: m.color, isCash: m.isCash, isActive: m.isActive, sortOrder: m.sortOrder });
    setModalOpen(true);
  };

  const handleSave = async () => {
    const values = await form.validateFields();
    const color = typeof values.color === 'string'
      ? values.color
      : (values.color as AggregationColor).toHexString();

    setSaving(true);
    try {
      if (editing) {
        const dto: UpdatePaymentMethodConfigDto = {
          name: values.name, color, isCash: values.isCash,
          isActive: values.isActive, sortOrder: values.sortOrder,
        };
        const r = await paymentMethodsApi.update(editing.id, dto);
        setMethods(prev => prev.map(m => m.id === editing.id ? r.data : m));
        message.success('Método actualizado');
      } else {
        const dto: CreatePaymentMethodConfigDto = {
          name: values.name, color, isCash: values.isCash, sortOrder: values.sortOrder,
        };
        const r = await paymentMethodsApi.create(dto);
        setMethods(prev => [...prev, r.data]);
        message.success('Método creado');
      }
      setModalOpen(false);
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'Error al guardar');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await paymentMethodsApi.remove(id);
      setMethods(prev => prev.filter(m => m.id !== id));
      message.success('Método eliminado');
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'No se pudo eliminar');
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Medios de pago</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Nuevo método</Button>
      </div>

      <Table
        dataSource={methods}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={false}
        columns={[
          {
            title: 'Nombre', key: 'name',
            render: (_, m) => (
              <Space>
                <span style={{ display: 'inline-block', width: 14, height: 14, borderRadius: '50%', background: m.color, border: '1px solid #d9d9d9' }} />
                <Text strong>{m.name}</Text>
                {m.isCash && <Tag color="green">Efectivo</Tag>}
              </Space>
            ),
          },
          {
            title: 'Estado', key: 'status', width: 110,
            render: (_, m) => <Tag color={m.isActive ? 'success' : 'default'}>{m.isActive ? 'Activo' : 'Inactivo'}</Tag>,
          },
          { title: 'Orden', dataIndex: 'sortOrder', key: 'sortOrder', width: 80, align: 'center' },
          {
            title: '', key: 'actions', width: 90,
            render: (_, m) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(m)} />
                <Popconfirm
                  title="¿Eliminar este método?"
                  description="Solo es posible si no tiene cobros registrados."
                  onConfirm={() => handleDelete(m.id)}
                  okText="Eliminar"
                  cancelText="Cancelar"
                  okButtonProps={{ danger: true }}
                >
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Editar medio de pago' : 'Nuevo medio de pago'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={handleSave}
        confirmLoading={saving}
        okText="Guardar"
        cancelText="Cancelar"
        destroyOnHidden
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="name" label="Nombre" rules={[{ required: true, message: 'Ingresa un nombre' }]}>
            <Input placeholder="Ej: Efectivo, Tarjeta, Vale corporativo..." maxLength={64} />
          </Form.Item>

          <Form.Item name="color" label="Color" rules={[{ required: true }]}>
            <ColorPicker
              presets={[{ label: 'Sugeridos', colors: DEFAULT_COLORS }]}
              showText
              format="hex"
            />
          </Form.Item>

          <div style={{ display: 'flex', gap: 24 }}>
            <Form.Item name="isCash" label="Es efectivo" valuePropName="checked" tooltip="Marca si este método puede generar vuelto">
              <Switch />
            </Form.Item>
            {editing && (
              <Form.Item name="isActive" label="Activo" valuePropName="checked">
                <Switch />
              </Form.Item>
            )}
            <Form.Item name="sortOrder" label="Orden" rules={[{ required: true }]}>
              <InputNumber min={1} style={{ width: 80 }} />
            </Form.Item>
          </div>
        </Form>
      </Modal>
    </div>
  );
}
