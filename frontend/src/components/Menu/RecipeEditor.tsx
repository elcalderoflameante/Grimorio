import { useEffect, useState } from 'react';
import { Table, Button, Modal, Form, Select, InputNumber, Input, Popconfirm, Typography, message, Tag } from 'antd';
import { PlusOutlined, DeleteOutlined, SaveOutlined } from '@ant-design/icons';
import { menuApi, inventoryApi } from '../../services/api';
import type { InventoryArticleDto, MeasurementUnitDto, UpsertRecipeIngredientDto } from '../../types';
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
  const [saving, setSaving] = useState(false);
  const [form] = Form.useForm();

  useEffect(() => {
    if (!open) return;
    const loadData = async () => {
      try {
        const [detalle, a, u] = await Promise.all([
          menuApi.getItem(itemId),
          inventoryApi.getArticles({ activeOnly: true }),
          inventoryApi.getUnits(),
        ]);
        setReceta(detalle.data.recipe.map(r => ({
          articleId: r.articleId,
          unitId: r.unitId,
          quantity: r.quantity,
          notes: r.notes,
        })));
        setArticulos(a.data);
        setUnidades(u.data);
      } catch (e) { message.error(formatError(e)); }
    };
    loadData();
  }, [open, itemId]);

  const addIngrediente = async () => {
    const values = await form.validateFields();
    setReceta(prev => [...prev, { ...values }]);
    form.resetFields();
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

  const articuloOptions = articulos.map(a => ({ label: `${a.name} (${a.baseUnitSymbol})`, value: a.id }));
  const unidadOptions = unidades.map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id }));

  const getArticuloNombre = (id: string) => articulos.find(a => a.id === id)?.name ?? id;
  const getUnidadSimbolo = (id: string) => unidades.find(u => u.id === id)?.symbol ?? id;

  return (
    <Modal
      title={`Receta — ${itemName}`}
      open={open}
      onCancel={onClose}
      width={640}
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
            render: (_: unknown, r: UpsertRecipeIngredientDto) => getArticuloNombre(r.articleId),
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

      <Form form={form} layout="inline" style={{ flexWrap: 'wrap', gap: 8 }}>
        <Form.Item name="articleId" rules={[{ required: true }]} style={{ flex: '1 1 200px' }}>
          <Select options={articuloOptions} placeholder="Ingrediente" showSearch optionFilterProp="label" />
        </Form.Item>
        <Form.Item name="quantity" rules={[{ required: true }]} style={{ width: 100 }}>
          <InputNumber placeholder="Cant." min={0} step={0.01} style={{ width: '100%' }} />
        </Form.Item>
        <Form.Item name="unitId" rules={[{ required: true }]} style={{ width: 120 }}>
          <Select options={unidadOptions} placeholder="Unidad" />
        </Form.Item>
        <Form.Item name="notes" style={{ flex: '1 1 150px' }}>
          <Input placeholder="Observación (opcional)" />
        </Form.Item>
        <Button icon={<PlusOutlined />} onClick={addIngrediente}>Agregar</Button>
      </Form>
    </Modal>
  );
}
