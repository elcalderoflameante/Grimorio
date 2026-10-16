import { useEffect, useState } from 'react';
import { App, Button, Col, DatePicker, Row, Select, Statistic, Table, Tooltip, Typography } from 'antd';
import { EditOutlined, ReloadOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { saleTipsApi } from '../../services/api';
import type { SaleTipReportDto, TipMethod } from '../../types/saleTips';
import { TIP_METHODS } from '../../types/saleTips';
import { branchDateRangeToUtcIso, formatBranchDateTime } from '../../utils/branchTimeZone';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import SaleTipModal from './SaleTipModal';

export default function SaleTipsSummary() {
  const { message } = App.useApp();
  const { hasPermission } = useAuth();
  const [range, setRange] = useState<[Dayjs | null, Dayjs | null]>([dayjs().startOf('day'), dayjs().endOf('day')]);
  const [method, setMethod] = useState<TipMethod>();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [refresh, setRefresh] = useState(0);
  const [result, setResult] = useState<{ key: string; data?: SaleTipReportDto }>({ key: '' });
  const [sale, setSale] = useState<{ id: string; orderNumber: number } | null>(null);
  const queryKey = JSON.stringify([range.map(date => date?.valueOf()), method, page, pageSize, refresh]);
  const loading = result.key !== queryKey;
  const report = loading ? undefined : result.data;

  useEffect(() => {
    let cancelled = false;
    saleTipsApi.report({ ...branchDateRangeToUtcIso(range), method, page, pageSize })
      .then(({ data }) => { if (!cancelled) setResult({ key: queryKey, data }); })
      .catch(error => {
        if (!cancelled) {
          setResult({ key: queryKey });
          message.error(formatError(error));
        }
      });
    return () => { cancelled = true; };
  }, [range, method, page, pageSize, queryKey, message]);

  return <div>
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap', marginBottom: 24 }}>
      <Typography.Title level={5} style={{ margin: 0 }}>Propinas</Typography.Title>
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', maxWidth: '100%' }}>
        <DatePicker.RangePicker value={range} format="DD/MM/YYYY" allowClear={false} style={{ width: 280, maxWidth: '100%' }}
          onChange={value => { setRange(value ? [value[0], value[1]] : [null, null]); setPage(1); }} />
        <Select placeholder="Metodo" aria-label="Filtrar por metodo" allowClear value={method} options={TIP_METHODS} style={{ width: 160 }}
          onChange={value => { setMethod(value); setPage(1); }} />
        <Tooltip title="Actualizar"><Button aria-label="Actualizar propinas" icon={<ReloadOutlined />} loading={loading}
          onClick={() => setRefresh(value => value + 1)} /></Tooltip>
      </div>
    </div>
    <Row gutter={[16, 20]} style={{ marginBottom: 24 }}>
      <Col xs={12} md={6}><Statistic title="Total propinas" value={report?.totalAmount ?? 0} precision={2} prefix="$" loading={loading} /></Col>
      {TIP_METHODS.map(item => <Col xs={12} md={6} key={item.value}>
        <Statistic title={item.label} value={report?.byMethod.find(m => m.method === item.value)?.amount ?? 0} precision={2} prefix="$" loading={loading} />
      </Col>)}
    </Row>
    <Table rowKey="id" size="small" dataSource={report?.items ?? []} loading={loading} scroll={{ x: 800 }}
      pagination={{ current: page, pageSize, total: report?.count ?? 0, showSizeChanger: true,
        onChange: (next, size) => { setPage(size !== pageSize ? 1 : next); setPageSize(size); } }}
      columns={[
        { title: 'Fecha registro', dataIndex: 'registeredAt', width: 170, render: (value: string) => formatBranchDateTime(value) },
        { title: 'Orden', dataIndex: 'orderNumber', width: 85, render: (value: number) => `#${value}` },
        { title: 'Fecha venta', dataIndex: 'saleDate', width: 170, render: (value: string) => formatBranchDateTime(value) },
        { title: 'Metodo', dataIndex: 'method', width: 130, render: (value: TipMethod) => TIP_METHODS.find(m => m.value === value)?.label },
        { title: 'Registrado por', dataIndex: 'registeredByName', width: 160 },
        { title: 'Importe', dataIndex: 'amount', align: 'right', width: 100, render: (value: number) => `$${value.toFixed(2)}` },
        ...(hasPermission(PERMISSIONS.billing.cashCharge) ? [{ title: '', key: 'actions', width: 48,
          render: (_: unknown, row: { orderPaymentId: string; orderNumber: number }) => <Tooltip title="Editar propina">
            <Button size="small" aria-label={`Editar propina de orden ${row.orderNumber}`} icon={<EditOutlined />}
              onClick={() => setSale({ id: row.orderPaymentId, orderNumber: row.orderNumber })} />
          </Tooltip> }] : []),
      ]} />
    <SaleTipModal sale={sale} onClose={() => setSale(null)} onSaved={() => { setPage(1); setRefresh(value => value + 1); }} />
  </div>;
}
