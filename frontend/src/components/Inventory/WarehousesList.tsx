import { useEffect, useState } from 'react';
import {
  Table, Button, Modal, Form, Input, Switch, Popconfirm, Space, Typography, message, Tag
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { inventoryApi } from '../../services/api';
import type { WarehouseDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Title } = Typography;

export default function WarehousesList() {
  const { hasPermission } = useAuth();
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<WarehouseDto | null>(null);
  const [form] = Form.useForm();
  const canManage = hasPermission(PERMISSIONS.inventory.configManage);

  const load = async () => {
    setLoading(true);
    try {
      const res = await inventoryApi.getWarehouses();
      setWarehouses(res.data);
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const openModal = (w?: WarehouseDto) => {
    setEditing(w ?? null);
    form.setFieldsValue(w ?? { name: '', description: '', location: '', isActive: true });
    setModal(true);
  };

  const save = async () => {
    const values = await form.validateFields();
    try {
      if (editing) {
        await inventoryApi.updateWarehouse(editing.id, { ...editing, ...values });
      } else {
        await inventoryApi.createWarehouse(values);
      }
      message.success('Guardado');
      setModal(false);
      load();
    } catch (e) {
      message.error(formatError(e));
    }
  };

  const remove = async (id: string) => {
    try {
      await inventoryApi.deleteWarehouse(id);
      message.success('Eliminado');
      load();
    } catch (e) {
      message.error(formatError(e));
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Bodegas</Title>
        {canManage && <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>
          Nueva bodega
        </Button>}
      </div>

      <Table
        dataSource={warehouses}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={{ defaultPageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
        columns={[
          { title: 'Nombre', dataIndex: 'name', key: 'name' },
          { title: 'Descripción', dataIndex: 'description', key: 'description' },
          { title: 'Ubicación', dataIndex: 'location', key: 'location' },
          {
            title: 'Estado', dataIndex: 'isActive', key: 'isActive',
            render: (v: boolean) => <Tag color={v ? 'green' : 'default'}>{v ? 'Activa' : 'Inactiva'}</Tag>,
          },
          ...(canManage ? [{
            title: 'Acciones', key: 'actions', width: 100,
            render: (_: unknown, w: WarehouseDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openModal(w)} />
                <Popconfirm title="¿Eliminar?" onConfirm={() => remove(w.id)}>
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          }] : []),
        ]}
      />

      <Modal
        title={editing ? 'Editar bodega' : 'Nueva bodega'}
        open={modal}
        onOk={save}
        onCancel={() => setModal(false)}
        okText="Guardar"
      >
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="Nombre" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="description" label="Descripción">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Form.Item name="location" label="Ubicación">
            <Input />
          </Form.Item>
          {editing && (
            <Form.Item name="isActive" label="Activa" valuePropName="checked">
              <Switch />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  );
}
