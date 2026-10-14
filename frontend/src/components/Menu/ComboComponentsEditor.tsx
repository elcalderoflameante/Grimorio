import { Button, Form, InputNumber, Select, Space, Tooltip, Typography } from 'antd';
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import type { MenuItemDto } from '../../types';

export default function ComboComponentsEditor({ items, editingId }: { items: MenuItemDto[]; editingId?: string }) {
  const form = Form.useFormInstance();
  const components = Form.useWatch('comboComponents', form) as { componentItemId?: string; quantity?: number }[] | undefined;
  const eligible = items.filter(item => !item.isCombo && item.id !== editingId && item.isActive && item.availableForSale
    && item.stationId && !item.modifierGroups?.some(group => group.isActive && (group.isRequired || group.minSelections > 0)));
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
                      options={candidates.map(item => ({ value: item.id, label: item.generalName && item.generalName.toLocaleLowerCase() !== item.name.toLocaleLowerCase()
                        ? `${item.name} · ${item.generalName}` : item.name }))} />
                  </Form.Item>
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>{selected?.stationName ?? 'Sin estación'}</Typography.Text>
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
