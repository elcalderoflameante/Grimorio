import { useEffect, useState } from 'react';
import {
  Table, Button, Modal, Form, Input, InputNumber, Select, Switch,
  Popconfirm, Space, Typography, message, Tag, Badge, Tooltip
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, UnorderedListOutlined } from '@ant-design/icons';
import { menuApi, posApi } from '../../services/api';
import type { MenuItemDto, MenuCategoryDto, CreateMenuItemDto, UpdateMenuItemDto, WorkStationDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import RecipeEditor from './RecipeEditor';

const { Title } = Typography;

export default function MenuItemsList() {
  const [items, setItems] = useState<MenuItemDto[]>([]);
  const [categorias, setCategorias] = useState<MenuCategoryDto[]>([]);
  const [estaciones, setEstaciones] = useState<WorkStationDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<MenuItemDto | null>(null);
  const [recetaItem, setRecetaItem] = useState<MenuItemDto | null>(null);
  const [form] = Form.useForm();

  const loadCatalogos = async () => {
    try {
      const [c, e] = await Promise.all([menuApi.getCategories(), posApi.getStations()]);
      setCategorias(c.data);
      setEstaciones(e.data);
    } catch { /* silencioso */ }
  };

  const load = async () => {
    setLoading(true);
    try {
      const [i, c, e] = await Promise.all([menuApi.getItems(), menuApi.getCategories(), posApi.getStations()]);
      setItems(i.data);
      setCategorias(c.data);
      setEstaciones(e.data);
    } catch (e) { message.error(formatError(e)); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const openModal = async (item?: MenuItemDto) => {
    setEditing(item ?? null);
    if (item) {
      form.setFieldsValue(item);
    } else {
      form.resetFields();
      form.setFieldsValue({ isActive: true, availableForSale: true, price: 0 });
    }
    await loadCatalogos();
    setModal(true);
  };

  const save = async () => {
    const values = await form.validateFields();
    try {
      if (editing) {
        await menuApi.updateItem(editing.id, values as UpdateMenuItemDto);
      } else {
        await menuApi.createItem(values as CreateMenuItemDto);
      }
      message.success('Guardado');
      setModal(false);
      load();
    } catch (e) { message.error(formatError(e)); }
  };

  const remove = async (id: string) => {
    try { await menuApi.deleteItem(id); message.success('Eliminado'); load(); }
    catch (e) { message.error(formatError(e)); }
  };

  const categoriaOptions = categorias.map(c => ({ label: c.name, value: c.id }));
  const estacionOptions = estaciones.map(e => ({ label: e.name, value: e.id }));

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Ítems del Menú</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>Nuevo ítem</Button>
      </div>

      <Table
        dataSource={items}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={{ pageSize: 20 }}
        columns={[
          {
            title: 'Nombre', key: 'nombre',
            render: (_: unknown, item: MenuItemDto) => (
              <Space>
                {item.name}
                {!item.isActive && <Tag>Inactivo</Tag>}
                {!item.availableForSale && <Tag color="orange">No disponible</Tag>}
              </Space>
            ),
          },
          { title: 'Código', dataIndex: 'internalCode', key: 'codigo', width: 100 },
          {
            title: 'Categoría', key: 'categoria',
            render: (_: unknown, item: MenuItemDto) => (
              <Space>
                {item.categoryColor && <span style={{ display: 'inline-block', width: 10, height: 10, borderRadius: 2, background: item.categoryColor }} />}
                {item.categoryName}
              </Space>
            ),
          },
          {
            title: 'Precio', dataIndex: 'price', key: 'precio', width: 100,
            render: (v: number) => `$${v.toFixed(2)}`,
          },
          {
            title: 'Estación', key: 'estacion', width: 120,
            render: (_: unknown, item: MenuItemDto) =>
              item.stationName ? <Tag>{item.stationName}</Tag> : <span style={{ color: '#999' }}>—</span>,
          },
          {
            title: 'Receta', key: 'receta', width: 90,
            render: (_: unknown, item: MenuItemDto) => (
              <Tooltip title="Ver/editar receta">
                <Badge count={item.totalIngredients} size="small" showZero>
                  <Button
                    size="small"
                    icon={<UnorderedListOutlined />}
                    onClick={() => setRecetaItem(item)}
                  />
                </Badge>
              </Tooltip>
            ),
          },
          {
            title: 'Acciones', key: 'acc', width: 100,
            render: (_: unknown, item: MenuItemDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openModal(item)} />
                <Popconfirm title="¿Eliminar?" onConfirm={() => remove(item.id)}>
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Editar ítem del menú' : 'Nuevo ítem del menú'}
        open={modal}
        onOk={save}
        onCancel={() => setModal(false)}
        okText="Guardar"
        width={560}
      >
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="Nombre" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="internalCode" label="Código interno"><Input /></Form.Item>
          <Form.Item name="description" label="Descripción"><Input.TextArea rows={2} /></Form.Item>
          <Space style={{ width: '100%' }} size="middle">
            <Form.Item name="menuCategoryId" label="Categoría" rules={[{ required: true }]} style={{ flex: 1 }}>
              <Select options={categoriaOptions} placeholder="Seleccionar" />
            </Form.Item>
            <Form.Item name="price" label="Precio ($)" rules={[{ required: true }]} style={{ width: 140 }}>
              <InputNumber style={{ width: '100%' }} min={0} step={0.01} prefix="$" />
            </Form.Item>
          </Space>
          <Form.Item name="stationId" label="Estación destino">
            <Select
              options={estacionOptions}
              placeholder="Sin estación asignada"
              allowClear
            />
          </Form.Item>
          {editing && (
            <Space>
              <Form.Item name="isActive" label="Activo" valuePropName="checked"><Switch /></Form.Item>
              <Form.Item name="availableForSale" label="Disponible para venta" valuePropName="checked"><Switch /></Form.Item>
            </Space>
          )}
        </Form>
      </Modal>

      {recetaItem && (
        <RecipeEditor
          itemId={recetaItem.id}
          itemName={recetaItem.name}
          open={!!recetaItem}
          onClose={() => { setRecetaItem(null); load(); }}
        />
      )}
    </div>
  );
}
