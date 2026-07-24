import { useEffect, useState } from 'react';
import { App as AntApp, Table, Button, Modal, Form, Select, InputNumber, Input, Popconfirm, Typography, Tag } from 'antd';
import { PlusOutlined, DeleteOutlined, SaveOutlined } from '@ant-design/icons';
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
  const { message } = AntApp.useApp();

  const [receta, setReceta] = useState<UpsertRecipeIngredientDto[]>([]);
  const [articulos, setArticulos] = useState<InventoryArticleDto[]>([]);
  const [unidades, setUnidades] = useState<MeasurementUnitDto[]>([]);
  const [conversiones, setConversiones] = useState<UnitConversionDto[]>([]);
  const [selectedArticleId, setSelectedArticleId] = useState<string | undefined>();
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
    setReceta(prev => [...prev, {
      articleId: values.articleId,
      unitId: values.unitId,
      quantity: values.quantity,
      notes: values.notes,
    }]);
    form.resetFields();
    setSelectedArticleId(undefined);
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
  };

  const getArticuloNombre = (id: string) => articulos.find(a => a.id === id)?.name ?? id;
  const getUnidadSimbolo = (id: string) => unidades.find(u => u.id === id)?.symbol ?? id;

  return (
    <Modal
      title={`Receta - ${itemName}`}
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
        locale={{ emptyText: 'Sin ingredientes aun' }}
        columns={[
          {
            title: 'Ingrediente', key: 'articulo',
            render: (_: unknown, r: UpsertRecipeIngredientDto) => <span>{getArticuloNombre(r.articleId)}</span>,
          },
          {
            title: 'Cantidad', key: 'quantity',
            render: (_: unknown, r: UpsertRecipeIngredientDto) =>
              <Tag>{r.quantity} {getUnidadSimbolo(r.unitId)}</Tag>,
          },
          { title: 'Observacion', dataIndex: 'notes', key: 'obs' },
          {
            title: '', key: 'del', width: 50,
            render: (_: unknown, __: unknown, idx: number) => (
              <Popconfirm title="Quitar?" onConfirm={() => removeIngrediente(idx)}>
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
          <Form.Item name="notes" label="Observacion" style={{ flex: '1 1 140px', marginBottom: 8 }}>
            <Input placeholder="Opcional" />
          </Form.Item>
        </div>

        <Form.Item name="articleId" label="Ingrediente" rules={[{ required: true }]} style={{ marginBottom: 8 }}>
          <Select options={articuloOptions} placeholder="Seleccionar ingrediente" showSearch optionFilterProp="label" onChange={handleArticleChange} />
        </Form.Item>

        <Button icon={<PlusOutlined />} onClick={addIngrediente} type="dashed" block>
          Agregar ingrediente
        </Button>
      </Form>
    </Modal>
  );
}
