import { Button, Form, InputNumber, Select, Space, Tooltip, Typography } from 'antd';
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import type { FixedComboModifier, MenuItemDto } from '../../types';

export default function ComboComponentsEditor({ items, editingId }: { items: MenuItemDto[]; editingId?: string }) {
  const form = Form.useFormInstance();
  const components = Form.useWatch('comboComponents', form) as { componentItemId?: string; quantity?: number; modifierSelections?: FixedComboModifier[] }[] | undefined;
  const eligible = items.filter(item => !item.isCombo && item.id !== editingId && item.isActive && item.availableForSale
    && item.stationId);
  return (
    <Form.List name="comboComponents" rules={[{
      validator: async (_, value) => {
        if (!value?.length) throw new Error('Agrega al menos un componente');
        if (value.length > 20) throw new Error('Máximo 20 componentes');
        const ids = value.map((row: { componentItemId?: string }) => row.componentItemId);
        if (new Set(ids).size !== ids.length) throw new Error('Cada plato debe aparecer una sola vez');
      },
    }]}>
      {(fields, { add, remove }, { errors }) => (
        <div style={{ marginBottom: 20 }}>
          <Typography.Text strong>Componentes del combo</Typography.Text>
          {!!fields.length && <div style={{ display: 'grid', gridTemplateColumns: 'minmax(0, 1fr) 90px 32px', gap: 8, marginTop: 8 }}>
            <Typography.Text type="secondary">Plato</Typography.Text>
            <Typography.Text type="secondary">Cantidad</Typography.Text>
          </div>}
          {fields.map(field => {
            const selectedId = components?.[field.name]?.componentItemId;
            const selected = items.find(item => item.id === selectedId);
            const candidates = eligible.filter(item => item.id === selectedId || !components?.some(row => row.componentItemId === item.id));
            if (selected && !candidates.some(item => item.id === selected.id)) candidates.push(selected);
            return (
              <div key={field.key} style={{ display: 'grid', gridTemplateColumns: 'minmax(0, 1fr) 90px 32px', gap: 8, marginTop: 12 }}>
                <div style={{ minWidth: 0 }}>
                  <Form.Item name={[field.name, 'componentItemId']} rules={[{ required: true, message: 'Selecciona un plato' }]} style={{ marginBottom: 4 }}>
                    <Select aria-label={`Plato del componente ${field.name + 1}`} showSearch optionFilterProp="label" placeholder="Plato"
                      onChange={() => form.setFieldValue(['comboComponents', field.name, 'modifierSelections'], [])}
                      options={candidates.map(item => ({ value: item.id, label: item.generalName && item.generalName.toLocaleLowerCase() !== item.name.toLocaleLowerCase()
                        ? `${item.name} · ${item.generalName}` : item.name }))} />
                  </Form.Item>
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>{selected?.stationName ?? 'Sin estación'}</Typography.Text>
                  <Form.Item name={[field.name, 'modifierSelections']} style={{ margin: '8px 0 0' }} rules={[{
                    validator: async (_, value: FixedComboModifier[] | undefined) => {
                      for (const group of selected?.modifierGroups?.filter(g => g.isActive) ?? []) {
                        const optionIds = new Set(group.options.filter(o => o.isActive).map(o => o.id));
                        const rows = (value ?? []).filter(row => optionIds.has(row.modifierOptionId));
                        const count = rows.reduce((sum, row) => sum + row.quantity, 0);
                        const min = Math.max(group.minSelections, group.isRequired ? 1 : 0);
                        if (count < min || count > group.maxSelections) throw new Error(`${group.name}: selecciona de ${min} a ${group.maxSelections}`);
                      }
                      const validIds = new Set(selected?.modifierGroups?.filter(g => g.isActive).flatMap(g => g.options.filter(o => o.isActive).map(o => o.id)));
                      if ((value ?? []).some(row => !validIds.has(row.modifierOptionId))) throw new Error('Revisa las opciones del componente');
                    },
                  }]}>
                    <FixedOptions item={selected} />
                  </Form.Item>
                </div>
                <Form.Item name={[field.name, 'quantity']} rules={[{ required: true, message: 'Cantidad' }]} style={{ marginBottom: 4 }}>
                  <InputNumber aria-label={`Cantidad del componente ${field.name + 1}`} min={1} max={100} precision={0} style={{ width: '100%' }} />
                </Form.Item>
                <Tooltip title="Quitar componente"><Button aria-label={`Quitar componente ${field.name + 1}`} icon={<DeleteOutlined />} danger onClick={() => remove(field.name)} /></Tooltip>
              </div>
            );
          })}
          <Space style={{ marginTop: 12 }}><Button icon={<PlusOutlined />} disabled={fields.length >= 20} onClick={() => add({ quantity: 1 })}>Agregar componente</Button></Space>
          <Form.ErrorList errors={errors} />
        </div>
      )}
    </Form.List>
  );
}

function FixedOptions({ item, value = [], onChange }: { item?: MenuItemDto; value?: FixedComboModifier[]; onChange?: (value: FixedComboModifier[]) => void }) {
  return <div style={{ display: 'grid', gap: 8 }}>
    {item?.modifierGroups?.filter(g => g.isActive).map(group => {
      const ids = new Set(group.options.map(o => o.id));
      const selected = value.filter(row => ids.has(row.modifierOptionId));
      const update = (rows: FixedComboModifier[]) => onChange?.([...value.filter(row => !ids.has(row.modifierOptionId)), ...rows]);
      return <div key={group.id}>
        <Typography.Text style={{ fontSize: 12 }}>{group.name}{group.isRequired || group.minSelections > 0 ? ' *' : ''}</Typography.Text>
        <Select aria-label={`Opciones fijadas: ${group.name}`} style={{ width: '100%' }} mode={group.maxSelections === 1 ? undefined : 'multiple'}
          allowClear placeholder={group.name} value={group.maxSelections === 1 ? selected[0]?.modifierOptionId : selected.map(row => row.modifierOptionId)}
          options={group.options.filter(o => o.isActive).map(o => ({ value: o.id, label: o.name }))}
          onChange={(next: string | string[] | undefined) => update((Array.isArray(next) ? next : next ? [next] : []).map(id => ({ modifierOptionId: id, quantity: value.find(row => row.modifierOptionId === id)?.quantity ?? 1 })))} />
        {group.allowDuplicates && selected.map(row => <Space key={row.modifierOptionId} style={{ marginTop: 4 }}>
          <Typography.Text>{group.options.find(o => o.id === row.modifierOptionId)?.name}</Typography.Text>
          <InputNumber aria-label={`Cantidad: ${group.options.find(o => o.id === row.modifierOptionId)?.name}`} min={1} max={Math.min(100, group.maxSelections)} precision={0}
            value={row.quantity} onChange={n => update(selected.map(s => s.modifierOptionId === row.modifierOptionId ? { ...s, quantity: n ?? 1 } : s))} />
        </Space>)}
      </div>;
    })}
  </div>;
}
