import { useState, useEffect, useCallback } from 'react';
import { Table, Button, Modal, Form, Input, Switch, Space, Tag, Popconfirm, message } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { SupplierDto, CreateSupplierDto, UpdateSupplierDto } from '../../types';
import { purchasesApi } from '../../services/api';

export default function SuppliersList() {
  const [proveedores, setProveedores] = useState<SupplierDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<SupplierDto | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await purchasesApi.getSuppliers();
      setProveedores(res.data);
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

  const openEdit = (p: SupplierDto) => {
    setEditing(p);
    form.setFieldsValue({ ...p });
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
    { title: 'Nombre', dataIndex: 'nombre', key: 'nombre', sorter: (a: SupplierDto, b: SupplierDto) => a.name.localeCompare(b.name) },
    { title: 'RUC/Cédula', dataIndex: 'rucCedula', key: 'rucCedula' },
    { title: 'Teléfono', dataIndex: 'telefono', key: 'telefono' },
    { title: 'Email', dataIndex: 'email', key: 'email' },
    { title: 'Contacto', dataIndex: 'contacto', key: 'contacto' },
    {
      title: 'Estado',
      key: 'estado',
      render: (_: unknown, r: SupplierDto) => (
        <Tag color={r.isActive ? 'green' : 'default'}>{r.isActive ? 'Activo' : 'Inactivo'}</Tag>
      ),
    },
    { title: 'Órdenes', dataIndex: 'totalOrdenes', key: 'totalOrdenes', align: 'right' as const },
    {
      title: '',
      key: 'actions',
      render: (_: unknown, r: SupplierDto) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Popconfirm title="¿Eliminar proveedor?" onConfirm={() => handleDelete(r.id)} okText="Sí" cancelText="No">
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <h2 style={{ margin: 0 }}>Proveedores</h2>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Nuevo proveedor</Button>
      </div>

      <Table
        columns={columns}
        dataSource={proveedores}
        rowKey="id"
        loading={loading}
        pagination={{ pageSize: 20 }}
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
          <Form.Item name="nombre" label="Nombre" rules={[{ required: true, message: 'Requerido' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="rucCedula" label="RUC / Cédula">
            <Input />
          </Form.Item>
          <Form.Item name="telefono" label="Teléfono">
            <Input />
          </Form.Item>
          <Form.Item name="email" label="Email">
            <Input type="email" />
          </Form.Item>
          <Form.Item name="direccion" label="Dirección">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Form.Item name="contacto" label="Persona de contacto">
            <Input />
          </Form.Item>
          {editing && (
            <Form.Item name="esActivo" label="Activo" valuePropName="checked">
              <Switch />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  );
}
