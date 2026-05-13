import { useState, useEffect, useCallback } from 'react';
import { Table, Tag, Space, Typography, Button, DatePicker, Row, Col, Statistic, Descriptions } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
import type { OrderPaymentDto, PaymentLineDto } from '../../types';
import { cashApi } from '../../services/api';
import { GenerateInvoiceButton } from './ElectronicInvoices';
import dayjs, { type Dayjs } from 'dayjs';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;

const formatMoney = (v: number) => `$${v.toFixed(2)}`;

const docLabel = (d: string) => d === 'Factura' ? 'Factura' : 'Nota de venta';
const docColor = (d: string) => d === 'Factura' ? 'blue' : 'default';

const orderTypeLabel = (t?: string) => {
  if (t === 'DineIn') return 'Mesa';
  if (t === 'Takeout') return 'Llevar';
  if (t === 'Delivery') return 'Domicilio';
  return t ?? '—';
};

function PaymentDetail({ payment }: { payment: OrderPaymentDto }) {
  return (
    <Descriptions size="small" column={{ xs: 1, sm: 2, md: 3 }} style={{ padding: '8px 16px' }}>
      {payment.customerName && (
        <Descriptions.Item label="Cliente">{payment.customerName}</Descriptions.Item>
      )}
      {payment.customerTaxId && (
        <Descriptions.Item label="RUC / Cédula">{payment.customerTaxId}</Descriptions.Item>
      )}
      {payment.lines.map(l => (
        <Descriptions.Item key={l.id} label={l.methodName}>
          <Space size={4}>
            <Tag color={l.methodColor} style={{ borderColor: l.methodColor }}>
              {formatMoney(l.netAmount)}
            </Tag>
            {l.change > 0 && <Text type="secondary">cambio: {formatMoney(l.change)}</Text>}
          </Space>
        </Descriptions.Item>
      ))}
    </Descriptions>
  );
}

export default function SalesHistory() {
  const [sales, setSales] = useState<OrderPaymentDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [dateRange, setDateRange] = useState<[Dayjs | null, Dayjs | null]>([
    dayjs().startOf('day'),
    dayjs().endOf('day'),
  ]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [from, to] = dateRange;
      const res = await cashApi.getSales({
        from: from?.toISOString(),
        to: to?.toISOString(),
        pageSize: 200,
      });
      setSales(res.data);
    } finally {
      setLoading(false);
    }
  }, [dateRange]);

  useEffect(() => { load(); }, [load]);

  const totalVentas = sales.reduce((s, p) => s + p.orderAmount, 0);

  const methodTotals = sales
    .flatMap(p => p.lines)
    .reduce<Record<string, { name: string; color: string; total: number }>>((acc, l) => {
      if (!acc[l.methodId]) acc[l.methodId] = { name: l.methodName, color: l.methodColor, total: 0 };
      acc[l.methodId].total += l.netAmount;
      return acc;
    }, {});

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Ventas realizadas</Title>
        <Space>
          <RangePicker
            value={dateRange}
            onChange={(v) => setDateRange(v ? [v[0], v[1]] : [null, null])}
            format="DD/MM/YYYY"
            allowClear={false}
          />
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Actualizar</Button>
        </Space>
      </div>

      {/* Resumen del período */}
      <Row gutter={[16, 16]} style={{ marginBottom: 20 }}>
        <Col xs={12} md={6}>
          <Statistic title="Total cobrado" value={totalVentas} prefix="$" precision={2} />
        </Col>
        <Col xs={12} md={6}>
          <Statistic title="Transacciones" value={sales.length} />
        </Col>
        {Object.values(methodTotals).map(m => (
          <Col xs={12} md={6} key={m.name}>
            <Statistic
              title={m.name}
              value={m.total}
              prefix="$"
              precision={2}
              valueStyle={{ color: m.color }}
            />
          </Col>
        ))}
      </Row>

      <Table
        size="small"
        dataSource={sales}
        rowKey="id"
        loading={loading}
        pagination={{ pageSize: 25, showSizeChanger: false }}
        expandable={{
          expandedRowRender: (r) => <PaymentDetail payment={r} />,
        }}
        columns={[
          {
            title: 'Fecha',
            dataIndex: 'paidAt',
            width: 150,
            render: (v: string) => dayjs(v).format('DD/MM/YYYY HH:mm'),
          },
          {
            title: 'Orden',
            dataIndex: 'orderNumber',
            width: 80,
            render: (v: number) => `#${v}`,
          },
          {
            title: 'Tipo',
            dataIndex: 'orderType',
            width: 90,
            render: (v?: string) => orderTypeLabel(v),
          },
          {
            title: 'Mesa / Cliente',
            width: 160,
            render: (_: unknown, r: OrderPaymentDto) =>
              r.tableCode
                ? <Text>{r.tableCode}{r.tableName ? ` - ${r.tableName}` : ''}</Text>
                : <Text type="secondary">{r.customerName ?? '—'}</Text>,
          },
          {
            title: 'Documento',
            dataIndex: 'documentType',
            width: 130,
            render: (v: string) => <Tag color={docColor(v)}>{docLabel(v)}</Tag>,
          },
          {
            title: 'Medios de pago',
            render: (_: unknown, r: OrderPaymentDto) => (
              <Space size={4} wrap>
                {r.lines.map((l: PaymentLineDto) => (
                  <Tag key={l.id} color={l.methodColor} style={{ borderColor: l.methodColor }}>
                    {l.methodName}: {formatMoney(l.netAmount)}
                  </Tag>
                ))}
              </Space>
            ),
          },
          {
            title: 'Total',
            dataIndex: 'orderAmount',
            align: 'right',
            width: 100,
            render: (v: number) => <Text strong>{formatMoney(v)}</Text>,
          },
          {
            title: '',
            width: 180,
            render: (_: unknown, r: OrderPaymentDto) => (
              <GenerateInvoiceButton
                orderPaymentId={r.id}
                documentType={r.documentType}
                electronicDocumentId={r.electronicDocumentId}
                electronicDocumentStatus={r.electronicDocumentStatus}
                onSuccess={load}
              />
            ),
          },
        ]}
      />
    </div>
  );
}
