import { useEffect, useState, type ReactNode } from 'react';
import { App as AntApp, Button, Grid, Input, InputNumber, Modal, Popconfirm, Select, Space, Switch, Tooltip, Typography } from 'antd';
import { DeleteOutlined, DownOutlined, PlusOutlined, SaveOutlined, UpOutlined } from '@ant-design/icons';
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

const createModifierId = () => {
  if (crypto.randomUUID) return crypto.randomUUID();
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, byte => byte.toString(16).padStart(2, '0'));
  return `${hex.slice(0, 4).join('')}-${hex.slice(4, 6).join('')}-${hex.slice(6, 8).join('')}-${hex.slice(8, 10).join('')}-${hex.slice(10).join('')}`;
};

interface Props {
  itemId: string;
  itemName: string;
  open: boolean;
  onClose: () => void;
}

const emptyOption = (displayOrder: number): UpsertMenuItemModifierOptionDto => ({
  id: createModifierId(),
  name: '',
  quantity: 0,
  priceDelta: 0,
  displayOrder,
  isActive: true,
});

const emptyGroup = (displayOrder: number): UpsertMenuItemModifierGroupDto => ({
  id: createModifierId(),
  name: '',
  minSelections: 1,
  maxSelections: 1,
  isRequired: true,
  allowDuplicates: false,
  displayOrder,
  isActive: true,
  options: [emptyOption(1)],
});

const nextDisplayOrder = (items: { displayOrder: number }[]) =>
  items.length === 0 ? 1 : Math.max(...items.map(item => item.displayOrder ?? 0)) + 1;

const normalizeDisplayOrder = <T extends { displayOrder: number }>(items: T[]) =>
  items.map((item, index) => ({ ...item, displayOrder: index + 1 }));

function OptionField({ area, label, showLabel, children }: { area: string; label: string; showLabel: boolean; children: ReactNode }) {
  return <div style={{ gridArea: area, minWidth: 0, display: 'grid', gap: 4 }}>
    {showLabel && <Text type="secondary" style={{ fontSize: 12 }}>{label}</Text>}
    {children}
  </div>;
}

export default function ModifierEditor({ itemId, itemName, open, onClose }: Props) {
  const { message } = AntApp.useApp();
  const screens = Grid.useBreakpoint();
  const optionColumns = screens.lg
    ? '56px 120px 110px minmax(160px, 1fr) 96px 140px 32px'
    : 'repeat(2, minmax(0, 1fr))';
  const optionAreas = screens.lg
    ? '"move name price article quantity unit remove" "additional additional additional additional additional additional additional"'
    : '"move remove" "name price" "article article" "quantity unit" "additional additional"';

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
          quantitySourceGroupId: g.quantitySourceGroupId ?? undefined,
          quantityRules: g.quantityRules ?? [],
          name: g.name,
          minSelections: g.minSelections,
          maxSelections: g.maxSelections,
          isRequired: g.isRequired,
          allowDuplicates: g.allowDuplicates,
          displayOrder: g.displayOrder ?? 1,
          isActive: g.isActive,
          options: g.options.map(o => ({
            id: o.id,
            name: o.name,
            articleId: o.articleId,
            unitId: o.unitId,
            quantity: o.quantity,
            additionalIngredients: o.additionalIngredients ?? [],
            priceDelta: o.priceDelta,
            displayOrder: o.displayOrder ?? 1,
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

  const moveOption = (groupIdx: number, optionIdx: number, direction: -1 | 1) =>
    setGroups(prev => prev.map((group, currentGroupIdx) => {
      if (currentGroupIdx !== groupIdx) return group;
      const targetIdx = optionIdx + direction;
      if (targetIdx < 0 || targetIdx >= group.options.length) return group;
      const options = [...group.options];
      [options[optionIdx], options[targetIdx]] = [options[targetIdx], options[optionIdx]];
      return { ...group, options: normalizeDisplayOrder(options) };
    }));

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
        if (option.articleId && (!option.unitId || (!group.quantitySourceGroupId && option.quantity <= 0))) return `${option.name} requiere unidad y cantidad de inventario.`;
        if (option.additionalIngredients?.some(i => !i.articleId || !i.unitId || i.quantity <= 0)) return `${option.name}: revisa los consumos adicionales.`;
      }
      if (group.quantitySourceGroupId) {
        const source = groups.find(g => g.id === group.quantitySourceGroupId);
        if (!source || source.quantitySourceGroupId || source.maxSelections !== 1 || !source.isActive || (!source.isRequired && source.minSelections !== 1)) return `${group.name}: revisa el grupo que determina la cantidad.`;
        if (source.options.filter(o => o.isActive).some(o => (group.quantityRules?.find(r => r.sourceOptionId === o.id)?.quantity ?? 0) <= 0)) return `${group.name}: configura la cantidad para cada tamaño.`;
        if (group.options.filter(o => o.isActive).some(o => !o.articleId || !o.unitId)) return `${group.name}: cada sabor requiere artículo y unidad.`;
        if (new Set(group.options.filter(o => o.isActive).map(o => o.unitId)).size !== 1) return `${group.name}: utiliza la misma unidad en todos los sabores.`;
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
      await menuApi.upsertModifiers(itemId, groups.map(group => {
        const rules = group.quantitySourceGroupId ? groups.find(g => g.id === group.quantitySourceGroupId)!.options.filter(o => o.isActive)
          .map(o => ({ sourceOptionId: o.id!, quantity: group.quantityRules!.find(r => r.sourceOptionId === o.id)!.quantity })) : [];
        return { ...group, quantityRules: rules, options: normalizeDisplayOrder(group.options).map(option => ({ ...option,
          quantity: rules.length ? Math.min(...rules.map(r => r.quantity)) : option.quantity })) };
      }));
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
      width={1040}
      footer={[
        <Button key="cancel" onClick={onClose}>Cancelar</Button>,
        <Button key="save" type="primary" icon={<SaveOutlined />} loading={saving} onClick={save}>Guardar modificadores</Button>,
      ]}
    >
      <Space orientation="vertical" style={{ width: '100%' }} size={12}>
        {groups.map((group, groupIdx) => (
          <div key={groupIdx} style={{ border: '1px solid #f0f0f0', borderRadius: 8, padding: 12 }}>
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: screens.lg ? 'minmax(140px, 1fr) 112px 104px 104px 122px 100px 32px' : 'repeat(2, minmax(0, 1fr))',
                gap: 8,
                alignItems: 'center',
                marginBottom: 10,
              }}
            >
              <Input
                placeholder="Nombre del grupo"
                aria-label={`Nombre del grupo ${groupIdx + 1}`}
                value={group.name}
                onChange={e => updateGroup(groupIdx, { name: e.target.value })}
                style={{ width: '100%' }}
              />
              <InputNumber
                aria-label={`Orden del grupo ${groupIdx + 1}`}
                min={0}
                value={group.displayOrder}
                onChange={v => updateGroup(groupIdx, { displayOrder: v ?? 0 })}
                addonBefore="Orden"
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

            <div style={{ marginBottom: 12 }}>
              <Text style={{ display: 'block', marginBottom: 4 }}>Cantidad de inventario según</Text>
              <Select aria-label={`Cantidad según: ${group.name}`} allowClear placeholder="Cantidad fija por opción" style={{ width: '100%' }}
                value={group.quantitySourceGroupId} options={groups.filter(g => g.id !== group.id && g.isActive && g.maxSelections === 1
                  && !g.allowDuplicates && !g.quantitySourceGroupId && (g.isRequired || g.minSelections === 1)).map(g => ({ value: g.id!, label: g.name }))}
                onChange={id => updateGroup(groupIdx, { quantitySourceGroupId: id, quantityRules: id
                  ? groups.find(g => g.id === id)!.options.filter(o => o.isActive).map(o => ({ sourceOptionId: o.id!, quantity: 0 })) : [] })} />
              {group.quantitySourceGroupId && <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(130px, 1fr))', gap: 8, marginTop: 8 }}>
                {groups.find(g => g.id === group.quantitySourceGroupId)?.options.filter(o => o.isActive).map(sourceOption => <label key={sourceOption.id}>
                  <Text style={{ display: 'block', fontSize: 12 }}>{sourceOption.name}</Text>
                  <InputNumber aria-label={`Consumo de ${group.name} para ${sourceOption.name}`} min={0.0001} precision={4} style={{ width: '100%' }}
                    addonAfter={units.find(u => u.id === group.options.find(o => o.isActive)?.unitId)?.symbol}
                    value={group.quantityRules?.find(r => r.sourceOptionId === sourceOption.id)?.quantity ?? 0}
                    onChange={n => updateGroup(groupIdx, { quantityRules: groups.find(g => g.id === group.quantitySourceGroupId)!.options.filter(o => o.isActive)
                      .map(o => ({ sourceOptionId: o.id!, quantity: o.id === sourceOption.id ? n ?? 0 : group.quantityRules?.find(r => r.sourceOptionId === o.id)?.quantity ?? 0 })) })} />
                </label>)}
              </div>}
            </div>
            {screens.lg && <div style={{ display: 'grid', gridTemplateColumns: optionColumns, gap: 8, marginBottom: 6 }}>
              {['', 'Nombre', 'Precio adicional', 'Artículo inventario', 'Cantidad', 'Unidad', ''].map((label, index) =>
                <Text key={index} type="secondary" style={{ fontSize: 12 }}>{label}</Text>)}
            </div>}
            <Space orientation="vertical" style={{ width: '100%' }} size={8}>
              {group.options.map((option, optionIdx) => {
                const compatibleUnitIds = getCompatibleUnitIds(option.articleId);
                const unitOptions = units
                  .filter(u => compatibleUnitIds.has(u.id))
                  .map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id }));
                return (
                  <div
                    key={option.id ?? optionIdx}
                    style={{
                      display: 'grid',
                      gridTemplateColumns: optionColumns,
                      gridTemplateAreas: optionAreas,
                      gap: 8,
                      alignItems: 'center',
                      width: '100%',
                    }}
                  >
                    <Space.Compact style={{ gridArea: 'move', alignSelf: 'center' }}>
                      <Tooltip title="Subir opción">
                        <Button
                          size="small"
                          aria-label={`Subir opción ${option.name || optionIdx + 1} de ${group.name || groupIdx + 1}`}
                          icon={<UpOutlined />}
                          disabled={optionIdx === 0}
                          onClick={() => moveOption(groupIdx, optionIdx, -1)}
                        />
                      </Tooltip>
                      <Tooltip title="Bajar opción">
                        <Button
                          size="small"
                          aria-label={`Bajar opción ${option.name || optionIdx + 1} de ${group.name || groupIdx + 1}`}
                          icon={<DownOutlined />}
                          disabled={optionIdx === group.options.length - 1}
                          onClick={() => moveOption(groupIdx, optionIdx, 1)}
                        />
                      </Tooltip>
                    </Space.Compact>
                    <OptionField area="name" label="Nombre" showLabel={!screens.lg}>
                      <Input
                        aria-label={`Nombre de opción ${optionIdx + 1} de ${group.name || groupIdx + 1}`}
                        placeholder="Opción"
                        value={option.name}
                        onChange={e => updateOption(groupIdx, optionIdx, { name: e.target.value })}
                        style={{ width: '100%' }}
                      />
                    </OptionField>
                    <OptionField area="price" label="Precio adicional" showLabel={!screens.lg}>
                      <InputNumber
                        aria-label={`Precio adicional de ${option.name || optionIdx + 1} de ${group.name || groupIdx + 1}`}
                        min={0}
                        step={0.01}
                        value={option.priceDelta}
                        onChange={v => updateOption(groupIdx, optionIdx, { priceDelta: v ?? 0 })}
                        addonBefore="+$"
                        style={{ width: '100%' }}
                      />
                    </OptionField>
                    <OptionField area="article" label="Artículo inventario" showLabel={!screens.lg}>
                      <Select
                        aria-label={`Artículo de ${option.name || optionIdx + 1} de ${group.name || groupIdx + 1}`}
                        allowClear
                        showSearch
                        optionFilterProp="label"
                        placeholder="Artículo inventario"
                        value={option.articleId}
                        options={articleOptions}
                        onChange={v => updateOption(groupIdx, optionIdx, { articleId: v, unitId: undefined, quantity: v ? option.quantity : 0 })}
                        style={{ width: '100%' }}
                      />
                    </OptionField>
                    <OptionField area="quantity" label="Cantidad" showLabel={!screens.lg}>
                      <InputNumber
                        aria-label={`Cantidad de ${option.name || optionIdx + 1} de ${group.name || groupIdx + 1}`}
                        min={0}
                        step={0.01}
                        value={group.quantitySourceGroupId ? (group.quantityRules?.length ? Math.min(...group.quantityRules.map(r => r.quantity)) : 0) : option.quantity}
                        disabled={!option.articleId || !!group.quantitySourceGroupId}
                        onChange={v => updateOption(groupIdx, optionIdx, { quantity: v ?? 0 })}
                        style={{ width: '100%' }}
                      />
                    </OptionField>
                    <OptionField area="unit" label="Unidad" showLabel={!screens.lg}>
                      <Select
                        aria-label={`Unidad de ${option.name || optionIdx + 1} de ${group.name || groupIdx + 1}`}
                        allowClear
                        placeholder="Unidad"
                        value={option.unitId}
                        disabled={!option.articleId}
                        options={unitOptions}
                        onChange={v => updateOption(groupIdx, optionIdx, { unitId: v })}
                        style={{ width: '100%' }}
                      />
                    </OptionField>
                    <div style={{ gridArea: 'remove', justifySelf: 'end', alignSelf: 'center' }}>
                      <Popconfirm title="Quitar opción" onConfirm={() => updateGroup(groupIdx, { options: group.options.filter((_, i) => i !== optionIdx) })}>
                        <Button aria-label={`Quitar opción ${option.name || optionIdx + 1} de ${group.name || groupIdx + 1}`} danger size="small" icon={<DeleteOutlined />} />
                      </Popconfirm>
                    </div>
                    <div style={{ gridArea: 'additional', padding: '0 0 8px 8px', borderBottom: '1px solid #f0f0f0' }}>
                      {(option.additionalIngredients ?? []).map((ingredient, ingredientIdx) => {
                        const update = (patch: Partial<typeof ingredient>) => updateOption(groupIdx, optionIdx, {
                          additionalIngredients: option.additionalIngredients!.map((current, idx) => idx === ingredientIdx ? { ...current, ...patch } : current),
                        });
                        return <div key={ingredientIdx} style={{ display: 'grid', gridTemplateColumns: screens.lg ? 'minmax(0, 1fr) 110px minmax(0, 1fr) 32px' : 'minmax(0, 1fr) 100px', gap: 8, marginBottom: 6 }}>
                          <Select aria-label="Artículo adicional" showSearch optionFilterProp="label" placeholder="Artículo adicional" value={ingredient.articleId || undefined}
                            options={articleOptions} onChange={id => update({ articleId: id, unitId: articles.find(a => a.id === id)?.baseUnitId ?? '' })} />
                          <InputNumber aria-label="Cantidad adicional" min={0.0001} precision={4} value={ingredient.quantity} style={{ width: '100%' }}
                            onChange={n => update({ quantity: n ?? 0 })} />
                          <Select aria-label="Unidad adicional" placeholder="Unidad" value={ingredient.unitId || undefined}
                            options={units.filter(u => getCompatibleUnitIds(ingredient.articleId).has(u.id)).map(u => ({ value: u.id, label: `${u.name} (${u.symbol})` }))}
                            onChange={id => update({ unitId: id })} />
                          <Tooltip title="Quitar consumo adicional"><Button icon={<DeleteOutlined />} danger aria-label="Quitar consumo adicional"
                            onClick={() => updateOption(groupIdx, optionIdx, { additionalIngredients: option.additionalIngredients!.filter((_, idx) => idx !== ingredientIdx) })} /></Tooltip>
                        </div>;
                      })}
                      <Button size="small" type="text" icon={<PlusOutlined />} disabled={(option.additionalIngredients?.length ?? 0) >= 20}
                        onClick={() => updateOption(groupIdx, optionIdx, { additionalIngredients: [...option.additionalIngredients ?? [], { articleId: '', unitId: '', quantity: 0 }] })}>
                        Consumo adicional
                      </Button>
                    </div>
                  </div>
                );
              })}
            </Space>

            <Button
              type="dashed"
              icon={<PlusOutlined />}
              style={{ marginTop: 10 }}
              onClick={() => updateGroup(groupIdx, { options: [...group.options, emptyOption(nextDisplayOrder(group.options))] })}
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
        onClick={() => setGroups(prev => [...prev, emptyGroup(nextDisplayOrder(prev))])}
      >
        Agregar grupo
      </Button>
    </Modal>
  );
}
