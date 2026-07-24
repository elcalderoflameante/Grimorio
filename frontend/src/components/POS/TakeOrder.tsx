import { useEffect, useMemo, useState } from 'react';
import { App as AntApp, Button, Card, Col, Divider, Empty, InputNumber, Modal,
  Row, Space, Spin, Tag, Typography, Input, Badge, Alert, Tooltip } from 'antd';
import { ArrowLeftOutlined, CheckOutlined,
  DeleteOutlined, MinusOutlined, PlusOutlined, SendOutlined, QuestionCircleOutlined } from '@ant-design/icons';
import { menuApi, posApi } from '../../services/api';
import type {
  MenuCategoryDto, CreateOrderItemDto, MenuItemDto,
  OrderDto, RestaurantTableDto, OrderType,
  CreateModifierSelectionDto, MenuItemAvailabilityDto
} from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Title, Text } = Typography;

interface Props {
  table?: RestaurantTableDto;
  orderType: OrderType;
  existingOrder?: OrderDto;
  directSale?: boolean;
  onClose: () => void;
  onConfirm: (order: OrderDto) => void;
}

interface OrderLine {
  menuItemId: string;
  name: string;
  price: number;
  quantity: number;
  notes?: string;
  modifierSelections?: CreateModifierSelectionDto[];
}

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

export default function TakeOrder({ table, orderType, existingOrder, directSale = false, onClose, onConfirm }: Props) {
  const { message } = AntApp.useApp();

  const [categories, setCategories] = useState<MenuCategoryDto[]>([]);
  const [items, setItems] = useState<MenuItemDto[]>([]);
  const [availability, setAvailability] = useState<MenuItemAvailabilityDto[]>([]);
  const [activeCategory, setActiveCategory] = useState<string | null>(null);
  const [lines, setLines] = useState<OrderLine[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [clientName, setClientName] = useState('');
  const [address, setAddress] = useState('');
  const [notes, setNotes] = useState('');
  const [obsModal, setObsModal] = useState<{ idx: number; obs: string } | null>(null);

  // Variable ingredient choice modal
  // recipeIngredientId → chosenArticleId (no defaults — must actively choose)
  const [modifierTarget, setModifierTarget] = useState<MenuItemDto | null>(null);
  const [modifierBaseLine, setModifierBaseLine] = useState<OrderLine | null>(null);
  const [pendingModifiers, setPendingModifiers] = useState<Record<string, number>>({});
  const [modifierError, setModifierError] = useState(false);

  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      try {
        const [cats, its] = await Promise.all([
          menuApi.getCategories(),
          menuApi.getItems({ activeOnly: true, availableOnly: true, lightweight: true }),
        ]);
        const stock = await menuApi.getAvailability({ activeOnly: true, availableOnly: true });
        let menuItems = its.data;
        if (existingOrder) {
          const itemIdsWithChoices = [...new Set(existingOrder.items
            .filter(i => i.modifierSelections?.length > 0)
            .map(i => i.menuItemId))];
          if (itemIdsWithChoices.length > 0) {
            const details = await Promise.all(itemIdsWithChoices.map(id => menuApi.getItem(id).then(r => r.data)));
            const detailsById = new Map(details.map(item => [item.id, item]));
            menuItems = menuItems.map(item => detailsById.get(item.id) ?? item);
          }
        }

        setCategories(cats.data);
        setItems(menuItems);
        setAvailability(stock.data);
        if (cats.data.length > 0) setActiveCategory(cats.data[0].id);

        if (existingOrder) {
          const linesFromOrder = existingOrder.items.map(i => ({
            menuItemId: i.menuItemId,
            name: i.itemName,
            price: i.unitPrice,
            quantity: i.quantity,
            notes: i.notes,
            modifierSelections: i.modifierSelections?.map(s => ({
              modifierOptionId: s.modifierOptionId,
              quantity: s.quantity,
            })),
          }));
          setLines(linesFromOrder);
        }
      } catch (e) { message.error(formatError(e)); }
      finally { setLoading(false); }
    };
    loadData();
  }, [existingOrder]);

  const itemsById = useMemo(() => new Map(items.map(item => [item.id, item])), [items]);
  const availabilityByItemId = useMemo(
    () => new Map(availability.map(item => [item.menuItemId, item])),
    [availability]
  );
  const filteredItems = useMemo(
    () => items.filter(i => i.menuCategoryId === activeCategory),
    [activeCategory, items]
  );

  const getLineCapacity = (line: OrderLine): number | null => {
    const itemAvailability = availabilityByItemId.get(line.menuItemId);
    const capacities: number[] = [];
    if (itemAvailability?.isTracked) capacities.push(itemAvailability.availableQuantity ?? 0);

    const item = itemsById.get(line.menuItemId);
    for (const selection of line.modifierSelections ?? []) {
      const option = item?.modifierGroups?.flatMap(group => group.options).find(o => o.id === selection.modifierOptionId);
      if (!option?.isTracked) continue;
      const available = option.availableQuantity ?? 0;
      capacities.push(selection.quantity > 0 ? Math.floor(available / selection.quantity) : 0);
    }

    return capacities.length > 0 ? Math.min(...capacities) : null;
  };

  const getLineSelectionKey = (line: OrderLine) => {
    const modifiersKey = [...(line.modifierSelections ?? [])]
      .sort((a, b) => a.modifierOptionId.localeCompare(b.modifierOptionId))
      .map(selection => `${selection.modifierOptionId}:${selection.quantity}`)
      .join('|');
    return `${line.menuItemId}:${modifiersKey}`;
  };

  const getRemainingLineCapacity = (line: OrderLine, idx?: number): number | null => {
    const capacity = getLineCapacity(line);
    if (capacity === null) return null;

    const key = getLineSelectionKey(line);
    const usedByOtherLines = lines.reduce((sum, current, currentIdx) => {
      if (idx !== undefined && currentIdx === idx) return sum;
      return getLineSelectionKey(current) === key ? sum + current.quantity : sum;
    }, 0);

    return Math.max(0, capacity - usedByOtherLines);
  };

  const formatStock = (quantity?: number | null) => {
    if (quantity === null || quantity === undefined) return 'Stock sin control';
    return `Disponible: ${Math.max(0, Math.floor(quantity))}`;
  };

  const ensureItemDetail = async (item: MenuItemDto) => {
    if (!item.hasModifiers || item.modifierGroups?.length > 0) return item;
    const detail = (await menuApi.getItem(item.id)).data;
    setItems(prev => prev.map(current => current.id === detail.id ? detail : current));
    return detail;
  };

  const addItem = async (item: MenuItemDto) => {
    const itemDetail = await ensureItemDetail(item);
    const itemAvailability = availabilityByItemId.get(itemDetail.id);
    const currentQty = lines
      .filter(l => l.menuItemId === itemDetail.id && !l.modifierSelections?.length)
      .reduce((sum, line) => sum + line.quantity, 0);

    if (itemAvailability?.isTracked && !itemAvailability.isAvailable) {
      message.warning(`${itemDetail.name} no tiene stock disponible`);
      return;
    }

    if (itemDetail.modifierGroups && itemDetail.modifierGroups.length > 0) {
      setModifierBaseLine({ menuItemId: itemDetail.id, name: itemDetail.name, price: itemDetail.price, quantity: 1 });
      setPendingModifiers({});
      setModifierError(false);
      setModifierTarget(itemDetail);
      return;
    }
    if (itemAvailability?.isTracked && itemAvailability.availableQuantity !== null && itemAvailability.availableQuantity !== undefined && currentQty >= itemAvailability.availableQuantity) {
      message.warning(`Solo hay ${Math.floor(itemAvailability.availableQuantity)} disponible(s) de ${itemDetail.name}`);
      return;
    }
    setLines(prev => {
      const existing = prev.findIndex(l => l.menuItemId === itemDetail.id && !l.modifierSelections?.length);
      if (existing >= 0) {
        return prev.map((l, i) => i === existing ? { ...l, quantity: l.quantity + 1 } : l);
      }
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

    setLines(prev => [...prev, {
      ...modifierBaseLine,
      price: modifierBaseLine.price + modifierPrice,
      modifierSelections,
    }]);
    setModifierTarget(null);
    setModifierBaseLine(null);
    setPendingModifiers({});
    setModifierError(false);
  };

  const changeQuantity = (idx: number, quantity: number) => {
    if (quantity <= 0) {
      setLines(prev => prev.filter((_, i) => i !== idx));
    } else {
      const line = lines[idx];
      const capacity = line ? getRemainingLineCapacity(line, idx) : null;
      if (capacity !== null && quantity > capacity) {
        message.warning(`Solo hay ${Math.floor(capacity)} disponible(s) para esta selección`);
        return;
      }
      setLines(prev => prev.map((l, i) => i === idx ? { ...l, quantity } : l));
    }
  };

  const removeLine = (idx: number) => setLines(prev => prev.filter((_, i) => i !== idx));

  const subtotal = useMemo(
    () => lines.reduce((sum, l) => sum + l.price * l.quantity, 0),
    [lines]
  );

  // El precio en menú ya incluye IVA — extraemos la base y el impuesto
  const fiscal = useMemo(() => lines.reduce((acc, l) => {
    const menuItem = itemsById.get(l.menuItemId);
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
  }, { base15: 0, base0: 0, baseExempt: 0, iva15: 0 }), [itemsById, lines]);

  const handleSave = async (confirm: boolean) => {
    if (lines.length === 0) { message.warning('Agrega al menos un ítem'); return; }
    const unavailableLine = lines.find(line => {
      const capacity = getLineCapacity(line);
      if (capacity === null) return false;
      const totalSameSelection = lines
        .filter(current => getLineSelectionKey(current) === getLineSelectionKey(line))
        .reduce((sum, current) => sum + current.quantity, 0);
      return totalSameSelection > capacity;
    });
    if (unavailableLine) {
      message.warning(`${unavailableLine.name} supera el stock disponible`);
      return;
    }
    setSaving(true);
    try {
      const itemsPayload: CreateOrderItemDto[] = lines.map(l => ({
        menuItemId: l.menuItemId,
        quantity: l.quantity,
        notes: l.notes,
        modifierSelections: l.modifierSelections,
      }));

      let order: OrderDto;
      if (directSale) {
        order = (await posApi.createDirectSale({
          type: 'Takeout',
          customerName: clientName || undefined,
          notes: notes || undefined,
          items: itemsPayload,
        })).data;
        message.success(`Venta #${order.number} lista para cobrar`);
        onConfirm(order);
        return;
      }

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
        message.success(`Pedido #${order.number} enviado a preparar`);
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
            {directSale ? 'Venta directa' : orderType === 'DineIn' && table ? `Mesa ${table.code}` : orderType === 'Takeout' ? 'Para llevar' : 'Delivery'}
          </Title>
          {(orderType === 'Takeout' || orderType === 'Delivery') && (
            <Text type="secondary" style={{ fontSize: 12 }}>
              {clientName || 'Sin nombre de cliente'}
            </Text>
          )}
        </div>
        <Tag color={directSale ? 'green' : orderType === 'DineIn' ? 'blue' : orderType === 'Takeout' ? 'orange' : 'purple'}>
          {directSale ? 'Mostrador' : orderType}
        </Tag>
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
              const hasModifiers = item.hasModifiers || item.modifierGroups?.length > 0;
              const itemAvailability = availabilityByItemId.get(item.id);
              const isSoldOut = itemAvailability?.isTracked && !itemAvailability.isAvailable;
              const reachedLimit = itemAvailability?.isTracked
                && itemAvailability.availableQuantity !== null
                && itemAvailability.availableQuantity !== undefined
                && totalQty >= itemAvailability.availableQuantity;
              const stockColor = isSoldOut ? 'red' : reachedLimit ? 'orange' : itemAvailability?.isTracked ? 'green' : 'default';
              return (
                <Card
                  key={item.id}
                  hoverable={!isSoldOut && !reachedLimit}
                  size="small"
                  onClick={() => { void addItem(item).catch(e => message.error(formatError(e))); }}
                  style={{
                    width: 130,
                    cursor: isSoldOut || reachedLimit ? 'not-allowed' : 'pointer',
                    borderColor: totalQty > 0 ? '#1677ff' : undefined,
                    background: totalQty > 0 ? '#e6f4ff' : undefined,
                    opacity: isSoldOut ? 0.55 : 1,
                  }}
                  styles={{ body: { padding: '8px 10px' } }}
                >
                  <div style={{ fontSize: 13, fontWeight: 600, lineHeight: 1.3 }}>{item.name}</div>
                  <div style={{ fontSize: 12, color: '#1677ff', marginTop: 4 }}>${item.price.toFixed(2)}</div>
                  <Tooltip title={isSoldOut && itemAvailability?.limitingArticleName ? `Falta ${itemAvailability.limitingArticleName}` : undefined}>
                    <Tag color={stockColor} style={{ fontSize: 10, marginTop: 4, marginInlineEnd: 0 }}>
                      {isSoldOut ? 'Agotado' : formatStock(itemAvailability?.availableQuantity)}
                    </Tag>
                  </Tooltip>
                  {hasModifiers && (
                    <QuestionCircleOutlined style={{ color: '#fa8c16', fontSize: 11, marginTop: 2 }} title="Tiene opciones" />
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
              <div style={{ flex: 1, overflowY: 'auto' }}>
                {lines.map((line, idx) => {
                  const modifierLabel = modifiersLabel(line.modifierSelections, items, line.menuItemId);
                  const capacity = getRemainingLineCapacity(line, idx);
                  return (
                    <div key={`${line.menuItemId}-${idx}`} style={{ padding: '6px 0', borderBottom: '1px solid #f0f0f0' }}>
                      <div style={{ width: '100%' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Text style={{ fontSize: 13, flex: 1 }}>{line.name}</Text>
                          <Button type="text" size="small" danger icon={<DeleteOutlined />} onClick={() => removeLine(idx)} />
                        </div>
                        {modifierLabel && (
                          <Tag color="blue" style={{ fontSize: 11, marginTop: 2 }}>{modifierLabel}</Tag>
                        )}
                        {capacity !== null && (
                          <Tag color={line.quantity > capacity ? 'red' : 'green'} style={{ fontSize: 11, marginTop: 2 }}>
                            Stock: {Math.floor(capacity)}
                          </Tag>
                        )}
                        <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 4 }}>
                          <Button size="small" icon={<MinusOutlined />} onClick={() => changeQuantity(idx, line.quantity - 1)} />
                          <InputNumber
                            size="small"
                            min={1}
                            max={capacity ?? undefined}
                            value={line.quantity}
                            onChange={v => changeQuantity(idx, v ?? 1)}
                            style={{ width: 50 }}
                          />
                          <Button
                            size="small"
                            icon={<PlusOutlined />}
                            disabled={capacity !== null && line.quantity >= capacity}
                            onClick={() => changeQuantity(idx, line.quantity + 1)}
                          />
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
                    </div>
                  );
                })}
              </div>
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

            <Space orientation="vertical" style={{ width: '100%' }} size={6}>
              <Button type="primary" icon={<SendOutlined />} block loading={saving} disabled={lines.length === 0} onClick={() => handleSave(true)}>
                {directSale ? 'Crear venta' : 'Enviar a preparar'}
              </Button>
              {!directSale && (
                <Button icon={<CheckOutlined />} block loading={saving} disabled={lines.length === 0} onClick={() => handleSave(false)}>
                  Guardar borrador
                </Button>
              )}
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
