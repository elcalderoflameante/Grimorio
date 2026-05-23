import { useCallback, useEffect, useState } from 'react';
import {
  Table, Button, Modal, Form, Select, InputNumber, Input,
  Space, Typography, message, Tag, DatePicker
} from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { inventoryApi } from '../../services/api';
import type {
  StockMovementDto, InventoryArticleDto, WarehouseDto, MeasurementUnitDto, MovementType
} from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Title } = Typography;
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
  'ManualExit', 'Waste', 'Spoilage', 'SaleDeduction', 'TransferOut', 'NegativeAdjustment'
]);

const tipoColor = (tipo: MovementType) =>
  SALIDAS.has(tipo) ? 'red' : 'green';

export default function StockMovements() {
  const { hasPermission } = useAuth();
  const [movimientos, setMovimientos] = useState<StockMovementDto[]>([]);
  const [articulos, setArticulos] = useState<InventoryArticleDto[]>([]);
  const [bodegas, setBodegas] = useState<WarehouseDto[]>([]);
  const [unidades, setUnidades] = useState<MeasurementUnitDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modal, setModal] = useState(false);
  const [filterArticulo, setFilterArticulo] = useState<string | undefined>();
  const [filterBodega, setFilterBodega] = useState<string | undefined>();
  const [filterTipo, setFilterTipo] = useState<MovementType | undefined>();
  const [filterRango, setFilterRango] = useState<[string, string] | undefined>();
  const [form] = Form.useForm();
  const canCreate = hasPermission(PERMISSIONS.inventory.movementsCreate);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [m, a, b, u] = await Promise.all([
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
      ]);
      setMovimientos(m.data);
      setArticulos(a.data);
      setBodegas(b.data);
      setUnidades(u.data);
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setLoading(false);
    }
  }, [filterArticulo, filterBodega, filterRango, filterTipo]);

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

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Movimientos de Stock</Title>
        {canCreate && <Button type="primary" icon={<PlusOutlined />} onClick={() => setModal(true)}>
          Registrar movimiento
        </Button>}
      </div>

      <Space style={{ marginBottom: 16 }} wrap>
        <Select
          allowClear
          placeholder="Artículo"
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
            title: 'Fecha', dataIndex: 'movedAt', key: 'fecha',
            render: (v: string) => dayjs(v).format('DD/MM/YYYY HH:mm'),
            width: 140,
          },
          { title: 'Artículo', dataIndex: 'articleName', key: 'articulo' },
          { title: 'Bodega', dataIndex: 'warehouseName', key: 'bodega' },
          {
            title: 'Tipo', dataIndex: 'type', key: 'tipo',
            render: (v: MovementType) => <Tag color={tipoColor(v)}>{v}</Tag>,
          },
          {
            title: 'Cantidad', key: 'cantidad',
            render: (_: unknown, m: StockMovementDto) =>
              `${SALIDAS.has(m.type) ? '-' : '+'}${m.quantity} ${m.unitSymbol}`,
          },
          {
            title: 'En unidad base', key: 'cantidadBase',
            render: (_: unknown, m: StockMovementDto) =>
              `${SALIDAS.has(m.type) ? '-' : '+'}${m.baseQuantity} ${m.baseUnitSymbol}`,
          },
          { title: 'Referencia', dataIndex: 'reference', key: 'referencia' },
          { title: 'Observación', dataIndex: 'notes', key: 'observacion' },
        ]}
      />

      <Modal
        title="Registrar movimiento de stock"
        open={modal}
        onOk={registrar}
        onCancel={() => { setModal(false); form.resetFields(); }}
        okText="Registrar"
        width={520}
      >
        <Form form={form} layout="vertical">
          <Form.Item name="articleId" label="Artículo" rules={[{ required: true }]}>
            <Select
              options={articulos.map(a => ({ label: `${a.name} (${a.baseUnitSymbol})`, value: a.id }))}
              placeholder="Seleccionar artículo"
              showSearch
              optionFilterProp="label"
            />
          </Form.Item>
          <Form.Item name="warehouseId" label="Bodega" rules={[{ required: true }]}>
            <Select
              options={bodegas.map(b => ({ label: b.name, value: b.id }))}
              placeholder="Seleccionar bodega"
            />
          </Form.Item>
          <Form.Item name="type" label="Tipo de movimiento" rules={[{ required: true }]}>
            <Select options={TIPO_OPTIONS} placeholder="Seleccionar tipo" />
          </Form.Item>
          <Space style={{ width: '100%' }} size="middle">
            <Form.Item name="quantity" label="Cantidad" rules={[{ required: true }]} style={{ flex: 1 }}>
              <InputNumber style={{ width: '100%' }} min={0} step={0.01} />
            </Form.Item>
            <Form.Item name="unitId" label="Unidad" rules={[{ required: true }]} style={{ flex: 1 }}>
              <Select
                options={unidades.map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id }))}
                placeholder="Seleccionar unidad"
              />
            </Form.Item>
          </Space>
          <Form.Item name="reference" label="Referencia">
            <Input placeholder="Nro factura, orden..." />
          </Form.Item>
          <Form.Item name="notes" label="Observación">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
