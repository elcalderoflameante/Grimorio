import { useEffect, useState } from 'react';
import { App as AntApp, Button, Input, InputNumber, Modal, Popconfirm, Select, Space, Switch, Tag, Typography } from 'antd';
import { DeleteOutlined, PlusOutlined, SaveOutlined } from '@ant-design/icons';
import { inventoryApi, menuApi } from '../../services/api';
import type {
  InventoryArticleDto,
  MeasurementUnitDto,
  UnitConversionDto,
  UpsertMenuItemModifierGroupDto,
  UpsertMenuItemModifierOptionDto,
} from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Text } = Typography;

interface Props {
  itemId: string;
  itemName: string;
  open: boolean;
  onClose: () => void;
}

const emptyOption = (displayOrder: number): UpsertMenuItemModifierOptionDto => ({
  name: '',
  quantity: 0,
  priceDelta: 0,
  displayOrder,
  isActive: true,
});

const emptyGroup = (displayOrder: number): UpsertMenuItemModifierGroupDto => ({
  name: '',
  minSelections: 1,
  maxSelections: 1,
  isRequired: true,
  allowDuplicates: false,
  displayOrder,
  isActive: true,
  options: [emptyOption(0)],
});

export default function ModifierEditor({ itemId, itemName, open, onClose }: Props) {
  const { message } = AntApp.useApp();

  const [groups, setGroups] = useState<UpsertMenuItemModifierGroupDto[]>([]);
  const [articles, setArticles] = useState<InventoryArticleDto[]>([]);
  const [units, setUnits] = useState<MeasurementUnitDto[]>([]);
  const [conversions, setConversions] = useState<UnitConversionDto[]>([]);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!open) return;
    const loadData = async () => {
      try {
        const [detail, articleRes, unitRes, conversionRes] = await Promise.all([
          menuApi.getItem(itemId),
          inventoryApi.getArticles({ activeOnly: true }),
          inventoryApi.getUnits(),
          inventoryApi.getConversions(),
        ]);
        setGroups((detail.data.modifierGroups ?? []).map(g => ({
          id: g.id,
          name: g.name,
          minSelections: g.minSelections,
          maxSelections: g.maxSelections,
          isRequired: g.isRequired,
          allowDuplicates: g.allowDuplicates,
          displayOrder: g.displayOrder,
          isActive: g.isActive,
          options: g.options.map(o => ({
            id: o.id,
            name: o.name,
            articleId: o.articleId,
            unitId: o.unitId,
            quantity: o.quantity,
            priceDelta: o.priceDelta,
            displayOrder: o.displayOrder,
            isActive: o.isActive,
          })),
        })));
        setArticles(articleRes.data);
        setUnits(unitRes.data);
        setConversions(conversionRes.data);
      } catch (e) { message.error(formatError(e)); }
    };
    loadData();
  }, [itemId, open]);

  const updateGroup = (idx: number, patch: Partial<UpsertMenuItemModifierGroupDto>) =>
    setGroups(prev => prev.map((g, i) => i === idx ? { ...g, ...patch } : g));

  const updateOption = (groupIdx: number, optionIdx: number, patch: Partial<UpsertMenuItemModifierOptionDto>) =>
    setGroups(prev => prev.map((g, i) => i === groupIdx
      ? { ...g, options: g.options.map((o, oi) => oi === optionIdx ? { ...o, ...patch } : o) }
      : g));

  const getCompatibleUnitIds = (articleId: string | undefined): Set<string> => {
    if (!articleId) return new Set(units.map(u => u.id));
    const article = articles.find(a => a.id === articleId);
    if (!article) return new Set(units.map(u => u.id));
    const ids = new Set<string>([article.baseUnitId]);
    for (const conversion of conversions) {
      if (conversion.originUnitId === article.baseUnitId) ids.add(conversion.destinationUnitId);
      if (conversion.destinationUnitId === article.baseUnitId) ids.add(conversion.originUnitId);
    }
    return ids;
  };

  const validate = () => {
    for (const group of groups) {
      if (!group.name.trim()) return 'Cada grupo requiere nombre.';
      if (group.maxSelections < 1) return `El grupo ${group.name} requiere máximo mayor a cero.`;
      if (group.minSelections < 0 || group.minSelections > group.maxSelections) return `Revisa mínimo/máximo en ${group.name}.`;
      if (group.options.length === 0) return `El grupo ${group.name} requiere opciones.`;
      for (const option of group.options) {
        if (!option.name.trim()) return `Una opción de ${group.name} requiere nombre.`;
        if (option.articleId && (!option.unitId || option.quantity <= 0)) return `${option.name} requiere unidad y cantidad de inventario.`;
      }
    }
    return null;
  };

  const save = async () => {
    const error = validate();
    if (error) {
      message.warning(error);
      return;
    }
    setSaving(true);
    try {
      await menuApi.upsertModifiers(itemId, groups);
      message.success('Modificadores guardados');
      onClose();
    } catch (e) { message.error(formatError(e)); }
    finally { setSaving(false); }
  };

  const articleOptions = articles.map(a => ({ label: `${a.name} (${a.baseUnitSymbol})`, value: a.id }));

  return (
    <Modal
      title={`Modificadores - ${itemName}`}
      open={open}
      onCancel={onClose}
      width={820}
      footer={[
        <Button key="cancel" onClick={onClose}>Cancelar</Button>,
        <Button key="save" type="primary" icon={<SaveOutlined />} loading={saving} onClick={save}>Guardar modificadores</Button>,
      ]}
    >
      <Text type="secondary" style={{ display: 'block', marginBottom: 14 }}>
        Define grupos como Salsas, Cerveza o Toppings, con mínimos, máximos y precios adicionales.
      </Text>

      <Space orientation="vertical" style={{ width: '100%' }} size={12}>
        {groups.map((group, groupIdx) => (
          <div key={groupIdx} style={{ border: '1px solid #f0f0f0', borderRadius: 8, padding: 12 }}>
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'minmax(170px, 1fr) 104px 104px 120px 96px 34px',
                gap: 8,
                alignItems: 'center',
                marginBottom: 10,
              }}
            >
              <Input
                placeholder="Nombre del grupo"
                value={group.name}
                onChange={e => updateGroup(groupIdx, { name: e.target.value })}
                style={{ width: '100%' }}
              />
              <InputNumber min={0} value={group.minSelections} onChange={v => updateGroup(groupIdx, { minSelections: v ?? 0 })} addonBefore="Min" style={{ width: '100%' }} />
              <InputNumber min={1} value={group.maxSelections} onChange={v => updateGroup(groupIdx, { maxSelections: v ?? 1 })} addonBefore="Max" style={{ width: '100%' }} />
              <Space size={4} style={{ whiteSpace: 'nowrap' }}><Text type="secondary">Obligatorio</Text><Switch checked={group.isRequired} onChange={v => updateGroup(groupIdx, { isRequired: v })} /></Space>
              <Space size={4} style={{ whiteSpace: 'nowrap' }}><Text type="secondary">Repetir</Text><Switch checked={group.allowDuplicates} onChange={v => updateGroup(groupIdx, { allowDuplicates: v })} /></Space>
              <Popconfirm title="Quitar grupo" onConfirm={() => setGroups(prev => prev.filter((_, i) => i !== groupIdx))}>
                <Button danger size="small" icon={<DeleteOutlined />} />
              </Popconfirm>
            </div>

            <Space orientation="vertical" style={{ width: '100%' }} size={8}>
              {group.options.map((option, optionIdx) => {
                const compatibleUnitIds = getCompatibleUnitIds(option.articleId);
                const unitOptions = units
                  .filter(u => compatibleUnitIds.has(u.id))
                  .map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id }));
                return (
                  <div
                    key={optionIdx}
                    style={{
                      display: 'grid',
                      gridTemplateColumns: '34px minmax(130px, 1fr) 110px minmax(180px, 1.4fr) 86px 118px 34px',
                      gap: 8,
                      alignItems: 'center',
                      width: '100%',
                    }}
                  >
                    <Tag color="blue" style={{ marginInlineEnd: 0, textAlign: 'center' }}>{optionIdx + 1}</Tag>
                    <Input
                      placeholder="Opción"
                      value={option.name}
                      onChange={e => updateOption(groupIdx, optionIdx, { name: e.target.value })}
                      style={{ width: '100%' }}
                    />
                    <InputNumber
                      min={0}
                      step={0.01}
                      value={option.priceDelta}
                      onChange={v => updateOption(groupIdx, optionIdx, { priceDelta: v ?? 0 })}
                      addonBefore="+$"
                      style={{ width: '100%' }}
                    />
                    <Select
                      allowClear
                      showSearch
                      optionFilterProp="label"
                      placeholder="Artículo inventario"
                      value={option.articleId}
                      options={articleOptions}
                      onChange={v => updateOption(groupIdx, optionIdx, { articleId: v, unitId: undefined, quantity: v ? option.quantity : 0 })}
                      style={{ width: '100%' }}
                    />
                    <InputNumber
                      min={0}
                      step={0.01}
                      value={option.quantity}
                      disabled={!option.articleId}
                      onChange={v => updateOption(groupIdx, optionIdx, { quantity: v ?? 0 })}
                      style={{ width: '100%' }}
                    />
                    <Select
                      allowClear
                      placeholder="Unidad"
                      value={option.unitId}
                      disabled={!option.articleId}
                      options={unitOptions}
                      onChange={v => updateOption(groupIdx, optionIdx, { unitId: v })}
                      style={{ width: '100%' }}
                    />
                    <Popconfirm title="Quitar opción" onConfirm={() => updateGroup(groupIdx, { options: group.options.filter((_, i) => i !== optionIdx) })}>
                      <Button danger size="small" icon={<DeleteOutlined />} />
                    </Popconfirm>
                  </div>
                );
              })}
            </Space>

            <Button
              type="dashed"
              icon={<PlusOutlined />}
              style={{ marginTop: 10 }}
              onClick={() => updateGroup(groupIdx, { options: [...group.options, emptyOption(group.options.length)] })}
            >
              Agregar opción
            </Button>
          </div>
        ))}
      </Space>

      <Button
        type="dashed"
        block
        icon={<PlusOutlined />}
        style={{ marginTop: 14 }}
        onClick={() => setGroups(prev => [...prev, emptyGroup(prev.length)])}
      >
        Agregar grupo
      </Button>
    </Modal>
  );
}
