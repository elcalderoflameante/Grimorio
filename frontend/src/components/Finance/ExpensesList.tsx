import { useCallback, useEffect, useMemo, useState } from 'react';
import { App as AntApp, Button, Col, DatePicker, Form, Input, InputNumber, Modal, Popconfirm, Row, Select, Space, Statistic, Table, Tag, Typography } from 'antd';
import { CloseCircleOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { cashApi, financeApi, paymentMethodsApi } from '../../services/api';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import type {
  CashSessionDto,
  CostCenterDto,
  CreateExpenseDto,
  ExpenseCategoryDto,
  ExpenseDto,
  ExpenseReportDto,
  ExpenseReportGroupDto,
  PaymentMethodConfigDto,
} from '../../types';
import { branchDateRangeToUtcIso, branchStartOfDayUtcIso, formatBranchDate } from '../../utils/branchTimeZone';

const { RangePicker } = DatePicker;
const { Text } = Typography;

const fmt = (value: number) => `$${value.toFixed(2)}`;

const statusLabels: Record<string, string> = {
  Registered: 'Registrado',
  Cancelled: 'Anulado',
};

const categoryTypeLabels: Record<string, string> = {
  Fixed: 'Fijo',
  Variable: 'Variable',
  Mixed: 'Mixto',
};

type ExpenseFormValues = Omit<CreateExpenseDto, 'expenseDate'> & {
  expenseDate: Dayjs;
};

export default function ExpensesList() {
  const { message } = AntApp.useApp();
  const { hasPermission } = useAuth();
  const [expenses, setExpenses] = useState<ExpenseDto[]>([]);
  const [costCenters, setCostCenters] = useState<CostCenterDto[]>([]);
  const [categories, setCategories] = useState<ExpenseCategoryDto[]>([]);
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethodConfigDto[]>([]);
  const [activeSession, setActiveSession] = useState<CashSessionDto | null>(null);
  const [report, setReport] = useState<ExpenseReportDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [dateRange, setDateRange] = useState<[Dayjs | null, Dayjs | null]>([
    dayjs().startOf('month'),
    dayjs().endOf('day'),
  ]);
  const [statusFilter, setStatusFilter] = useState<string>('Registered');
  const [costCenterFilter, setCostCenterFilter] = useState<string>();
  const [categoryFilter, setCategoryFilter] = useState<string>();
  const [form] = Form.useForm<ExpenseFormValues>();
  const canCreate = hasPermission(PERMISSIONS.finance.expensesCreate);
  const canCancel = hasPermission(PERMISSIONS.finance.expensesCancel);

  const loadCatalogs = useCallback(async () => {
    const [ccRes, catRes, pmRes, sessionRes] = await Promise.allSettled([
      financeApi.getCostCenters(true),
      financeApi.getExpenseCategories({ activeOnly: true }),
      paymentMethodsApi.getAll(true),
      cashApi.getActiveSession(),
    ]);

    setCostCenters(ccRes.status === 'fulfilled' ? ccRes.value.data : []);
    setCategories(catRes.status === 'fulfilled' ? catRes.value.data : []);
    setPaymentMethods(pmRes.status === 'fulfilled' ? pmRes.value.data : []);
    setActiveSession(sessionRes.status === 'fulfilled' ? sessionRes.value.data : null);
  }, []);

  const loadExpenses = useCallback(async () => {
    setLoading(true);
    try {
      const [from, to] = dateRange;
      const range = branchDateRangeToUtcIso([from, to]);
      const params = {
        costCenterId: costCenterFilter,
        expenseCategoryId: categoryFilter,
        from: range.from,
        to: range.to,
      };
      const [expensesRes, reportRes] = await Promise.all([
        financeApi.getExpenses({
          ...params,
          status: statusFilter,
          pageSize: 200,
        }),
        financeApi.getExpenseReport(params),
      ]);
      setExpenses(expensesRes.data);
      setReport(reportRes.data);
    } finally {
      setLoading(false);
    }
  }, [categoryFilter, costCenterFilter, dateRange, statusFilter]);

  useEffect(() => { loadCatalogs(); }, [loadCatalogs]);
  useEffect(() => { loadExpenses(); }, [loadExpenses]);

  const paymentMethodById = useMemo(
    () => new Map(paymentMethods.map(m => [m.id, m])),
    [paymentMethods],
  );

  const openCreate = () => {
    form.setFieldsValue({
      expenseDate: dayjs(),
      amount: 0,
    });
    setModalOpen(true);
  };

  const handleSave = async () => {
    const values = await form.validateFields();
    const method = values.paymentMethodConfigId ? paymentMethodById.get(values.paymentMethodConfigId) : undefined;
    const dto: CreateExpenseDto = {
      ...values,
      expenseDate: branchStartOfDayUtcIso(values.expenseDate) ?? dayjs(values.expenseDate).toISOString(),
      cashSessionId: method?.isCash && activeSession ? activeSession.id : undefined,
    };

    try {
      await financeApi.createExpense(dto);
      message.success('Gasto registrado');
      setModalOpen(false);
      form.resetFields();
      loadCatalogs();
      loadExpenses();
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } | string } };
      const data = err.response?.data;
      message.error(typeof data === 'string' ? data : data?.message ?? 'Error al registrar gasto');
    }
  };

  const handleCancel = async (expense: ExpenseDto) => {
    try {
      await financeApi.cancelExpense(expense.id, 'Anulado desde pantalla de gastos');
      message.success('Gasto anulado');
      loadCatalogs();
      loadExpenses();
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } | string } };
      const data = err.response?.data;
      message.error(typeof data === 'string' ? data : data?.message ?? 'Error al anular gasto');
    }
  };

  const total = expenses
    .filter(e => e.status === 'Registered')
    .reduce((sum, e) => sum + e.amount, 0);

  const groupColumns = [
    { title: 'Nombre', dataIndex: 'name', key: 'name' },
    {
      title: 'Tipo',
      dataIndex: 'type',
      key: 'type',
      width: 100,
      render: (value?: string) => value ? <Tag>{categoryTypeLabels[value] ?? value}</Tag> : '-',
    },
    { title: 'Cant.', dataIndex: 'count', key: 'count', align: 'right' as const, width: 70 },
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

  const typeRows: ExpenseReportGroupDto[] = report?.byType.map(row => ({
    ...row,
    name: categoryTypeLabels[row.name] ?? row.name,
  })) ?? [];

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, marginBottom: 16, flexWrap: 'wrap' }}>
        <Space wrap>
          <h2 style={{ margin: 0 }}>Gastos</h2>
          <Tag color="blue">Total: {fmt(total)}</Tag>
          {activeSession && (
            <Tag color="green">
              Caja activa: {activeSession.cashRegisterName} ({activeSession.cashRegisterCode})
            </Tag>
          )}
        </Space>
        <Space wrap>
          <RangePicker
            value={dateRange}
            onChange={(value) => setDateRange(value ? [value[0], value[1]] : [null, null])}
            format="DD/MM/YYYY"
            allowClear={false}
          />
          <Select
            value={statusFilter}
            onChange={setStatusFilter}
            style={{ width: 130 }}
            options={[
              { value: 'Registered', label: 'Registrados' },
              { value: 'Cancelled', label: 'Anulados' },
            ]}
          />
          <Select
            allowClear
            placeholder="Centro"
            value={costCenterFilter}
            onChange={setCostCenterFilter}
            style={{ width: 170 }}
            options={costCenters.map(c => ({ value: c.id, label: c.name }))}
          />
          <Select
            allowClear
            placeholder="Categoria"
            value={categoryFilter}
            onChange={setCategoryFilter}
            style={{ width: 180 }}
            options={categories.map(c => ({ value: c.id, label: c.name }))}
          />
          <Button icon={<ReloadOutlined />} onClick={loadExpenses} loading={loading}>Actualizar</Button>
          {canCreate && <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Nuevo gasto</Button>}
        </Space>
      </div>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={12} md={6}>
          <Statistic title="Gastos registrados" value={report?.totalExpenses ?? 0} prefix="$" precision={2} />
        </Col>
        <Col xs={12} md={6}>
          <Statistic title="Movimientos" value={report?.totalCount ?? 0} />
        </Col>
        <Col xs={12} md={4}>
          <Statistic title="Fijos" value={report?.fixedTotal ?? 0} prefix="$" precision={2} />
        </Col>
        <Col xs={12} md={4}>
          <Statistic title="Variables" value={report?.variableTotal ?? 0} prefix="$" precision={2} />
        </Col>
        <Col xs={12} md={4}>
          <Statistic title="Mixtos" value={report?.mixedTotal ?? 0} prefix="$" precision={2} />
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginBottom: 20 }}>
        <Col xs={24} lg={8}>
          <Table
            title={() => 'Por centro de costo'}
            size="small"
            rowKey={(row) => row.id ?? row.name}
            dataSource={report?.byCostCenter ?? []}
            columns={groupColumns.filter(c => c.key !== 'type')}
            pagination={false}
          />
        </Col>
        <Col xs={24} lg={8}>
          <Table
            title={() => 'Por categoria'}
            size="small"
            rowKey={(row) => row.id ?? row.name}
            dataSource={report?.byCategory ?? []}
            columns={groupColumns}
            pagination={false}
          />
        </Col>
        <Col xs={24} lg={8}>
          <Table
            title={() => 'Por tipo'}
            size="small"
            rowKey={(row) => row.type ?? row.name}
            dataSource={typeRows}
            columns={groupColumns.filter(c => c.key !== 'type')}
            pagination={false}
          />
        </Col>
      </Row>

      <Table
        size="small"
        rowKey="id"
        dataSource={expenses}
        loading={loading}
        pagination={{ defaultPageSize: 25, showSizeChanger: true, pageSizeOptions: ['10', '25', '50', '100'] }}
        columns={[
          {
            title: 'Fecha',
            dataIndex: 'expenseDate',
            width: 120,
            render: (value: string) => formatBranchDate(value),
          },
          { title: 'Centro de costo', dataIndex: 'costCenterName', width: 160 },
          {
            title: 'Categoria',
            width: 190,
            render: (_: unknown, row: ExpenseDto) => (
              <Space size={4}>
                <Text>{row.expenseCategoryName}</Text>
                <Tag>{categoryTypeLabels[row.expenseCategoryType] ?? row.expenseCategoryType}</Tag>
              </Space>
            ),
          },
          {
            title: 'Pago',
            width: 150,
            render: (_: unknown, row: ExpenseDto) => row.paymentMethodName
              ? <Tag color={row.paymentMethodColor}>{row.paymentMethodName}</Tag>
              : <Text type="secondary">Sin medio</Text>,
          },
          {
            title: 'Caja',
            width: 150,
            render: (_: unknown, row: ExpenseDto) => row.cashRegisterCode
              ? `${row.cashRegisterName} (${row.cashRegisterCode})`
              : <Text type="secondary">-</Text>,
          },
          { title: 'Proveedor', dataIndex: 'supplierName', render: (value?: string) => value || '-' },
          { title: 'Documento', dataIndex: 'documentNumber', width: 120, render: (value?: string) => value || '-' },
          {
            title: 'Estado',
            dataIndex: 'status',
            width: 105,
            render: (value: string) => (
              <Tag color={value === 'Registered' ? 'green' : 'red'}>{statusLabels[value] ?? value}</Tag>
            ),
          },
          {
            title: 'Valor',
            dataIndex: 'amount',
            align: 'right',
            width: 110,
            render: (value: number, row: ExpenseDto) => (
              <Text strong delete={row.status === 'Cancelled'}>{fmt(value)}</Text>
            ),
          },
          {
            title: '',
            width: 60,
            render: (_: unknown, row: ExpenseDto) => (
              canCancel && row.status === 'Registered'
                ? (
                  <Popconfirm
                    title="Anular gasto?"
                    description="Si esta asociado a caja abierta, dejara de restar en el cierre."
                    okText="Anular"
                    cancelText="No"
                    onConfirm={() => handleCancel(row)}
                  >
                    <Button size="small" danger icon={<CloseCircleOutlined />} />
                  </Popconfirm>
                )
                : null
            ),
          },
        ]}
      />

      <Modal
        title="Nuevo gasto"
        open={modalOpen}
        onOk={handleSave}
        onCancel={() => setModalOpen(false)}
        okText="Registrar"
        cancelText="Cancelar"
        width={680}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Space align="start" style={{ width: '100%' }} size={12}>
            <Form.Item name="expenseDate" label="Fecha" rules={[{ required: true }]} style={{ flex: '0 0 160px' }}>
              <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
            </Form.Item>
            <Form.Item name="amount" label="Valor" rules={[{ required: true, message: 'Requerido' }]} style={{ flex: '0 0 140px' }}>
              <InputNumber min={0.01} precision={2} prefix="$" style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="paymentMethodConfigId" label="Medio de pago" style={{ flex: 1 }}>
              <Select
                allowClear
                options={paymentMethods.map(m => ({ value: m.id, label: m.name }))}
              />
            </Form.Item>
          </Space>
          <Space align="start" style={{ width: '100%' }} size={12}>
            <Form.Item name="costCenterId" label="Centro de costo" rules={[{ required: true, message: 'Requerido' }]} style={{ flex: 1 }}>
              <Select options={costCenters.map(c => ({ value: c.id, label: c.name }))} />
            </Form.Item>
            <Form.Item name="expenseCategoryId" label="Categoria" rules={[{ required: true, message: 'Requerido' }]} style={{ flex: 1 }}>
              <Select options={categories.map(c => ({ value: c.id, label: `${c.name} (${categoryTypeLabels[c.type]})` }))} />
            </Form.Item>
          </Space>
          <Space align="start" style={{ width: '100%' }} size={12}>
            <Form.Item name="supplierName" label="Proveedor / Beneficiario" style={{ flex: 1 }}>
              <Input maxLength={200} />
            </Form.Item>
            <Form.Item name="documentNumber" label="Documento" style={{ flex: '0 0 180px' }}>
              <Input maxLength={80} />
            </Form.Item>
          </Space>
          <Form.Item name="notes" label="Notas">
            <Input.TextArea rows={2} maxLength={500} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
