import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  App as AntApp,
  Button,
  DatePicker,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Typography,
} from 'antd';
import { PlayCircleOutlined, PlusOutlined, ReloadOutlined, ToolOutlined } from '@ant-design/icons';
import { inventoryApi } from '../../services/api';
import type {
  InventoryArticleDto,
  MeasurementUnitDto,
  ProductionOrderDto,
  ProductionRecipeDto,
  UnitConversionDto,
  WarehouseDto,
} from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import { branchDateRangeToUtcIso, formatBranchDateTime } from '../../utils/branchTimeZone';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;

type RecipeFormValues = {
  outputArticleId: string;
  outputQuantity: number;
  outputUnitId: string;
  notes?: string;
  isActive: boolean;
  ingredients: Array<{
    articleId: string;
    quantity: number;
    unitId: string;
    notes?: string;
  }>;
};

type ProductionFormValues = {
  productionRecipeId: string;
  sourceWarehouseId: string;
  destinationWarehouseId: string;
  outputQuantity: number;
  outputUnitId: string;
  notes?: string;
};

export default function Production() {
  const { message } = AntApp.useApp();
  const { hasPermission } = useAuth();
  const [recipes, setRecipes] = useState<ProductionRecipeDto[]>([]);
  const [orders, setOrders] = useState<ProductionOrderDto[]>([]);
  const [articles, setArticles] = useState<InventoryArticleDto[]>([]);
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [units, setUnits] = useState<MeasurementUnitDto[]>([]);
  const [conversions, setConversions] = useState<UnitConversionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [savingRecipe, setSavingRecipe] = useState(false);
  const [producing, setProducing] = useState(false);
  const [recipeModal, setRecipeModal] = useState(false);
  const [productionModal, setProductionModal] = useState(false);
  const [editingRecipe, setEditingRecipe] = useState<ProductionRecipeDto | null>(null);
  const [filterArticle, setFilterArticle] = useState<string | undefined>();
  const [filterWarehouse, setFilterWarehouse] = useState<string | undefined>();
  const [filterRange, setFilterRange] = useState<[string, string] | undefined>();
  const [recipeForm] = Form.useForm<RecipeFormValues>();
  const [productionForm] = Form.useForm<ProductionFormValues>();
  const selectedOutputArticleId = Form.useWatch('outputArticleId', recipeForm);
  const selectedProductionRecipeId = Form.useWatch('productionRecipeId', productionForm);
  const canCreate = hasPermission(PERMISSIONS.inventory.movementsCreate);

  const elaboratedProducts = useMemo(
    () => articles.filter(a => a.type === 'ElaboratedProduct' && a.isActive),
    [articles]
  );

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [recipeRes, orderRes, articleRes, warehouseRes, unitRes, conversionRes] = await Promise.all([
        inventoryApi.getProductionRecipes(),
        inventoryApi.getProductionOrders({
          outputArticleId: filterArticle,
          warehouseId: filterWarehouse,
          from: filterRange?.[0],
          to: filterRange?.[1],
          pageSize: 100,
        }),
        inventoryApi.getArticles({ activeOnly: true }),
        inventoryApi.getWarehouses(true),
        inventoryApi.getUnits(),
        inventoryApi.getConversions(),
      ]);
      setRecipes(recipeRes.data);
      setOrders(orderRes.data);
      setArticles(articleRes.data);
      setWarehouses(warehouseRes.data);
      setUnits(unitRes.data);
      setConversions(conversionRes.data);
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setLoading(false);
    }
  }, [filterArticle, filterRange, filterWarehouse, message]);

  useEffect(() => { load(); }, [load]);

  const compatibleUnitIds = (articleId?: string) => {
    if (!articleId) return new Set<string>();
    const article = articles.find(a => a.id === articleId);
    if (!article) return new Set<string>();

    const ids = new Set<string>([article.baseUnitId]);
    for (const conversion of conversions) {
      if (conversion.originUnitId === article.baseUnitId) ids.add(conversion.destinationUnitId);
      if (conversion.destinationUnitId === article.baseUnitId) ids.add(conversion.originUnitId);
    }
    return ids;
  };

  const unitOptionsFor = (articleId?: string) => {
    const ids = compatibleUnitIds(articleId);
    return units
      .filter(u => ids.has(u.id))
      .map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id }));
  };

  const openRecipeModal = (recipe?: ProductionRecipeDto) => {
    setEditingRecipe(recipe ?? null);
    if (recipe) {
      recipeForm.setFieldsValue({
        outputArticleId: recipe.outputArticleId,
        outputQuantity: recipe.outputQuantity,
        outputUnitId: recipe.outputUnitId,
        notes: recipe.notes,
        isActive: recipe.isActive,
        ingredients: recipe.ingredients.map(i => ({
          articleId: i.articleId,
          quantity: i.quantity,
          unitId: i.unitId,
          notes: i.notes,
        })),
      });
    } else {
      recipeForm.setFieldsValue({ outputQuantity: 1, isActive: true, ingredients: [{} as RecipeFormValues['ingredients'][number]] });
    }
    setRecipeModal(true);
  };

  const closeRecipeModal = () => {
    setRecipeModal(false);
    setEditingRecipe(null);
    recipeForm.resetFields();
  };

  const saveRecipe = async () => {
    const values = await recipeForm.validateFields();
    setSavingRecipe(true);
    try {
      await inventoryApi.upsertProductionRecipe(values);
      message.success('Receta de producción guardada');
      closeRecipeModal();
      load();
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setSavingRecipe(false);
    }
  };

  const openProductionModal = (recipe?: ProductionRecipeDto) => {
    const selected = recipe ?? recipes.find(r => r.id === selectedProductionRecipeId);
    productionForm.setFieldsValue({
      productionRecipeId: selected?.id,
      outputQuantity: selected?.outputQuantity ?? 1,
      outputUnitId: selected?.outputUnitId,
    });
    setProductionModal(true);
  };

  const closeProductionModal = () => {
    setProductionModal(false);
    productionForm.resetFields();
  };

  const registerProduction = async () => {
    const values = await productionForm.validateFields();
    setProducing(true);
    try {
      await inventoryApi.registerProduction(values);
      message.success('Producción registrada');
      closeProductionModal();
      load();
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setProducing(false);
    }
  };

  const handleRecipeOutputChange = (articleId: string) => {
    const article = articles.find(a => a.id === articleId);
    recipeForm.setFieldValue('outputUnitId', article?.baseUnitId);
  };

  const handleProductionRecipeChange = (recipeId: string) => {
    const recipe = recipes.find(r => r.id === recipeId);
    productionForm.setFieldsValue({
      outputQuantity: recipe?.outputQuantity,
      outputUnitId: recipe?.outputUnitId,
    });
  };

  const selectedProductionRecipe = recipes.find(r => r.id === selectedProductionRecipeId);

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Producción</Title>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading} />
          {canCreate && (
            <>
              <Button icon={<ToolOutlined />} onClick={() => openRecipeModal()}>
                Nueva receta
              </Button>
            </>
          )}
        </Space>
      </div>

      <Table
        dataSource={recipes}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={false}
        style={{ marginBottom: 20 }}
        columns={[
          { title: 'Producto elaborado', dataIndex: 'outputArticleName', key: 'outputArticleName' },
          {
            title: 'Rinde',
            key: 'yield',
            render: (_: unknown, r: ProductionRecipeDto) => `${r.outputQuantity} ${r.outputUnitSymbol}`,
            width: 130,
          },
          {
            title: 'Insumos',
            key: 'ingredients',
            render: (_: unknown, r: ProductionRecipeDto) => r.ingredients.length,
            width: 90,
          },
          {
            title: 'Estado',
            key: 'status',
            render: (_: unknown, r: ProductionRecipeDto) => <Tag color={r.isActive ? 'green' : 'default'}>{r.isActive ? 'Activa' : 'Inactiva'}</Tag>,
            width: 100,
          },
          {
            title: 'Acciones',
            key: 'actions',
            width: 190,
            render: (_: unknown, r: ProductionRecipeDto) => (
              <Space>
                <Button size="small" icon={<ToolOutlined />} onClick={() => openRecipeModal(r)}>
                  Editar
                </Button>
                {canCreate && (
                  <Button size="small" type="primary" icon={<PlayCircleOutlined />} onClick={() => openProductionModal(r)}>
                    Producir
                  </Button>
                )}
              </Space>
            ),
          },
        ]}
        expandable={{
          expandedRowRender: r => (
            <Table
              dataSource={r.ingredients}
              rowKey="id"
              size="small"
              pagination={false}
              columns={[
                { title: 'Insumo', dataIndex: 'articleName', key: 'articleName' },
                {
                  title: 'Cantidad',
                  key: 'quantity',
                  render: (_: unknown, i) => `${i.quantity} ${i.unitSymbol}`,
                  width: 140,
                },
                { title: 'Observación', dataIndex: 'notes', key: 'notes' },
              ]}
            />
          ),
        }}
      />

      <Space style={{ marginBottom: 16 }} wrap>
        <Select
          allowClear
          placeholder="Producto elaborado"
          style={{ width: 240 }}
          options={elaboratedProducts.map(a => ({ label: a.name, value: a.id }))}
          onChange={setFilterArticle}
          showSearch
          optionFilterProp="label"
        />
        <Select
          allowClear
          placeholder="Bodega"
          style={{ width: 180 }}
          options={warehouses.map(w => ({ label: w.name, value: w.id }))}
          onChange={setFilterWarehouse}
        />
        <RangePicker onChange={v => {
          const range = branchDateRangeToUtcIso(v ? [v[0], v[1]] : undefined);
          setFilterRange(range.from && range.to ? [range.from, range.to] : undefined);
        }} />
      </Space>

      <Table
        dataSource={orders}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={{ defaultPageSize: 20 }}
        columns={[
          { title: 'Fecha', dataIndex: 'producedAt', key: 'producedAt', render: (v: string) => formatBranchDateTime(v), width: 140 },
          { title: 'Número', dataIndex: 'number', key: 'number', width: 150 },
          { title: 'Producto', dataIndex: 'outputArticleName', key: 'outputArticleName' },
          { title: 'Origen', dataIndex: 'sourceWarehouseName', key: 'sourceWarehouseName' },
          { title: 'Destino', dataIndex: 'destinationWarehouseName', key: 'destinationWarehouseName' },
          {
            title: 'Producido',
            key: 'output',
            render: (_: unknown, o: ProductionOrderDto) => `${o.outputQuantity} ${o.outputUnitSymbol}`,
            width: 120,
          },
          {
            title: 'Costo total',
            key: 'totalCost',
            render: (_: unknown, o: ProductionOrderDto) => `$${o.totalCost.toFixed(2)}`,
            width: 110,
          },
          {
            title: 'Costo unit.',
            key: 'unitCost',
            render: (_: unknown, o: ProductionOrderDto) => `$${o.unitCost.toFixed(4)} / ${o.outputBaseUnitSymbol}`,
            width: 150,
          },
        ]}
        expandable={{
          expandedRowRender: o => (
            <Table
              dataSource={o.ingredients}
              rowKey="articleId"
              size="small"
              pagination={false}
              columns={[
                { title: 'Insumo', dataIndex: 'articleName', key: 'articleName' },
                { title: 'Cantidad', key: 'quantity', render: (_: unknown, i) => `${i.quantity} ${i.unitSymbol}` },
                { title: 'Base', key: 'base', render: (_: unknown, i) => `${i.baseQuantity} ${i.baseUnitSymbol}` },
                { title: 'Costo unit.', key: 'unitCost', render: (_: unknown, i) => `$${i.unitCost.toFixed(4)}` },
                { title: 'Costo total', key: 'totalCost', render: (_: unknown, i) => `$${i.totalCost.toFixed(2)}` },
              ]}
            />
          ),
        }}
      />

      <Modal
        title={editingRecipe ? 'Editar receta de producción' : 'Nueva receta de producción'}
        open={recipeModal}
        onOk={saveRecipe}
        onCancel={savingRecipe ? undefined : closeRecipeModal}
        confirmLoading={savingRecipe}
        maskClosable={!savingRecipe}
        closable={!savingRecipe}
        okText="Guardar"
        cancelText="Cancelar"
        width={900}
      >
        <Form form={recipeForm} layout="vertical" requiredMark={false}>
          <div style={{ display: 'grid', gridTemplateColumns: 'minmax(0, 1fr) 120px minmax(160px, 190px) 110px', gap: 12 }}>
            <Form.Item name="outputArticleId" label="Producto elaborado" rules={[{ required: true }]}>
              <Select
                options={elaboratedProducts.map(a => ({ label: `${a.name} (${a.baseUnitSymbol})`, value: a.id }))}
                showSearch
                optionFilterProp="label"
                onChange={handleRecipeOutputChange}
              />
            </Form.Item>
            <Form.Item name="outputQuantity" label="Rinde" rules={[{ required: true }]}>
              <InputNumber style={{ width: '100%' }} min={0.0001} step={0.01} />
            </Form.Item>
            <Form.Item name="outputUnitId" label="Unidad" rules={[{ required: true }]}>
              <Select options={unitOptionsFor(selectedOutputArticleId)} disabled={!selectedOutputArticleId} />
            </Form.Item>
            <Form.Item name="isActive" label="Activa" valuePropName="checked">
              <Switch />
            </Form.Item>
          </div>
          <Form.Item name="notes" label="Observación">
            <Input.TextArea rows={2} />
          </Form.Item>

          <Text strong>Insumos</Text>
          <Form.List name="ingredients">
            {(fields, { add, remove }) => (
              <div style={{ marginTop: 8 }}>
                {fields.map(field => (
                  <div
                    key={field.key}
                    style={{ display: 'grid', gridTemplateColumns: 'minmax(0, 1fr) 110px minmax(150px, 180px) minmax(0, 1fr) 72px', gap: 10, alignItems: 'start' }}
                  >
                    <Form.Item name={[field.name, 'articleId']} rules={[{ required: true }]} style={{ marginBottom: 10 }}>
                      <Select
                        placeholder="Insumo"
                        options={articles
                          .filter(a => a.isActive)
                          .map(a => ({ label: `${a.name} (${a.baseUnitSymbol})`, value: a.id }))}
                        showSearch
                        optionFilterProp="label"
                        onChange={(articleId: string) => {
                          const article = articles.find(a => a.id === articleId);
                          recipeForm.setFieldValue(['ingredients', field.name, 'unitId'], article?.baseUnitId);
                        }}
                      />
                    </Form.Item>
                    <Form.Item name={[field.name, 'quantity']} rules={[{ required: true }]} style={{ marginBottom: 10 }}>
                      <InputNumber style={{ width: '100%' }} min={0.0001} step={0.01} placeholder="Cant." />
                    </Form.Item>
                    <Form.Item
                      noStyle
                      shouldUpdate={(prev, current) =>
                        prev.ingredients?.[field.name]?.articleId !== current.ingredients?.[field.name]?.articleId
                      }
                    >
                      {({ getFieldValue }) => {
                        const ingredientArticleId = getFieldValue(['ingredients', field.name, 'articleId']);
                        return (
                          <Form.Item name={[field.name, 'unitId']} rules={[{ required: true }]} style={{ marginBottom: 10 }}>
                            <Select
                              options={unitOptionsFor(ingredientArticleId)}
                              placeholder={ingredientArticleId ? 'Unidad' : 'Selecciona insumo'}
                              disabled={!ingredientArticleId}
                            />
                          </Form.Item>
                        );
                      }}
                    </Form.Item>
                    <Form.Item name={[field.name, 'notes']} style={{ marginBottom: 10 }}>
                      <Input placeholder="Observación" />
                    </Form.Item>
                    <Button danger onClick={() => remove(field.name)}>
                      Quitar
                    </Button>
                  </div>
                ))}
                <Button icon={<PlusOutlined />} onClick={() => add()}>
                  Agregar insumo
                </Button>
              </div>
            )}
          </Form.List>
        </Form>
      </Modal>

      <Modal
        title="Registrar producción"
        open={productionModal}
        onOk={registerProduction}
        onCancel={producing ? undefined : closeProductionModal}
        confirmLoading={producing}
        maskClosable={!producing}
        closable={!producing}
        okText="Producir"
        cancelText="Cancelar"
        width={720}
      >
        <Form form={productionForm} layout="vertical" requiredMark={false}>
          <Form.Item name="productionRecipeId" label="Receta" rules={[{ required: true }]}>
            <Select
              options={recipes.filter(r => r.isActive).map(r => ({ label: `${r.outputArticleName} (${r.outputQuantity} ${r.outputUnitSymbol})`, value: r.id }))}
              showSearch
              optionFilterProp="label"
              onChange={handleProductionRecipeChange}
            />
          </Form.Item>
          <div style={{ display: 'grid', gridTemplateColumns: 'minmax(0, 1fr) minmax(0, 1fr)', gap: 12 }}>
            <Form.Item name="sourceWarehouseId" label="Bodega origen insumos" rules={[{ required: true }]}>
              <Select options={warehouses.map(w => ({ label: w.name, value: w.id }))} />
            </Form.Item>
            <Form.Item name="destinationWarehouseId" label="Bodega destino producto" rules={[{ required: true }]}>
              <Select options={warehouses.map(w => ({ label: w.name, value: w.id }))} />
            </Form.Item>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '160px minmax(170px, 220px) minmax(0, 1fr)', gap: 12 }}>
            <Form.Item name="outputQuantity" label="Cantidad" rules={[{ required: true }]}>
              <InputNumber style={{ width: '100%' }} min={0.0001} step={0.01} />
            </Form.Item>
            <Form.Item name="outputUnitId" label="Unidad" rules={[{ required: true }]}>
              <Select options={unitOptionsFor(selectedProductionRecipe?.outputArticleId)} disabled={!selectedProductionRecipe} />
            </Form.Item>
            <Form.Item name="notes" label="Observación">
              <Input />
            </Form.Item>
          </div>
        </Form>
      </Modal>
    </div>
  );
}
