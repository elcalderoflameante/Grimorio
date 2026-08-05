import { useCallback, useEffect, useMemo, useState } from 'react';
import { App as AntApp, Table, Button, Modal, Form, Input, InputNumber, Select, Switch,
  Popconfirm, Space, Typography, Tag, Badge, Row, Col } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, WarningOutlined, ReloadOutlined } from '@ant-design/icons';
import { inventoryApi } from '../../services/api';
import type {
  InventoryArticleDto, InventoryCategoryDto, MeasurementUnitDto, WarehouseDto, WarehouseStockDto,
  CreateInventoryArticleDto, ArticleType
} from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Title } = Typography;

const TIPO_OPTIONS = [
  { label: 'Ingrediente', value: 'Ingredient' },
  { label: 'Producto terminado', value: 'FinishedProduct' },
  { label: 'Suministro', value: 'Supply' },
];

const TIPO_COLOR: Record<string, string> = {
  Ingredient: 'blue',
  FinishedProduct: 'green',
  Supply: 'orange',
};

export default function ArticlesList() {
  const { message } = AntApp.useApp();

  const { hasPermission } = useAuth();
  const [articulos, setArticulos] = useState<InventoryArticleDto[]>([]);
  const [categorias, setCategorias] = useState<InventoryCategoryDto[]>([]);
  const [unidades, setUnidades] = useState<MeasurementUnitDto[]>([]);
  const [bodegas, setBodegas] = useState<WarehouseDto[]>([]);
  const [stock, setStock] = useState<WarehouseStockDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [modal, setModal] = useState(false);
  const [filterBodega, setFilterBodega] = useState<string | undefined>();
  const [filterCategoria, setFilterCategoria] = useState<string | undefined>();
  const [filterNombre, setFilterNombre] = useState('');
  const [editing, setEditing] = useState<InventoryArticleDto | null>(null);
  const [form] = Form.useForm();
  const canManage = hasPermission(PERMISSIONS.inventory.articlesManage);

  const loadCatalogos = useCallback(async () => {
    const [c, u] = await Promise.all([
      inventoryApi.getCategories(),
      inventoryApi.getUnits(),
    ]);
    setCategorias(c.data);
    setUnidades(u.data);
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [a, c, u, b, s] = await Promise.all([
        inventoryApi.getArticles({ categoryId: filterCategoria }),
        inventoryApi.getCategories(),
        inventoryApi.getUnits(),
        inventoryApi.getWarehouses(),
        inventoryApi.getStock({ warehouseId: filterBodega, categoryId: filterCategoria }),
      ]);
      setArticulos(a.data);
      setCategorias(c.data);
      setUnidades(u.data);
      setBodegas(b.data);
      setStock(s.data);
    } catch (e) {
      message.error(formatError(e));
      // Si falla la carga de artículos, igual cargamos los catálogos para el formulario
      try { await loadCatalogos(); } catch { /* silencioso */ }
    } finally {
      setLoading(false);
    }
  }, [filterBodega, filterCategoria, loadCatalogos, message]);

  useEffect(() => { load(); }, [load]);

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
    setSaving(true);
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
    } finally {
      setSaving(false);
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

  const stockByArticle = useMemo(() => {
    const map = new Map<string, WarehouseStockDto>();
    for (const row of stock) map.set(row.articleId, row);
    return map;
  }, [stock]);

  const filteredArticulos = useMemo(() => {
    const search = filterNombre.trim().toLowerCase();
    return articulos.filter(a => {
      const matchesWarehouse = !filterBodega || stockByArticle.has(a.id);
      const matchesName = !search || a.name.toLowerCase().includes(search);
      return matchesWarehouse && matchesName;
    });
  }, [articulos, filterBodega, filterNombre, stockByArticle]);

  const renderStock = (article: InventoryArticleDto) => {
    const warehouseStock = stockByArticle.get(article.id);
    const quantity = filterBodega && warehouseStock ? warehouseStock.quantity : article.totalStock;
    const lowStock = filterBodega && warehouseStock ? warehouseStock.lowStock : article.lowStock;
    const unit = filterBodega && warehouseStock ? warehouseStock.unitSymbol : article.baseUnitSymbol;

    return (
      <Badge
        status={lowStock ? 'error' : 'success'}
        text={`${quantity} ${unit}`}
      />
    );
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Artículos</Title>
        {canManage && (
          <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>
            Nuevo artículo
          </Button>
        )}
      </div>

      <Space style={{ marginBottom: 16 }} wrap>
        <Input.Search
          allowClear
          placeholder="Buscar por nombre"
          style={{ width: 260 }}
          value={filterNombre}
          onChange={(e) => setFilterNombre(e.target.value)}
        />
        <Select
          allowClear
          placeholder="Filtrar por bodega"
          style={{ width: 200 }}
          options={bodegas.map(b => ({ label: b.name, value: b.id }))}
          onChange={setFilterBodega}
        />
        <Select
          allowClear
          placeholder="Filtrar por categoría"
          style={{ width: 200 }}
          options={categorias.map(c => ({ label: c.name, value: c.id }))}
          onChange={setFilterCategoria}
        />
        <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Actualizar</Button>
      </Space>

      <Table
        dataSource={filteredArticulos}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={{ defaultPageSize: 20, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
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
            render: (_: unknown, a: InventoryArticleDto) => renderStock(a),
          },
          {
            title: 'Stock mín.', key: 'stockMin',
            render: (_: unknown, a: InventoryArticleDto) => `${a.minStock} ${a.baseUnitSymbol}`,
          },
          ...(canManage ? [{
            title: 'Acciones', key: 'acciones', width: 100,
            render: (_: unknown, a: InventoryArticleDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openModal(a)} />
                <Popconfirm title="¿Eliminar?" onConfirm={() => remove(a.id)}>
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          }] : []),
        ]}
      />

      <Modal
        title={editing ? 'Editar artículo' : 'Nuevo artículo'}
        open={modal}
        onOk={save}
        onCancel={saving ? undefined : () => setModal(false)}
        confirmLoading={saving}
        maskClosable={!saving}
        closable={!saving}
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
          <Row gutter={12}>
            <Col span={8}>
              <Form.Item name="type" label="Tipo" rules={[{ required: true }]}>
                <Select options={TIPO_OPTIONS} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="categoryId" label="Categoría" rules={[{ required: true }]}>
                <Select
                  options={categorias.map(c => ({ label: c.name, value: c.id }))}
                  placeholder="Seleccionar"
                  style={{ width: '100%' }}
                />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="baseUnitId" label="Unidad base" rules={[{ required: true }]}>
                <Select
                  options={unidades.map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id }))}
                  placeholder="Seleccionar"
                  style={{ width: '100%' }}
                />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12} align="bottom">
            <Col span={12}>
              <Form.Item name="minStock" label="Stock mínimo">
                <InputNumber style={{ width: '100%' }} min={0} />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item name="stockAlertActive" label="Alerta de stock" valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
            {editing && (
              <Col span={6}>
                <Form.Item name="isActive" label="Activo" valuePropName="checked">
                  <Switch />
                </Form.Item>
              </Col>
            )}
          </Row>
        </Form>
      </Modal>
    </div>
  );
}
