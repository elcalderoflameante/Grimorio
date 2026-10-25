import { useEffect, useState } from 'react';
import { Alert, Button, DatePicker, Modal, Space, Table, Tag, Typography } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
import type { Dayjs } from 'dayjs';
import type { CustomerDto, CustomerSalesDto, OrderPaymentDto } from '../../types';
import { customersApi } from '../../services/api';
import { branchDateRangeToUtcIso, formatBranchDateTime } from '../../utils/branchTimeZone';
import { formatError } from '../../utils/errorHandler';
import { PaymentDetail } from './SalesHistory';

export default function CustomerSalesModal({ customer, onClose }: { customer: CustomerDto; onClose: () => void }) {
  const [data, setData] = useState<CustomerSalesDto>({ totalCount: 0, totalAmount: 0, items: [] });
  const [range, setRange] = useState<[Dayjs | null, Dayjs | null]>([null, null]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [revision, setRevision] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  useEffect(() => {
    let cancelled = false;
    const dates = branchDateRangeToUtcIso(range);
    customersApi.getSales(customer.id, { from: dates.from, to: dates.to, page, pageSize })
      .then(response => { if (!cancelled) setData(response.data); })
      .catch(e => { if (!cancelled) { setData({ totalCount: 0, totalAmount: 0, items: [] }); setError(formatError(e)); } })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [customer.id, range, page, pageSize, revision]);

  return <Modal open title={<span style={{ overflowWrap: 'anywhere', display: 'block', paddingRight: 24 }}>Ventas de {customer.name}</span>}
    onCancel={onClose} footer={null} width={1050} styles={{ body: { maxHeight: '75vh', overflowY: 'auto' } }}>
    <Typography.Paragraph type="secondary" style={{ marginTop: 12 }}>{customer.taxId}</Typography.Paragraph>
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginBottom: 16 }}>
      <DatePicker.RangePicker aria-label="Fechas de ventas" value={range} allowClear format="DD/MM/YYYY"
        style={{ width: 280, maxWidth: '100%' }} onChange={value => { setLoading(true); setError(''); setPage(1); setRange(value ? [value[0], value[1]] : [null, null]); }} />
      <Button icon={<ReloadOutlined />} aria-label="Actualizar ventas del cliente" loading={loading} onClick={() => { setLoading(true); setError(''); setRevision(v => v + 1); }}>Actualizar</Button>
    </div>
    {error ? <Alert type="error" showIcon title="No se pudieron cargar las ventas" description={error} style={{ marginBottom: 16 }} /> :
      <Space wrap style={{ marginBottom: 16 }}>
        <Typography.Text>Cobros: <strong>{data.totalCount}</strong></Typography.Text>
        <Typography.Text>Total: <strong>${data.totalAmount.toFixed(2)}</strong></Typography.Text>
      </Space>}
    <Table<OrderPaymentDto> rowKey="id" size="small" loading={loading} dataSource={data.items} scroll={{ x: 850 }}
      locale={{ emptyText: error ? 'Ventas no disponibles' : 'Este cliente no tiene ventas en el periodo seleccionado' }}
      pagination={{ current: page, pageSize, total: data.totalCount, showSizeChanger: true, pageSizeOptions: [10, 25, 50],
        onChange: (current, size) => { setLoading(true); setError(''); setPage(size !== pageSize ? 1 : current); setPageSize(size); } }}
      expandable={{ expandedRowRender: payment => <PaymentDetail payment={payment} /> }} columns={[
        { title: 'Fecha', dataIndex: 'paidAt', width: 160, render: (value: string) => formatBranchDateTime(value) },
        { title: 'Orden', dataIndex: 'orderNumber', width: 85, render: (value: number) => `#${value}` },
        { title: 'Documento', key: 'document', render: (_, payment) => <Space orientation="vertical" size={2}>
          <Tag color={payment.documentType === 'Factura' ? 'blue' : 'default'}>{payment.documentType === 'Factura' ? 'Factura' : 'Nota de venta'}</Tag>
          {payment.electronicDocumentNumber && <Typography.Text>{payment.electronicDocumentNumber}</Typography.Text>}
        </Space> },
        { title: 'Medios de pago', key: 'methods', render: (_, payment) => <Space wrap size={4}>
          {payment.lines.map(line => <Tag key={line.id} color={line.methodColor}>{line.methodName}: ${line.netAmount.toFixed(2)}</Tag>)}
        </Space> },
        { title: 'Total', dataIndex: 'orderAmount', align: 'right', width: 110, render: (value: number) => `$${value.toFixed(2)}` },
      ]} />
  </Modal>;
}
