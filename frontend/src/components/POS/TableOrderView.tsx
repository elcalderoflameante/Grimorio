import { useCallback, useEffect, useState } from 'react';
import {
  Alert, Button, Card, Checkbox, Col, Divider, Empty, Input, InputNumber, Popconfirm,
  List, Modal, Row, Select, Space, Spin, Tabs, Tag, Tooltip, Typography, message,
} from 'antd';
import {
  ArrowLeftOutlined, CheckCircleOutlined, DeleteOutlined, DollarOutlined, EditOutlined,
  MinusOutlined, PlusOutlined, QuestionCircleOutlined, ReloadOutlined, SplitCellsOutlined,
} from '@ant-design/icons';
import type {
  OrderDto, RestaurantTableDto, PaymentMethodConfigDto,
  OrderPaymentDto, AddOrderPaymentDto, CustomerDto, MenuCategoryDto,
  MenuItemDto, CreateOrderItemDto, CreateModifierSelectionDto, CardBankDto,
} from '../../types';
import { cashApi, menuApi, paymentMethodsApi, posApi } from '../../services/api';
import { formatError } from '../../utils/errorHandler';
import CustomerSelector from '../Billing/CustomerSelector';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

function modifiersLabel(selections: CreateModifierSelectionDto[] | undefined, items: MenuItemDto[], menuItemId: string): string | null {
  if (!selections || selections.length === 0) return null;
  const item = items.find(i => i.id === menuItemId);
  if (!item) return null;
  return selections.map(s => {
    const opt = item.modifierGroups?.flatMap(g => g.options).find(o => o.id === s.modifierOptionId);
    if (!opt) return '';
    return s.quantity > 1 ? `${opt.name} x${s.quantity}` : opt.name;
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
const CARD_TYPE_OPTIONS = [
  { value: 'Debit', label: 'Debito' },
  { value: 'Credit', label: 'Credito' },
];
const CARD_BRAND_OPTIONS = ['Visa', 'Mastercard', 'Diners', 'American Express', 'Discover', 'Otro']
  .map(v => ({ value: v, label: v }));

interface PayLine {
  methodId: string;
  amountTendered: number;
  cardPaymentType?: 'Credit' | 'Debit';
  cardBankId?: string;
  cardBrand?: string;
  authorizationNumber?: string;
}
interface SplitRow { checked: boolean; qty: number }
interface CartLine {
  menuItemId: string; name: string; price: number; quantity: number;
  notes?: string; modifierSelections?: CreateModifierSelectionDto[];
}

interface Props {
  orderId: string;
  table?: RestaurantTableDto;
  branchId: string;
  onClose: () => void;
  onTableUpdated: () => void;
}

export default function TableOrderView({ orderId, table, branchId, onClose, onTableUpdated }: Props) {
  const { hasPermission } = useAuth();
  const [order, setOrder] = useState<OrderDto | null>(null);
  const [payments, setPayments] = useState<OrderPaymentDto[]>([]);
  const [methods, setMethods] = useState<PaymentMethodConfigDto[]>([]);
  const [cardBanks, setCardBanks] = useState<CardBankDto[]>([]);
  const [categories, setCategories] = useState<MenuCategoryDto[]>([]);
  const [menuItems, setMenuItems] = useState<MenuItemDto[]>([]);
  const [loading, setLoading] = useState(true);

  const [cart, setCart] = useState<CartLine[]>([]);
  const [activeCategory, setActiveCategory] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [modifierTarget, setModifierTarget] = useState<MenuItemDto | null>(null);
  const [modifierBaseLine, setModifierBaseLine] = useState<CartLine | null>(null);
  const [pendingModifiers, setPendingModifiers] = useState<Record<string, number>>({});
  const [modifierError, setModifierError] = useState(false);
  // Observation modal
  const [obsModal, setObsModal] = useState<{ idx: number; obs: string } | null>(null);

  const [showPayModal, setShowPayModal] = useState(false);
  const [payTab, setPayTab] = useState<'single' | 'split'>('single');
  const [docType, setDocType] = useState('NotaDeVenta');
  const [customer, setCustomer] = useState<CustomerDto | null>(null);
  const [payLines, setPayLines] = useState<PayLine[]>([]);
  const [splitRows, setSplitRows] = useState<Record<string, SplitRow>>({});
  const [paying, setPaying] = useState(false);
  const [cancellingOrder, setCancellingOrder] = useState(false);
  const [cancellingItemId, setCancellingItemId] = useState<string | null>(null);
  const [editingItemObs, setEditingItemObs] = useState<{ itemId: string; itemName: string; obs: string } | null>(null);
  const [savingItemObs, setSavingItemObs] = useState(false);
  const [cashSessionId, setCashSessionId] = useState<string | undefined>(undefined);
  const canUpdateOrders = hasPermission(PERMISSIONS.pos.ordersUpdate);
  const canCancelOrders = hasPermission(PERMISSIONS.pos.ordersCancel);
  const canChargeOrders = hasPermission(PERMISSIONS.billing.cashCharge);

  const loadAll = useCallback(async () => {
    setLoading(true);
    try {
      const [orderRes, paymentsRes, methodsRes, banksRes, catsRes, itemsRes, sessionRes] = await Promise.allSettled([
        posApi.getOrden(orderId),
        cashApi.getOrderPayments(orderId),
        paymentMethodsApi.getAll(true),
        paymentMethodsApi.getCardBanks(true),
        menuApi.getCategories(),
        menuApi.getItems({ activeOnly: true, lightweight: true }),
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
      if (banksRes.status === 'fulfilled') setCardBanks(banksRes.value.data);
      if (catsRes.status === 'fulfilled') setCategories(catsRes.value.data);
      if (itemsRes.status === 'fulfilled') {
        let loadedMenuItems = itemsRes.value.data;
        if (orderRes.status === 'fulfilled') {
          const itemIdsWithChoices = [...new Set(orderRes.value.data.items
            .filter(i => i.modifierSelections?.length > 0)
            .map(i => i.menuItemId))];
          if (itemIdsWithChoices.length > 0) {
            const details = await Promise.all(itemIdsWithChoices.map(id => menuApi.getItem(id).then(r => r.data)));
            const detailsById = new Map(details.map(item => [item.id, item]));
            loadedMenuItems = loadedMenuItems.map(item => detailsById.get(item.id) ?? item);
          }
        }
        setMenuItems(loadedMenuItems);
      }
      setCashSessionId(sessionRes.status === 'fulfilled' ? sessionRes.value.data.id : undefined);
    } finally {
      setLoading(false);
    }
  }, [orderId]);

  useEffect(() => { loadAll(); }, [loadAll]);

  useEffect(() => {
    if (showPayModal && methods.length > 0) {
      const method = methods[0];
      setPayLines([method.isCard
        ? {
            methodId: method.id,
            amountTendered: 0,
            cardPaymentType: 'Debit',
            cardBankId: cardBanks[0]?.id,
            cardBrand: 'Visa',
            authorizationNumber: '',
          }
        : { methodId: method.id, amountTendered: 0 },
      ]);
      setDocType('NotaDeVenta');
      setCustomer(null);
      setPayTab('single');
    }
  }, [cardBanks, methods, showPayModal]);

  const alreadyPaid = payments.reduce((s, p) => s + p.orderAmount, 0);
  const paidQtyByItem = payments.reduce<Record<string, number>>((acc, payment) => {
    payment.items?.forEach(item => {
      acc[item.orderItemId] = (acc[item.orderItemId] ?? 0) + item.quantity;
    });
    return acc;
  }, {});
  const hasAmbiguousPayments = payments.some(payment => !payment.items?.length);
  const getPendingItemQty = (itemId: string, quantity: number) =>
    Math.max(0, quantity - (paidQtyByItem[itemId] ?? 0));
  const isItemPaid = (itemId: string) => (paidQtyByItem[itemId] ?? 0) > 0;
  const activeOrderItems = order?.items.filter(item => item.status !== 'Cancelled') ?? [];
  const canCancelWholeOrder = canCancelOrders
    && payments.length === 0
    && activeOrderItems.length > 0
    && activeOrderItems.every(item => item.status === 'Pending');
  const remaining = order ? Math.max(0, order.total - alreadyPaid) : 0;
  const isFullyPaid = remaining <= 0.01;
  const cartSubtotal = cart.reduce((s, l) => s + l.price * l.quantity, 0);

  const splitSubtotal = order
    ? order.items.reduce((sum, item) => {
        const row = splitRows[item.id];
        const pendingQty = getPendingItemQty(item.id, item.quantity);
        const qty = Math.min(row?.qty ?? pendingQty, pendingQty);
        return row?.checked ? sum + item.unitPrice * qty : sum;
      }, 0)
    : 0;

  const getMethod = (id: string) => methods.find(m => m.id === id);
  const buildLineForMethod = (methodId: string, amountTendered = 0): PayLine => {
    const method = getMethod(methodId);
    return method?.isCard
      ? {
          methodId, amountTendered,
          cardPaymentType: 'Debit',
          cardBankId: cardBanks[0]?.id,
          cardBrand: 'Visa',
          authorizationNumber: '',
        }
      : { methodId, amountTendered };
  };
  const targetAmount = payTab === 'split' ? splitSubtotal : remaining;
  const totalTendered = payLines.reduce((s, l) => s + (l.amountTendered || 0), 0);
  const totalChange = Math.max(0, totalTendered - targetAmount);
  const hasCashLine = payLines.some(l => getMethod(l.methodId)?.isCash ?? false);
  const tenderCoversAmount = totalTendered >= targetAmount;
  const needsCustomer = docType === 'Factura' && !customer;
  const cardDetailsComplete = payLines.every(l => {
    const method = getMethod(l.methodId);
    if (!method?.isCard) return true;
    return !!l.cardPaymentType && !!l.cardBankId && !!l.cardBrand && !!l.authorizationNumber?.trim();
  });
  const canPay = targetAmount > 0.001 && !needsCustomer && tenderCoversAmount && cardDetailsComplete;

  const getCartLineCapacity = (line: CartLine): number | null => {
    const item = menuItems.find(menuItem => menuItem.id === line.menuItemId);
    const capacities: number[] = [];
    for (const selection of line.modifierSelections ?? []) {
      const option = item?.modifierGroups?.flatMap(group => group.options).find(o => o.id === selection.modifierOptionId);
      if (!option?.isTracked) continue;
      const available = option.availableQuantity ?? 0;
      capacities.push(selection.quantity > 0 ? Math.floor(available / selection.quantity) : 0);
    }

    return capacities.length > 0 ? Math.min(...capacities) : null;
  };

  const ensureItemDetail = async (item: MenuItemDto) => {
    if (!item.hasModifiers || item.modifierGroups?.length > 0) return item;
    const detail = (await menuApi.getItem(item.id)).data;
    setMenuItems(prev => prev.map(current => current.id === detail.id ? detail : current));
    return detail;
  };

  const addToCart = async (item: MenuItemDto) => {
    if (!canUpdateOrders) {
      message.warning('No tienes permiso para modificar pedidos');
      return;
    }
    if (isFullyPaid) {
      message.info('La orden ya está pagada. No se pueden agregar más productos.');
      return;
    }
    const itemDetail = await ensureItemDetail(item);
    if (itemDetail.modifierGroups?.length > 0) {
      setModifierBaseLine({ menuItemId: itemDetail.id, name: itemDetail.name, price: itemDetail.price, quantity: 1 });
      setPendingModifiers({});
      setModifierError(false);
      setModifierTarget(itemDetail);
      return;
    }
    setCart(prev => {
      const existing = prev.findIndex(l => l.menuItemId === itemDetail.id && !l.modifierSelections?.length);
      if (existing >= 0) return prev.map((l, i) => i === existing ? { ...l, quantity: l.quantity + 1 } : l);
      return [...prev, { menuItemId: itemDetail.id, name: itemDetail.name, price: itemDetail.price, quantity: 1 }];
    });
  };

  const confirmModifiers = () => {
    if (!modifierTarget || !modifierBaseLine) return;
    const groups = modifierTarget.modifierGroups ?? [];
    const invalid = groups.some(group => {
      const total = group.options.reduce((sum, option) => sum + (pendingModifiers[option.id] ?? 0), 0);
      const min = group.isRequired && group.minSelections === 0 ? 1 : group.minSelections;
      const exceedsStock = group.options.some(option =>
        option.isTracked && (pendingModifiers[option.id] ?? 0) > Math.floor(option.availableQuantity ?? 0));
      return total < min
        || total > group.maxSelections
        || exceedsStock
        || (!group.allowDuplicates && group.options.some(o => (pendingModifiers[o.id] ?? 0) > 1));
    });
    if (invalid) {
      setModifierError(true);
      return;
    }

    const modifierSelections: CreateModifierSelectionDto[] = groups
      .flatMap(group => group.options.map(option => ({ option, quantity: pendingModifiers[option.id] ?? 0 })))
      .filter(x => x.quantity > 0)
      .map(x => ({ modifierOptionId: x.option.id, quantity: x.quantity }));
    const modifierPrice = groups
      .flatMap(group => group.options)
      .reduce((sum, option) => sum + option.priceDelta * (pendingModifiers[option.id] ?? 0), 0);

    setCart(prev => [...prev, {
      ...modifierBaseLine,
      price: modifierBaseLine.price + modifierPrice,
      modifierSelections,
    }]);
    setModifierTarget(null);
    setModifierBaseLine(null);
    setPendingModifiers({});
    setModifierError(false);
  };

  const updateCartQty = (idx: number, qty: number) => {
    if (qty <= 0) setCart(prev => prev.filter((_, i) => i !== idx));
    else {
      const capacity = getCartLineCapacity(cart[idx]);
      if (capacity !== null && qty > capacity) {
        message.warning(`Solo hay ${Math.floor(capacity)} disponible(s) para esta selección`);
        return;
      }
      setCart(prev => prev.map((l, i) => i === idx ? { ...l, quantity: qty } : l));
    }
  };

  const handleSave = async () => {
    if (!order || cart.length === 0) return;
    if (isFullyPaid) {
      message.info('La orden ya está pagada. No se pueden agregar más productos.');
      setCart([]);
      return;
    }
    setSaving(true);
    try {
      const newItems: CreateOrderItemDto[] = cart.map(l => ({
        menuItemId: l.menuItemId, quantity: l.quantity, notes: l.notes,
        modifierSelections: l.modifierSelections,
      }));

      let itemsPayload: CreateOrderItemDto[];
      if (order.status === 'Draft') {
        // Draft: enviar todos los ítems (reemplaza)
        const existingItems: CreateOrderItemDto[] = order.items.map(i => ({
          menuItemId: i.menuItemId, quantity: i.quantity, notes: i.notes,
          modifierSelections: i.modifierSelections?.map(s => ({
            modifierOptionId: s.modifierOptionId,
            quantity: s.quantity,
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
        items: payTab === 'split'
          ? order.items
              .filter(item => splitRows[item.id]?.checked)
              .map(item => ({
                orderItemId: item.id,
                quantity: Math.min(splitRows[item.id]?.qty ?? 0, getPendingItemQty(item.id, item.quantity)),
              }))
              .filter(item => item.quantity > 0)
          : [],
        lines: payLines.map(l => ({
          methodId: l.methodId,
          amountTendered: l.amountTendered,
          cardPaymentType: l.cardPaymentType,
          cardBankId: l.cardBankId,
          cardBrand: l.cardBrand,
          authorizationNumber: l.authorizationNumber?.trim(),
        })),
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

  const handleCancelOrder = async () => {
    if (!order) return;
    setCancellingOrder(true);
    try {
      await posApi.cancelOrder(order.id);
      message.success('Orden cancelada');
      await loadAll();
      onTableUpdated();
      onClose();
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'Error al cancelar la orden');
    } finally {
      setCancellingOrder(false);
    }
  };

  const handleCancelItem = async (orderItemId: string) => {
    setCancellingItemId(orderItemId);
    try {
      await posApi.cancelOrderItem(orderItemId);
      message.success('Item cancelado');
      await loadAll();
      onTableUpdated();
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'Error al cancelar el item');
    } finally {
      setCancellingItemId(null);
    }
  };

  const handleUpdateItemObservation = async () => {
    if (!editingItemObs) return;
    setSavingItemObs(true);
    try {
      await posApi.updateOrderItemNotes(editingItemObs.itemId, editingItemObs.obs || undefined);
      message.success('Observacion actualizada');
      setEditingItemObs(null);
      await loadAll();
      onTableUpdated();
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'Error al actualizar la observacion');
    } finally {
      setSavingItemObs(false);
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
            onClick={() => setPayLines(prev => [...prev, buildLineForMethod(methods[0].id)])}>
            Agregar
          </Button>
        </div>
        {payLines.map((line, idx) => {
          const m = getMethod(line.methodId);
          const cashLines = payLines.map((l, i) => ({ ...l, i })).filter(l => getMethod(l.methodId)?.isCash);
          const isLastCash = m?.isCash && cashLines.at(-1)?.i === idx;
          const lineChange = isLastCash ? totalChange : 0;
          return (
            <div key={idx} style={{ marginBottom: 8, padding: m?.isCard ? 8 : 0, border: m?.isCard ? '1px solid #f0f0f0' : undefined, borderRadius: 6 }}>
              <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                <Select
                  style={{ width: 150 }}
                  value={line.methodId}
                  onChange={v => setPayLines(prev => prev.map((l, i) => i === idx ? buildLineForMethod(v, l.amountTendered) : l))}
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
              {m?.isCard && (
                <Row gutter={[6, 6]} style={{ marginTop: 8 }}>
                  <Col xs={12}>
                    <Select
                      style={{ width: '100%' }}
                      value={line.cardPaymentType}
                      options={CARD_TYPE_OPTIONS}
                      onChange={v => setPayLines(prev => prev.map((l, i) => i === idx ? { ...l, cardPaymentType: v } : l))}
                    />
                  </Col>
                  <Col xs={12}>
                    <Select
                      style={{ width: '100%' }}
                      placeholder="Banco"
                      value={line.cardBankId}
                      options={cardBanks.map(b => ({ value: b.id, label: b.name }))}
                      onChange={v => setPayLines(prev => prev.map((l, i) => i === idx ? { ...l, cardBankId: v } : l))}
                    />
                  </Col>
                  <Col xs={12}>
                    <Select
                      style={{ width: '100%' }}
                      placeholder="Tipo"
                      value={line.cardBrand}
                      options={CARD_BRAND_OPTIONS}
                      onChange={v => setPayLines(prev => prev.map((l, i) => i === idx ? { ...l, cardBrand: v } : l))}
                    />
                  </Col>
                  <Col xs={12}>
                    <Input
                      placeholder="Autorizacion"
                      maxLength={50}
                      value={line.authorizationNumber}
                      onChange={e => setPayLines(prev => prev.map((l, i) => i === idx ? { ...l, authorizationNumber: e.target.value } : l))}
                    />
                  </Col>
                </Row>
              )}
            </div>
          );
        })}
        {!cardDetailsComplete && (
          <Text type="danger" style={{ fontSize: 12 }}>
            Completa banco, tipo y autorizacion de la tarjeta
          </Text>
        )}
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
      {order.items.filter(i => i.status !== 'Cancelled' && getPendingItemQty(i.id, i.quantity) > 0).map(item => {
        const pendingQty = getPendingItemQty(item.id, item.quantity);
        const row = splitRows[item.id] ?? { checked: false, qty: pendingQty };
        const selectedQty = Math.min(row.qty, pendingQty);
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
                ...prev,
                [item.id]: {
                  ...prev[item.id],
                  checked: e.target.checked,
                  qty: Math.min(prev[item.id]?.qty ?? pendingQty, pendingQty),
                },
              }))}
            />
            <div style={{ flex: 1, minWidth: 0 }}>
              <Text ellipsis style={{ display: 'block', fontSize: 13 }}>{item.itemName}</Text>
              <Text type="secondary" style={{ fontSize: 11 }}>${item.unitPrice.toFixed(2)} c/u</Text>
            </div>
            {row.checked ? (
              <InputNumber
                size="small" min={1} max={pendingQty} value={selectedQty} style={{ width: 60 }}
                onChange={v => setSplitRows(prev => ({ ...prev, [item.id]: { ...prev[item.id], qty: Math.min(v ?? 1, pendingQty) } }))}
              />
            ) : (
              <Text type="secondary" style={{ fontSize: 12 }}>×{pendingQty}</Text>
            )}
            <Text strong style={{ width: 60, textAlign: 'right', fontSize: 13 }}>
              ${(item.unitPrice * (row.checked ? selectedQty : pendingQty)).toFixed(2)}
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
          <Title level={5} style={{ margin: 0 }}>{table ? `Mesa ${table.code}` : 'Venta directa'}</Title>
          <Tag>Orden #{order.number}</Tag>
          <Tag color={STATUS_COLORS[order.status]}>{STATUS_LABELS[order.status] ?? order.status}</Tag>
        </Space>
        <Button icon={<ReloadOutlined />} size="small" onClick={loadAll} />
      </div>

      <Row gutter={16} style={{ flex: 1, minHeight: 0 }}>
        {/* LEFT: Category filter + menu items */}
        <Col xs={24} md={14} style={{ height: '100%', overflowY: 'auto', paddingBottom: 16 }}>
          {isFullyPaid && (
            <Alert
              type="success"
              showIcon
              title="Orden pagada"
              description="La mesa fue liberada y ya no se pueden agregar productos a este pedido."
              style={{ marginBottom: 12 }}
            />
          )}
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
              const hasModifiers = item.hasModifiers || item.modifierGroups?.length > 0;
              return (
                <Card
                  key={item.id}
                  size="small"
                  hoverable={!isFullyPaid && canUpdateOrders}
                  onClick={() => { void addToCart(item).catch(e => message.error(formatError(e))); }}
                  style={{
                    width: 'calc(50% - 4px)', cursor: isFullyPaid || !canUpdateOrders ? 'not-allowed' : 'pointer',
                    opacity: isFullyPaid || !canUpdateOrders ? 0.55 : 1,
                    borderColor: cartQty > 0 ? '#1677ff' : undefined,
                    background: cartQty > 0 ? '#e6f4ff' : undefined,
                  }}
                  styles={{ body: { padding: '10px 12px' } }}
                >
                  <Text strong ellipsis style={{ display: 'block', fontSize: 13 }}>{item.name}</Text>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 2 }}>
                    <Text type="secondary" style={{ fontSize: 12 }}>${item.price.toFixed(2)}</Text>
                    {hasModifiers && <QuestionCircleOutlined style={{ color: '#fa8c16', fontSize: 11 }} />}
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
                  renderItem={item => {
                    const canCancelItem = canCancelOrders
                      && item.status === 'Pending'
                      && !isItemPaid(item.id)
                      && !hasAmbiguousPayments;
                    const canEditItemNotes = canUpdateOrders && item.status === 'Pending';
                    return (
                    <List.Item style={{ padding: '4px 0' }}>
                      <div style={{ width: '100%', display: 'flex', gap: 8, alignItems: 'flex-start' }}>
                        <Text strong style={{ minWidth: 24, fontSize: 13 }}>{item.quantity}×</Text>
                        <div style={{ flex: 1, minWidth: 0 }}>
                          <Text ellipsis style={{ fontSize: 13 }}>{item.itemName}</Text>
                          {item.modifierSelections?.length > 0 && (
                            <div>
                              {item.modifierSelections.map((s, i) => (
                                <Tag key={i} color="blue" style={{ fontSize: 11 }}>
                                  {s.quantity > 1 ? `${s.optionName} x${s.quantity}` : s.optionName}
                                </Tag>
                              ))}
                            </div>
                          )}
                          {item.notes && <Text type="secondary" style={{ fontSize: 11 }}>{item.notes}</Text>}
                        </div>
                        <Tag color={ITEM_STATUS_COLORS[item.status]} style={{ fontSize: 11 }}>
                          {ITEM_STATUS_LABELS[item.status] ?? item.status}
                        </Tag>
                        {canEditItemNotes && (
                          <Tooltip title="Editar observacion">
                            <Button
                              size="small"
                              type="text"
                              icon={<EditOutlined />}
                              onClick={() => setEditingItemObs({
                                itemId: item.id,
                                itemName: item.itemName,
                                obs: item.notes ?? '',
                              })}
                            />
                          </Tooltip>
                        )}
                        {canCancelItem && (
                          <Popconfirm
                            title="Cancelar item"
                            description="Se liberara la reserva de inventario de este plato."
                            okText="Si, cancelar"
                            cancelText="No"
                            onConfirm={() => handleCancelItem(item.id)}
                          >
                            <Button
                              danger
                              size="small"
                              type="text"
                              icon={<DeleteOutlined />}
                              loading={cancellingItemId === item.id}
                            />
                          </Popconfirm>
                        )}
                        <Text strong style={{ minWidth: 56, textAlign: 'right', fontSize: 13 }}>
                          ${item.totalPrice.toFixed(2)}
                        </Text>
                      </div>
                    </List.Item>
                    );
                  }}
                />

                {cart.length > 0 && (
                  <>
                    <Divider style={{ margin: '8px 0' }}>Nuevos ítems</Divider>
                    {cart.map((line, idx) => {
                      const modifierLabel = modifiersLabel(line.modifierSelections, menuItems, line.menuItemId);
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
                          {modifierLabel && <Tag color="blue" style={{ fontSize: 11, marginTop: 4 }}>{modifierLabel}</Tag>}
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
                            <Tooltip
                              key={li}
                              title={l.isCard ? [l.cardPaymentType === 'Credit' ? 'Credito' : 'Debito', l.cardBankName, l.cardBrand, l.authorizationNumber].filter(Boolean).join(' - ') : undefined}
                            >
                              <Tag color={l.methodColor} style={{ fontSize: 11 }}>{l.methodName}</Tag>
                            </Tooltip>
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
            {canUpdateOrders && cart.length > 0 && (
              <Button block loading={saving} onClick={handleSave} icon={<PlusOutlined />}>
                Guardar nuevos ítems
              </Button>
            )}
            {canCancelWholeOrder && (
              <Popconfirm
                title="Cancelar orden"
                description={table ? 'Se liberaran las reservas de inventario y la mesa quedara disponible.' : 'Se liberaran las reservas de inventario de esta venta.'}
                okText="Si, cancelar"
                cancelText="No"
                onConfirm={handleCancelOrder}
              >
                <Button block danger loading={cancellingOrder} icon={<DeleteOutlined />}>
                  Cancelar orden
                </Button>
              </Popconfirm>
            )}
            {isFullyPaid ? (
              <Alert type="success" icon={<CheckCircleOutlined />} title="Orden pagada completamente" showIcon />
            ) : canChargeOrders ? (
              <Button
                type="primary" block size="large" icon={<DollarOutlined />}
                onClick={() => setShowPayModal(true)}
                disabled={methods.length === 0}
              >
                Cobrar
              </Button>
            ) : null}
          </Space>
        </Col>
      </Row>

      {/* Payment modal */}
      <Modal
        title={
          <Space>
            <Text strong>Cobrar Orden #{order.number}</Text>
            <Tag>{table?.code ?? 'Mostrador'}</Tag>
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

      <Modal
        title={editingItemObs ? `Observacion: ${editingItemObs.itemName}` : 'Observacion del item'}
        open={!!editingItemObs}
        onOk={handleUpdateItemObservation}
        confirmLoading={savingItemObs}
        onCancel={() => setEditingItemObs(null)}
        okText="Guardar"
        cancelText="Cancelar"
        width={360}
      >
        <Input.TextArea
          rows={3}
          placeholder="Ej: salsa aparte, sin cebolla..."
          value={editingItemObs?.obs}
          onChange={e => editingItemObs && setEditingItemObs({ ...editingItemObs, obs: e.target.value })}
        />
        <Text type="secondary" style={{ display: 'block', marginTop: 8, fontSize: 12 }}>
          Solo se puede editar mientras el plato siga pendiente.
        </Text>
      </Modal>

      <Modal
        title={modifierTarget?.name}
        open={!!modifierTarget}
        onOk={confirmModifiers}
        onCancel={() => {
          setModifierTarget(null);
          setModifierBaseLine(null);
          setPendingModifiers({});
          setModifierError(false);
        }}
        okText="Agregar al pedido"
        cancelText="Cancelar"
        width={460}
        okButtonProps={{ size: 'large' }}
        cancelButtonProps={{ size: 'large' }}
      >
        {modifierTarget && (
          <div style={{ paddingTop: 8 }}>
            {modifierError && (
              <Alert
                type="error"
                showIcon
                title="Revisa las opciones requeridas"
                style={{ marginBottom: 16 }}
              />
            )}
            {modifierTarget.modifierGroups.map(group => {
              const selectedCount = group.options.reduce((sum, option) => sum + (pendingModifiers[option.id] ?? 0), 0);
              const min = group.isRequired && group.minSelections === 0 ? 1 : group.minSelections;
              const groupInvalid = modifierError && (selectedCount < min || selectedCount > group.maxSelections);
              return (
                <div key={group.id} style={{ marginBottom: 18 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                    <Text strong style={{ fontSize: 13 }}>{group.name}</Text>
                    <Tag color={groupInvalid ? 'red' : 'default'} style={{ fontSize: 11 }}>
                      {selectedCount}/{group.maxSelections}
                    </Tag>
                  </div>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
                    {group.options.map(option => {
                      const qty = pendingModifiers[option.id] ?? 0;
                      const selected = qty > 0;
                      const stockLimit = option.isTracked ? Math.floor(option.availableQuantity ?? 0) : null;
                      const outOfStock = stockLimit !== null && stockLimit <= 0;
                      const reachedOptionStock = stockLimit !== null && qty >= stockLimit;
                      const disabledAdd = selectedCount >= group.maxSelections
                        || (!group.allowDuplicates && qty >= 1)
                        || reachedOptionStock;
                      return (
                        <div
                          key={option.id}
                          style={{
                            flex: '1 1 calc(50% - 10px)',
                            minWidth: 140,
                            padding: 10,
                            borderRadius: 8,
                            border: `1px solid ${selected ? '#1677ff' : groupInvalid || outOfStock ? '#ff4d4f' : '#d9d9d9'}`,
                            background: outOfStock ? '#fff1f0' : selected ? '#e6f4ff' : '#fff',
                            opacity: outOfStock ? 0.65 : 1,
                          }}
                        >
                          <div style={{ display: 'flex', justifyContent: 'space-between', gap: 6 }}>
                            <Text style={{ fontSize: 13 }}>{option.name}</Text>
                            {option.priceDelta !== 0 && (
                              <Text type="secondary" style={{ fontSize: 12 }}>
                                +${option.priceDelta.toFixed(2)}
                              </Text>
                            )}
                          </div>
                          {option.isTracked && (
                            <Tag color={outOfStock ? 'red' : reachedOptionStock ? 'orange' : 'green'} style={{ fontSize: 11, marginTop: 6 }}>
                              {outOfStock ? 'Sin stock' : `Disp. ${stockLimit}`}
                            </Tag>
                          )}
                          <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 8 }}>
                            <Button
                              size="small"
                              icon={<MinusOutlined />}
                              disabled={qty <= 0}
                              onClick={() => setPendingModifiers(prev => ({ ...prev, [option.id]: Math.max(0, qty - 1) }))}
                            />
                            <Text style={{ width: 24, textAlign: 'center' }}>{qty}</Text>
                            <Button
                              size="small"
                              icon={<PlusOutlined />}
                              disabled={disabledAdd}
                              onClick={() => {
                                setPendingModifiers(prev => ({ ...prev, [option.id]: qty + 1 }));
                                setModifierError(false);
                              }}
                            />
                          </div>
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
