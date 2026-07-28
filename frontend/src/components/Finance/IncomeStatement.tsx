import { useCallback, useEffect, useState } from 'react';
import { Alert, Button, Col, DatePicker, Row, Space, Statistic, Table, Tag, Typography } from 'antd';
import { ReloadOutlined, WarningOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { financeApi } from '../../services/api';
import type { ExpenseReportGroupDto, IncomeStatementDto, IncomeStatementLineDto } from '../../types';

const { RangePicker } = DatePicker;
const { Text } = Typography;

const fmt = (value: number) => `$${value.toFixed(2)}`;

const categoryTypeLabels: Record<string, string> = {
  Fixed: 'Fijo',
  Variable: 'Variable',
  Mixed: 'Mixto',
};

export default function IncomeStatement() {
  const [report, setReport] = useState<IncomeStatementDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [dateRange, setDateRange] = useState<[Dayjs | null, Dayjs | null]>([
    dayjs().startOf('month'),
    dayjs().endOf('day'),
  ]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [from, to] = dateRange;
      const res = await financeApi.getIncomeStatement({
        from: from?.startOf('day').toISOString(),
        to: to?.endOf('day').toISOString(),
      });
      setReport(res.data);
    } finally {
      setLoading(false);
    }
  }, [dateRange]);

  useEffect(() => { load(); }, [load]);

  const groupColumns = [
    { title: 'Nombre', dataIndex: 'name', key: 'name' },
    {
      title: 'Tipo',
      dataIndex: 'type',
      key: 'type',
      width: 100,
      render: (value?: string) => value ? <Tag>{categoryTypeLabels[value] ?? value}</Tag> : '-',
    },
    {
      title: '%',
      dataIndex: 'percentage',
      key: 'percentage',
      align: 'right' as const,
      width: 70,
      render: (value: number) => `${value.toFixed(1)}%`,
    },
    {
      title: 'Total',
      dataIndex: 'total',
      key: 'total',
      align: 'right' as const,
      width: 110,
      render: (value: number) => <Text strong>{fmt(value)}</Text>,
    },
  ];

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, marginBottom: 16, flexWrap: 'wrap' }}>
        <h2 style={{ margin: 0 }}>Estado de resultados</h2>
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
          description={`${report.missingCostLines} lineas sin costo y ${report.conversionWarningLines} lineas con advertencias de conversion pueden afectar la utilidad.`}
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
          <Statistic title="Gastos operativos" value={report?.operatingExpenses ?? 0} prefix="$" precision={2} />
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

      <Row gutter={[12, 12]} style={{ marginBottom: 20 }}>
        <Col xs={12} md={6}>
          <Statistic title="Food cost %" value={report?.foodCostPercentage ?? 0} suffix="%" precision={2} />
        </Col>
        <Col xs={12} md={6}>
          <Statistic title="Margen bruto" value={report?.grossMarginPercentage ?? 0} suffix="%" precision={2} />
        </Col>
        <Col xs={12} md={6}>
          <Statistic title="Margen operativo" value={report?.operatingMarginPercentage ?? 0} suffix="%" precision={2} />
        </Col>
        <Col xs={12} md={6}>
          <Statistic title="Ordenes / gastos" value={`${report?.totalOrders ?? 0} / ${report?.expenseCount ?? 0}`} />
        </Col>
      </Row>

      <Table
        size="small"
        rowKey="key"
        dataSource={report?.lines ?? []}
        pagination={false}
        loading={loading}
        style={{ marginBottom: 20 }}
        columns={[
          {
            title: 'Concepto',
            dataIndex: 'label',
            render: (value: string, row: IncomeStatementLineDto) => (
              <Text strong={row.isSubtotal}>{value}</Text>
            ),
          },
          {
            title: '% ventas netas',
            dataIndex: 'percentageOfNetSales',
            align: 'right',
            width: 150,
            render: (value: number) => `${value.toFixed(2)}%`,
          },
          {
            title: 'Valor',
            dataIndex: 'amount',
            align: 'right',
            width: 150,
            render: (value: number, row: IncomeStatementLineDto) => (
              <Text strong={row.isSubtotal} type={value < 0 ? 'danger' : undefined}>
                {fmt(value)}
              </Text>
            ),
          },
        ]}
      />

      <Row gutter={[16, 16]}>
        <Col xs={24} lg={12}>
          <Table
            title={() => 'Gastos por centro de costo'}
            size="small"
            rowKey={(row: ExpenseReportGroupDto) => row.id ?? row.name}
            dataSource={report?.expensesByCostCenter ?? []}
            columns={groupColumns.filter(c => c.key !== 'type')}
            pagination={false}
          />
        </Col>
        <Col xs={24} lg={12}>
          <Table
            title={() => 'Gastos por categoria'}
            size="small"
            rowKey={(row: ExpenseReportGroupDto) => row.id ?? row.name}
            dataSource={report?.expensesByCategory ?? []}
            columns={groupColumns}
            pagination={false}
          />
        </Col>
      </Row>
    </div>
  );
}
