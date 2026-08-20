import { App as AntApp, Button, Col, DatePicker, Form, Input, InputNumber, Modal, Popconfirm, Row, Select, Space, Switch, Table, Tag, TimePicker, Typography } from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { useEffect, useMemo, useState } from 'react';
import { menuApi, posApi } from '../../services/api';
import type { MenuCategoryDto, MenuItemDto, PromotionDto, PromotionPaymentPolicy, PromotionType, UpsertPromotionDto } from '../../types';

const { Text } = Typography;
const { RangePicker } = DatePicker;

type FormValues = Omit<UpsertPromotionDto, 'startsOn' | 'endsOn' | 'startsAt' | 'endsAt'> & {
  dateRange?: [Dayjs, Dayjs];
  timeRange?: [Dayjs, Dayjs];
};

const dayOptions = [
  { label: 'Dom', value: 1 },
  { label: 'Lun', value: 2 },
  { label: 'Mar', value: 4 },
  { label: 'Mié', value: 8 },
  { label: 'Jue', value: 16 },
  { label: 'Vie', value: 32 },
  { label: 'Sáb', value: 64 },
];

const typeLabels: Record<PromotionType, string> = {
  Percentage: 'Porcentaje',
  FixedAmount: 'Descuento fijo',
  FixedPrice: 'Precio fijo',
  BuyXPayY: 'Paga X de Y',
};

const paymentPolicyLabels: Record<PromotionPaymentPolicy, string> = {
  AnyPayment: 'Cualquier medio',
  CashTransferOnly: 'Solo efectivo/transferencia',
  CardAlternativePrice: 'Precio distinto con tarjeta',
};

const money = (value?: number | null) => `$${Number(value ?? 0).toFixed(2)}`;

const formatPromotionValue = (promo: PromotionDto) => {
  if (promo.type === 'Percentage') return `${promo.discountPercent ?? 0}%`;
  if (promo.type === 'FixedAmount') return money(promo.discountAmount);
  if (promo.type === 'FixedPrice') return money(promo.fixedPrice);
  return `${promo.buyQuantity ?? 0}x${promo.payQuantity ?? 0}`;
};

const formatPaymentPolicy = (promo: PromotionDto) => {
  if (promo.paymentPolicy === 'CardAlternativePrice') return `Tarjeta: ${money(promo.cardPrice)}`;
  return paymentPolicyLabels[promo.paymentPolicy] ?? paymentPolicyLabels.AnyPayment;
};

const formatDays = (mask: number) => {
  if (!mask) return 'Todos';
  return dayOptions.filter(day => (mask & day.value) !== 0).map(day => day.label).join(', ');
};

const timeValue = (value?: string) => value ? dayjs(`2000-01-01T${value}`) : undefined;

export default function PromotionsList() {
  const { message } = AntApp.useApp();
  const [form] = Form.useForm<FormValues>();
  const [promotions, setPromotions] = useState<PromotionDto[]>([]);
  const [categories, setCategories] = useState<MenuCategoryDto[]>([]);
  const [items, setItems] = useState<MenuItemDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<PromotionDto | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const [promosRes, categoriesRes, itemsRes] = await Promise.all([
        posApi.getPromotions(),
        menuApi.getCategories(),
        menuApi.getItems({ activeOnly: true, availableOnly: true, lightweight: true }),
      ]);
      setPromotions(promosRes.data);
      setCategories(categoriesRes.data);
      setItems(itemsRes.data);
    } catch {
      message.error('No se pudieron cargar las promociones.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const itemOptions = useMemo(() => items.map(item => ({
    label: `${item.name}${item.categoryName ? ` · ${item.categoryName}` : ''}`,
    value: item.id,
  })), [items]);

  const categoryOptions = useMemo(() => categories.map(category => ({
    label: category.name,
    value: category.id,
  })), [categories]);

  const openModal = (promotion?: PromotionDto) => {
    setEditing(promotion ?? null);
    form.setFieldsValue(promotion ? {
      name: promotion.name,
      description: promotion.description,
      type: promotion.type,
      isActive: promotion.isActive,
      daysOfWeekMask: promotion.daysOfWeekMask,
      discountPercent: promotion.discountPercent,
      discountAmount: promotion.discountAmount,
      fixedPrice: promotion.fixedPrice,
      paymentPolicy: promotion.paymentPolicy,
      cardPrice: promotion.cardPrice,
      buyQuantity: promotion.buyQuantity,
      payQuantity: promotion.payQuantity,
      priority: promotion.priority,
      menuItemIds: promotion.menuItemIds,
      menuCategoryIds: promotion.menuCategoryIds,
      dateRange: promotion.startsOn && promotion.endsOn ? [dayjs(promotion.startsOn), dayjs(promotion.endsOn)] : undefined,
      timeRange: promotion.startsAt && promotion.endsAt ? [timeValue(promotion.startsAt)!, timeValue(promotion.endsAt)!] : undefined,
    } : {
      type: 'Percentage',
      isActive: true,
      daysOfWeekMask: 0,
      paymentPolicy: 'AnyPayment',
      priority: 0,
      menuItemIds: [],
      menuCategoryIds: [],
    });
    setModalOpen(true);
  };

  const toPayload = (values: FormValues): UpsertPromotionDto => ({
    name: values.name,
    description: values.description,
    type: values.type,
    isActive: values.isActive,
    startsOn: values.dateRange?.[0]?.format('YYYY-MM-DD'),
    endsOn: values.dateRange?.[1]?.format('YYYY-MM-DD'),
    startsAt: values.timeRange?.[0]?.format('HH:mm:ss'),
    endsAt: values.timeRange?.[1]?.format('HH:mm:ss'),
    daysOfWeekMask: values.daysOfWeekMask ?? 0,
    discountPercent: values.discountPercent,
    discountAmount: values.discountAmount,
    fixedPrice: values.fixedPrice,
    paymentPolicy: values.paymentPolicy ?? 'AnyPayment',
    cardPrice: values.cardPrice,
    buyQuantity: values.buyQuantity,
    payQuantity: values.payQuantity,
    priority: values.priority ?? 0,
    menuItemIds: values.menuItemIds ?? [],
    menuCategoryIds: values.menuCategoryIds ?? [],
  });

  const save = async () => {
    const values = await form.validateFields();
    setSaving(true);
    try {
      const payload = toPayload(values);
      if (editing) await posApi.updatePromotion(editing.id, payload);
      else await posApi.createPromotion(payload);
      message.success('Promoción guardada.');
      setModalOpen(false);
      await load();
    } catch {
      message.error('No se pudo guardar la promoción.');
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: string) => {
    try {
      await posApi.deletePromotion(id);
      message.success('Promoción eliminada.');
      await load();
    } catch {
      message.error('No se pudo eliminar la promoción.');
    }
  };

  return (
    <Space orientation="vertical" size="middle" style={{ width: '100%' }}>
      <Row justify="space-between" align="middle" gutter={[12, 12]}>
        <Col>
          <Typography.Title level={3} style={{ margin: 0 }}>Promociones</Typography.Title>
        </Col>
        <Col>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>
            Nueva promoción
          </Button>
        </Col>
      </Row>

      <Table
        rowKey="id"
        loading={loading}
        dataSource={promotions}
        pagination={{ pageSize: 10 }}
        columns={[
          { title: 'Nombre', dataIndex: 'name' },
          { title: 'Tipo', dataIndex: 'type', render: (type: PromotionType) => typeLabels[type] },
          { title: 'Valor', render: (_, promo) => formatPromotionValue(promo) },
          { title: 'Pago', render: (_, promo) => formatPaymentPolicy(promo) },
          { title: 'Días', dataIndex: 'daysOfWeekMask', render: formatDays },
          {
            title: 'Horario',
            render: (_, promo) => promo.startsAt && promo.endsAt ? `${promo.startsAt.slice(0, 5)} - ${promo.endsAt.slice(0, 5)}` : 'Todo el día',
          },
          {
            title: 'Estado',
            render: (_, promo) => (
              <Space>
                <Tag color={promo.isActive ? 'green' : 'default'}>{promo.isActive ? 'Activa' : 'Inactiva'}</Tag>
                <Tag color={promo.isCurrentlyActive ? 'blue' : 'orange'}>{promo.isCurrentlyActive ? 'Disponible ahora' : 'Fuera de horario'}</Tag>
              </Space>
            ),
          },
          {
            title: 'Acciones',
            width: 120,
            render: (_, promo) => (
              <Space>
                <Button icon={<EditOutlined />} onClick={() => openModal(promo)} />
                <Popconfirm title="¿Eliminar promoción?" onConfirm={() => remove(promo.id)}>
                  <Button danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Editar promoción' : 'Nueva promoción'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={save}
        okText="Guardar"
        confirmLoading={saving}
        width={1040}
        style={{ top: 24 }}
        destroyOnHidden
        forceRender
      >
        <Form form={form} layout="vertical">
          <Row gutter={12}>
            <Col xs={24} md={10}>
              <Form.Item name="name" label="Nombre" rules={[{ required: true, message: 'Ingresa el nombre.' }]}>
                <Input maxLength={160} />
              </Form.Item>
            </Col>
            <Col xs={24} md={6}>
              <Form.Item name="type" label="Tipo" rules={[{ required: true }]}>
                <Select
                  options={[
                    { value: 'Percentage', label: 'Porcentaje' },
                    { value: 'FixedAmount', label: 'Descuento fijo' },
                    { value: 'FixedPrice', label: 'Precio fijo' },
                    { value: 'BuyXPayY', label: 'Paga X de Y' },
                  ]}
                />
              </Form.Item>
            </Col>
            <Col xs={12} md={4}>
              <Form.Item name="priority" label="Prioridad">
                <InputNumber min={0} precision={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={12} md={4}>
              <Form.Item name="isActive" label="Activa" valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={12}>
            <Col xs={24} md={14}>
              <Form.Item name="description" label="Descripción">
                <Input.TextArea rows={2} maxLength={500} />
              </Form.Item>
            </Col>
            <Col xs={24} md={10}>
              <Form.Item name="paymentPolicy" label="Política de pago" rules={[{ required: true }]}>
                <Select
                  options={[
                    { value: 'AnyPayment', label: paymentPolicyLabels.AnyPayment },
                    { value: 'CashTransferOnly', label: paymentPolicyLabels.CashTransferOnly },
                    { value: 'CardAlternativePrice', label: paymentPolicyLabels.CardAlternativePrice },
                  ]}
                />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item noStyle shouldUpdate={(prev, next) =>
            prev.type !== next.type || prev.paymentPolicy !== next.paymentPolicy}>
            {() => {
              const type = form.getFieldValue('type') as PromotionType;
              const hasCardPrice = form.getFieldValue('paymentPolicy') === 'CardAlternativePrice';
              return (
                <Row gutter={12}>
                  <Col xs={24} md={hasCardPrice ? 12 : 24}>
                    {type === 'Percentage' && (
                      <Form.Item name="discountPercent" label="Porcentaje" rules={[{ required: true }]}>
                        <InputNumber min={0.01} max={100} precision={2} suffix="%" style={{ width: '100%' }} />
                      </Form.Item>
                    )}
                    {type === 'FixedAmount' && (
                      <Form.Item name="discountAmount" label="Descuento fijo" rules={[{ required: true }]}>
                        <InputNumber min={0.01} precision={2} prefix="$" style={{ width: '100%' }} />
                      </Form.Item>
                    )}
                    {type === 'FixedPrice' && (
                      <Form.Item name="fixedPrice" label="Precio promocional efectivo/transferencia" rules={[{ required: true }]}>
                        <InputNumber min={0} precision={2} prefix="$" style={{ width: '100%' }} />
                      </Form.Item>
                    )}
                    {type === 'BuyXPayY' && (
                      <Row gutter={12}>
                        <Col xs={12}>
                          <Form.Item name="buyQuantity" label="Cantidad comprada" rules={[{ required: true }]}>
                            <InputNumber min={2} precision={0} style={{ width: '100%' }} />
                          </Form.Item>
                        </Col>
                        <Col xs={12}>
                          <Form.Item name="payQuantity" label="Cantidad pagada" rules={[{ required: true }]}>
                            <InputNumber min={1} precision={0} style={{ width: '100%' }} />
                          </Form.Item>
                        </Col>
                      </Row>
                    )}
                  </Col>
                  {hasCardPrice && (
                    <Col xs={24} md={12}>
                      <Form.Item name="cardPrice" label="Precio promocional con tarjeta" rules={[{ required: true }]}>
                        <InputNumber min={0} precision={2} prefix="$" style={{ width: '100%' }} />
                      </Form.Item>
                    </Col>
                  )}
                </Row>
              );
            }}
          </Form.Item>

          <Row gutter={12}>
            <Col xs={24} md={8}>
              <Form.Item name="dateRange" label="Fechas">
                <RangePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item name="timeRange" label="Horario">
                <TimePicker.RangePicker format="HH:mm" style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item name="daysOfWeekMask" hidden>
                <InputNumber />
              </Form.Item>
              <Form.Item label="Días" shouldUpdate>
                {() => (
                  <Select
                    mode="multiple"
                    placeholder="Todos los días"
                    options={dayOptions}
                    value={dayOptions.filter(day => ((form.getFieldValue('daysOfWeekMask') ?? 0) & day.value) !== 0).map(day => day.value)}
                    onChange={(values) => form.setFieldValue('daysOfWeekMask', values.reduce((sum: number, value: number) => sum + value, 0))}
                  />
                )}
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={12}>
            <Col xs={24} md={12}>
              <Form.Item name="menuCategoryIds" label="Categorías">
                <Select mode="multiple" allowClear showSearch optionFilterProp="label" options={categoryOptions} />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="menuItemIds" label="Ítems específicos">
                <Select mode="multiple" allowClear showSearch optionFilterProp="label" options={itemOptions} />
              </Form.Item>
            </Col>
          </Row>
          <Text type="secondary">Selecciona categorías, ítems específicos, o ambos.</Text>
        </Form>
      </Modal>
    </Space>
  );
}
