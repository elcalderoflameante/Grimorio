import { useCallback, useEffect, useMemo, useState } from 'react';
import { App as AntApp, Button, Form, Input, InputNumber, Modal, Popconfirm, Select, Space, Switch, Table, Tabs, Tag } from 'antd';
import { AppstoreOutlined, DeleteOutlined, EditOutlined, PartitionOutlined, PlusOutlined } from '@ant-design/icons';
import { financeApi } from '../../services/api';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import type {
  CostCenterDto,
  ExpenseCategoryDto,
  ExpenseCategoryType,
  UpsertCostCenterDto,
  UpsertExpenseCategoryDto,
} from '../../types';

const expenseTypeLabels: Record<ExpenseCategoryType, string> = {
  Fixed: 'Fijo',
  Variable: 'Variable',
  Mixed: 'Mixto',
};

const expenseTypeColors: Record<ExpenseCategoryType, string> = {
  Fixed: 'blue',
  Variable: 'green',
  Mixed: 'gold',
};

function CostCentersList() {
  const { message } = AntApp.useApp();
  const { hasPermission } = useAuth();
  const [items, setItems] = useState<CostCenterDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<CostCenterDto | null>(null);
  const [form] = Form.useForm<UpsertCostCenterDto>();
  const canManage = hasPermission(PERMISSIONS.finance.configManage);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await financeApi.getCostCenters();
      setItems(res.data);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openCreate = () => {
    setEditing(null);
    form.setFieldsValue({ name: '', code: undefined, description: undefined, displayOrder: 0, isActive: true });
    setModalOpen(true);
  };

  const openEdit = (item: CostCenterDto) => {
    setEditing(item);
    form.setFieldsValue(item);
    setModalOpen(true);
  };

  const handleSave = async () => {
    const values = await form.validateFields();
    try {
      if (editing) {
        await financeApi.updateCostCenter(editing.id, values);
        message.success('Centro de costo actualizado');
      } else {
        await financeApi.createCostCenter(values);
        message.success('Centro de costo creado');
      }
      setModalOpen(false);
      load();
    } catch {
      message.error('Error al guardar centro de costo');
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await financeApi.deleteCostCenter(id);
      message.success('Centro de costo eliminado');
      load();
    } catch {
      message.error('Error al eliminar centro de costo');
    }
  };

  const columns = useMemo(() => [
    { title: 'Orden', dataIndex: 'displayOrder', key: 'displayOrder', width: 80, align: 'right' as const },
    { title: 'Codigo', dataIndex: 'code', key: 'code', width: 120, render: (value?: string) => value || '-' },
    {
      title: 'Nombre',
      dataIndex: 'name',
      key: 'name',
      sorter: (a: CostCenterDto, b: CostCenterDto) => a.name.localeCompare(b.name),
    },
    { title: 'Descripcion', dataIndex: 'description', key: 'description', render: (value?: string) => value || '-' },
    {
      title: 'Estado',
      key: 'isActive',
      width: 100,
      render: (_: unknown, row: CostCenterDto) => (
        <Tag color={row.isActive ? 'green' : 'default'}>{row.isActive ? 'Activo' : 'Inactivo'}</Tag>
      ),
    },
    ...(canManage ? [{
      title: '',
      key: 'actions',
      width: 96,
      render: (_: unknown, row: CostCenterDto) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(row)} />
          <Popconfirm title="Eliminar centro de costo?" onConfirm={() => handleDelete(row.id)} okText="Si" cancelText="No">
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    }] : []),
  ], [canManage]);

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, marginBottom: 16 }}>
        <h2 style={{ margin: 0 }}>Centros de costo</h2>
        {canManage && <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Nuevo centro</Button>}
      </div>

      <Table
        columns={columns}
        dataSource={items}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={{ defaultPageSize: 20, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
      />

      <Modal
        title={editing ? 'Editar centro de costo' : 'Nuevo centro de costo'}
        open={modalOpen}
        onOk={handleSave}
        onCancel={() => setModalOpen(false)}
        okText="Guardar"
        cancelText="Cancelar"
        width={620}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Space align="start" style={{ width: '100%' }} size={12}>
            <Form.Item name="code" label="Codigo" style={{ flex: '0 0 150px' }}>
              <Input maxLength={30} />
            </Form.Item>
            <Form.Item name="name" label="Nombre" rules={[{ required: true, message: 'Requerido' }]} style={{ flex: 1 }}>
              <Input maxLength={120} />
            </Form.Item>
            <Form.Item name="displayOrder" label="Orden" style={{ flex: '0 0 110px' }}>
              <InputNumber min={0} style={{ width: '100%' }} />
            </Form.Item>
          </Space>
          <Form.Item name="description" label="Descripcion">
            <Input.TextArea rows={2} maxLength={300} />
          </Form.Item>
          <Form.Item name="isActive" label="Activo" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

function ExpenseCategoriesList() {
  const { message } = AntApp.useApp();
  const { hasPermission } = useAuth();
  const [items, setItems] = useState<ExpenseCategoryDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ExpenseCategoryDto | null>(null);
  const [form] = Form.useForm<UpsertExpenseCategoryDto>();
  const canManage = hasPermission(PERMISSIONS.finance.configManage);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await financeApi.getExpenseCategories();
      setItems(res.data);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openCreate = () => {
    setEditing(null);
    form.setFieldsValue({ name: '', description: undefined, type: 'Variable', displayOrder: 0, isActive: true });
    setModalOpen(true);
  };

  const openEdit = (item: ExpenseCategoryDto) => {
    setEditing(item);
    form.setFieldsValue(item);
    setModalOpen(true);
  };

  const handleSave = async () => {
    const values = await form.validateFields();
    try {
      if (editing) {
        await financeApi.updateExpenseCategory(editing.id, values);
        message.success('Categoria de gasto actualizada');
      } else {
        await financeApi.createExpenseCategory(values);
        message.success('Categoria de gasto creada');
      }
      setModalOpen(false);
      load();
    } catch {
      message.error('Error al guardar categoria de gasto');
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await financeApi.deleteExpenseCategory(id);
      message.success('Categoria de gasto eliminada');
      load();
    } catch {
      message.error('Error al eliminar categoria de gasto');
    }
  };

  const columns = useMemo(() => [
    { title: 'Orden', dataIndex: 'displayOrder', key: 'displayOrder', width: 80, align: 'right' as const },
    {
      title: 'Nombre',
      dataIndex: 'name',
      key: 'name',
      sorter: (a: ExpenseCategoryDto, b: ExpenseCategoryDto) => a.name.localeCompare(b.name),
    },
    {
      title: 'Tipo',
      key: 'type',
      width: 120,
      render: (_: unknown, row: ExpenseCategoryDto) => (
        <Tag color={expenseTypeColors[row.type]}>{expenseTypeLabels[row.type]}</Tag>
      ),
    },
    { title: 'Descripcion', dataIndex: 'description', key: 'description', render: (value?: string) => value || '-' },
    {
      title: 'Estado',
      key: 'isActive',
      width: 100,
      render: (_: unknown, row: ExpenseCategoryDto) => (
        <Tag color={row.isActive ? 'green' : 'default'}>{row.isActive ? 'Activo' : 'Inactivo'}</Tag>
      ),
    },
    ...(canManage ? [{
      title: '',
      key: 'actions',
      width: 96,
      render: (_: unknown, row: ExpenseCategoryDto) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(row)} />
          <Popconfirm title="Eliminar categoria de gasto?" onConfirm={() => handleDelete(row.id)} okText="Si" cancelText="No">
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    }] : []),
  ], [canManage]);

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, marginBottom: 16 }}>
        <h2 style={{ margin: 0 }}>Categorias de gasto</h2>
        {canManage && <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Nueva categoria</Button>}
      </div>

      <Table
        columns={columns}
        dataSource={items}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={{ defaultPageSize: 20, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
      />

      <Modal
        title={editing ? 'Editar categoria de gasto' : 'Nueva categoria de gasto'}
        open={modalOpen}
        onOk={handleSave}
        onCancel={() => setModalOpen(false)}
        okText="Guardar"
        cancelText="Cancelar"
        width={620}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Space align="start" style={{ width: '100%' }} size={12}>
            <Form.Item name="name" label="Nombre" rules={[{ required: true, message: 'Requerido' }]} style={{ flex: 1 }}>
              <Input maxLength={120} />
            </Form.Item>
            <Form.Item name="type" label="Tipo" rules={[{ required: true, message: 'Requerido' }]} style={{ flex: '0 0 150px' }}>
              <Select
                options={[
                  { value: 'Fixed', label: 'Fijo' },
                  { value: 'Variable', label: 'Variable' },
                  { value: 'Mixed', label: 'Mixto' },
                ]}
              />
            </Form.Item>
            <Form.Item name="displayOrder" label="Orden" style={{ flex: '0 0 110px' }}>
              <InputNumber min={0} style={{ width: '100%' }} />
            </Form.Item>
          </Space>
          <Form.Item name="description" label="Descripcion">
            <Input.TextArea rows={2} maxLength={300} />
          </Form.Item>
          <Form.Item name="isActive" label="Activo" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

export default function FinanceConfig() {
  return (
    <Tabs
      defaultActiveKey="cost-centers"
      type="card"
      items={[
        {
          key: 'cost-centers',
          label: <><PartitionOutlined /> Centros de costo</>,
          children: <CostCentersList />,
        },
        {
          key: 'expense-categories',
          label: <><AppstoreOutlined /> Categorias de gasto</>,
          children: <ExpenseCategoriesList />,
        },
      ]}
    />
  );
}
