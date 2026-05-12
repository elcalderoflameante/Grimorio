import { useEffect, useState } from 'react';
import { Table, Button, Modal, Form, Select, InputNumber, Input, Switch, Popconfirm, Typography, message, Tag, Space } from 'antd';
import { PlusOutlined, DeleteOutlined, SaveOutlined, QuestionCircleOutlined } from '@ant-design/icons';
import { menuApi, inventoryApi } from '../../services/api';
import type { InventoryArticleDto, MeasurementUnitDto, UnitConversionDto, UpsertRecipeIngredientDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Text } = Typography;

interface Props {
  itemId: string;
  itemName: string;
  open: boolean;
  onClose: () => void;
}

export default function RecipeEditor({ itemId, itemName, open, onClose }: Props) {
  const [receta, setReceta] = useState<UpsertRecipeIngredientDto[]>([]);
  const [articulos, setArticulos] = useState<InventoryArticleDto[]>([]);
  const [unidades, setUnidades] = useState<MeasurementUnitDto[]>([]);
  const [conversiones, setConversiones] = useState<UnitConversionDto[]>([]);
  const [selectedArticleId, setSelectedArticleId] = useState<string | undefined>();
  const [isVariable, setIsVariable] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form] = Form.useForm();

  useEffect(() => {
    if (!open) return;
    const loadData = async () => {
      try {
        const [detalle, a, u, c] = await Promise.all([
          menuApi.getItem(itemId),
          inventoryApi.getArticles({ activeOnly: true }),
          inventoryApi.getUnits(),
          inventoryApi.getConversions(),
        ]);
        setReceta(detalle.data.recipe.map(r => ({
          articleId: r.articleId,
          unitId: r.unitId,
          quantity: r.quantity,
          notes: r.notes,
          isVariable: r.isVariable,
          alternativeArticleIds: r.alternatives.map(a => a.articleId),
        })));
        setArticulos(a.data);
        setUnidades(u.data);
        setConversiones(c.data);
      } catch (e) { message.error(formatError(e)); }
    };
    loadData();
  }, [open, itemId]);

  const addIngrediente = async () => {
    const values = await form.validateFields();
    let articleId: string;
    let alternativeArticleIds: string[];

    if (values.isVariable) {
      // All options come from the multi-select; first = default
      const allOptions: string[] = values.optionArticleIds ?? [];
      if (allOptions.length === 0) {
        return;
      }
      articleId = allOptions[0];
      alternativeArticleIds = allOptions.slice(1);
    } else {
      articleId = values.articleId;
      alternativeArticleIds = [];
    }

    setReceta(prev => [...prev, {
      articleId,
      unitId: values.unitId,
      quantity: values.quantity,
      notes: values.notes,
      isVariable: values.isVariable ?? false,
      alternativeArticleIds,
    }]);
    form.resetFields();
    setSelectedArticleId(undefined);
    setIsVariable(false);
  };

  const removeIngrediente = (idx: number) => {
    setReceta(prev => prev.filter((_, i) => i !== idx));
  };

  const save = async () => {
    setSaving(true);
    try {
      await menuApi.upsertRecipe(itemId, receta);
      message.success('Receta guardada');
      onClose();
    } catch (e) { message.error(formatError(e)); }
    finally { setSaving(false); }
  };

  const getCompatibleUnitIds = (articleId: string | undefined): Set<string> => {
    if (!articleId) return new Set(unidades.map(u => u.id));
    const article = articulos.find(a => a.id === articleId);
    if (!article) return new Set(unidades.map(u => u.id));
    const ids = new Set<string>([article.baseUnitId]);
    for (const c of conversiones) {
      if (c.originUnitId === article.baseUnitId) ids.add(c.destinationUnitId);
      if (c.destinationUnitId === article.baseUnitId) ids.add(c.originUnitId);
    }
    return ids;
  };

  const compatibleUnitIds = getCompatibleUnitIds(selectedArticleId);
  const unidadOptions = unidades
    .filter(u => compatibleUnitIds.has(u.id))
    .map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id }));

  const articuloOptions = articulos.map(a => ({ label: `${a.name} (${a.baseUnitSymbol})`, value: a.id }));

  const handleArticleChange = (id: string) => {
    setSelectedArticleId(id);
    const newCompatible = getCompatibleUnitIds(id);
    const currentUnit = form.getFieldValue('unitId');
    if (currentUnit && !newCompatible.has(currentUnit)) {
      form.setFieldValue('unitId', undefined);
    }
    form.setFieldValue('alternativeArticleIds', []);
  };

  const getArticuloNombre = (id: string) => articulos.find(a => a.id === id)?.name ?? id;
  const getUnidadSimbolo = (id: string) => unidades.find(u => u.id === id)?.symbol ?? id;

  return (
    <Modal
      title={`Receta — ${itemName}`}
      open={open}
      onCancel={onClose}
      width={680}
      footer={[
        <Button key="cancel" onClick={onClose}>Cancelar</Button>,
        <Button key="save" type="primary" icon={<SaveOutlined />} loading={saving} onClick={save}>
          Guardar receta
        </Button>,
      ]}
    >
      <Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
        Define los ingredientes y cantidades necesarios para preparar este plato.
      </Text>

      <Table
        dataSource={receta}
        rowKey={(_, i) => String(i)}
        size="small"
        pagination={false}
        style={{ marginBottom: 16 }}
        locale={{ emptyText: 'Sin ingredientes aún' }}
        columns={[
          {
            title: 'Ingrediente', key: 'articulo',
            render: (_: unknown, r: UpsertRecipeIngredientDto) => {
              if (r.isVariable) {
                const allOptions = [r.articleId, ...r.alternativeArticleIds];
                return (
                  <Space size={4} wrap>
                    <Tag icon={<QuestionCircleOutlined />} color="orange" style={{ fontSize: 11 }}>Variable</Tag>
                    {allOptions.map(id => (
                      <Tag key={id} style={{ fontSize: 11 }}>{getArticuloNombre(id)}</Tag>
                    ))}
                  </Space>
                );
              }
              return <span>{getArticuloNombre(r.articleId)}</span>;
            },
          },
          {
            title: 'Cantidad', key: 'quantity',
            render: (_: unknown, r: UpsertRecipeIngredientDto) =>
              <Tag>{r.quantity} {getUnidadSimbolo(r.unitId)}</Tag>,
          },
          { title: 'Observación', dataIndex: 'notes', key: 'obs' },
          {
            title: '', key: 'del', width: 50,
            render: (_: unknown, __: unknown, idx: number) => (
              <Popconfirm title="¿Quitar?" onConfirm={() => removeIngrediente(idx)}>
                <Button size="small" danger icon={<DeleteOutlined />} />
              </Popconfirm>
            ),
          },
        ]}
      />

      <Form form={form} layout="vertical" style={{ background: '#fafafa', padding: 12, borderRadius: 8 }}>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'flex-end' }}>
          <Form.Item name="quantity" label="Cantidad" rules={[{ required: true }]} style={{ width: 100, marginBottom: 8 }}>
            <InputNumber placeholder="Cant." min={0} step={0.01} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="unitId" label="Unidad" rules={[{ required: true }]} style={{ width: 130, marginBottom: 8 }}>
            <Select options={unidadOptions} placeholder="Unidad" />
          </Form.Item>
          <Form.Item name="notes" label="Observación" style={{ flex: '1 1 140px', marginBottom: 8 }}>
            <Input placeholder="Opcional" />
          </Form.Item>
          <Form.Item name="isVariable" label="Variable" valuePropName="checked" style={{ marginBottom: 8 }}>
            <Switch onChange={v => {
              setIsVariable(v);
              form.setFieldValue('articleId', undefined);
              form.setFieldValue('optionArticleIds', []);
              setSelectedArticleId(undefined);
            }} />
          </Form.Item>
        </div>

        {isVariable ? (
          <Form.Item
            name="optionArticleIds"
            label="Opciones disponibles"
            help="El mesero elegirá entre estas opciones al tomar el pedido. La primera de la lista es la predeterminada."
            rules={[{ required: true, type: 'array', min: 2, message: 'Agrega al menos 2 opciones' }]}
          >
            <Select
              mode="multiple"
              options={articuloOptions}
              placeholder="Seleccionar opciones (ej: Salsa BBQ, Salsa mostaza, Salsa teriyaki)..."
              showSearch
              optionFilterProp="label"
              style={{ width: '100%' }}
            />
          </Form.Item>
        ) : (
          <Form.Item name="articleId" label="Ingrediente" rules={[{ required: true }]} style={{ marginBottom: 8 }}>
            <Select options={articuloOptions} placeholder="Seleccionar ingrediente" showSearch optionFilterProp="label" onChange={handleArticleChange} />
          </Form.Item>
        )}

        <Button icon={<PlusOutlined />} onClick={addIngrediente} type="dashed" block>
          Agregar ingrediente
        </Button>
      </Form>
    </Modal>
  );
}
