import { useCallback, useEffect, useState } from 'react';
import {
  Alert, Button, Card, Checkbox, Col, Divider, Empty, Input, InputNumber,
  List, Modal, Row, Select, Space, Spin, Tabs, Tag, Tooltip, Typography, message,
} from 'antd';
import {
  ArrowLeftOutlined, CheckCircleOutlined, CheckOutlined, DeleteOutlined, DollarOutlined,
  MinusOutlined, PlusOutlined, QuestionCircleOutlined, ReloadOutlined, SplitCellsOutlined,
} from '@ant-design/icons';
import type {
  OrderDto, RestaurantTableDto, PaymentMethodConfigDto,
  OrderPaymentDto, AddOrderPaymentDto, CustomerDto, MenuCategoryDto,
  MenuItemDto, CreateOrderItemDto, CreateIngredientChoiceDto,
} from '../../types';
import { cashApi, menuApi, paymentMethodsApi, posApi } from '../../services/api';
import CustomerSelector from '../Billing/CustomerSelector';

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

const { Title, Text } = Typography;

const STATUS_COLORS: Record<string, string> = {
  Draft: 'default', Confirmed: 'processing', InPreparation: 'orange',
  Ready: 'cyan', Delivered: 'green', Cancelled: 'red',
};
const STATUS_LABELS: Record<string, string> = {
  Draft: 'Borrador', Confirmed: 'Confirmada', InPreparation: 'En prep.',
  Ready: 'Lista', Delivered: 'Entregada', Cancelled: 'Cancelada',
};
const ITEM_STATUS_COLORS: Record<string, string> = {
  Pending: 'default', InPreparation: 'orange', Ready: 'cyan', Cancelled: 'red',
};
const ITEM_STATUS_LABELS: Record<string, string> = {
  Pending: 'Pendiente', InPreparation: 'En prep.', Ready: 'Listo', Cancelled: 'Cancelado',
};
const DOC_OPTIONS = [
  { value: 'NotaDeVenta', label: 'Nota de Venta' },
  { value: 'Factura', label: 'Factura (SRI)' },
];

interface PayLine { methodId: string; amountTendered: number }
interface SplitRow { checked: boolean; qty: number }
interface CartLine {
  menuItemId: string; name: string; price: number; quantity: number;
  notes?: string; ingredientChoices: CreateIngredientChoiceDto[];
}

interface Props {
  orderId: string;
  table: RestaurantTableDto;
  branchId: string;
  onClose: () => void;
  onTableUpdated: () => void;
}

export default function TableOrderView({ orderId, table, branchId, onClose, onTableUpdated }: Props) {
  const [order, setOrder] = useState<OrderDto | null>(null);
  const [payments, setPayments] = useState<OrderPaymentDto[]>([]);
  const [methods, setMethods] = useState<PaymentMethodConfigDto[]>([]);
  const [categories, setCategories] = useState<MenuCategoryDto[]>([]);
  const [menuItems, setMenuItems] = useState<MenuItemDto[]>([]);
  const [loading, setLoading] = useState(true);

  const [cart, setCart] = useState<CartLine[]>([]);
  const [activeCategory, setActiveCategory] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  // Ingredient choice modal
  const [choiceTarget, setChoiceTarget] = useState<MenuItemDto | null>(null);
  const [pendingChoices, setPendingChoices] = useState<Record<string, string>>({});
  const [choiceError, setChoiceError] = useState(false);
  // Observation modal
  const [obsModal, setObsModal] = useState<{ idx: number; obs: string } | null>(null);

  const [showPayModal, setShowPayModal] = useState(false);
  const [payTab, setPayTab] = useState<'single' | 'split'>('single');
  const [docType, setDocType] = useState('NotaDeVenta');
  const [customer, setCustomer] = useState<CustomerDto | null>(null);
  const [payLines, setPayLines] = useState<PayLine[]>([]);
  const [splitRows, setSplitRows] = useState<Record<string, SplitRow>>({});
  const [paying, setPaying] = useState(false);
  const [cashSessionId, setCashSessionId] = useState<string | undefined>(undefined);

  const loadAll = useCallback(async () => {
    setLoading(true);
    try {
      const [orderRes, paymentsRes, methodsRes, catsRes, itemsRes, sessionRes] = await Promise.allSettled([
        posApi.getOrden(orderId),
        cashApi.getOrderPayments(orderId),
        paymentMethodsApi.getAll(true),
        menuApi.getCategories(),
        menuApi.getItems({ activeOnly: true }),
        cashApi.getActiveSession(),
      ]);
      if (orderRes.status === 'fulfilled') {
        setOrder(orderRes.value.data);
        const rows: Record<string, SplitRow> = {};
        orderRes.value.data.items.forEach(i => { rows[i.id] = { checked: false, qty: i.quantity }; });
        setSplitRows(rows);
      } else {
        message.error('Error al cargar la orden');
      }
      if (paymentsRes.status === 'fulfilled') setPayments(paymentsRes.value.data);
      if (methodsRes.status === 'fulfilled') setMethods(methodsRes.value.data);
      if (catsRes.status === 'fulfilled') setCategories(catsRes.value.data);
      if (itemsRes.status === 'fulfilled') setMenuItems(itemsRes.value.data);
      setCashSessionId(sessionRes.status === 'fulfilled' ? sessionRes.value.data.id : undefined);
    } finally {
      setLoading(false);
    }
  }, [orderId]);

  useEffect(() => { loadAll(); }, [loadAll]);

  useEffect(() => {
    if (showPayModal && methods.length > 0) {
      setPayLines([{ methodId: methods[0].id, amountTendered: 0 }]);
      setDocType('NotaDeVenta');
      setCustomer(null);
      setPayTab('single');
    }
  }, [methods, showPayModal]);

  const alreadyPaid = payments.reduce((s, p) => s + p.orderAmount, 0);
  const remaining = order ? Math.max(0, order.total - alreadyPaid) : 0;
  const isFullyPaid = remaining <= 0.01;
  const cartSubtotal = cart.reduce((s, l) => s + l.price * l.quantity, 0);

  const splitSubtotal = order
    ? order.items.reduce((sum, item) => {
        const row = splitRows[item.id];
        return row?.checked ? sum + item.unitPrice * row.qty : sum;
      }, 0)
    : 0;

  const getMethod = (id: string) => methods.find(m => m.id === id);
  const targetAmount = payTab === 'split' ? splitSubtotal : remaining;
  const totalTendered = payLines.reduce((s, l) => s + (l.amountTendered || 0), 0);
  const totalChange = Math.max(0, totalTendered - targetAmount);
  const hasCashLine = payLines.some(l => getMethod(l.methodId)?.isCash ?? false);
  const tenderCoversAmount = totalTendered >= targetAmount;
  const needsCustomer = docType === 'Factura' && !customer;
  const canPay = targetAmount > 0.001 && !needsCustomer && tenderCoversAmount;

  const addToCart = (item: MenuItemDto) => {
    if (item.variableIngredients?.length > 0) {
      setPendingChoices({});
      setChoiceError(false);
      setChoiceTarget(item);
      return;
    }
    setCart(prev => {
      const existing = prev.findIndex(l => l.menuItemId === item.id && !l.ingredientChoices.length);
      if (existing >= 0) return prev.map((l, i) => i === existing ? { ...l, quantity: l.quantity + 1 } : l);
      return [...prev, { menuItemId: item.id, name: item.name, price: item.price, quantity: 1, ingredientChoices: [] }];
    });
  };

  const confirmChoices = () => {
    if (!choiceTarget) return;
    const missing = choiceTarget.variableIngredients.some(slot => !pendingChoices[slot.recipeIngredientId]);
    if (missing) { setChoiceError(true); return; }
    const choices: CreateIngredientChoiceDto[] = choiceTarget.variableIngredients.map(slot => ({
      recipeIngredientId: slot.recipeIngredientId,
      chosenArticleId: pendingChoices[slot.recipeIngredientId],
    }));
    setCart(prev => [...prev, {
      menuItemId: choiceTarget.id, name: choiceTarget.name,
      price: choiceTarget.price, quantity: 1, ingredientChoices: choices,
    }]);
    setChoiceTarget(null);
    setPendingChoices({});
    setChoiceError(false);
  };

  const updateCartQty = (idx: number, qty: number) => {
    if (qty <= 0) setCart(prev => prev.filter((_, i) => i !== idx));
    else setCart(prev => prev.map((l, i) => i === idx ? { ...l, quantity: qty } : l));
  };

  const handleSave = async () => {
    if (!order || cart.length === 0) return;
    setSaving(true);
    try {
      const newItems: CreateOrderItemDto[] = cart.map(l => ({
        menuItemId: l.menuItemId, quantity: l.quantity, notes: l.notes,
        ingredientChoices: l.ingredientChoices,
      }));

      let itemsPayload: CreateOrderItemDto[];
      if (order.status === 'Draft') {
        // Draft: enviar todos los ítems (reemplaza)
        const existingItems: CreateOrderItemDto[] = order.items.map(i => ({
          menuItemId: i.menuItemId, quantity: i.quantity, notes: i.notes,
          ingredientChoices: i.ingredientChoices.map(c => ({
            recipeIngredientId: c.recipeIngredientId, chosenArticleId: c.chosenArticleId,
          })),
        }));
        itemsPayload = [...existingItems, ...newItems];
      } else {
        // Confirmada/EnPreparación: solo enviar los nuevos (agrega)
        itemsPayload = newItems;
      }

      await posApi.updateItems(order.id, itemsPayload);
      message.success('Ítems agregados');
      setCart([]);
      await loadAll();
      onTableUpdated();
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'Error al guardar');
    } finally {
      setSaving(false);
    }
  };

  const handlePay = async () => {
    if (!canPay || !order) return;
    if (totalChange > 0 && !hasCashLine) {
      message.warning('Hay excedente pero ningún medio acepta vuelto.');
      return;
    }
    setPaying(true);
    try {
      const dto: AddOrderPaymentDto = {
        orderAmount: targetAmount,
        documentType: docType,
        customerId: customer?.id,
        cashSessionId,
        lines: payLines.map(l => ({ methodId: l.methodId, amountTendered: l.amountTendered })),
      };
      await cashApi.payOrder(order.id, dto);
      message.success('Cobro registrado');
      setShowPayModal(false);
      await loadAll();
      onTableUpdated();
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'Error al registrar cobro');
    } finally {
      setPaying(false);
    }
  };

  if (loading) return <Spin style={{ display: 'block', margin: '80px auto' }} />;
  if (!order) return <Alert type="error" title="No se pudo cargar la orden" />;

  const visibleItems = activeCategory
    ? menuItems.filter(i => i.menuCategoryId === activeCategory)
    : menuItems;

  const methodOptions = methods.map(m => ({
    value: m.id,
    label: (
      <Space>
        <span style={{ display: 'inline-block', width: 10, height: 10, borderRadius: '50%', background: m.color }} />
        {m.name}
      </Space>
    ),
  }));

  const paymentForm = (
    <>
      <div style={{ marginBottom: 10 }}>
        <Text type="secondary" style={{ display: 'block', marginBottom: 4 }}>Documento</Text>
        <Select
          style={{ width: '100%' }}
          options={DOC_OPTIONS}
          value={docType}
          onChange={v => { setDocType(v); if (v === 'NotaDeVenta') setCustomer(null); }}
        />
      </div>

      <div style={{ marginBottom: 12 }}>
        <Text type="secondary" style={{ display: 'block', marginBottom: 4 }}>
          Cliente{docType === 'Factura' ? <Text type="danger"> *</Text> : ' (opcional)'}
        </Text>
        <CustomerSelector branchId={branchId} value={customer} onChange={setCustomer} />
        {needsCustomer && <Text type="danger" style={{ fontSize: 12 }}>La factura requiere cliente</Text>}
      </div>

      <div style={{ marginBottom: 8 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
          <Text type="secondary">Medios de pago</Text>
          <Button size="small" icon={<PlusOutlined />}
            onClick={() => setPayLines(prev => [...prev, { methodId: methods[0].id, amountTendered: 0 }])}>
            Agregar
          </Button>
        </div>
        {payLines.map((line, idx) => {
          const m = getMethod(line.methodId);
          const cashLines = payLines.map((l, i) => ({ ...l, i })).filter(l => getMethod(l.methodId)?.isCash);
          const isLastCash = m?.isCash && cashLines.at(-1)?.i === idx;
          const lineChange = isLastCash ? totalChange : 0;
          return (
            <div key={idx} style={{ display: 'flex', gap: 6, marginBottom: 6, alignItems: 'center' }}>
              <Select
                style={{ width: 150 }}
                value={line.methodId}
                onChange={v => setPayLines(prev => prev.map((l, i) => i === idx ? { ...l, methodId: v } : l))}
                options={methodOptions}
              />
              <InputNumber
                style={{ flex: 1 }}
                prefix="$"
                min={0}
                precision={2}
                placeholder="Monto"
                value={line.amountTendered || undefined}
                onChange={v => setPayLines(prev => prev.map((l, i) => i === idx ? { ...l, amountTendered: v ?? 0 } : l))}
              />
              {lineChange > 0 && (
                <Tooltip title="Vuelto estimado"><Tag color="orange">${lineChange.toFixed(2)}</Tag></Tooltip>
              )}
              {payLines.length > 1 && (
                <Button size="small" danger icon={<DeleteOutlined />}
                  onClick={() => setPayLines(prev => prev.filter((_, i) => i !== idx))} />
              )}
            </div>
          );
        })}
        {tenderCoversAmount && totalChange > 0 && !hasCashLine && (
          <Alert type="warning" showIcon title="Agrega línea de efectivo para el vuelto" style={{ marginTop: 6 }} />
        )}
        {!tenderCoversAmount && targetAmount > 0 && (
          <Text type="danger" style={{ fontSize: 12 }}>
            Faltan ${(targetAmount - totalTendered).toFixed(2)} por cubrir
          </Text>
        )}
      </div>
    </>
  );

  const splitSelector = (
    <div style={{ marginBottom: 12 }}>
      <Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
        Selecciona los ítems de este cobro:
      </Text>
      {order.items.filter(i => i.status !== 'Cancelled').map(item => {
        const row = splitRows[item.id] ?? { checked: false, qty: item.quantity };
        return (
          <div key={item.id} style={{
            display: 'flex', alignItems: 'center', gap: 8,
            padding: '6px 8px', marginBottom: 4, borderRadius: 6,
            background: row.checked ? '#e6f4ff' : '#fafafa',
            border: `1px solid ${row.checked ? '#91caff' : '#f0f0f0'}`,
          }}>
            <Checkbox
              checked={row.checked}
              onChange={e => setSplitRows(prev => ({
                ...prev, [item.id]: { ...prev[item.id], checked: e.target.checked },
              }))}
            />
            <div style={{ flex: 1, minWidth: 0 }}>
              <Text ellipsis style={{ display: 'block', fontSize: 13 }}>{item.itemName}</Text>
              <Text type="secondary" style={{ fontSize: 11 }}>${item.unitPrice.toFixed(2)} c/u</Text>
            </div>
            {row.checked ? (
              <InputNumber
                size="small" min={1} max={item.quantity} value={row.qty} style={{ width: 60 }}
                onChange={v => setSplitRows(prev => ({ ...prev, [item.id]: { ...prev[item.id], qty: v ?? 1 } }))}
              />
            ) : (
              <Text type="secondary" style={{ fontSize: 12 }}>×{item.quantity}</Text>
            )}
            <Text strong style={{ width: 60, textAlign: 'right', fontSize: 13 }}>
              ${(item.unitPrice * (row.checked ? row.qty : item.quantity)).toFixed(2)}
            </Text>
          </div>
        );
      })}
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 8, padding: '6px 8px', background: '#f5f5f5', borderRadius: 6 }}>
        <Text strong>Subtotal: <Text strong style={{ color: '#1677ff' }}>${splitSubtotal.toFixed(2)}</Text></Text>
      </div>
    </div>
  );

  return (
    <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16, flexWrap: 'wrap', gap: 8 }}>
        <Space>
          <Button icon={<ArrowLeftOutlined />} onClick={onClose}>Volver al mapa</Button>
          <Title level={5} style={{ margin: 0 }}>Mesa {table.code}</Title>
          <Tag>Orden #{order.number}</Tag>
          <Tag color={STATUS_COLORS[order.status]}>{STATUS_LABELS[order.status] ?? order.status}</Tag>
        </Space>
        <Button icon={<ReloadOutlined />} size="small" onClick={loadAll} />
      </div>

      <Row gutter={16} style={{ flex: 1, minHeight: 0 }}>
        {/* LEFT: Category filter + menu items */}
        <Col xs={24} md={14} style={{ height: '100%', overflowY: 'auto', paddingBottom: 16 }}>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 12 }}>
            <Tag
              style={{ cursor: 'pointer', padding: '4px 10px', fontSize: 13 }}
              color={!activeCategory ? 'blue' : 'default'}
              onClick={() => setActiveCategory(null)}
            >
              Todos
            </Tag>
            {categories.map(c => (
              <Tag
                key={c.id}
                style={{ cursor: 'pointer', padding: '4px 10px', fontSize: 13 }}
                color={activeCategory === c.id ? 'blue' : 'default'}
                onClick={() => setActiveCategory(c.id)}
              >
                {c.name}
              </Tag>
            ))}
          </div>

          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
            {visibleItems.map(item => {
              const inCart = cart.filter(l => l.menuItemId === item.id);
              const cartQty = inCart.reduce((s, l) => s + l.quantity, 0);
              const hasVariable = item.variableIngredients?.length > 0;
              return (
                <Card
                  key={item.id}
                  size="small"
                  hoverable
                  onClick={() => addToCart(item)}
                  style={{
                    width: 'calc(50% - 4px)', cursor: 'pointer',
                    borderColor: cartQty > 0 ? '#1677ff' : undefined,
                    background: cartQty > 0 ? '#e6f4ff' : undefined,
                  }}
                  styles={{ body: { padding: '10px 12px' } }}
                >
                  <Text strong ellipsis style={{ display: 'block', fontSize: 13 }}>{item.name}</Text>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 2 }}>
                    <Text type="secondary" style={{ fontSize: 12 }}>${item.price.toFixed(2)}</Text>
                    {hasVariable && <QuestionCircleOutlined style={{ color: '#fa8c16', fontSize: 11 }} />}
                    {cartQty > 0 && (
                      <span style={{ marginLeft: 'auto', background: '#1677ff', color: '#fff', borderRadius: 10, padding: '0 6px', fontSize: 11 }}>
                        {cartQty}
                      </span>
                    )}
                  </div>
                </Card>
              );
            })}
            {visibleItems.length === 0 && (
              <Empty description="Sin ítems en esta categoría" image={Empty.PRESENTED_IMAGE_SIMPLE} style={{ width: '100%' }} />
            )}
          </div>
        </Col>

        {/* RIGHT: Order summary + actions */}
        <Col xs={24} md={10} style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
          <Card
            size="small"
            title={<Text strong>Pedido #{order.number}</Text>}
            style={{ flex: 1, overflowY: 'auto', marginBottom: 8 }}
          >
            {order.items.length === 0 && cart.length === 0 ? (
              <Empty description="Sin ítems" image={Empty.PRESENTED_IMAGE_SIMPLE} />
            ) : (
              <>
                <List
                  dataSource={order.items}
                  size="small"
                  renderItem={item => (
                    <List.Item style={{ padding: '4px 0' }}>
                      <div style={{ width: '100%', display: 'flex', gap: 8, alignItems: 'flex-start' }}>
                        <Text strong style={{ minWidth: 24, fontSize: 13 }}>{item.quantity}×</Text>
                        <div style={{ flex: 1, minWidth: 0 }}>
                          <Text ellipsis style={{ fontSize: 13 }}>{item.itemName}</Text>
                          {item.ingredientChoices.length > 0 && (
                            <div>
                              {item.ingredientChoices.map((c, i) => (
                                <Tag key={i} color="orange" style={{ fontSize: 11 }}>{c.chosenArticleName}</Tag>
                              ))}
                            </div>
                          )}
                          {item.notes && <Text type="secondary" style={{ fontSize: 11 }}>{item.notes}</Text>}
                        </div>
                        <Tag color={ITEM_STATUS_COLORS[item.status]} style={{ fontSize: 11 }}>
                          {ITEM_STATUS_LABELS[item.status] ?? item.status}
                        </Tag>
                        <Text strong style={{ minWidth: 56, textAlign: 'right', fontSize: 13 }}>
                          ${item.totalPrice.toFixed(2)}
                        </Text>
                      </div>
                    </List.Item>
                  )}
                />

                {cart.length > 0 && (
                  <>
                    <Divider style={{ margin: '8px 0' }}>Nuevos ítems</Divider>
                    {cart.map((line, idx) => {
                      const label = choicesLabel(line.ingredientChoices, menuItems);
                      return (
                        <div key={idx} style={{
                          background: '#e6f4ff', borderRadius: 6, padding: '6px 8px', marginBottom: 6,
                        }}>
                          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                            <Text style={{ flex: 1, fontSize: 13 }} ellipsis>{line.name}</Text>
                            <Button size="small" icon={<MinusOutlined />} onClick={() => updateCartQty(idx, line.quantity - 1)} />
                            <Text style={{ minWidth: 20, textAlign: 'center' }}>{line.quantity}</Text>
                            <Button size="small" icon={<PlusOutlined />} onClick={() => updateCartQty(idx, line.quantity + 1)} />
                            <Text strong style={{ minWidth: 52, textAlign: 'right', fontSize: 13 }}>
                              ${(line.price * line.quantity).toFixed(2)}
                            </Text>
                            <Button size="small" type="text" danger icon={<DeleteOutlined />}
                              onClick={() => updateCartQty(idx, 0)} />
                          </div>
                          {label && <Tag color="orange" style={{ fontSize: 11, marginTop: 4 }}>{label}</Tag>}
                          <Button
                            type="link" size="small" style={{ padding: 0, fontSize: 11, display: 'block', marginTop: 2 }}
                            onClick={() => setObsModal({ idx, obs: line.notes ?? '' })}
                          >
                            {line.notes ? `Obs: ${line.notes}` : '+ observación'}
                          </Button>
                        </div>
                      );
                    })}
                  </>
                )}

                <Divider style={{ margin: '8px 0' }} />

                {/* Resumen tributario SRI */}
                {(() => {
                  const hasBase15 = order.taxableBase15 > 0;
                  const hasBase0 = order.taxableBase0 > 0;
                  const hasExempt = order.taxableBaseExempt > 0;
                  const hasDiscount = order.discountTotal > 0;
                  const rows: { label: string; value: number; color?: string }[] = [
                    { label: 'Subtotal', value: order.subtotal },
                    ...(hasDiscount ? [{ label: '(-) Descuentos', value: -order.discountTotal, color: '#ff4d4f' }] : []),
                    ...(hasBase15 ? [{ label: 'Base imponible 15%', value: order.taxableBase15 }] : []),
                    ...(hasBase0 ? [{ label: 'Base imponible 0%', value: order.taxableBase0 }] : []),
                    ...(hasExempt ? [{ label: 'Base exenta', value: order.taxableBaseExempt }] : []),
                    ...(hasBase15 ? [{ label: 'IVA 15%', value: order.iva15, color: '#1677ff' }] : []),
                    ...(order.ice > 0 ? [{ label: 'ICE', value: order.ice, color: '#1677ff' }] : []),
                  ];
                  return (
                    <div style={{ marginBottom: 6 }}>
                      {rows.map((r, i) => (
                        <div key={i} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 1 }}>
                          <Text type="secondary" style={{ fontSize: 11 }}>{r.label}</Text>
                          <Text style={{ fontSize: 11, color: r.color }}>
                            {r.value < 0 ? `-$${Math.abs(r.value).toFixed(2)}` : `$${r.value.toFixed(2)}`}
                          </Text>
                        </div>
                      ))}
                    </div>
                  );
                })()}

                {cart.length > 0 && (
                  <>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 2 }}>
                      <Text type="secondary" style={{ fontSize: 12 }}>Pedido actual</Text>
                      <Text style={{ fontSize: 12 }}>${order.total.toFixed(2)}</Text>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                      <Text type="secondary" style={{ fontSize: 12 }}>Nuevos ítems</Text>
                      <Text style={{ fontSize: 12 }}>+${cartSubtotal.toFixed(2)}</Text>
                    </div>
                  </>
                )}
                <div style={{ display: 'flex', justifyContent: 'space-between', borderTop: '1px solid #f0f0f0', paddingTop: 4 }}>
                  <Text strong>Total</Text>
                  <Text strong style={{ fontSize: 15 }}>${(order.total + cartSubtotal).toFixed(2)}</Text>
                </div>

                {payments.length > 0 && (
                  <>
                    <Divider style={{ margin: '8px 0' }} />
                    {payments.map((p, i) => (
                      <div key={p.id} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 3 }}>
                        <Space size={4} wrap>
                          <Text type="secondary" style={{ fontSize: 12 }}>Cobro {i + 1}</Text>
                          {p.lines.map((l, li) => (
                            <Tag key={li} color={l.methodColor} style={{ fontSize: 11 }}>{l.methodName}</Tag>
                          ))}
                          {p.documentType === 'Factura' && <Tag color="gold" style={{ fontSize: 11 }}>Factura</Tag>}
                        </Space>
                        <Text style={{ fontSize: 12 }}>${p.orderAmount.toFixed(2)}</Text>
                      </div>
                    ))}
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 4 }}>
                      <Text type={isFullyPaid ? 'success' : 'warning'} style={{ fontSize: 12 }}>
                        {isFullyPaid ? 'Pagada completamente' : 'Pendiente'}
                      </Text>
                      <Text strong style={{ color: isFullyPaid ? '#52c41a' : '#faad14', fontSize: 13 }}>
                        ${remaining.toFixed(2)}
                      </Text>
                    </div>
                  </>
                )}
              </>
            )}
          </Card>

          <Space direction="vertical" style={{ width: '100%' }}>
            {cart.length > 0 && (
              <Button block loading={saving} onClick={handleSave} icon={<PlusOutlined />}>
                Guardar nuevos ítems
              </Button>
            )}
            {isFullyPaid ? (
              <Alert type="success" icon={<CheckCircleOutlined />} title="Orden pagada completamente" showIcon />
            ) : (
              <Button
                type="primary" block size="large" icon={<DollarOutlined />}
                onClick={() => setShowPayModal(true)}
                disabled={methods.length === 0}
              >
                Cobrar
              </Button>
            )}
          </Space>
        </Col>
      </Row>

      {/* Payment modal */}
      <Modal
        title={
          <Space>
            <Text strong>Cobrar Orden #{order.number}</Text>
            <Tag>{table.code}</Tag>
          </Space>
        }
        open={showPayModal}
        onCancel={() => setShowPayModal(false)}
        footer={null}
        width={520}
        destroyOnHidden
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 12, marginTop: 4 }}>
          <Text type="secondary">Total pendiente</Text>
          <Text strong style={{ fontSize: 16, color: '#faad14' }}>${remaining.toFixed(2)}</Text>
        </div>

        <Tabs
          activeKey={payTab}
          onChange={k => setPayTab(k as 'single' | 'split')}
          size="small"
          items={[
            {
              key: 'single',
              label: <Space><DollarOutlined />Cobro único</Space>,
              children: paymentForm,
            },
            {
              key: 'split',
              label: <Space><SplitCellsOutlined />Cuenta dividida</Space>,
              children: (
                <div>
                  {splitSelector}
                  {splitSubtotal > 0.001 ? (
                    <>
                      <Divider style={{ margin: '8px 0' }} />
                      {paymentForm}
                    </>
                  ) : (
                    <Text type="secondary" style={{ fontSize: 12 }}>
                      Selecciona ítems para calcular el monto de este cobro
                    </Text>
                  )}
                </div>
              ),
            },
          ]}
        />

        <Divider style={{ margin: '12px 0' }} />

        <Button
          type="primary" block size="large" loading={paying}
          disabled={!canPay} onClick={handlePay} icon={<DollarOutlined />}
        >
          Confirmar cobro ${targetAmount.toFixed(2)}
        </Button>
      </Modal>

      {/* Observation modal */}
      <Modal
        title="Observación del ítem"
        open={!!obsModal}
        onOk={() => {
          if (obsModal) {
            setCart(prev => prev.map((l, i) => i === obsModal.idx ? { ...l, notes: obsModal.obs || undefined } : l));
            setObsModal(null);
          }
        }}
        onCancel={() => setObsModal(null)}
        okText="Guardar"
        cancelText="Cancelar"
        width={320}
      >
        <Input.TextArea
          rows={3}
          placeholder="Ej: sin cebolla, bien cocido..."
          value={obsModal?.obs}
          onChange={e => obsModal && setObsModal({ ...obsModal, obs: e.target.value })}
        />
      </Modal>

      {/* Variable ingredient choice modal */}
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
                type="error" showIcon
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
                      {slotIdx === 0 ? 'Elige una opción:' : 'Elige otra opción:'}
                    </Text>
                  )}
                  <div style={{
                    display: 'flex', flexWrap: 'wrap', gap: 10,
                    marginBottom: slotIdx < choiceTarget.variableIngredients.length - 1 ? 20 : 0,
                  }}>
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
                            flex: '1 1 calc(33% - 10px)', minWidth: 100,
                            padding: '14px 10px', borderRadius: 10, textAlign: 'center',
                            cursor: 'pointer', userSelect: 'none', transition: 'all 0.15s',
                            border: `2px solid ${selected ? '#1677ff' : slotMissing ? '#ff4d4f' : '#d9d9d9'}`,
                            background: selected ? '#e6f4ff' : '#fff',
                          }}
                        >
                          <div style={{ fontSize: 14, fontWeight: selected ? 600 : 400, color: selected ? '#1677ff' : '#262626', lineHeight: 1.3 }}>
                            {opt.articleName}
                          </div>
                          {selected && <CheckOutlined style={{ color: '#1677ff', fontSize: 12, marginTop: 4 }} />}
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
