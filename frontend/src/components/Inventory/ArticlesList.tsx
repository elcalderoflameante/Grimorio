import { useEffect, useState } from 'react';
import {
  Table, Button, Modal, Form, Input, InputNumber, Select, Switch,
  Popconfirm, Space, Typography, message, Tag, Badge
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, WarningOutlined } from '@ant-design/icons';
import { inventoryApi } from '../../services/api';
import type {
  InventoryArticleDto, InventoryCategoryDto, MeasurementUnitDto,
  CreateInventoryArticleDto, ArticleType
} from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Title } = Typography;

const TIPO_OPTIONS = [
  { label: 'Ingrediente', value: 'Ingrediente' },
  { label: 'Producto terminado', value: 'ProductoTerminado' },
  { label: 'Suministro', value: 'Suministro' },
];

const TIPO_COLOR: Record<string, string> = {
  Ingrediente: 'blue',
  ProductoTerminado: 'green',
  Suministro: 'orange',
};

export default function ArticlesList() {
  const [articulos, setArticulos] = useState<InventoryArticleDto[]>([]);
  const [categorias, setCategorias] = useState<InventoryCategoryDto[]>([]);
  const [unidades, setUnidades] = useState<MeasurementUnitDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<InventoryArticleDto | null>(null);
  const [form] = Form.useForm();

  const loadCatalogos = async () => {
    const [c, u] = await Promise.all([
      inventoryApi.getCategories(),
      inventoryApi.getUnits(),
    ]);
    setCategorias(c.data);
    setUnidades(u.data);
  };

  const load = async () => {
    setLoading(true);
    try {
      const [a, c, u] = await Promise.all([
        inventoryApi.getArticles(),
        inventoryApi.getCategories(),
        inventoryApi.getUnits(),
      ]);
      setArticulos(a.data);
      setCategorias(c.data);
      setUnidades(u.data);
    } catch (e) {
      message.error(formatError(e));
      // Si falla la carga de artículos, igual cargamos los catálogos para el formulario
      try { await loadCatalogos(); } catch { /* silencioso */ }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const openModal = async (a?: InventoryArticleDto) => {
    setEditing(a ?? null);
    if (a) {
      form.setFieldsValue({
        name: a.name,
        description: a.description,
        internalCode: a.internalCode,
        type: a.type,
        categoryId: a.categoryId,
        baseUnitId: a.baseUnitId,
        minStock: a.minStock,
        stockAlertActive: a.stockAlertActive,
        isActive: a.isActive,
      });
    } else {
      form.resetFields();
      form.setFieldsValue({ stockAlertActive: true, isActive: true, type: 'Ingredient', minStock: 0 });
    }
    // Siempre recarga catálogos al abrir el modal para evitar datos stale
    try { await loadCatalogos(); } catch { /* silencioso */ }
    setModal(true);
  };

  const save = async () => {
    const values = await form.validateFields();
    try {
      if (editing) {
        await inventoryApi.updateArticle(editing.id, values);
      } else {
        await inventoryApi.createArticle(values as CreateInventoryArticleDto);
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
      await inventoryApi.deleteArticle(id);
      message.success('Eliminado');
      load();
    } catch (e) {
      message.error(formatError(e));
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Artículos</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>
          Nuevo artículo
        </Button>
      </div>

      <Table
        dataSource={articulos}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={{ pageSize: 20 }}
        columns={[
          {
            title: 'Nombre', key: 'nombre',
            render: (_: unknown, a: InventoryArticleDto) => (
              <Space>
                {a.lowStock && <WarningOutlined style={{ color: '#faad14' }} />}
                {a.name}
                {!a.isActive && <Tag>Inactivo</Tag>}
              </Space>
            ),
          },
          { title: 'Código', dataIndex: 'internalCode', key: 'internalCode' },
          {
            title: 'Tipo', dataIndex: 'type', key: 'tipo',
            render: (v: ArticleType) => <Tag color={TIPO_COLOR[v]}>{v}</Tag>,
          },
          { title: 'Categoría', dataIndex: 'categoryName', key: 'categoryName' },
          {
            title: 'Stock', key: 'stock',
            render: (_: unknown, a: InventoryArticleDto) => (
              <Badge
                status={a.lowStock ? 'error' : 'success'}
                text={`${a.totalStock} ${a.baseUnitSymbol}`}
              />
            ),
          },
          {
            title: 'Stock mín.', key: 'stockMin',
            render: (_: unknown, a: InventoryArticleDto) => `${a.minStock} ${a.baseUnitSymbol}`,
          },
          {
            title: 'Acciones', key: 'acciones', width: 100,
            render: (_: unknown, a: InventoryArticleDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openModal(a)} />
                <Popconfirm title="¿Eliminar?" onConfirm={() => remove(a.id)}>
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Editar artículo' : 'Nuevo artículo'}
        open={modal}
        onOk={save}
        onCancel={() => setModal(false)}
        okText="Guardar"
        width={600}
      >
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="Nombre" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="internalCode" label="Código interno">
            <Input />
          </Form.Item>
          <Form.Item name="description" label="Descripción">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Space style={{ width: '100%' }} size="middle">
            <Form.Item name="type" label="Tipo" rules={[{ required: true }]} style={{ flex: 1 }}>
              <Select options={TIPO_OPTIONS} />
            </Form.Item>
            <Form.Item name="categoryId" label="Categoría" rules={[{ required: true }]} style={{ flex: 1 }}>
              <Select
                options={categorias.map(c => ({ label: c.name, value: c.id }))}
                placeholder="Seleccionar"
              />
            </Form.Item>
          </Space>
          <Space style={{ width: '100%' }} size="middle">
            <Form.Item name="baseUnitId" label="Unidad base" rules={[{ required: true }]} style={{ flex: 1 }}>
              <Select
                options={unidades.map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id }))}
                placeholder="Seleccionar"
              />
            </Form.Item>
            <Form.Item name="minStock" label="Stock mínimo" style={{ flex: 1 }}>
              <InputNumber style={{ width: '100%' }} min={0} />
            </Form.Item>
          </Space>
          <Space>
            <Form.Item name="stockAlertActive" label="Alerta de stock" valuePropName="checked">
              <Switch />
            </Form.Item>
            {editing && (
              <Form.Item name="isActive" label="Activo" valuePropName="checked">
                <Switch />
              </Form.Item>
            )}
          </Space>
        </Form>
      </Modal>
    </div>
  );
}
