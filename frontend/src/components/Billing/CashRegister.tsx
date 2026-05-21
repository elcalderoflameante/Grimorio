import { useEffect, useState, useRef } from 'react';
import {
  Card, Button, InputNumber, Form, Modal, Table, Tag, Space, Typography,
  Row, Col, Descriptions, Alert, message, Divider, Badge, Tooltip,
} from 'antd';
import {
  UnlockOutlined, LockOutlined, ReloadOutlined,
  ShoppingCartOutlined, CheckCircleOutlined, DollarOutlined,
  BankOutlined, ClockCircleOutlined, SyncOutlined, FireOutlined,
} from '@ant-design/icons';
import type { CashSessionDto, OpenCashSessionDto, CloseCashSessionDto, ActiveOrderSummaryDto } from '../../types';
import { cashApi, posApi } from '../../services/api';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import 'dayjs/locale/es';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

dayjs.extend(relativeTime);
dayjs.locale('es');

const { Title, Text } = Typography;

const fmt = (v: number) => `$${v.toFixed(2)}`;

// Colores y etiquetas de estado de orden
const ORDER_STATUS_COLOR: Record<string, string> = {
  Confirmed: 'processing', InPreparation: 'warning',
  Ready: 'cyan', Delivered: 'success', Cancelled: 'default', Draft: 'default',
};
const ORDER_STATUS_LABEL: Record<string, string> = {
  Confirmed: 'Confirmada', InPreparation: 'En preparación',
  Ready: 'Lista', Delivered: 'Entregada', Cancelled: 'Cancelada', Draft: 'Borrador',
};
const ORDER_TYPE_LABEL: Record<string, string> = {
  DineIn: 'Mesa', Takeout: 'Llevar', Delivery: 'Domicilio',
};

export default function CashRegister() {
  const { hasPermission } = useAuth();
  const [activeSession, setActiveSession] = useState<CashSessionDto | null | undefined>(undefined);
  const [history, setHistory] = useState<CashSessionDto[]>([]);
  const [activeOrders, setActiveOrders] = useState<ActiveOrderSummaryDto[]>([]);
  const [recentSales, setRecentSales] = useState<import('../../types').OrderPaymentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [lastRefresh, setLastRefresh] = useState<dayjs.Dayjs>(dayjs());
  const [openForm] = Form.useForm();
  const [closeForm] = Form.useForm();
  const [showOpenModal, setShowOpenModal] = useState(false);
  const [showCloseModal, setShowCloseModal] = useState(false);
  const [saving, setSaving] = useState(false);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const canOpenCash = hasPermission(PERMISSIONS.billing.cashOpen);
  const canCloseCash = hasPermission(PERMISSIONS.billing.cashClose);

  const load = async (silent = false) => {
    if (!silent) setLoading(true);
    try {
      const [sessionRes, historyRes, ordersRes, salesRes] = await Promise.allSettled([
        cashApi.getActiveSession(),
        cashApi.getSessions({ pageSize: 20 }),
        posApi.getActiveOrderSummaries(),
        cashApi.getSales({ from: dayjs().startOf('day').toISOString(), pageSize: 10 }),
      ]);

      setActiveSession(sessionRes.status === 'fulfilled' ? sessionRes.value.data : null);
      setHistory(historyRes.status === 'fulfilled' ? historyRes.value.data : []);
      setActiveOrders(ordersRes.status === 'fulfilled' ? ordersRes.value.data : []);
      setRecentSales(salesRes.status === 'fulfilled' ? salesRes.value.data : []);
      setLastRefresh(dayjs());
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    load();
    intervalRef.current = setInterval(() => load(true), 20_000);
    return () => { if (intervalRef.current) clearInterval(intervalRef.current); };
  }, []);

  const handleOpen = async () => {
    const values = await openForm.validateFields();
    setSaving(true);
    try {
      const dto: OpenCashSessionDto = { openingBalance: values.openingBalance };
      const r = await cashApi.openSession(dto);
      setActiveSession(r.data);
      setShowOpenModal(false);
      openForm.resetFields();
      message.success('Caja abierta');
      load(true);
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      message.error(err?.response?.data?.message ?? 'Error al abrir caja');
    } finally {
      setSaving(false);
    }
  };

  const handleClose = async () => {
    if (!activeSession) return;
    const values = await closeForm.validateFields();
    setSaving(true);
    try {
      const dto: CloseCashSessionDto = { actualCash: values.actualCash, notes: values.notes };
      const r = await cashApi.closeSession(activeSession.id, dto);
      setActiveSession(null);
      setHistory(prev => [r.data, ...prev]);
      setShowCloseModal(false);
      closeForm.resetFields();
      message.success('Caja cerrada');
    } catch {
      message.error('Error al cerrar caja');
    } finally {
      setSaving(false);
    }
  };

  if (loading || activeSession === undefined) return null;

  // Contadores de pedidos activos por estado
  const ordersByStatus = activeOrders.reduce<Record<string, number>>((acc, o) => {
    acc[o.status] = (acc[o.status] ?? 0) + 1;
    return acc;
  }, {});

  return (
    <div>
      {/* ── Encabezado ─────────────────────────────────────────────────────── */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Space align="center" size={12}>
          <Title level={4} style={{ margin: 0 }}>Caja</Title>
          <Tooltip title={`Última actualización: ${dayjs(lastRefresh).format('HH:mm:ss')}`}>
            <Text type="secondary" style={{ fontSize: 12 }}>
              <SyncOutlined spin={loading} style={{ marginRight: 4 }} />
              {dayjs(lastRefresh).format('HH:mm:ss')}
            </Text>
          </Tooltip>
        </Space>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={() => load(false)} loading={loading}>
            Actualizar
          </Button>
          {activeSession
            ? canCloseCash && <Button danger icon={<LockOutlined />} onClick={() => setShowCloseModal(true)}>
                Cerrar caja
              </Button>
            : canOpenCash && <Button type="primary" icon={<UnlockOutlined />} onClick={() => setShowOpenModal(true)}>
                Abrir caja
              </Button>
          }
        </Space>
      </div>

      {activeSession ? (
        <>
          {/* ── Barra de sesión ──────────────────────────────────────────── */}
          <div
            style={{
              background: '#f6ffed',
              border: '1px solid #b7eb8f',
              borderRadius: 8,
              padding: '8px 16px',
              marginBottom: 16,
              display: 'flex',
              flexWrap: 'wrap',
              gap: 8,
              alignItems: 'center',
            }}
          >
            <Tag color="green" icon={<CheckCircleOutlined />} style={{ margin: 0 }}>Sesión activa</Tag>
            <Divider orientation="vertical" />
            <Text type="secondary" style={{ fontSize: 13 }}>
              <strong>{activeSession.openedByName}</strong>
            </Text>
            <Divider orientation="vertical" />
            <Text type="secondary" style={{ fontSize: 13 }}>
              <ClockCircleOutlined style={{ marginRight: 4 }} />
              Desde {dayjs(activeSession.openedAt).format('HH:mm')}
              {' · '}
              {dayjs(activeSession.openedAt).fromNow()}
            </Text>
            <Divider orientation="vertical" />
            <Text type="secondary" style={{ fontSize: 13 }}>
              Fondo inicial: <strong>{fmt(activeSession.openingBalance)}</strong>
            </Text>
          </div>

          {/* ── KPIs principales ─────────────────────────────────────────── */}
          <Row gutter={[12, 12]} style={{ marginBottom: 12 }}>

            {/* Total ventas */}
            <Col xs={24} sm={12} lg={7}>
              <Card
                style={{
                  background: 'linear-gradient(135deg, #52c41a 0%, #237804 100%)',
                  border: 'none', borderRadius: 12, height: '100%',
                }}
                styles={{ body: { padding: 20 } }}
              >
                <DollarOutlined style={{ fontSize: 26, color: 'rgba(255,255,255,0.65)', marginBottom: 6 }} />
                <div style={{ color: 'rgba(255,255,255,0.8)', fontSize: 12, marginBottom: 2 }}>
                  Total recaudado
                </div>
                <div style={{ color: '#fff', fontSize: 34, fontWeight: 800, lineHeight: 1.1 }}>
                  {fmt(activeSession.totalSales)}
                </div>
                <div style={{ color: 'rgba(255,255,255,0.65)', fontSize: 12, marginTop: 6 }}>
                  {activeSession.totalOrders} cobro{activeSession.totalOrders !== 1 ? 's' : ''} en esta sesión
                </div>
              </Card>
            </Col>

            {/* Efectivo esperado */}
            <Col xs={12} sm={6} lg={4}>
              <Card style={{ borderRadius: 12, borderColor: '#95de64', height: '100%' }} styles={{ body: { padding: 16 } }}>
                <BankOutlined style={{ fontSize: 20, color: '#52c41a', marginBottom: 6 }} />
                <div style={{ color: '#8c8c8c', fontSize: 11, marginBottom: 3 }}>Efectivo en caja</div>
                <div style={{ fontSize: 22, fontWeight: 700, color: '#389e0d' }}>
                  {fmt(activeSession.expectedCash)}
                </div>
                <div style={{ color: '#8c8c8c', fontSize: 10, marginTop: 4 }}>
                  fondo + cobros efectivo
                </div>
              </Card>
            </Col>

            {/* Cobrados */}
            <Col xs={12} sm={6} lg={3}>
              <Card style={{ borderRadius: 12, borderColor: '#91caff', height: '100%' }} styles={{ body: { padding: 16 } }}>
                <CheckCircleOutlined style={{ fontSize: 20, color: '#1677ff', marginBottom: 6 }} />
                <div style={{ color: '#8c8c8c', fontSize: 11, marginBottom: 3 }}>Cobrados</div>
                <div style={{ fontSize: 22, fontWeight: 700, color: '#1677ff' }}>
                  {activeSession.totalOrders}
                </div>
                <div style={{ color: '#8c8c8c', fontSize: 10, marginTop: 4 }}>esta sesión</div>
              </Card>
            </Col>

            {/* Pedidos activos */}
            <Col xs={24} sm={12} lg={10}>
              <Card style={{ borderRadius: 12, borderColor: '#ffd591', height: '100%' }} styles={{ body: { padding: 16 } }}>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8 }}>
                  <Space size={6}>
                    <ShoppingCartOutlined style={{ fontSize: 18, color: '#fa8c16' }} />
                    <Text style={{ fontSize: 13, fontWeight: 600, color: '#d46b08' }}>
                      {activeOrders.length} pedido{activeOrders.length !== 1 ? 's' : ''} en curso
                    </Text>
                  </Space>
                  {activeOrders.length > 0 && (
                    <Badge count={activeOrders.length} color="#fa8c16" />
                  )}
                </div>
                {activeOrders.length === 0 ? (
                  <Text type="secondary" style={{ fontSize: 12 }}>Sin pedidos activos</Text>
                ) : (
                  <Space size={[6, 6]} wrap>
                    {Object.entries(ordersByStatus).map(([status, count]) => (
                      <Tag key={status} color={ORDER_STATUS_COLOR[status] ?? 'default'} style={{ margin: 0 }}>
                        {ORDER_STATUS_LABEL[status] ?? status}: {count}
                      </Tag>
                    ))}
                  </Space>
                )}
                {/* Lista breve de pedidos activos */}
                {activeOrders.length > 0 && (
                  <div style={{ marginTop: 8, maxHeight: 80, overflowY: 'auto' }}>
                    {activeOrders.slice(0, 6).map(o => (
                      <div
                        key={o.id}
                        style={{
                          display: 'flex', justifyContent: 'space-between',
                          fontSize: 11, color: '#595959', borderBottom: '1px solid #f0f0f0',
                          padding: '2px 0',
                        }}
                      >
                        <span>
                          #{o.number} · {ORDER_TYPE_LABEL[o.type] ?? o.type}
                          {o.tableCode ? ` · Mesa ${o.tableCode}` : ''}
                          {o.customerName ? ` · ${o.customerName}` : ''}
                        </span>
                        <Tag color={ORDER_STATUS_COLOR[o.status]} style={{ margin: 0, fontSize: 10 }}>
                          {ORDER_STATUS_LABEL[o.status] ?? o.status}
                        </Tag>
                      </div>
                    ))}
                    {activeOrders.length > 6 && (
                      <Text type="secondary" style={{ fontSize: 10 }}>
                        +{activeOrders.length - 6} más...
                      </Text>
                    )}
                  </div>
                )}
              </Card>
            </Col>
          </Row>

          {/* ── Medios de pago ───────────────────────────────────────────── */}
          {activeSession.totals.length > 0 ? (
            <>
              <Title level={5} style={{ marginBottom: 10, marginTop: 4 }}>Recaudación por medio de pago</Title>
              <Row gutter={[10, 10]} style={{ marginBottom: 16 }}>
                {activeSession.totals.map(t => (
                  <Col xs={12} sm={8} md={6} lg={4} key={t.methodId}>
                    <Card
                      size="small"
                      style={{ borderRadius: 10, borderColor: t.methodColor, borderWidth: 2, textAlign: 'center' }}
                      styles={{ body: { padding: '12px 8px' } }}
                    >
                      <div style={{
                        width: 8, height: 8, borderRadius: '50%',
                        background: t.methodColor, margin: '0 auto 6px',
                      }} />
                      <div style={{ fontSize: 11, color: '#8c8c8c', marginBottom: 4 }}>{t.methodName}</div>
                      <div style={{ fontSize: 20, fontWeight: 700, color: t.methodColor }}>
                        {fmt(t.total)}
                      </div>
                    </Card>
                  </Col>
                ))}
              </Row>
            </>
          ) : (
            <Alert
              type="info" showIcon icon={<DollarOutlined />}
              title="Sin ventas registradas aún en esta sesión"
              style={{ marginBottom: 16 }}
            />
          )}

          {/* ── Últimas ventas del día ──────────────────────────────────── */}
          {recentSales.length > 0 && (
            <>
              <Title level={5} style={{ marginBottom: 10 }}>
                <FireOutlined style={{ color: '#fa8c16', marginRight: 6 }} />
                Últimas ventas del día
              </Title>
              <Table
                size="small"
                dataSource={recentSales}
                rowKey="id"
                pagination={false}
                style={{ marginBottom: 24 }}
                columns={[
                  {
                    title: 'Hora',
                    dataIndex: 'paidAt',
                    width: 60,
                    render: (v: string) => (
                      <Text style={{ fontSize: 12 }}>{dayjs(v).format('HH:mm')}</Text>
                    ),
                  },
                  {
                    title: 'Orden',
                    dataIndex: 'orderNumber',
                    width: 70,
                    render: (v: number) => <Text strong>#{v}</Text>,
                  },
                  {
                    title: 'Mesa / Cliente',
                    width: 130,
                    render: (_: unknown, r: import('../../types').OrderPaymentDto) => (
                      <Text style={{ fontSize: 12 }}>
                        {r.tableCode ? `Mesa ${r.tableCode}` : r.customerName ?? '—'}
                      </Text>
                    ),
                  },
                  {
                    title: 'Medios',
                    render: (_: unknown, r: import('../../types').OrderPaymentDto) => (
                      <Space size={3} wrap>
                        {r.lines.map(l => (
                          <Tag
                            key={l.id}
                            style={{
                              margin: 0, fontSize: 11,
                              background: l.methodColor + '22',
                              color: l.methodColor,
                              borderColor: l.methodColor,
                            }}
                          >
                            {l.methodName}
                          </Tag>
                        ))}
                      </Space>
                    ),
                  },
                  {
                    title: 'Total',
                    dataIndex: 'orderAmount',
                    align: 'right',
                    width: 80,
                    render: (v: number) => <Text strong>{fmt(v)}</Text>,
                  },
                ]}
              />
            </>
          )}
        </>
      ) : (
        <Alert
          type="warning"
          title="No hay sesión de caja abierta"
          description="Abre la caja antes de registrar cobros."
          style={{ marginBottom: 24 }}
          showIcon
        />
      )}

      {/* ── Historial de sesiones ───────────────────────────────────────── */}
      <Title level={5}>Historial de sesiones</Title>
      <Table
        size="small"
        dataSource={history}
        rowKey="id"
        pagination={{ defaultPageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
        expandable={{ expandedRowRender: (r) => <SessionDetail session={r} /> }}
        columns={[
          {
            title: 'Fecha', dataIndex: 'openedAt', width: 150,
            render: v => dayjs(v).format('DD/MM/YYYY HH:mm'),
          },
          { title: 'Cajero', dataIndex: 'openedByName', width: 150 },
          {
            title: 'Estado', dataIndex: 'status', width: 100,
            render: v => (
              <Tag color={v === 'Open' ? 'green' : 'default'}>
                {v === 'Open' ? 'Abierta' : 'Cerrada'}
              </Tag>
            ),
          },
          {
            title: 'Total ventas', dataIndex: 'totalSales',
            render: v => fmt(v), align: 'right', width: 120,
          },
          { title: 'Órdenes', dataIndex: 'totalOrders', align: 'center', width: 80 },
          {
            title: 'Diferencia', dataIndex: 'cashDifference', align: 'right', width: 110,
            render: v => v == null ? '—' : (
              <Text type={v >= 0 ? 'success' : 'danger'}>
                {v > 0 ? '+' : ''}{fmt(v)}
              </Text>
            ),
          },
        ]}
      />

      {/* ── Modal abrir caja ────────────────────────────────────────────── */}
      <Modal
        title="Abrir caja"
        open={showOpenModal}
        onCancel={() => setShowOpenModal(false)}
        onOk={handleOpen}
        confirmLoading={saving}
        okText="Abrir"
        cancelText="Cancelar"
      >
        <Form form={openForm} layout="vertical" initialValues={{ openingBalance: 0 }}>
          <Form.Item
            name="openingBalance"
            label="Fondo inicial en caja ($)"
            rules={[{ required: true }]}
          >
            <InputNumber style={{ width: '100%' }} min={0} precision={2} prefix="$" />
          </Form.Item>
        </Form>
      </Modal>

      {/* ── Modal cerrar caja ───────────────────────────────────────────── */}
      <Modal
        title="Cerrar caja"
        open={showCloseModal}
        onCancel={() => setShowCloseModal(false)}
        onOk={handleClose}
        confirmLoading={saving}
        okText="Cerrar caja"
        cancelText="Cancelar"
      >
        {activeSession && (
          <div
            style={{
              marginBottom: 16, padding: '10px 14px',
              background: '#f6ffed', borderRadius: 6,
              border: '1px solid #b7eb8f',
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
              <Text type="secondary">Efectivo esperado en caja:</Text>
              <Text strong style={{ color: '#389e0d', fontSize: 16 }}>
                {fmt(activeSession.expectedCash)}
              </Text>
            </div>
            <Divider style={{ margin: '8px 0' }} />
            <Row gutter={12}>
              {activeSession.totals.map(t => (
                <Col key={t.methodId} xs={12}>
                  <div style={{ fontSize: 12, color: '#8c8c8c' }}>{t.methodName}</div>
                  <div style={{ fontWeight: 600, color: t.methodColor }}>{fmt(t.total)}</div>
                </Col>
              ))}
            </Row>
          </div>
        )}
        <Form form={closeForm} layout="vertical">
          <Form.Item
            name="actualCash"
            label="Efectivo contado al cierre ($)"
            rules={[{ required: true }]}
          >
            <InputNumber style={{ width: '100%' }} min={0} precision={2} prefix="$" />
          </Form.Item>
          <Form.Item name="notes" label="Notas de cierre">
            <input
              style={{ width: '100%', padding: '4px 8px', border: '1px solid #d9d9d9', borderRadius: 6 }}
              placeholder="Observaciones..."
              onChange={e => closeForm.setFieldValue('notes', e.target.value)}
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

function SessionDetail({ session }: { session: CashSessionDto }) {
  const fmt = (v: number) => `$${v.toFixed(2)}`;
  return (
    <Descriptions size="small" column={{ xs: 1, sm: 2, md: 3 }} style={{ padding: '8px 16px' }}>
      <Descriptions.Item label="Fondo inicial">{fmt(session.openingBalance)}</Descriptions.Item>
      {session.totals.map(t => (
        <Descriptions.Item key={t.methodId} label={t.methodName}>
          <Tag color={t.methodColor} style={{ borderColor: t.methodColor }}>{fmt(t.total)}</Tag>
        </Descriptions.Item>
      ))}
      <Descriptions.Item label="Esperado en caja">{fmt(session.expectedCash)}</Descriptions.Item>
      {session.actualCash != null && (
        <Descriptions.Item label="Contado al cierre">{fmt(session.actualCash)}</Descriptions.Item>
      )}
      {session.cashDifference != null && (
        <Descriptions.Item label="Diferencia">
          <Typography.Text type={session.cashDifference >= 0 ? 'success' : 'danger'}>
            {session.cashDifference > 0 ? '+' : ''}{fmt(session.cashDifference)}
          </Typography.Text>
        </Descriptions.Item>
      )}
      {session.closedAt && (
        <Descriptions.Item label="Cierre">
          {dayjs(session.closedAt).format('DD/MM/YYYY HH:mm')}
        </Descriptions.Item>
      )}
      {session.closeNotes && (
        <Descriptions.Item label="Notas">{session.closeNotes}</Descriptions.Item>
      )}
    </Descriptions>
  );
}
