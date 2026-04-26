import { useEffect, useState } from 'react';
import {
  Table, Button, Modal, Form, Input, ColorPicker, Popconfirm, Space, Typography, message, Tag
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { inventoryApi } from '../../services/api';
import type { InventoryCategoryDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Title } = Typography;

export default function CategoriesList() {
  const [categorias, setCategorias] = useState<InventoryCategoryDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<InventoryCategoryDto | null>(null);
  const [form] = Form.useForm();

  const load = async () => {
    setLoading(true);
    try {
      const res = await inventoryApi.getCategories();
      setCategorias(res.data);
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const openModal = (c?: InventoryCategoryDto) => {
    setEditing(c ?? null);
    form.setFieldsValue(c ?? { nombre: '', descripcion: '', color: '#1677ff' });
    setModal(true);
  };

  const save = async () => {
    const values = await form.validateFields();
    const color = typeof values.color === 'string' ? values.color : values.color?.toHexString?.() ?? values.color;
    try {
      if (editing) {
        await inventoryApi.updateCategory(editing.id, { ...values, color });
      } else {
        await inventoryApi.createCategory({ ...values, color });
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
      await inventoryApi.deleteCategory(id);
      message.success('Eliminado');
      load();
    } catch (e) {
      message.error(formatError(e));
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Categorías</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>
          Nueva categoría
        </Button>
      </div>

      <Table
        dataSource={categorias}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={false}
        columns={[
          {
            title: 'Nombre', dataIndex: 'nombre', key: 'nombre',
            render: (nombre: string, c: InventoryCategoryDto) => (
              <Space>
                {c.color && <span style={{ display: 'inline-block', width: 12, height: 12, borderRadius: 3, background: c.color }} />}
                {nombre}
              </Space>
            ),
          },
          { title: 'Descripción', dataIndex: 'descripcion', key: 'descripcion' },
          {
            title: 'Artículos', dataIndex: 'totalArticulos', key: 'totalArticulos',
            render: (v: number) => <Tag color="blue">{v}</Tag>,
          },
          {
            title: 'Acciones', key: 'acciones', width: 100,
            render: (_: unknown, c: InventoryCategoryDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openModal(c)} />
                <Popconfirm title="¿Eliminar?" onConfirm={() => remove(c.id)}>
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Editar categoría' : 'Nueva categoría'}
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
          <Form.Item name="color" label="Color">
            <ColorPicker format="hex" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
