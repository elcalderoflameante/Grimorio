import { useState, useEffect, useCallback } from 'react';
import { App as AntApp, Table, Tag, Space, Typography, Button, DatePicker, Row, Col, Statistic, Descriptions, Select, Tooltip } from 'antd';
import { BankOutlined, FileProtectOutlined, GiftOutlined, PrinterOutlined, ReloadOutlined, UserOutlined, SwapOutlined } from '@ant-design/icons';
import CorrectPaymentMethodModal from './CorrectPaymentMethodModal';
import ConvertSaleNoteModal from './ConvertSaleNoteModal';
import SaleTipModal from './SaleTipModal';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import type { OrderPaymentDto, PaymentLineDto } from '../../types';
import { cashApi } from '../../services/api';
import { GenerateInvoiceButton } from './ElectronicInvoices';
import { printThermalReceipt } from '../../utils/thermalReceiptPrinter';
import { branchDateRangeToUtcIso, formatBranchDateTime } from '../../utils/branchTimeZone';
import dayjs, { type Dayjs } from 'dayjs';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;

const formatMoney = (v: number) => `$${v.toFixed(2)}`;

const docLabel = (d: string) => d === 'Factura' ? 'Factura' : 'Nota de venta';
const docColor = (d: string) => d === 'Factura' ? 'blue' : 'default';
const electronicStatusLabel = (status?: string) => ({
  Pending: 'Pendiente',
  Sent: 'Enviada',
  Authorized: 'Autorizada',
  Rejected: 'Rechazada',
  Cancelled: 'Anulada',
}[status ?? ''] ?? status);

const formatQuantity = (value: number) => value.toLocaleString('es-EC', {
  minimumFractionDigits: Number.isInteger(value) ? 0 : 2,
  maximumFractionDigits: 2,
});

const orderTypeLabel = (t?: string) => {
  if (t === 'DineIn') return 'Mesa';
  if (t === 'Takeout') return 'Llevar';
  if (t === 'Delivery') return 'Domicilio';
  return t ?? '—';
};

function PaymentDetail({ payment }: { payment: OrderPaymentDto }) {
  return (
    <div style={{ padding: '8px 16px' }}>
      <Descriptions size="small" column={{ xs: 1, sm: 2, md: 3 }}>
        {payment.noteConvertedAt && <Descriptions.Item label="Nota de venta original">
          Sustituida por factura el {formatBranchDateTime(payment.noteConvertedAt)}
        </Descriptions.Item>}
        {payment.noteConversionReason && <Descriptions.Item label="Motivo de correccion">{payment.noteConversionReason}</Descriptions.Item>}
        <Descriptions.Item label="Caja">
          {payment.cashRegisterCode
            ? `${payment.cashRegisterName} (${payment.cashRegisterCode})`
            : payment.cashRegisterName ?? '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Cajero">{payment.cashierName ?? '—'}</Descriptions.Item>
        {payment.customerName && (
          <Descriptions.Item label="Cliente">{payment.customerName}</Descriptions.Item>
        )}
        {payment.customerTaxId && (
          <Descriptions.Item label="RUC / Cédula">{payment.customerTaxId}</Descriptions.Item>
        )}
        {payment.documentType === 'Factura' && (
          <Descriptions.Item label="N° factura">
            {payment.electronicDocumentNumber ?? 'Pendiente de generación'}
          </Descriptions.Item>
        )}
        {payment.electronicDocumentStatus && (
          <Descriptions.Item label="Estado SRI">
            <Tag>{electronicStatusLabel(payment.electronicDocumentStatus)}</Tag>
          </Descriptions.Item>
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

      <Text strong>Ítems cobrados</Text>
      <Table
        size="small"
        dataSource={payment.items}
        rowKey="id"
        pagination={false}
        locale={{ emptyText: 'Este cobro no tiene ítems detallados' }}
        style={{ marginTop: 8 }}
        columns={[
          {
            title: 'Producto', dataIndex: 'itemName',
            render: (name: string, item: OrderPaymentDto['items'][number]) => (
              <div style={{ minWidth: 220, maxWidth: 520, overflowWrap: 'anywhere' }}>
                <div>{name}</div>
                {!!item.modifierSelections?.length && <Space size={[4, 4]} wrap style={{ marginTop: 4, maxWidth: '100%' }}>
                  {item.modifierSelections.map((selection, index) => (
                    <Tag key={`${selection.modifierGroupId}:${selection.modifierOptionId}:${index}`}
                      style={{ margin: 0, whiteSpace: 'normal', overflowWrap: 'anywhere', maxWidth: '100%' }}>
                      {selection.groupName ? `${selection.groupName}: ` : ''}
                      {selection.quantity > 1 ? `${formatQuantity(selection.quantity)} x ` : ''}{selection.optionName}
                    </Tag>
                  ))}
                </Space>}
              </div>
            ),
          },
          { title: 'Cantidad', dataIndex: 'quantity', width: 100, align: 'right', render: formatQuantity },
          { title: 'Precio unit.', dataIndex: 'unitPrice', width: 120, align: 'right', render: formatMoney },
          { title: 'Total', dataIndex: 'total', width: 110, align: 'right', render: formatMoney },
        ]}
      />
    </div>
  );
}

export default function SalesHistory() {
  const { message } = AntApp.useApp();
  const { hasPermission } = useAuth();
  const [tipSale, setTipSale] = useState<{ id: string; orderNumber: number } | null>(null);
  const [noteToInvoice, setNoteToInvoice] = useState<OrderPaymentDto | null>(null);
  const [paymentToCorrect, setPaymentToCorrect] = useState<OrderPaymentDto | null>(null);
  const [sales, setSales] = useState<OrderPaymentDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [cashierFilter, setCashierFilter] = useState<string>();
  const [registerFilter, setRegisterFilter] = useState<string>();
  const [documentFilter, setDocumentFilter] = useState<string>();
  const [dateRange, setDateRange] = useState<[Dayjs | null, Dayjs | null]>([
    dayjs().startOf('day'),
    dayjs().endOf('day'),
  ]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [from, to] = dateRange;
      const range = branchDateRangeToUtcIso([from, to]);
      const res = await cashApi.getSales({
        from: range.from,
        to: range.to,
        pageSize: 200,
      });
      setSales(res.data);
    } finally {
      setLoading(false);
    }
  }, [dateRange]);

  useEffect(() => { load(); }, [load]);

  const handlePrint = useCallback(async (paymentId: string) => {
    try {
      const { data } = await cashApi.getThermalReceipt(paymentId);
      printThermalReceipt(data);
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } }; message?: string };
      message.error(err?.response?.data?.message ?? err.message ?? 'No se pudo imprimir el comprobante');
    }
  }, [message]);

  const cashierOptions = Array.from(new Set(sales.map(s => s.cashierName).filter(Boolean) as string[]))
    .sort()
    .map(name => ({ value: name, label: name }));

  const registerOptions = Array.from(
    new Map(sales
      .filter(s => s.cashRegisterId)
      .map(s => [s.cashRegisterId!, {
        value: s.cashRegisterId!,
        label: s.cashRegisterCode ? `${s.cashRegisterName} (${s.cashRegisterCode})` : s.cashRegisterName ?? 'Caja',
      }])).values()
  );

  const filteredSales = sales.filter(s => {
    if (cashierFilter && s.cashierName !== cashierFilter) return false;
    if (registerFilter && s.cashRegisterId !== registerFilter) return false;
    if (documentFilter && s.documentType !== documentFilter) return false;
    return true;
  });

  const totalVentas = filteredSales.reduce((s, p) => s + p.orderAmount, 0);

  const methodTotals = filteredSales
    .flatMap(p => p.lines)
    .reduce<Record<string, { name: string; color: string; total: number }>>((acc, l) => {
      if (!acc[l.methodId]) acc[l.methodId] = { name: l.methodName, color: l.methodColor, total: 0 };
      acc[l.methodId].total += l.netAmount;
      return acc;
    }, {});

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12, flexWrap: 'wrap', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0, flexShrink: 0 }}>Ventas del restaurante</Title>
        <Space wrap style={{ maxWidth: '100%' }}>
          <RangePicker
            value={dateRange}
            onChange={(v) => setDateRange(v ? [v[0], v[1]] : [null, null])}
            format="DD/MM/YYYY"
            allowClear={false}
            style={{ width: 280, maxWidth: '100%' }}
          />
          <Select
            allowClear
            placeholder="Caja"
            style={{ width: 180 }}
            value={registerFilter}
            onChange={setRegisterFilter}
            options={registerOptions}
          />
          <Select
            allowClear
            placeholder="Cajero"
            style={{ width: 180 }}
            value={cashierFilter}
            onChange={setCashierFilter}
            options={cashierOptions}
          />
          <Select
            allowClear
            placeholder="Documento"
            style={{ width: 150 }}
            value={documentFilter}
            onChange={setDocumentFilter}
            options={[
              { value: 'NotaDeVenta', label: 'Nota de venta' },
              { value: 'Factura', label: 'Factura' },
            ]}
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
          <Statistic title="Transacciones" value={filteredSales.length} />
        </Col>
        {Object.values(methodTotals).map(m => (
          <Col xs={12} md={6} key={m.name}>
            <Statistic
              title={m.name}
              value={m.total}
              prefix="$"
              precision={2}
              styles={{ content: { color: m.color } }}
            />
          </Col>
        ))}
      </Row>

      <Table
        size="small"
        dataSource={filteredSales}
        rowKey="id"
        loading={loading}
        scroll={{ x: 1400 }}
        pagination={{ defaultPageSize: 25, showSizeChanger: true, pageSizeOptions: ['10', '25', '50', '100'] }}
        expandable={{
          expandedRowRender: (r) => <PaymentDetail payment={r} />,
        }}
        columns={[
          {
            title: 'Fecha',
            dataIndex: 'paidAt',
            width: 150,
            render: (v: string) => formatBranchDateTime(v),
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
                ? <Text>Mesa {r.tableCode}</Text>
                : <Text type="secondary">{r.customerName ?? '—'}</Text>,
          },
          {
            title: 'Caja',
            width: 150,
            render: (_: unknown, r: OrderPaymentDto) => (
              <Space size={4}>
                <BankOutlined />
                <Text>{r.cashRegisterCode ? `${r.cashRegisterName} (${r.cashRegisterCode})` : r.cashRegisterName ?? '—'}</Text>
              </Space>
            ),
          },
          {
            title: 'Cajero',
            dataIndex: 'cashierName',
            width: 140,
            render: (v?: string) => (
              <Space size={4}>
                <UserOutlined />
                <Text>{v ?? '—'}</Text>
              </Space>
            ),
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
            width: 220,
            render: (_: unknown, r: OrderPaymentDto) => (
              <Space size={4}>
                <Tooltip title="Imprimir comprobante 80mm">
                  <Button
                    size="small"
                    icon={<PrinterOutlined />}
                    onClick={() => handlePrint(r.id)}
                  />
                </Tooltip>
                {hasPermission(PERMISSIONS.billing.cashCharge) && <Tooltip title="Registrar o editar propina">
                  <Button size="small" icon={<GiftOutlined />} aria-label={`Propina de orden ${r.orderNumber}`}
                    onClick={() => setTipSale({ id: r.id, orderNumber: r.orderNumber })} />
                </Tooltip>}
                {r.documentType === 'NotaDeVenta' && !r.electronicDocumentId && hasPermission(PERMISSIONS.billing.paymentsCorrectMethod) && (
                  <Tooltip title="Corregir medio de pago / consultar historial">
                    <Button size="small" icon={<SwapOutlined />} aria-label={`Corregir medio de pago de orden ${r.orderNumber}`}
                      onClick={() => setPaymentToCorrect(r)} />
                  </Tooltip>
                )}
                <GenerateInvoiceButton
                  orderPaymentId={r.id}
                  documentType={r.documentType}
                  paidAt={r.paidAt}
                  electronicDocumentId={r.electronicDocumentId}
                  electronicDocumentStatus={r.electronicDocumentStatus}
                  onSuccess={load}
                />
                {r.documentType === 'NotaDeVenta' && !r.electronicDocumentId && hasPermission(PERMISSIONS.billing.sriGenerate) && (
                  <Tooltip title="Emitir factura">
                    <Button size="small" icon={<FileProtectOutlined />} aria-label={`Emitir factura de orden ${r.orderNumber}`}
                      onClick={() => setNoteToInvoice(r)} />
                  </Tooltip>
                )}
              </Space>
            ),
          },
        ]}
      />
      <SaleTipModal sale={tipSale} onClose={() => setTipSale(null)} />
      <ConvertSaleNoteModal payment={noteToInvoice} onClose={() => setNoteToInvoice(null)} onSuccess={load} />
      <CorrectPaymentMethodModal payment={paymentToCorrect} onClose={() => setPaymentToCorrect(null)} onSuccess={load} />
    </div>
  );
}
