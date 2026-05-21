import { useEffect, useState } from 'react';
import {
  Table, Button, Modal, Form, Input, Switch, InputNumber,
  Space, Tag, Popconfirm, Typography, message, ColorPicker, Divider,
} from 'antd';
import type { AggregationColor } from 'antd/es/color-picker/color';
import { PlusOutlined, EditOutlined, DeleteOutlined, BankOutlined } from '@ant-design/icons';
import type {
  PaymentMethodConfigDto, CreatePaymentMethodConfigDto, UpdatePaymentMethodConfigDto,
  CardBankDto, CreateCardBankDto, UpdateCardBankDto,
} from '../../types';
import { paymentMethodsApi } from '../../services/api';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Title, Text } = Typography;

const DEFAULT_COLORS = ['#52c41a', '#1677ff', '#722ed1', '#13c2c2', '#fa8c16', '#eb2f96', '#f5222d', '#faad14'];

export default function PaymentMethodsSettings() {
  const { hasPermission } = useAuth();
  const [methods, setMethods] = useState<PaymentMethodConfigDto[]>([]);
  const [banks, setBanks] = useState<CardBankDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [bankLoading, setBankLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [bankModalOpen, setBankModalOpen] = useState(false);
  const [editing, setEditing] = useState<PaymentMethodConfigDto | null>(null);
  const [editingBank, setEditingBank] = useState<CardBankDto | null>(null);
  const [saving, setSaving] = useState(false);
  const [bankSaving, setBankSaving] = useState(false);
  const [form] = Form.useForm();
  const [bankForm] = Form.useForm();
  const canManage = hasPermission(PERMISSIONS.billing.paymentMethodsManage);

  const loadMethods = async () => {
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

  const loadBanks = async () => {
    setBankLoading(true);
    try {
      const r = await paymentMethodsApi.getCardBanks(false);
      setBanks(r.data);
    } catch {
      message.error('Error al cargar bancos');
    } finally {
      setBankLoading(false);
    }
  };

  useEffect(() => { loadMethods(); loadBanks(); }, []);

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({ color: '#1677ff', isCash: false, isCard: false, isActive: true, sortOrder: methods.length + 1 });
    setModalOpen(true);
  };

  const openEdit = (m: PaymentMethodConfigDto) => {
    setEditing(m);
    form.setFieldsValue({
      name: m.name, color: m.color, isCash: m.isCash,
      isCard: m.isCard, isActive: m.isActive, sortOrder: m.sortOrder,
    });
    setModalOpen(true);
  };

  const openCreateBank = () => {
    setEditingBank(null);
    bankForm.resetFields();
    bankForm.setFieldsValue({ name: '', isActive: true, sortOrder: banks.length + 1 });
    setBankModalOpen(true);
  };

  const openEditBank = (bank: CardBankDto) => {
    setEditingBank(bank);
    bankForm.setFieldsValue({ name: bank.name, isActive: bank.isActive, sortOrder: bank.sortOrder });
    setBankModalOpen(true);
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
          name: values.name, color,
          isCash: !!values.isCash, isCard: !!values.isCard,
          isActive: values.isActive, sortOrder: values.sortOrder,
        };
        const r = await paymentMethodsApi.update(editing.id, dto);
        setMethods(prev => prev.map(m => m.id === editing.id ? r.data : m));
        message.success('Metodo actualizado');
      } else {
        const dto: CreatePaymentMethodConfigDto = {
          name: values.name, color,
          isCash: !!values.isCash, isCard: !!values.isCard,
          sortOrder: values.sortOrder,
        };
        const r = await paymentMethodsApi.create(dto);
        setMethods(prev => [...prev, r.data]);
        message.success('Metodo creado');
      }
      setModalOpen(false);
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'Error al guardar');
    } finally {
      setSaving(false);
    }
  };

  const handleSaveBank = async () => {
    const values = await bankForm.validateFields();
    setBankSaving(true);
    try {
      if (editingBank) {
        const dto: UpdateCardBankDto = {
          name: values.name, isActive: values.isActive, sortOrder: values.sortOrder,
        };
        const r = await paymentMethodsApi.updateCardBank(editingBank.id, dto);
        setBanks(prev => prev.map(b => b.id === editingBank.id ? r.data : b));
        message.success('Banco actualizado');
      } else {
        const dto: CreateCardBankDto = { name: values.name, sortOrder: values.sortOrder };
        const r = await paymentMethodsApi.createCardBank(dto);
        setBanks(prev => [...prev, r.data]);
        message.success('Banco creado');
      }
      setBankModalOpen(false);
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'Error al guardar banco');
    } finally {
      setBankSaving(false);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await paymentMethodsApi.remove(id);
      setMethods(prev => prev.filter(m => m.id !== id));
      message.success('Metodo eliminado');
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'No se pudo eliminar');
    }
  };

  const handleDeleteBank = async (id: string) => {
    try {
      await paymentMethodsApi.removeCardBank(id);
      setBanks(prev => prev.filter(b => b.id !== id));
      message.success('Banco eliminado');
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'No se pudo eliminar');
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Medios de pago</Title>
        {canManage && <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Nuevo metodo</Button>}
      </div>

      <Table
        dataSource={methods}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={{ defaultPageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
        columns={[
          {
            title: 'Nombre', key: 'name',
            render: (_: unknown, m: PaymentMethodConfigDto) => (
              <Space>
                <span style={{ display: 'inline-block', width: 14, height: 14, borderRadius: '50%', background: m.color, border: '1px solid #d9d9d9' }} />
                <Text strong>{m.name}</Text>
                {m.isCash && <Tag color="green">Efectivo</Tag>}
                {m.isCard && <Tag color="blue">Tarjeta</Tag>}
              </Space>
            ),
          },
          {
            title: 'Estado', key: 'status', width: 110,
            render: (_: unknown, m: PaymentMethodConfigDto) => <Tag color={m.isActive ? 'success' : 'default'}>{m.isActive ? 'Activo' : 'Inactivo'}</Tag>,
          },
          { title: 'Orden', dataIndex: 'sortOrder', key: 'sortOrder', width: 80, align: 'center' },
          ...(canManage ? [{
            title: '', key: 'actions', width: 90,
            render: (_: unknown, m: PaymentMethodConfigDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(m)} />
                <Popconfirm
                  title="Eliminar este metodo?"
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
          }] : []),
        ]}
      />

      <Divider />

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Bancos para tarjetas</Title>
        {canManage && <Button icon={<BankOutlined />} onClick={openCreateBank}>Nuevo banco</Button>}
      </div>

      <Table
        dataSource={banks}
        rowKey="id"
        loading={bankLoading}
        size="small"
        pagination={{ defaultPageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
        columns={[
          { title: 'Banco', dataIndex: 'name', key: 'name', render: (v: string) => <Text strong>{v}</Text> },
          {
            title: 'Estado', key: 'status', width: 110,
            render: (_, b) => <Tag color={b.isActive ? 'success' : 'default'}>{b.isActive ? 'Activo' : 'Inactivo'}</Tag>,
          },
          { title: 'Orden', dataIndex: 'sortOrder', key: 'sortOrder', width: 80, align: 'center' },
          ...(canManage ? [{
            title: '', key: 'actions', width: 90,
            render: (_: unknown, b: CardBankDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openEditBank(b)} />
                <Popconfirm
                  title="Eliminar este banco?"
                  description="Si ya tiene cobros registrados, se desactivara."
                  onConfirm={() => handleDeleteBank(b.id)}
                  okText="Eliminar"
                  cancelText="Cancelar"
                  okButtonProps={{ danger: true }}
                >
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          }] : []),
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
            <Input placeholder="Ej: Efectivo, Tarjeta, Transferencia..." maxLength={64} />
          </Form.Item>

          <Form.Item name="color" label="Color" rules={[{ required: true }]}>
            <ColorPicker presets={[{ label: 'Sugeridos', colors: DEFAULT_COLORS }]} showText format="hex" />
          </Form.Item>

          <div style={{ display: 'flex', gap: 24, flexWrap: 'wrap' }}>
            <Form.Item name="isCash" label="Es efectivo" valuePropName="checked" tooltip="Marca si este metodo puede generar vuelto">
              <Switch />
            </Form.Item>
            <Form.Item name="isCard" label="Es tarjeta" valuePropName="checked" tooltip="Pedira banco, tipo y autorizacion al cobrar">
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

      <Modal
        title={editingBank ? 'Editar banco' : 'Nuevo banco'}
        open={bankModalOpen}
        onCancel={() => setBankModalOpen(false)}
        onOk={handleSaveBank}
        confirmLoading={bankSaving}
        okText="Guardar"
        cancelText="Cancelar"
        destroyOnHidden
      >
        <Form form={bankForm} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="name" label="Banco" rules={[{ required: true, message: 'Ingresa el banco' }]}>
            <Input placeholder="Ej: Pichincha, Pacifico, Produbanco..." maxLength={80} />
          </Form.Item>
          <div style={{ display: 'flex', gap: 24 }}>
            {editingBank && (
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
