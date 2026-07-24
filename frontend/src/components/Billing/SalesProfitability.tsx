import { useEffect, useMemo, useState } from 'react';
import { App as AntApp, Alert, Card, Col, DatePicker, Progress, Row, Select, Space, Statistic, Table, Tag, Tooltip, Typography } from 'antd';
import { BarChartOutlined, WarningOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { cashApi } from '../../services/api';
import type { CashRegisterDto, SalesProfitabilityItemDto, SalesProfitabilityReportDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

const { RangePicker } = DatePicker;
const { Text, Title } = Typography;

const money = (value?: number) => `$${(value ?? 0).toFixed(2)}`;
const pct = (value?: number) => `${(value ?? 0).toFixed(2)}%`;

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

  const load = async () => {
    setLoading(true);
    try {
      const [profitabilityRes, registersRes] = await Promise.all([
        cashApi.getSalesProfitability({
          from: range[0].toISOString(),
          to: range[1].toISOString(),
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
  const itemRows = report?.items ?? [];

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

      <Table
        dataSource={itemRows}
        rowKey="menuItemId"
        loading={loading}
        size="small"
        tableLayout="fixed"
        scroll={{ x: 1180 }}
        pagination={{ defaultPageSize: 15, showSizeChanger: true, pageSizeOptions: ['15', '30', '50', '100'] }}
        columns={[
          {
            title: 'Plato',
            width: 260,
            render: (_: unknown, item: SalesProfitabilityItemDto) => (
              <Space orientation="vertical" size={0} style={{ maxWidth: 240 }}>
                <Text strong ellipsis={{ tooltip: item.menuItemName }}>{item.menuItemName}</Text>
                <Text type="secondary" style={{ fontSize: 12 }} ellipsis={{ tooltip: item.categoryName }}>{item.categoryName}</Text>
              </Space>
            ),
          },
          { title: 'Cant.', dataIndex: 'quantity', width: 80, align: 'right', render: (v: number) => v.toFixed(2) },
          { title: 'Venta neta', dataIndex: 'netSales', width: 120, align: 'right', render: money },
          { title: 'IVA', dataIndex: 'taxAmount', width: 90, align: 'right', render: money },
          { title: 'Costo unit.', dataIndex: 'unitRecipeCost', width: 110, align: 'right', render: money },
          { title: 'Costo total', dataIndex: 'totalFoodCost', width: 120, align: 'right', render: money },
          { title: 'Utilidad', dataIndex: 'grossProfit', width: 120, align: 'right', render: (v: number) => <Text type={v < 0 ? 'danger' : undefined}>{money(v)}</Text> },
          { title: 'Food cost', dataIndex: 'foodCostPercentage', width: 110, align: 'right', render: pct },
          { title: 'Margen', dataIndex: 'grossMarginPercentage', width: 100, align: 'right', render: pct },
          {
            title: 'Alertas',
            width: 130,
            render: (_: unknown, item: SalesProfitabilityItemDto) => (
              <Space size={4} wrap>
                {item.hasMissingCosts && <Tooltip title="Hay ingredientes sin compras registradas"><Tag color="gold" icon={<WarningOutlined />}>Costos</Tag></Tooltip>}
                {item.hasConversionWarnings && <Tooltip title="Hay unidades sin conversion"><Tag color="orange" icon={<WarningOutlined />}>Unidades</Tag></Tooltip>}
                {!item.hasMissingCosts && !item.hasConversionWarnings && <Tag color="green" icon={<BarChartOutlined />}>OK</Tag>}
              </Space>
            ),
          },
        ]}
      />
    </div>
  );
}
