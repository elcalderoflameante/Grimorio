import { useEffect, useState } from 'react';
import { Table, Button, Modal, Form, Input, InputNumber, ColorPicker, Switch, Popconfirm, Space, Typography, message, Tag } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { menuApi } from '../../services/api';
import type { MenuCategoryDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Title } = Typography;

export default function MenuCategoriesList() {
  const [categorias, setCategorias] = useState<MenuCategoryDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<MenuCategoryDto | null>(null);
  const [form] = Form.useForm();

  const load = async () => {
    setLoading(true);
    try { setCategorias((await menuApi.getCategories()).data); }
    catch (e) { message.error(formatError(e)); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const openModal = (c?: MenuCategoryDto) => {
    setEditing(c ?? null);
    form.setFieldsValue(c ?? { nombre: '', descripcion: '', color: '#1677ff', orden: 0, esActiva: true });
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
        <Title level={5} style={{ margin: 0 }}>Categorías del Menú</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>Nueva categoría</Button>
      </div>

      <Table dataSource={categorias} rowKey="id" loading={loading} size="small" pagination={false}
        columns={[
          {
            title: 'Nombre', key: 'nombre',
            render: (_: unknown, c: MenuCategoryDto) => (
              <Space>
                {c.color && <span style={{ display: 'inline-block', width: 12, height: 12, borderRadius: 3, background: c.color }} />}
                {c.name}
              </Space>
            ),
          },
          { title: 'Descripción', dataIndex: 'descripcion', key: 'descripcion' },
          { title: 'Orden', dataIndex: 'orden', key: 'orden', width: 80 },
          {
            title: 'Items', dataIndex: 'totalItems', key: 'totalItems', width: 80,
            render: (v: number) => <Tag color="blue">{v}</Tag>,
          },
          {
            title: 'Estado', dataIndex: 'esActiva', key: 'esActiva', width: 90,
            render: (v: boolean) => <Tag color={v ? 'green' : 'default'}>{v ? 'Activa' : 'Inactiva'}</Tag>,
          },
          {
            title: 'Acciones', key: 'acc', width: 100,
            render: (_: unknown, c: MenuCategoryDto) => (
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

      <Modal title={editing ? 'Editar categoría' : 'Nueva categoría'} open={modal} onOk={save} onCancel={() => setModal(false)} okText="Guardar">
        <Form form={form} layout="vertical">
          <Form.Item name="nombre" label="Nombre" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="descripcion" label="Descripción"><Input.TextArea rows={2} /></Form.Item>
          <Space>
            <Form.Item name="color" label="Color"><ColorPicker format="hex" /></Form.Item>
            <Form.Item name="orden" label="Orden de visualización"><InputNumber min={0} /></Form.Item>
          </Space>
          {editing && (
            <Form.Item name="esActiva" label="Activa" valuePropName="checked"><Switch /></Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  );
}
