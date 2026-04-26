import { useEffect, useState } from 'react';
import {
  Table, Button, Modal, Form, Input, Switch, Popconfirm, Space, Typography, message, Tag
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { inventoryApi } from '../../services/api';
import type { WarehouseDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Title } = Typography;

export default function WarehousesList() {
  const [bodegas, setBodegas] = useState<WarehouseDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<WarehouseDto | null>(null);
  const [form] = Form.useForm();

  const load = async () => {
    setLoading(true);
    try {
      const res = await inventoryApi.getWarehouses();
      setBodegas(res.data);
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const openModal = (b?: WarehouseDto) => {
    setEditing(b ?? null);
    form.setFieldsValue(b ?? { nombre: '', descripcion: '', ubicacion: '', esActiva: true });
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
        <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>
          Nueva bodega
        </Button>
      </div>

      <Table
        dataSource={bodegas}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={false}
        columns={[
          { title: 'Nombre', dataIndex: 'nombre', key: 'nombre' },
          { title: 'Descripción', dataIndex: 'descripcion', key: 'descripcion' },
          { title: 'Ubicación', dataIndex: 'ubicacion', key: 'ubicacion' },
          {
            title: 'Estado', dataIndex: 'esActiva', key: 'esActiva',
            render: (v: boolean) => <Tag color={v ? 'green' : 'default'}>{v ? 'Activa' : 'Inactiva'}</Tag>,
          },
          {
            title: 'Acciones', key: 'acciones', width: 100,
            render: (_: unknown, b: WarehouseDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openModal(b)} />
                <Popconfirm title="¿Eliminar?" onConfirm={() => remove(b.id)}>
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          },
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
          <Form.Item name="nombre" label="Nombre" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="descripcion" label="Descripción">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Form.Item name="ubicacion" label="Ubicación">
            <Input />
          </Form.Item>
          {editing && (
            <Form.Item name="esActiva" label="Activa" valuePropName="checked">
              <Switch />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  );
}
