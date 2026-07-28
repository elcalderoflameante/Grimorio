import { useCallback, useEffect, useState } from 'react';
import { Alert, Button, Col, DatePicker, Row, Space, Statistic, Table, Tag, Typography } from 'antd';
import { ReloadOutlined, WarningOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { financeApi } from '../../services/api';
import type { CostCenterProfitabilityDto, CostCenterProfitabilityReportDto } from '../../types';

const { RangePicker } = DatePicker;
const { Text } = Typography;

const fmt = (value: number) => `$${value.toFixed(2)}`;

export default function CostCenterProfitability() {
  const [report, setReport] = useState<CostCenterProfitabilityReportDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [dateRange, setDateRange] = useState<[Dayjs | null, Dayjs | null]>([
    dayjs().startOf('month'),
    dayjs().endOf('day'),
  ]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [from, to] = dateRange;
      const res = await financeApi.getCostCenterProfitability({
        from: from?.startOf('day').toISOString(),
        to: to?.endOf('day').toISOString(),
      });
      setReport(res.data);
    } finally {
      setLoading(false);
    }
  }, [dateRange]);

  useEffect(() => { load(); }, [load]);

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, marginBottom: 16, flexWrap: 'wrap' }}>
        <h2 style={{ margin: 0 }}>Rentabilidad por centro</h2>
        <Space wrap>
          <RangePicker
            value={dateRange}
            onChange={(value) => setDateRange(value ? [value[0], value[1]] : [null, null])}
            format="DD/MM/YYYY"
            allowClear={false}
          />
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Actualizar</Button>
        </Space>
      </div>

      {(report?.missingCostLines || report?.conversionWarningLines) ? (
        <Alert
          type="warning"
          showIcon
          icon={<WarningOutlined />}
          style={{ marginBottom: 16 }}
          message="Hay costos de receta pendientes de revisar"
          description={`${report.missingCostLines} lineas sin costo y ${report.conversionWarningLines} lineas con advertencias de conversion pueden afectar la utilidad por centro.`}
        />
      ) : null}

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={12} md={6}>
          <Statistic title="Ventas netas" value={report?.netSales ?? 0} prefix="$" precision={2} />
        </Col>
        <Col xs={12} md={6}>
          <Statistic title="Costo recetas" value={report?.foodCost ?? 0} prefix="$" precision={2} />
        </Col>
        <Col xs={12} md={6}>
          <Statistic title="Gastos" value={report?.operatingExpenses ?? 0} prefix="$" precision={2} />
        </Col>
        <Col xs={12} md={6}>
          <Statistic
            title="Utilidad operativa"
            value={report?.operatingProfit ?? 0}
            prefix="$"
            precision={2}
            valueStyle={{ color: (report?.operatingProfit ?? 0) >= 0 ? '#3f8600' : '#cf1322' }}
          />
        </Col>
      </Row>

      <Table
        size="small"
        rowKey={(row: CostCenterProfitabilityDto) => row.costCenterId ?? row.costCenterName}
        dataSource={report?.centers ?? []}
        loading={loading}
        pagination={{ defaultPageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
        columns={[
          {
            title: 'Centro de costo',
            dataIndex: 'costCenterName',
            key: 'costCenterName',
            render: (value: string, row: CostCenterProfitabilityDto) => (
              <Space>
                <Text strong>{value}</Text>
                {!row.costCenterId && <Tag>Sin asignar</Tag>}
                {row.missingCostLines > 0 && <Tag color="orange">Sin costo</Tag>}
                {row.conversionWarningLines > 0 && <Tag color="gold">Conversion</Tag>}
              </Space>
            ),
          },
          {
            title: 'Ventas netas',
            dataIndex: 'netSales',
            key: 'netSales',
            align: 'right',
            render: (value: number) => fmt(value),
          },
          {
            title: 'Costo receta',
            dataIndex: 'foodCost',
            key: 'foodCost',
            align: 'right',
            render: (value: number) => fmt(value),
          },
          {
            title: 'Utilidad bruta',
            dataIndex: 'grossProfit',
            key: 'grossProfit',
            align: 'right',
            render: (value: number) => <Text type={value < 0 ? 'danger' : undefined}>{fmt(value)}</Text>,
          },
          {
            title: 'Gastos',
            dataIndex: 'operatingExpenses',
            key: 'operatingExpenses',
            align: 'right',
            render: (value: number) => fmt(value),
          },
          {
            title: 'Utilidad operativa',
            dataIndex: 'operatingProfit',
            key: 'operatingProfit',
            align: 'right',
            render: (value: number) => <Text strong type={value < 0 ? 'danger' : undefined}>{fmt(value)}</Text>,
          },
          {
            title: 'Food cost',
            dataIndex: 'foodCostPercentage',
            key: 'foodCostPercentage',
            align: 'right',
            width: 105,
            render: (value: number) => `${value.toFixed(2)}%`,
          },
          {
            title: 'Margen op.',
            dataIndex: 'operatingMarginPercentage',
            key: 'operatingMarginPercentage',
            align: 'right',
            width: 105,
            render: (value: number) => `${value.toFixed(2)}%`,
          },
          {
            title: 'Items',
            dataIndex: 'totalItems',
            key: 'totalItems',
            align: 'right',
            width: 90,
            render: (value: number) => value.toFixed(2),
          },
        ]}
      />
    </div>
  );
}
