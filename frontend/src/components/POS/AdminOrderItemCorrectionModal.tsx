import { useEffect, useRef, useState } from 'react';
import { App, Button, Checkbox, Form, Input, InputNumber, Modal, Radio, Space, Spin, Tag, Tooltip, Typography } from 'antd';
import { InfoCircleOutlined } from '@ant-design/icons';
import type { MenuItemModifierGroupDto, OrderDto, OrderItemDto } from '../../types';
import { menuApi, posApi } from '../../services/api';
import { formatError } from '../../utils/errorHandler';
import { createIdempotencyKey } from '../../utils/idempotencyKey';

export default function AdminOrderItemCorrectionModal({ target, isDraft, onClose, onCorrected }: {
  target: { item: OrderItemDto; replace: boolean } | null;
  isDraft: boolean;
  onClose: () => void;
  onCorrected: (order: OrderDto) => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm<{ reason: string; disposition: string }>();
  const [groups, setGroups] = useState<MenuItemModifierGroupDto[]>([]);
  const [selections, setSelections] = useState<Record<string, number>>({});
  const [loading, setLoading] = useState(false);
  const [loadFailed, setLoadFailed] = useState(false);
  const [saving, setSaving] = useState(false);
  const requestId = useRef('');
  const inFlight = useRef(false);
  const replacementInfo = target?.replace
    ? isDraft
      ? ' El plato con las opciones corregidas queda en borrador, sin reservar ingredientes hasta enviar a preparar.'
      : ' Ademas, se reservan los ingredientes del plato con las opciones corregidas.'
    : '';

  useEffect(() => {
    if (!target) return;
    let cancelled = false;
    requestId.current = createIdempotencyKey();
    form.resetFields();
    setLoading(target.replace);
    setLoadFailed(false);
    setSelections(Object.fromEntries(target.item.modifierSelections.map(s => [s.modifierOptionId, s.quantity])));
    setGroups([]);
    if (!target.replace) return;
    menuApi.getItem(target.item.menuItemId).then(({ data }) => {
      if (!cancelled) setGroups(data.modifierGroups.filter(g => g.isActive));
    }).catch(error => {
      if (!cancelled) { setLoadFailed(true); message.error(formatError(error)); }
    }).finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [target, form, message]);

  const submit = async () => {
    if (!target || inFlight.current || loading || loadFailed) return;
    inFlight.current = true;
    setSaving(true);
    try {
      let values;
      try { values = await form.validateFields(); } catch { return; }
      const options = target.replace ? groups.flatMap(g => g.options.filter(o => o.isActive)
        .map(o => ({ modifierOptionId: o.id, quantity: selections[o.id] ?? 0 }))).filter(o => o.quantity > 0) : [];
      if (target.replace) {
        for (const group of groups) {
          const count = group.options.reduce((sum, option) => sum + (selections[option.id] ?? 0), 0);
          if (count < Math.max(group.minSelections, group.isRequired ? 1 : 0) || count > group.maxSelections) {
            message.warning(`Revisa las opciones de ${group.name}`);
            return;
          }
        }
        if (groups.length === 0) { message.warning('Este plato no tiene opciones para cambiar'); return; }
      }
      const { data } = await posApi.correctOrderItem(target.item.id, {
        requestId: requestId.current, replace: target.replace, disposition: values.disposition,
        reason: values.reason.trim(), modifierSelections: options,
      });
      message.success(target.replace ? 'Opciones corregidas' : 'Plato cancelado');
      onCorrected(data);
      onClose();
    } catch (error) { message.error(formatError(error)); }
    finally { inFlight.current = false; setSaving(false); }
  };

  return <Modal open={!!target} width={620} title={target?.replace ? 'Cambiar opciones del plato' : 'Cancelar plato'}
    okText="Confirmar correccion" cancelText="Cancelar" onOk={submit} confirmLoading={saving}
    okButtonProps={{ disabled: loading || loadFailed }} cancelButtonProps={{ disabled: saving }}
    closable={!saving} maskClosable={!saving} onCancel={() => { if (!inFlight.current) onClose(); }}>
    <Spin spinning={loading}>
      <Space orientation="vertical" style={{ width: '100%', margin: '12px 0' }}>
        <Typography.Text strong style={{ overflowWrap: 'anywhere' }}>{target?.item.quantity} x {target?.item.itemName}</Typography.Text>
        <Space wrap size={4}>{target?.item.modifierSelections.map(s => <Tag key={s.modifierOptionId}>{s.optionName}</Tag>)}</Space>
      </Space>
      <Form form={form} layout="vertical" disabled={saving || loading || loadFailed}>
        <Form.Item name="disposition" label="Tratamiento del inventario" rules={[{ required: true, message: 'Selecciona el tratamiento' }]}>
          <Radio.Group>
            <Space wrap size={[20, 8]}>
              <Space size={0}>
                <Radio value="RegistrationError">Error de registro</Radio>
                <Tooltip trigger={['hover', 'focus', 'click']} title={isDraft
                  ? `En un pedido en borrador no hay ingredientes reservados. Cancela el registro sin descontar stock ni registrar merma.${replacementInfo}`
                  : `Para un plato que no se preparo: libera sus ingredientes reservados y los deja disponibles. No descuenta stock ni registra merma.${replacementInfo}`}>
                  <Button type="text" size="small" shape="circle" icon={<InfoCircleOutlined />}
                    aria-label="Informacion de inventario: error de registro" />
                </Tooltip>
              </Space>
              <Space size={0}>
                <Radio value="PreparedWaste" disabled={isDraft}>Preparado / merma</Radio>
                <Tooltip trigger={['hover', 'focus', 'click']} title={isDraft
                  ? 'No disponible en borradores: todavia no hay ingredientes reservados para registrar merma.'
                  : `Para un plato cuyos ingredientes ya se utilizaron: descuenta del stock los ingredientes reservados del plato original y registra una salida por merma. No los devuelve al disponible.${replacementInfo}`}>
                  <Button type="text" size="small" shape="circle" icon={<InfoCircleOutlined />}
                    aria-label="Informacion de inventario: preparado o merma" />
                </Tooltip>
              </Space>
            </Space>
          </Radio.Group>
        </Form.Item>
        {target?.replace && groups.map(group => <div key={group.id} style={{ borderTop: '1px solid #e5e7eb', paddingTop: 12, marginBottom: 16 }}>
          <Typography.Text strong>{group.name}{group.isRequired ? ' *' : ''}</Typography.Text>
          {group.maxSelections === 1 ? <Radio.Group disabled={saving} style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 8 }}
            value={group.options.find(o => (selections[o.id] ?? 0) > 0)?.id}
            onChange={event => setSelections(previous => ({ ...previous, ...Object.fromEntries(group.options.map(o => [o.id, o.id === event.target.value ? 1 : 0])) }))}>
            {!group.isRequired && group.minSelections === 0 && <Radio value={undefined}>Ninguna</Radio>}
            {group.options.filter(o => o.isActive).map(option => <Radio key={option.id} value={option.id} style={{ whiteSpace: 'normal', overflowWrap: 'anywhere' }}>
              {option.name}{option.priceDelta !== 0 ? ` (+$${option.priceDelta.toFixed(2)})` : ''}
            </Radio>)}
          </Radio.Group> : group.options.filter(o => o.isActive).map(option => <div key={option.id} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, marginTop: 8 }}>
            <Checkbox disabled={saving} checked={(selections[option.id] ?? 0) > 0}
              onChange={event => setSelections(previous => ({ ...previous, [option.id]: event.target.checked ? 1 : 0 }))}>
              {option.name}{option.priceDelta !== 0 ? ` (+$${option.priceDelta.toFixed(2)})` : ''}
            </Checkbox>
            {group.allowDuplicates && <InputNumber aria-label={`Cantidad ${option.name}`} disabled={saving} min={0} max={group.maxSelections} precision={0}
              style={{ width: 80, flexShrink: 0 }} value={selections[option.id] ?? 0}
              onChange={value => setSelections(previous => ({ ...previous, [option.id]: value ?? 0 }))} />}
          </div>)}
        </div>)}
        <Form.Item name="reason" label="Motivo" rules={[{ required: true, whitespace: true, message: 'Indica el motivo' },
          { min: 5, max: 500, message: 'Usa entre 5 y 500 caracteres' }]}>
          <Input.TextArea rows={2} maxLength={500} />
        </Form.Item>
      </Form>
    </Spin>
  </Modal>;
}
