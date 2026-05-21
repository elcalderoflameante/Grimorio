import { useState, useEffect, useCallback } from 'react';
import { Table, Button, Modal, Form, Input, Switch, Space, Tag, Popconfirm, message } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { SupplierDto, CreateSupplierDto, UpdateSupplierDto } from '../../types';
import { purchasesApi } from '../../services/api';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

export default function SuppliersList() {
  const { hasPermission } = useAuth();
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<SupplierDto | null>(null);
  const [form] = Form.useForm();
  const canManage = hasPermission(PERMISSIONS.purchases.suppliersManage);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await purchasesApi.getSuppliers();
      setSuppliers(res.data);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    setModalOpen(true);
  };

  const openEdit = (s: SupplierDto) => {
    setEditing(s);
    form.setFieldsValue({ ...s });
    setModalOpen(true);
  };

  const handleSave = async () => {
    const values = await form.validateFields();
    try {
      if (editing) {
        await purchasesApi.updateSupplier(editing.id, values as UpdateSupplierDto);
        message.success('Proveedor actualizado');
      } else {
        await purchasesApi.createSupplier(values as CreateSupplierDto);
        message.success('Proveedor creado');
      }
      setModalOpen(false);
      load();
    } catch {
      message.error('Error al guardar proveedor');
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await purchasesApi.deleteSupplier(id);
      message.success('Proveedor eliminado');
      load();
    } catch {
      message.error('Error al eliminar proveedor');
    }
  };

  const columns = [
    {
      title: 'Nombre', dataIndex: 'name', key: 'name',
      sorter: (a: SupplierDto, b: SupplierDto) => a.name.localeCompare(b.name),
    },
    { title: 'RUC/Cédula', dataIndex: 'taxId', key: 'taxId' },
    { title: 'Teléfono', dataIndex: 'phone', key: 'phone' },
    { title: 'Email', dataIndex: 'email', key: 'email' },
    { title: 'Contacto', dataIndex: 'contactName', key: 'contactName' },
    {
      title: 'Estado', key: 'isActive',
      render: (_: unknown, r: SupplierDto) => (
        <Tag color={r.isActive ? 'green' : 'default'}>{r.isActive ? 'Activo' : 'Inactivo'}</Tag>
      ),
    },
    { title: 'Compras', dataIndex: 'totalPurchases', key: 'totalPurchases', align: 'right' as const },
    ...(canManage ? [{
      title: '', key: 'actions',
      render: (_: unknown, r: SupplierDto) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Popconfirm title="¿Eliminar proveedor?" onConfirm={() => handleDelete(r.id)} okText="Sí" cancelText="No">
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    }] : []),
  ];

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <h2 style={{ margin: 0 }}>Proveedores</h2>
        {canManage && <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Nuevo proveedor</Button>}
      </div>

      <Table
        columns={columns}
        dataSource={suppliers}
        rowKey="id"
        loading={loading}
        pagination={{ defaultPageSize: 20, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
        size="small"
      />

      <Modal
        title={editing ? 'Editar proveedor' : 'Nuevo proveedor'}
        open={modalOpen}
        onOk={handleSave}
        onCancel={() => setModalOpen(false)}
        okText="Guardar"
        cancelText="Cancelar"
        width={500}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="name" label="Nombre" rules={[{ required: true, message: 'Requerido' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="taxId" label="RUC / Cédula">
            <Input />
          </Form.Item>
          <Form.Item name="phone" label="Teléfono">
            <Input />
          </Form.Item>
          <Form.Item name="email" label="Email">
            <Input type="email" />
          </Form.Item>
          <Form.Item name="address" label="Dirección">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Form.Item name="contactName" label="Persona de contacto">
            <Input />
          </Form.Item>
          {editing && (
            <Form.Item name="isActive" label="Activo" valuePropName="checked">
              <Switch />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  );
}
