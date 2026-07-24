import { useCallback, useEffect, useState } from 'react';
import {
  App as AntApp,
  Table,
  Button,
  Modal,
  Form,
  Select,
  InputNumber,
  Input,
  Space,
  Typography,
  Tag,
  DatePicker,
} from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { inventoryApi } from '../../services/api';
import type {
  StockMovementDto,
  InventoryArticleDto,
  WarehouseDto,
  MeasurementUnitDto,
  UnitConversionDto,
  MovementType,
} from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;

const TIPO_OPTIONS: { label: string; value: MovementType }[] = [
  { label: 'Inventario inicial', value: 'InitialInventory' },
  { label: 'Entrada por compra', value: 'PurchaseEntry' },
  { label: 'Entrada manual', value: 'ManualEntry' },
  { label: 'Salida manual', value: 'ManualExit' },
  { label: 'Waste', value: 'Waste' },
  { label: 'Spoilage', value: 'Spoilage' },
  { label: 'Descuento por venta', value: 'SaleDeduction' },
  { label: 'Transferencia entrada', value: 'TransferIn' },
  { label: 'Transferencia salida', value: 'TransferOut' },
  { label: 'Ajuste positivo', value: 'PositiveAdjustment' },
  { label: 'Ajuste negativo', value: 'NegativeAdjustment' },
];

const SALIDAS = new Set<MovementType>([
  'ManualExit',
  'Waste',
  'Spoilage',
  'SaleDeduction',
  'TransferOut',
  'NegativeAdjustment',
]);

const COST_MOVEMENTS = new Set<MovementType>([
  'InitialInventory',
  'ManualEntry',
  'PositiveAdjustment',
]);

const tipoColor = (tipo: MovementType) => (SALIDAS.has(tipo) ? 'red' : 'green');

export default function StockMovements() {
  const { message } = AntApp.useApp();
  const { hasPermission } = useAuth();
  const [movimientos, setMovimientos] = useState<StockMovementDto[]>([]);
  const [articulos, setArticulos] = useState<InventoryArticleDto[]>([]);
  const [bodegas, setBodegas] = useState<WarehouseDto[]>([]);
  const [unidades, setUnidades] = useState<MeasurementUnitDto[]>([]);
  const [conversiones, setConversiones] = useState<UnitConversionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modal, setModal] = useState(false);
  const [filterArticulo, setFilterArticulo] = useState<string | undefined>();
  const [filterBodega, setFilterBodega] = useState<string | undefined>();
  const [filterTipo, setFilterTipo] = useState<MovementType | undefined>();
  const [filterRango, setFilterRango] = useState<[string, string] | undefined>();
  const [form] = Form.useForm();
  const selectedArticleId = Form.useWatch('articleId', form);
  const selectedMovementType = Form.useWatch('type', form) as MovementType | undefined;
  const canCreate = hasPermission(PERMISSIONS.inventory.movementsCreate);
  const shouldShowCost = selectedMovementType ? COST_MOVEMENTS.has(selectedMovementType) : false;

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [m, a, b, u, c] = await Promise.all([
        inventoryApi.getMovements({
          articleId: filterArticulo,
          warehouseId: filterBodega,
          type: filterTipo,
          from: filterRango?.[0],
          to: filterRango?.[1],
          pageSize: 100,
        }),
        inventoryApi.getArticles(),
        inventoryApi.getWarehouses(),
        inventoryApi.getUnits(),
        inventoryApi.getConversions(),
      ]);
      setMovimientos(m.data);
      setArticulos(a.data);
      setBodegas(b.data);
      setUnidades(u.data);
      setConversiones(c.data);
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setLoading(false);
    }
  }, [filterArticulo, filterBodega, filterRango, filterTipo, message]);

  useEffect(() => { load(); }, [load]);

  const registrar = async () => {
    const values = await form.validateFields();
    try {
      await inventoryApi.registerMovement(values);
      message.success('Movimiento registrado');
      setModal(false);
      form.resetFields();
      load();
    } catch (e) {
      message.error(formatError(e));
    }
  };

  const closeModal = () => {
    setModal(false);
    form.resetFields();
  };

  const getCompatibleUnitIds = (articleId: string | undefined): Set<string> => {
    if (!articleId) return new Set<string>();
    const article = articulos.find(a => a.id === articleId);
    if (!article) return new Set<string>();

    const ids = new Set<string>([article.baseUnitId]);
    for (const conversion of conversiones) {
      if (conversion.originUnitId === article.baseUnitId) ids.add(conversion.destinationUnitId);
      if (conversion.destinationUnitId === article.baseUnitId) ids.add(conversion.originUnitId);
    }
    return ids;
  };

  const compatibleUnitIds = getCompatibleUnitIds(selectedArticleId);
  const unidadOptions = unidades
    .filter(u => compatibleUnitIds.has(u.id))
    .map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id }));
  const selectedArticle = articulos.find(a => a.id === selectedArticleId);

  const handleArticleChange = (articleId: string) => {
    const article = articulos.find(a => a.id === articleId);
    form.setFieldValue('unitId', article?.baseUnitId);
  };

  const handleTypeChange = (type: MovementType) => {
    if (!COST_MOVEMENTS.has(type)) {
      form.setFieldValue('unitCost', undefined);
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Movimientos de Stock</Title>
        {canCreate && (
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setModal(true)}>
            Registrar movimiento
          </Button>
        )}
      </div>

      <Space style={{ marginBottom: 16 }} wrap>
        <Select
          allowClear
          placeholder="Articulo"
          style={{ width: 200 }}
          options={articulos.map(a => ({ label: a.name, value: a.id }))}
          onChange={setFilterArticulo}
          showSearch
          optionFilterProp="label"
        />
        <Select
          allowClear
          placeholder="Bodega"
          style={{ width: 160 }}
          options={bodegas.map(b => ({ label: b.name, value: b.id }))}
          onChange={setFilterBodega}
        />
        <Select
          allowClear
          placeholder="Tipo"
          style={{ width: 180 }}
          options={TIPO_OPTIONS}
          onChange={v => setFilterTipo(v as MovementType)}
        />
        <RangePicker
          onChange={v => setFilterRango(v ? [v[0]!.toISOString(), v[1]!.toISOString()] : undefined)}
        />
      </Space>

      <Table
        dataSource={movimientos}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={{ defaultPageSize: 30, showSizeChanger: true, pageSizeOptions: ['10', '30', '50', '100'] }}
        columns={[
          {
            title: 'Fecha',
            dataIndex: 'movedAt',
            key: 'fecha',
            render: (v: string) => dayjs(v).format('DD/MM/YYYY HH:mm'),
            width: 140,
          },
          { title: 'Articulo', dataIndex: 'articleName', key: 'articulo' },
          { title: 'Bodega', dataIndex: 'warehouseName', key: 'bodega' },
          {
            title: 'Tipo',
            dataIndex: 'type',
            key: 'tipo',
            render: (v: MovementType) => <Tag color={tipoColor(v)}>{v}</Tag>,
          },
          {
            title: 'Cantidad',
            key: 'cantidad',
            render: (_: unknown, m: StockMovementDto) =>
              `${SALIDAS.has(m.type) ? '-' : '+'}${m.quantity} ${m.unitSymbol}`,
          },
          {
            title: 'En unidad base',
            key: 'cantidadBase',
            render: (_: unknown, m: StockMovementDto) =>
              `${SALIDAS.has(m.type) ? '-' : '+'}${m.baseQuantity} ${m.baseUnitSymbol}`,
          },
          {
            title: 'Costo',
            key: 'costo',
            render: (_: unknown, m: StockMovementDto) =>
              m.unitCost === undefined || m.unitCost === null
                ? '-'
                : `$${m.unitCost.toFixed(4)} / ${m.baseUnitSymbol}`,
          },
          {
            title: 'Costo total',
            key: 'costoTotal',
            render: (_: unknown, m: StockMovementDto) =>
              m.totalCost === undefined || m.totalCost === null ? '-' : `$${m.totalCost.toFixed(2)}`,
          },
          { title: 'Referencia', dataIndex: 'reference', key: 'referencia' },
          { title: 'Observacion', dataIndex: 'notes', key: 'observacion' },
        ]}
      />

      <Modal
        title="Registrar movimiento de stock"
        open={modal}
        onOk={registrar}
        onCancel={closeModal}
        okText="Registrar"
        cancelText="Cancelar"
        width={680}
      >
        <Form form={form} layout="vertical" requiredMark={false}>
          <div style={{ display: 'grid', gridTemplateColumns: 'minmax(0, 1fr) minmax(180px, 220px)', gap: 12 }}>
            <Form.Item name="articleId" label="Articulo" rules={[{ required: true }]} style={{ marginBottom: 10 }}>
              <Select
                options={articulos.map(a => ({ label: `${a.name} (${a.baseUnitSymbol})`, value: a.id }))}
                placeholder="Seleccionar articulo"
                showSearch
                optionFilterProp="label"
                onChange={handleArticleChange}
              />
            </Form.Item>
            <Form.Item name="warehouseId" label="Bodega" rules={[{ required: true }]} style={{ marginBottom: 10 }}>
              <Select
                options={bodegas.map(b => ({ label: b.name, value: b.id }))}
                placeholder="Seleccionar bodega"
              />
            </Form.Item>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: shouldShowCost ? 'minmax(190px, 1fr) 110px minmax(160px, 190px) 130px' : 'minmax(220px, 1fr) minmax(120px, 150px) minmax(180px, 220px)', gap: 12 }}>
            <Form.Item name="type" label="Tipo de movimiento" rules={[{ required: true }]} style={{ marginBottom: 4 }}>
              <Select options={TIPO_OPTIONS} placeholder="Seleccionar tipo" onChange={handleTypeChange} />
            </Form.Item>
            <Form.Item name="quantity" label="Cantidad" rules={[{ required: true }]} style={{ marginBottom: 4 }}>
              <InputNumber style={{ width: '100%' }} min={0} step={0.01} />
            </Form.Item>
            <Form.Item name="unitId" label="Unidad" rules={[{ required: true }]} style={{ marginBottom: 4 }}>
              <Select
                options={unidadOptions}
                placeholder={selectedArticleId ? 'Seleccionar unidad' : 'Selecciona un articulo'}
                disabled={!selectedArticleId}
              />
            </Form.Item>
            {shouldShowCost && (
              <Form.Item name="unitCost" label="Costo unit." rules={[{ required: true }]} style={{ marginBottom: 4 }}>
                <InputNumber style={{ width: '100%' }} min={0} step={0.0001} precision={4} prefix="$" />
              </Form.Item>
            )}
          </div>
          <Text type="secondary" style={{ display: 'block', fontSize: 12, marginBottom: 14 }}>
            {shouldShowCost
              ? `El costo unitario se registra en unidad base${selectedArticle ? ` (${selectedArticle.baseUnitSymbol})` : ''}.`
              : selectedArticle
                ? `Solo se muestran la unidad base (${selectedArticle.baseUnitSymbol}) y sus unidades equivalentes.`
                : 'Selecciona un articulo para ver sus unidades disponibles.'}
          </Text>

          <Form.Item name="reference" label="Referencia" style={{ marginBottom: 10 }}>
            <Input placeholder="Nro factura, orden..." />
          </Form.Item>
          <Form.Item name="notes" label="Observacion" style={{ marginBottom: 0 }}>
            <Input.TextArea rows={3} placeholder="Detalle opcional del movimiento" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
