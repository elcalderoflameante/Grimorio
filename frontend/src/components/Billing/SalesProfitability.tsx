import { useEffect, useMemo, useState } from 'react';
import { App as AntApp, Alert, Card, Col, DatePicker, Input, Progress, Row, Segmented, Select, Space, Statistic, Table, Tag, Tooltip, Typography } from 'antd';
import { BarChartOutlined, WarningOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { cashApi } from '../../services/api';
import type { CashRegisterDto, SalesProfitabilityBreakdownDto, SalesProfitabilityItemDto, SalesProfitabilityReportDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { branchDateTimeUtcIso } from '../../utils/branchTimeZone';

const { RangePicker } = DatePicker;
const { Text, Title } = Typography;

const money = (value?: number) => `$${(value ?? 0).toFixed(2)}`;
const pct = (value?: number) => `${(value ?? 0).toFixed(2)}%`;
type ReportRow = SalesProfitabilityItemDto & Partial<SalesProfitabilityBreakdownDto>;
const searchable = (value: string) => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();

const gaugeColor = (value: number) => {
  if (value <= 0) return '#8c8c8c';
  if (value <= 35) return '#52c41a';
  if (value <= 45) return '#faad14';
  return '#ff4d4f';
};

export default function SalesProfitability() {
  const { message } = AntApp.useApp();

  const [report, setReport] = useState<SalesProfitabilityReportDto | null>(null);
  const [registers, setRegisters] = useState<CashRegisterDto[]>([]);
  const [range, setRange] = useState<[Dayjs, Dayjs]>([dayjs().startOf('day'), dayjs().endOf('day')]);
  const [cashRegisterId, setCashRegisterId] = useState<string | undefined>();
  const [loading, setLoading] = useState(false);
  const [view, setView] = useState('breakdown');
  const [search, setSearch] = useState('');
  const [saleKind, setSaleKind] = useState<string | undefined>();
  const [promotionId, setPromotionId] = useState<string | undefined>();

  const load = async () => {
    setLoading(true);
    try {
      const [profitabilityRes, registersRes] = await Promise.all([
        cashApi.getSalesProfitability({
          from: branchDateTimeUtcIso(range[0]),
          to: branchDateTimeUtcIso(range[1]),
          cashRegisterId,
        }),
        cashApi.getRegisters(true),
      ]);
      setReport(profitabilityRes.data);
      setRegisters(registersRes.data);
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [range, cashRegisterId]);

  const registerOptions = useMemo(
    () => registers.map(r => ({ label: `${r.code} · ${r.name}`, value: r.id })),
    [registers],
  );

  const cashRegisterRows = report?.cashRegisters ?? [];
  const itemRows: ReportRow[] = (view === 'items' ? report?.items ?? []
    : view === 'promotions' ? report?.promotions ?? [] : report?.salesBreakdown ?? [])
    .filter(row => {
      const sale = row as ReportRow;
      return (!search || searchable(`${sale.menuItemName} ${sale.promotionName ?? ''} ${sale.categoryName} ${sale.internalCode ?? ''}`).includes(searchable(search.trim())))
        && (view !== 'breakdown' || !saleKind || sale.saleKind === saleKind)
        && (view === 'items' || !promotionId || sale.promotionId === promotionId);
    });
  const promotionOptions = (report?.promotions ?? []).filter(p => p.promotionId)
    .map(p => ({ value: p.promotionId!, label: p.promotionName || 'Promocion sin nombre' }));

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, marginBottom: 16, flexWrap: 'wrap' }}>
        <Space orientation="vertical" size={0}>
          <Title level={5} style={{ margin: 0 }}>Rentabilidad de ventas</Title>
          <Text type="secondary">Venta neta sin IVA menos costo promedio de ingredientes vendidos.</Text>
        </Space>
        <Space wrap>
          <RangePicker
            value={range}
            showTime
            format="DD/MM/YYYY HH:mm"
            onChange={value => {
              if (!value?.[0] || !value?.[1]) return;
              setRange([value[0], value[1]]);
            }}
          />
          <Select
            allowClear
            placeholder="Todas las cajas"
            value={cashRegisterId}
            options={registerOptions}
            onChange={setCashRegisterId}
            style={{ width: 240 }}
          />
        </Space>
      </div>

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        title="Este reporte muestra utilidad bruta de comida. No incluye arriendo, sueldos, luz, agua, gas, comisiones ni otros gastos operativos."
      />

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={24} sm={12} lg={6}>
          <Card size="small"><Statistic title="Venta neta" value={report?.netSales ?? 0} precision={2} prefix="$" loading={loading} /></Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card size="small"><Statistic title="Costo comida" value={report?.foodCost ?? 0} precision={2} prefix="$" loading={loading} /></Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card size="small"><Statistic title="Utilidad bruta" value={report?.grossProfit ?? 0} precision={2} prefix="$" loading={loading} /></Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card size="small"><Statistic title="Ordenes cobradas" value={report?.totalOrders ?? 0} loading={loading} /></Card>
        </Col>
      </Row>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={24} md={8}>
          <Card size="small" style={{ height: '100%' }}>
            <Space align="center" size={18}>
              <Progress
                type="dashboard"
                percent={Math.min(report?.foodCostPercentage ?? 0, 100)}
                strokeColor={gaugeColor(report?.foodCostPercentage ?? 0)}
                format={() => pct(report?.foodCostPercentage)}
              />
              <Space orientation="vertical" size={2}>
                <Text strong>Food cost del periodo</Text>
                <Text type="secondary">Objetivo saludable: 25% a 35%</Text>
                <Text>Margen bruto: <strong>{pct(report?.grossMarginPercentage)}</strong></Text>
              </Space>
            </Space>
          </Card>
        </Col>
        <Col xs={24} md={16}>
          <Table
            dataSource={cashRegisterRows}
            rowKey={row => row.cashRegisterId ?? row.cashRegisterName}
            loading={loading}
            size="small"
            pagination={false}
            columns={[
              { title: 'Caja', dataIndex: 'cashRegisterName' },
              { title: 'Venta neta', dataIndex: 'netSales', align: 'right', render: money },
              { title: 'Costo', dataIndex: 'foodCost', align: 'right', render: money },
              { title: 'Utilidad', dataIndex: 'grossProfit', align: 'right', render: money },
              { title: 'Food cost', dataIndex: 'foodCostPercentage', align: 'right', render: pct },
              { title: 'Ordenes', dataIndex: 'totalOrders', align: 'right' },
            ]}
          />
        </Col>
      </Row>

      {(report?.missingCostLines || report?.conversionWarningLines) ? (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 16 }}
          message={`${report.missingCostLines} linea(s) con costos faltantes y ${report.conversionWarningLines} linea(s) con conversiones por revisar.`}
        />
      ) : null}

      <Space wrap style={{ marginBottom: 16, width: '100%' }}>
        <Segmented value={view} onChange={setView} options={[
          { value: 'breakdown', label: 'Detalle' },
          { value: 'promotions', label: 'Promociones' },
          { value: 'items', label: 'Por plato' },
        ]} />
        <Input.Search allowClear placeholder="Buscar plato o promocion" value={search}
          onChange={e => setSearch(e.target.value)} style={{ width: 250, maxWidth: '100%' }} />
        {view === 'breakdown' && <Select aria-label="Tipo de venta" allowClear placeholder="Tipo de venta" value={saleKind}
          onChange={setSaleKind} style={{ width: 210 }} options={[
            { value: 'Regular', label: 'Sin promocion registrada' },
            { value: 'Promotion', label: 'Con promocion' },
            { value: 'Unclassified', label: 'Descuento sin promocion' },
          ]} />}
        {view !== 'items' && <Select aria-label="Promocion" allowClear showSearch optionFilterProp="label" placeholder="Todas las promociones"
          value={promotionId} onChange={setPromotionId} options={promotionOptions} style={{ width: 250, maxWidth: '100%' }} />}
      </Space>
      {view !== 'items' && report && !report.salesBreakdown && <Alert type="warning" showIcon
        title="Actualiza el backend para consultar el detalle por promociones." style={{ marginBottom: 16 }} />}
      <Table<ReportRow>
        dataSource={itemRows}
        rowKey={row => row.rowKey ?? row.menuItemId}
        loading={loading}
        size="small"
        tableLayout="fixed"
        scroll={{ x: view === 'items' ? 1180 : 1510 }}
        pagination={{ defaultPageSize: 15, showSizeChanger: true, pageSizeOptions: ['15', '30', '50', '100'] }}
        columns={[
          {
            title: view === 'promotions' ? 'Promocion' : 'Plato',
            width: 260,
            render: (_: unknown, item: ReportRow) => (
              <Space orientation="vertical" size={0} style={{ maxWidth: 240 }}>
                <Text strong style={{ overflowWrap: 'anywhere' }}>{view === 'promotions' ? item.promotionName || 'Promocion sin nombre' : item.menuItemName}</Text>
                {view === 'breakdown' ? <Text type={item.saleKind === 'Unclassified' ? 'warning' : 'secondary'} style={{ fontSize: 12, overflowWrap: 'anywhere' }}>
                  {item.saleKind === 'Promotion' ? item.promotionName || 'Promocion sin nombre'
                    : item.saleKind === 'Unclassified' ? 'Descuento sin promocion registrada' : 'Sin promocion registrada'}
                </Text> : view === 'items' && <Text type="secondary" style={{ fontSize: 12 }}>{item.categoryName}</Text>}
              </Space>
            ),
          },
          { title: 'Unidades / combos', dataIndex: 'quantity', width: 100, align: 'right', render: (v: number) => v.toFixed(2), sorter: (a, b) => a.quantity - b.quantity },
          ...(view === 'items' ? [] : [{ title: <Tooltip title="Parejas promocionales prorrateadas segun las unidades cobradas. Puede haber fracciones en cobros parciales. Sin dato cuando no existe grupo historico.">Pares equiv.</Tooltip>,
            key: 'pairs', width: 95, align: 'right' as const, render: (_: unknown, row: ReportRow) => row.pairEquivalents?.toFixed(2) ?? '-' },
          { title: 'Descuento', dataIndex: 'discountAmount', width: 105, align: 'right' as const, render: money }]),
          { title: 'Venta neta', dataIndex: 'netSales', width: 120, align: 'right', render: money },
          { title: 'IVA', dataIndex: 'taxAmount', width: 90, align: 'right', render: money },
          ...(view === 'promotions' ? [] : [{ title: 'Costo unit.', dataIndex: 'unitRecipeCost', width: 110, align: 'right' as const, render: money }]),
          { title: 'Costo total', dataIndex: 'totalFoodCost', width: 120, align: 'right', render: money },
          { title: 'Utilidad', dataIndex: 'grossProfit', width: 120, align: 'right', render: (v: number) => <Text type={v < 0 ? 'danger' : undefined}>{money(v)}</Text> },
          { title: 'Food cost', dataIndex: 'foodCostPercentage', width: 110, align: 'right', render: pct },
          { title: 'Margen', dataIndex: 'grossMarginPercentage', width: 100, align: 'right', render: pct },
          {
            title: 'Alertas',
            width: 130,
            render: (_: unknown, item: ReportRow) => (
              <Space size={4} wrap>
                {item.hasEstimatedQuantities && <Tooltip title="Cobro antiguo sin detalle por plato; cantidades e importes distribuidos proporcionalmente."><Tag color="gold">Estimado</Tag></Tooltip>}
                {item.hasMissingCosts && <Tooltip title="Hay ingredientes sin compras registradas"><Tag color="gold" icon={<WarningOutlined />}>Costos</Tag></Tooltip>}
                {item.hasConversionWarnings && <Tooltip title="Hay unidades sin conversion"><Tag color="orange" icon={<WarningOutlined />}>Unidades</Tag></Tooltip>}
                {!item.hasMissingCosts && !item.hasConversionWarnings && !item.hasEstimatedQuantities && <Tag color="green" icon={<BarChartOutlined />}>OK</Tag>}
              </Space>
            ),
          },
        ]}
      />
    </div>
  );
}
