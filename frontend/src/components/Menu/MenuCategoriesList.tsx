import { useEffect, useState } from 'react';
import { Table, Button, Modal, Form, Input, InputNumber, ColorPicker, Switch, Popconfirm, Space, Typography, message, Tag } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { menuApi } from '../../services/api';
import type { MenuCategoryDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Title } = Typography;

export default function MenuCategoriesList() {
  const { hasPermission } = useAuth();
  const [categories, setCategories] = useState<MenuCategoryDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<MenuCategoryDto | null>(null);
  const [form] = Form.useForm();
  const canManage = hasPermission(PERMISSIONS.menu.categoriesManage);

  const load = async () => {
    setLoading(true);
    try { setCategories((await menuApi.getCategories()).data); }
    catch (e) { message.error(formatError(e)); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const openModal = (c?: MenuCategoryDto) => {
    setEditing(c ?? null);
    form.setFieldsValue(c ?? { name: '', description: '', color: '#1677ff', order: 0, isActive: true });
    setModal(true);
  };

  const save = async () => {
    const values = await form.validateFields();
    const color = typeof values.color === 'string' ? values.color : values.color?.toHexString?.() ?? values.color;
    try {
      if (editing) {
        await menuApi.updateCategory(editing.id, { ...editing, ...values, color });
      } else {
        await menuApi.createCategory({ ...values, color });
      }
      message.success('Guardado');
      setModal(false);
      load();
    } catch (e) { message.error(formatError(e)); }
  };

  const remove = async (id: string) => {
    try { await menuApi.deleteCategory(id); message.success('Eliminado'); load(); }
    catch (e) { message.error(formatError(e)); }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Categorias del Menu</Title>
        {canManage && (
          <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>
            Nueva categoria
          </Button>
        )}
      </div>

      <Table dataSource={categories} rowKey="id" loading={loading} size="small" pagination={{ defaultPageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
        columns={[
          {
            title: 'Nombre', key: 'name',
            render: (_: unknown, c: MenuCategoryDto) => (
              <Space>
                {c.color && <span style={{ display: 'inline-block', width: 12, height: 12, borderRadius: 3, background: c.color }} />}
                {c.name}
              </Space>
            ),
          },
          { title: 'Descripcion', dataIndex: 'description', key: 'description' },
          { title: 'Orden', dataIndex: 'order', key: 'order', width: 80 },
          {
            title: 'Items', dataIndex: 'totalItems', key: 'totalItems', width: 80,
            render: (v: number) => <Tag color="blue">{v}</Tag>,
          },
          {
            title: 'Estado', dataIndex: 'isActive', key: 'isActive', width: 90,
            render: (v: boolean) => <Tag color={v ? 'green' : 'default'}>{v ? 'Activa' : 'Inactiva'}</Tag>,
          },
          ...(canManage ? [{
            title: 'Acciones', key: 'acc', width: 100,
            render: (_: unknown, c: MenuCategoryDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openModal(c)} />
                <Popconfirm title="Eliminar?" onConfirm={() => remove(c.id)}>
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          }] : []),
        ]}
      />

      <Modal title={editing ? 'Editar categoria' : 'Nueva categoria'} open={modal} onOk={save} onCancel={() => setModal(false)} okText="Guardar">
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="Nombre" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="description" label="Descripcion"><Input.TextArea rows={2} /></Form.Item>
          <Space>
            <Form.Item name="color" label="Color"><ColorPicker format="hex" /></Form.Item>
            <Form.Item name="order" label="Orden de visualizacion"><InputNumber min={0} /></Form.Item>
          </Space>
          {editing && (
            <Form.Item name="isActive" label="Activa" valuePropName="checked"><Switch /></Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  );
}
