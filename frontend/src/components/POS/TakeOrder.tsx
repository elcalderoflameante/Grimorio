import { useEffect, useState } from 'react';
import {
  Button, Card, Col, Divider, Empty, InputNumber, List, Modal,
  Row, Space, Spin, Tag, Typography, message, Input, Badge, Alert
} from 'antd';
import {
  ArrowLeftOutlined, CheckOutlined,
  DeleteOutlined, MinusOutlined, PlusOutlined, SendOutlined, QuestionCircleOutlined
} from '@ant-design/icons';
import { menuApi, posApi } from '../../services/api';
import type {
  MenuCategoryDto, CreateOrderItemDto, MenuItemDto,
  OrderDto, RestaurantTableDto, OrderType,
  CreateIngredientChoiceDto
} from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Title, Text } = Typography;

interface Props {
  table?: RestaurantTableDto;
  orderType: OrderType;
  existingOrder?: OrderDto;
  onClose: () => void;
  onConfirm: (order: OrderDto) => void;
}

interface OrderLine {
  menuItemId: string;
  name: string;
  price: number;
  quantity: number;
  notes?: string;
  ingredientChoices?: CreateIngredientChoiceDto[];
}

// Returns a human-readable label for the choices of a line
function choicesLabel(choices: CreateIngredientChoiceDto[] | undefined, items: MenuItemDto[]): string | null {
  if (!choices || choices.length === 0) return null;
  const item = items.find(i => i.variableIngredients?.some(v => choices.some(c => c.recipeIngredientId === v.recipeIngredientId)));
  if (!item) return null;
  return choices.map(c => {
    const slot = item.variableIngredients.find(v => v.recipeIngredientId === c.recipeIngredientId);
    if (!slot) return '';
    const allOptions = [
      { articleId: slot.defaultArticleId, articleName: slot.defaultArticleName },
      ...slot.alternatives,
    ];
    return allOptions.find(o => o.articleId === c.chosenArticleId)?.articleName ?? c.chosenArticleId;
  }).filter(Boolean).join(', ');
}

export default function TakeOrder({ table, orderType, existingOrder, onClose, onConfirm }: Props) {
  const [categories, setCategories] = useState<MenuCategoryDto[]>([]);
  const [items, setItems] = useState<MenuItemDto[]>([]);
  const [activeCategory, setActiveCategory] = useState<string | null>(null);
  const [lines, setLines] = useState<OrderLine[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [clientName, setClientName] = useState('');
  const [address, setAddress] = useState('');
  const [notes, setNotes] = useState('');
  const [obsModal, setObsModal] = useState<{ idx: number; obs: string } | null>(null);

  // Variable ingredient choice modal
  const [choiceTarget, setChoiceTarget] = useState<MenuItemDto | null>(null);
  // recipeIngredientId → chosenArticleId (no defaults — must actively choose)
  const [pendingChoices, setPendingChoices] = useState<Record<string, string>>({});
  const [choiceError, setChoiceError] = useState(false);

  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      try {
        const [cats, its] = await Promise.all([
          menuApi.getCategories(),
          menuApi.getItems({ activeOnly: true, availableOnly: true }),
        ]);
        setCategories(cats.data);
        setItems(its.data);
        if (cats.data.length > 0) setActiveCategory(cats.data[0].id);

        if (existingOrder) {
          const linesFromOrder = existingOrder.items.map(i => ({
            menuItemId: i.menuItemId,
            name: i.itemName,
            price: i.unitPrice,
            quantity: i.quantity,
            notes: i.notes,
            ingredientChoices: i.ingredientChoices?.map(c => ({
              recipeIngredientId: c.recipeIngredientId,
              chosenArticleId: c.chosenArticleId,
            })),
          }));
          setLines(linesFromOrder);
        }
      } catch (e) { message.error(formatError(e)); }
      finally { setLoading(false); }
    };
    loadData();
  }, [existingOrder]);

  const filteredItems = items.filter(i => i.menuCategoryId === activeCategory);

  const addItem = (item: MenuItemDto) => {
    if (item.variableIngredients && item.variableIngredients.length > 0) {
      setPendingChoices({});
      setChoiceError(false);
      setChoiceTarget(item);
      return;
    }
    setLines(prev => {
      const existing = prev.findIndex(l => l.menuItemId === item.id && !l.ingredientChoices?.length);
      if (existing >= 0) {
        return prev.map((l, i) => i === existing ? { ...l, quantity: l.quantity + 1 } : l);
      }
      return [...prev, { menuItemId: item.id, name: item.name, price: item.price, quantity: 1 }];
    });
  };

  const confirmChoices = () => {
    if (!choiceTarget) return;
    const missing = choiceTarget.variableIngredients.some(
      slot => !pendingChoices[slot.recipeIngredientId]
    );
    if (missing) {
      setChoiceError(true);
      return;
    }
    const choices: CreateIngredientChoiceDto[] = choiceTarget.variableIngredients.map(slot => ({
      recipeIngredientId: slot.recipeIngredientId,
      chosenArticleId: pendingChoices[slot.recipeIngredientId],
    }));
    setLines(prev => [...prev, {
      menuItemId: choiceTarget.id,
      name: choiceTarget.name,
      price: choiceTarget.price,
      quantity: 1,
      ingredientChoices: choices,
    }]);
    setChoiceTarget(null);
    setPendingChoices({});
    setChoiceError(false);
  };

  const changeQuantity = (idx: number, quantity: number) => {
    if (quantity <= 0) {
      setLines(prev => prev.filter((_, i) => i !== idx));
    } else {
      setLines(prev => prev.map((l, i) => i === idx ? { ...l, quantity } : l));
    }
  };

  const removeLine = (idx: number) => setLines(prev => prev.filter((_, i) => i !== idx));

  const subtotal = lines.reduce((sum, l) => sum + l.price * l.quantity, 0);

  // El precio en menú ya incluye IVA — extraemos la base y el impuesto
  const fiscal = lines.reduce((acc, l) => {
    const menuItem = items.find(i => i.id === l.menuItemId);
    const sriCode = menuItem?.taxRateSriCode;
    const taxPct = menuItem?.taxRatePercentage;
    const gross = l.price * l.quantity;
    if (sriCode === '10' && taxPct) {
      const base = Math.round(gross / (1 + taxPct / 100) * 100) / 100;
      const iva = Math.round((gross - base) * 100) / 100;
      acc.base15 += base;
      acc.iva15 += iva;
    } else if (sriCode === '0' || sriCode === '8') {
      acc.base0 += gross;
    } else {
      acc.baseExempt += gross;
    }
    return acc;
  }, { base15: 0, base0: 0, baseExempt: 0, iva15: 0 });

  const handleSave = async (confirm: boolean) => {
    if (lines.length === 0) { message.warning('Agrega al menos un ítem'); return; }
    setSaving(true);
    try {
      const itemsPayload: CreateOrderItemDto[] = lines.map(l => ({
        menuItemId: l.menuItemId,
        quantity: l.quantity,
        notes: l.notes,
        ingredientChoices: l.ingredientChoices,
      }));

      let order: OrderDto;
      if (existingOrder) {
        order = (await posApi.updateItems(existingOrder.id, itemsPayload)).data;
      } else {
        order = (await posApi.createOrder({
          type: orderType,
          tableId: table?.id,
          customerName: clientName || undefined,
          deliveryAddress: address || undefined,
          notes: notes || undefined,
          items: itemsPayload,
        })).data;
      }

      if (confirm) {
        order = (await posApi.confirmOrder(order.id)).data;
        message.success(`Pedido #${order.number} enviado a cocina`);
        onConfirm(order);
      } else {
        message.success('Pedido guardado como borrador');
        onClose();
      }
    } catch (e) { message.error(formatError(e)); }
    finally { setSaving(false); }
  };

  if (loading) return <Spin style={{ display: 'block', margin: 40 }} />;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 16 }}>
        <Button icon={<ArrowLeftOutlined />} onClick={onClose} />
        <div>
          <Title level={5} style={{ margin: 0 }}>
            {orderType === 'DineIn' && table ? `Mesa ${table.code}` : orderType === 'Takeout' ? 'Para llevar' : 'Delivery'}
          </Title>
          {(orderType === 'Takeout' || orderType === 'Delivery') && (
            <Text type="secondary" style={{ fontSize: 12 }}>
              {clientName || 'Sin nombre de cliente'}
            </Text>
          )}
        </div>
        <Tag color={orderType === 'DineIn' ? 'blue' : orderType === 'Takeout' ? 'orange' : 'purple'}>{orderType}</Tag>
      </div>

      {/* Cliente info for non-table orders */}
      {orderType !== 'DineIn' && (
        <Row gutter={8} style={{ marginBottom: 12 }}>
          <Col span={orderType === 'Delivery' ? 12 : 24}>
            <Input
              placeholder="Nombre del cliente"
              value={clientName}
              onChange={e => setClientName(e.target.value)}
              size="small"
            />
          </Col>
          {orderType === 'Delivery' && (
            <Col span={12}>
              <Input
                placeholder="Dirección de entrega"
                value={address}
                onChange={e => setAddress(e.target.value)}
                size="small"
              />
            </Col>
          )}
        </Row>
      )}

      <Row gutter={12} style={{ flex: 1, minHeight: 0 }}>
        {/* Menú selector */}
        <Col xs={24} md={14} style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {/* Categorías */}
          <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
            {categories.map(c => (
              <Button
                key={c.id}
                size="small"
                type={activeCategory === c.id ? 'primary' : 'default'}
                onClick={() => setActiveCategory(c.id)}
                style={c.color && activeCategory !== c.id ? { borderColor: c.color, color: c.color } : {}}
              >
                {c.name}
              </Button>
            ))}
          </div>

          {/* Items */}
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, overflowY: 'auto', maxHeight: 380 }}>
            {filteredItems.length === 0 && (
              <Empty description="Sin ítems en esta categoría" style={{ width: '100%' }} image={Empty.PRESENTED_IMAGE_SIMPLE} />
            )}
            {filteredItems.map(item => {
              const inOrder = lines.filter(l => l.menuItemId === item.id);
              const totalQty = inOrder.reduce((s, l) => s + l.quantity, 0);
              const hasVariable = item.variableIngredients?.length > 0;
              return (
                <Card
                  key={item.id}
                  hoverable
                  size="small"
                  onClick={() => addItem(item)}
                  style={{
                    width: 130,
                    cursor: 'pointer',
                    borderColor: totalQty > 0 ? '#1677ff' : undefined,
                    background: totalQty > 0 ? '#e6f4ff' : undefined,
                  }}
                  styles={{ body: { padding: '8px 10px' } }}
                >
                  <div style={{ fontSize: 13, fontWeight: 600, lineHeight: 1.3 }}>{item.name}</div>
                  <div style={{ fontSize: 12, color: '#1677ff', marginTop: 4 }}>${item.price.toFixed(2)}</div>
                  {hasVariable && (
                    <QuestionCircleOutlined style={{ color: '#fa8c16', fontSize: 11, marginTop: 2 }} title="Tiene ingredientes variables" />
                  )}
                  {totalQty > 0 && (
                    <Badge count={totalQty} style={{ background: '#1677ff', fontSize: 11 }} />
                  )}
                </Card>
              );
            })}
          </div>
        </Col>

        {/* Resumen pedido */}
        <Col xs={24} md={10}>
          <div style={{ background: '#fafafa', borderRadius: 8, padding: 12, height: '100%', display: 'flex', flexDirection: 'column' }}>
            <Text strong style={{ marginBottom: 8, display: 'block' }}>Pedido</Text>

            {lines.length === 0 ? (
              <Empty description="Sin ítems" image={Empty.PRESENTED_IMAGE_SIMPLE} style={{ flex: 1 }} />
            ) : (
              <List
                dataSource={lines}
                style={{ flex: 1, overflowY: 'auto' }}
                renderItem={(line, idx) => {
                  const label = choicesLabel(line.ingredientChoices, items);
                  return (
                    <List.Item style={{ padding: '6px 0' }}>
                      <div style={{ width: '100%' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Text style={{ fontSize: 13, flex: 1 }}>{line.name}</Text>
                          <Button type="text" size="small" danger icon={<DeleteOutlined />} onClick={() => removeLine(idx)} />
                        </div>
                        {label && (
                          <Tag color="orange" style={{ fontSize: 11, marginTop: 2 }}>{label}</Tag>
                        )}
                        <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 4 }}>
                          <Button size="small" icon={<MinusOutlined />} onClick={() => changeQuantity(idx, line.quantity - 1)} />
                          <InputNumber
                            size="small"
                            min={1}
                            value={line.quantity}
                            onChange={v => changeQuantity(idx, v ?? 1)}
                            style={{ width: 50 }}
                          />
                          <Button size="small" icon={<PlusOutlined />} onClick={() => changeQuantity(idx, line.quantity + 1)} />
                          <Text type="secondary" style={{ fontSize: 12, marginLeft: 'auto' }}>
                            ${(line.price * line.quantity).toFixed(2)}
                          </Text>
                        </div>
                        <Button
                          type="link"
                          size="small"
                          style={{ padding: 0, fontSize: 11 }}
                          onClick={() => setObsModal({ idx, obs: line.notes ?? '' })}
                        >
                          {line.notes ? `Obs: ${line.notes}` : '+ observación'}
                        </Button>
                      </div>
                    </List.Item>
                  );
                }}
              />
            )}

            <Divider style={{ margin: '8px 0' }} />
            {fiscal.base15 > 0 && (
              <div style={{ marginBottom: 4 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Text type="secondary" style={{ fontSize: 11 }}>Base 15%</Text>
                  <Text style={{ fontSize: 11 }}>${fiscal.base15.toFixed(2)}</Text>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Text type="secondary" style={{ fontSize: 11 }}>IVA 15%</Text>
                  <Text style={{ fontSize: 11, color: '#1677ff' }}>${fiscal.iva15.toFixed(2)}</Text>
                </div>
              </div>
            )}
            {fiscal.base0 > 0 && (
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                <Text type="secondary" style={{ fontSize: 11 }}>Base 0%</Text>
                <Text style={{ fontSize: 11 }}>${fiscal.base0.toFixed(2)}</Text>
              </div>
            )}
            {fiscal.baseExempt > 0 && (
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                <Text type="secondary" style={{ fontSize: 11 }}>Base exenta</Text>
                <Text style={{ fontSize: 11 }}>${fiscal.baseExempt.toFixed(2)}</Text>
              </div>
            )}
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 12, borderTop: '1px solid #f0f0f0', paddingTop: 4 }}>
              <Text strong>Total (IVA incluido)</Text>
              <Text strong style={{ fontSize: 16 }}>${subtotal.toFixed(2)}</Text>
            </div>

            <Input.TextArea
              placeholder="Observaciones del pedido..."
              rows={2}
              value={notes}
              onChange={e => setNotes(e.target.value)}
              style={{ marginBottom: 8, fontSize: 12 }}
            />

            <Space direction="vertical" style={{ width: '100%' }} size={6}>
              <Button type="primary" icon={<SendOutlined />} block loading={saving} disabled={lines.length === 0} onClick={() => handleSave(true)}>
                Enviar a cocina
              </Button>
              <Button icon={<CheckOutlined />} block loading={saving} disabled={lines.length === 0} onClick={() => handleSave(false)}>
                Guardar borrador
              </Button>
            </Space>
          </div>
        </Col>
      </Row>

      {/* Modal observación del ítem */}
      <Modal
        title="Observación del ítem"
        open={!!obsModal}
        onOk={() => {
          if (obsModal) {
            setLines(prev => prev.map((l, i) => i === obsModal.idx ? { ...l, notes: obsModal.obs || undefined } : l));
            setObsModal(null);
          }
        }}
        onCancel={() => setObsModal(null)}
        okText="Guardar"
        width={320}
      >
        <Input.TextArea
          rows={3}
          placeholder="Ej: sin cebolla, bien cocido..."
          value={obsModal?.obs}
          onChange={e => obsModal && setObsModal({ ...obsModal, obs: e.target.value })}
        />
      </Modal>

      {/* Modal selección de ingredientes variables */}
      <Modal
        title={choiceTarget?.name}
        open={!!choiceTarget}
        onOk={confirmChoices}
        onCancel={() => { setChoiceTarget(null); setPendingChoices({}); setChoiceError(false); }}
        okText="Agregar al pedido"
        cancelText="Cancelar"
        width={420}
        okButtonProps={{ size: 'large' }}
        cancelButtonProps={{ size: 'large' }}
      >
        {choiceTarget && (
          <div style={{ paddingTop: 8 }}>
            {choiceError && (
              <Alert
                type="error"
                showIcon
                title="Debes seleccionar una opción antes de continuar"
                style={{ marginBottom: 16 }}
              />
            )}
            {choiceTarget.variableIngredients.map((slot, slotIdx) => {
              const allOptions = [
                { articleId: slot.defaultArticleId, articleName: slot.defaultArticleName },
                ...slot.alternatives,
              ];
              const chosen = pendingChoices[slot.recipeIngredientId];
              const slotMissing = choiceError && !chosen;
              return (
                <div key={slot.recipeIngredientId}>
                  {choiceTarget.variableIngredients.length > 1 && (
                    <Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 8 }}>
                      {slotIdx === 0 ? 'Elige una opción:' : `Elige otra opción:`}
                    </Text>
                  )}
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, marginBottom: slotIdx < choiceTarget.variableIngredients.length - 1 ? 20 : 0 }}>
                    {allOptions.map(opt => {
                      const selected = chosen === opt.articleId;
                      return (
                        <div
                          key={opt.articleId}
                          onClick={() => {
                            setPendingChoices(prev => ({ ...prev, [slot.recipeIngredientId]: opt.articleId }));
                            setChoiceError(false);
                          }}
                          style={{
                            flex: '1 1 calc(33% - 10px)',
                            minWidth: 100,
                            padding: '14px 10px',
                            borderRadius: 10,
                            border: `2px solid ${selected ? '#1677ff' : slotMissing ? '#ff4d4f' : '#d9d9d9'}`,
                            background: selected ? '#e6f4ff' : '#fff',
                            cursor: 'pointer',
                            textAlign: 'center',
                            transition: 'all 0.15s',
                            userSelect: 'none',
                          }}
                        >
                          <div style={{
                            fontSize: 14,
                            fontWeight: selected ? 600 : 400,
                            color: selected ? '#1677ff' : '#262626',
                            lineHeight: 1.3,
                          }}>
                            {opt.articleName}
                          </div>
                          {selected && (
                            <CheckOutlined style={{ color: '#1677ff', fontSize: 12, marginTop: 4 }} />
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </Modal>
    </div>
  );
}
