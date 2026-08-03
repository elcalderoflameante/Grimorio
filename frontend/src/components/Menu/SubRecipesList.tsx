import { useCallback, useEffect, useMemo, useState } from 'react';
import { App as AntApp, Button, Form, Input, InputNumber, Modal, Popconfirm, Select, Space, Switch, Table, Tag, Typography } from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined, ReloadOutlined, SaveOutlined } from '@ant-design/icons';
import { inventoryApi, menuApi } from '../../services/api';
import type { InventoryArticleDto, MeasurementUnitDto, SubRecipeDto, UnitConversionDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Title } = Typography;

type SubRecipeFormValues = {
  name: string;
  description?: string;
  outputQuantity: number;
  outputUnitId: string;
  isActive: boolean;
  ingredients: Array<{ articleId: string; quantity: number; unitId: string; notes?: string }>;
};

export default function SubRecipesList() {
  const { message } = AntApp.useApp();
  const { hasPermission } = useAuth();
  const [subRecipes, setSubRecipes] = useState<SubRecipeDto[]>([]);
  const [articles, setArticles] = useState<InventoryArticleDto[]>([]);
  const [units, setUnits] = useState<MeasurementUnitDto[]>([]);
  const [conversions, setConversions] = useState<UnitConversionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<SubRecipeDto | null>(null);
  const [form] = Form.useForm<SubRecipeFormValues>();
  const canManage = hasPermission(PERMISSIONS.menu.itemsManage);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [subRecipeRes, articleRes, unitRes, conversionRes] = await Promise.all([
        menuApi.getSubRecipes(),
        inventoryApi.getArticles({ activeOnly: true }),
        inventoryApi.getUnits(),
        inventoryApi.getConversions(),
      ]);
      setSubRecipes(subRecipeRes.data);
      setArticles(articleRes.data);
      setUnits(unitRes.data);
      setConversions(conversionRes.data);
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setLoading(false);
    }
  }, [message]);

  useEffect(() => { load(); }, [load]);

  const unitOptionsForArticle = (articleId?: string) => {
    const article = articles.find(a => a.id === articleId);
    if (!article) return [];
    const ids = new Set<string>([article.baseUnitId]);
    conversions.forEach(c => {
      if (c.originUnitId === article.baseUnitId) ids.add(c.destinationUnitId);
      if (c.destinationUnitId === article.baseUnitId) ids.add(c.originUnitId);
    });
    return units.filter(u => ids.has(u.id)).map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id }));
  };

  const unitOptions = useMemo(() => units.map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id })), [units]);
  const articleOptions = useMemo(() => articles.map(a => ({ label: `${a.name} (${a.baseUnitSymbol})`, value: a.id })), [articles]);

  const openModal = (subRecipe?: SubRecipeDto) => {
    setEditing(subRecipe ?? null);
    form.setFieldsValue(subRecipe ? {
      name: subRecipe.name,
      description: subRecipe.description,
      outputQuantity: subRecipe.outputQuantity,
      outputUnitId: subRecipe.outputUnitId,
      isActive: subRecipe.isActive,
      ingredients: subRecipe.ingredients.map(i => ({ articleId: i.articleId, quantity: i.quantity, unitId: i.unitId, notes: i.notes })),
    } : { outputQuantity: 1, isActive: true, ingredients: [{} as SubRecipeFormValues['ingredients'][number]] });
    setOpen(true);
  };

  const closeModal = () => {
    setOpen(false);
    setEditing(null);
    form.resetFields();
  };

  const save = async () => {
    const values = await form.validateFields();
    setSaving(true);
    try {
      if (editing) await menuApi.updateSubRecipe(editing.id, values);
      else await menuApi.createSubRecipe(values);
      message.success('Subreceta guardada');
      closeModal();
      load();
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: string) => {
    try {
      await menuApi.deleteSubRecipe(id);
      message.success('Subreceta eliminada');
      load();
    } catch (e) {
      message.error(formatError(e));
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Subrecetas</Title>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading} />
          {canManage && <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>Nueva subreceta</Button>}
        </Space>
      </div>

      <Table
        dataSource={subRecipes}
        rowKey="id"
        loading={loading}
        size="small"
        columns={[
          { title: 'Nombre', dataIndex: 'name' },
          { title: 'Rinde', render: (_: unknown, r: SubRecipeDto) => `${r.outputQuantity} ${r.outputUnitSymbol}`, width: 120 },
          { title: 'Insumos', render: (_: unknown, r: SubRecipeDto) => r.ingredients.length, width: 90 },
          { title: 'Estado', render: (_: unknown, r: SubRecipeDto) => <Tag color={r.isActive ? 'green' : 'default'}>{r.isActive ? 'Activa' : 'Inactiva'}</Tag>, width: 110 },
          {
            title: '', width: 90,
            render: (_: unknown, r: SubRecipeDto) => canManage && (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openModal(r)} />
                <Popconfirm title="Eliminar subreceta?" onConfirm={() => remove(r.id)}>
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Editar subreceta' : 'Nueva subreceta'}
        open={open}
        onCancel={closeModal}
        width={860}
        confirmLoading={saving}
        footer={[
          <Button key="cancel" onClick={closeModal}>Cancelar</Button>,
          <Button key="save" type="primary" icon={<SaveOutlined />} loading={saving} onClick={save}>Guardar</Button>,
        ]}
      >
        <Form form={form} layout="vertical">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 180px 180px 110px', gap: 12 }}>
            <Form.Item name="name" label="Nombre" rules={[{ required: true }]}><Input /></Form.Item>
            <Form.Item name="outputQuantity" label="Rinde" rules={[{ required: true }]}><InputNumber min={0.0001} step={0.01} style={{ width: '100%' }} /></Form.Item>
            <Form.Item name="outputUnitId" label="Unidad" rules={[{ required: true }]}><Select options={unitOptions} showSearch optionFilterProp="label" /></Form.Item>
            <Form.Item name="isActive" label="Activa" valuePropName="checked"><Switch /></Form.Item>
          </div>
          <Form.Item name="description" label="Descripción"><Input.TextArea rows={2} /></Form.Item>

          <Form.List name="ingredients">
            {(fields, { add, remove }) => (
              <>
                <Table
                  dataSource={fields}
                  rowKey="key"
                  size="small"
                  pagination={false}
                  columns={[
                    {
                      title: 'Artículo',
                      render: (_: unknown, field) => (
                        <Form.Item name={[field.name, 'articleId']} rules={[{ required: true }]} style={{ margin: 0 }}>
                          <Select options={articleOptions} showSearch optionFilterProp="label" />
                        </Form.Item>
                      ),
                    },
                    {
                      title: 'Cantidad',
                      width: 120,
                      render: (_: unknown, field) => (
                        <Form.Item name={[field.name, 'quantity']} rules={[{ required: true }]} style={{ margin: 0 }}>
                          <InputNumber min={0.0001} step={0.01} style={{ width: '100%' }} />
                        </Form.Item>
                      ),
                    },
                    {
                      title: 'Unidad',
                      width: 170,
                      render: (_: unknown, field) => (
                        <Form.Item shouldUpdate noStyle>
                          {() => (
                            <Form.Item name={[field.name, 'unitId']} rules={[{ required: true }]} style={{ margin: 0 }}>
                              <Select options={unitOptionsForArticle(form.getFieldValue(['ingredients', field.name, 'articleId']))} showSearch optionFilterProp="label" />
                            </Form.Item>
                          )}
                        </Form.Item>
                      ),
                    },
                    {
                      title: 'Notas',
                      render: (_: unknown, field) => <Form.Item name={[field.name, 'notes']} style={{ margin: 0 }}><Input /></Form.Item>,
                    },
                    { title: '', width: 48, render: (_: unknown, field) => <Button danger size="small" icon={<DeleteOutlined />} onClick={() => remove(field.name)} /> },
                  ]}
                />
                <Button type="dashed" icon={<PlusOutlined />} onClick={() => add()} block style={{ marginTop: 12 }}>Agregar insumo</Button>
              </>
            )}
          </Form.List>
        </Form>
      </Modal>
    </div>
  );
}
