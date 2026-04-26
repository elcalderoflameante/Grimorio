import { useEffect, useState } from 'react';
import { Card, Button, InputNumber, Form, Modal, Table, Tag, Space, Typography, Statistic, Row, Col, Descriptions, Alert, message } from 'antd';
import { UnlockOutlined, LockOutlined, ReloadOutlined } from '@ant-design/icons';
import type { CashSessionDto, OpenCashSessionDto, CloseCashSessionDto } from '../../types';
import { cashApi } from '../../services/api';
import dayjs from 'dayjs';

const { Title, Text } = Typography;

const formatMoney = (v: number) => `$${v.toFixed(2)}`;

export default function CashRegister() {
  const [activeSession, setActiveSession] = useState<CashSessionDto | null | undefined>(undefined);
  const [history, setHistory] = useState<CashSessionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [openForm] = Form.useForm();
  const [closeForm] = Form.useForm();
  const [showOpenModal, setShowOpenModal] = useState(false);
  const [showCloseModal, setShowCloseModal] = useState(false);
  const [saving, setSaving] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const [sessionRes, historyRes] = await Promise.allSettled([
        cashApi.getActiveSession(),
        cashApi.getSessions({ pageSize: 20 }),
      ]);
      setActiveSession(sessionRes.status === 'fulfilled' ? sessionRes.value.data : null);
      setHistory(historyRes.status === 'fulfilled' ? historyRes.value.data : []);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

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

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Caja</Title>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={load}>Actualizar</Button>
          {activeSession
            ? <Button danger icon={<LockOutlined />} onClick={() => setShowCloseModal(true)}>Cerrar caja</Button>
            : <Button type="primary" icon={<UnlockOutlined />} onClick={() => setShowOpenModal(true)}>Abrir caja</Button>
          }
        </Space>
      </div>

      {/* Sesión activa */}
      {activeSession ? (
        <Card style={{ marginBottom: 24, borderColor: '#52c41a' }}>
          <Tag color="green" style={{ marginBottom: 12 }}>Sesión activa</Tag>
          <Row gutter={[24, 16]}>
            <Col xs={12} md={6}><Statistic title="Total ventas" value={activeSession.totalSales} prefix="$" precision={2} /></Col>
            <Col xs={12} md={6}><Statistic title="Órdenes cobradas" value={activeSession.totalOrders} /></Col>
            <Col xs={12} md={6}><Statistic title="Efectivo" value={activeSession.totalCash} prefix="$" precision={2} /></Col>
            <Col xs={12} md={6}><Statistic title="Tarjeta / Transf." value={activeSession.totalCard + activeSession.totalTransfer + activeSession.totalQr} prefix="$" precision={2} /></Col>
          </Row>
          <Descriptions size="small" style={{ marginTop: 16 }} column={{ xs: 1, sm: 2 }}>
            <Descriptions.Item label="Apertura">{dayjs(activeSession.openedAt).format('DD/MM/YYYY HH:mm')}</Descriptions.Item>
            <Descriptions.Item label="Abierta por">{activeSession.openedByName}</Descriptions.Item>
            <Descriptions.Item label="Fondo inicial">{formatMoney(activeSession.openingBalance)}</Descriptions.Item>
            <Descriptions.Item label="Efectivo esperado en caja">{formatMoney(activeSession.expectedCash)}</Descriptions.Item>
          </Descriptions>
        </Card>
      ) : (
        <Alert
          type="warning"
          message="No hay sesión de caja abierta"
          description="Abre la caja antes de registrar cobros."
          style={{ marginBottom: 24 }}
        />
      )}

      {/* Historial */}
      <Title level={5}>Historial de sesiones</Title>
      <Table
        size="small"
        dataSource={history}
        rowKey="id"
        pagination={{ pageSize: 10 }}
        expandable={{
          expandedRowRender: (r) => <SessionDetail session={r} />,
        }}
        columns={[
          { title: 'Fecha', dataIndex: 'openedAt', render: v => dayjs(v).format('DD/MM/YYYY HH:mm'), width: 160 },
          { title: 'Cajero', dataIndex: 'openedByName', width: 160 },
          { title: 'Estado', dataIndex: 'status', width: 100, render: v => <Tag color={v === 'Open' ? 'green' : 'default'}>{v === 'Open' ? 'Abierta' : 'Cerrada'}</Tag> },
          { title: 'Total ventas', dataIndex: 'totalSales', render: v => formatMoney(v), align: 'right', width: 130 },
          { title: 'Órdenes', dataIndex: 'totalOrders', align: 'center', width: 90 },
          { title: 'Diferencia', dataIndex: 'cashDifference', align: 'right', width: 120,
            render: v => v == null ? '—' : (
              <Text type={v === 0 ? 'success' : v > 0 ? 'success' : 'danger'}>
                {v > 0 ? '+' : ''}{formatMoney(v)}
              </Text>
            ),
          },
        ]}
      />

      {/* Modal abrir caja */}
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
          <Form.Item name="openingBalance" label="Fondo inicial en caja ($)" rules={[{ required: true }]}>
            <InputNumber style={{ width: '100%' }} min={0} precision={2} prefix="$" />
          </Form.Item>
        </Form>
      </Modal>

      {/* Modal cerrar caja */}
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
          <div style={{ marginBottom: 16 }}>
            <Text type="secondary">Efectivo esperado en caja: </Text>
            <Text strong>{formatMoney(activeSession.expectedCash)}</Text>
          </div>
        )}
        <Form form={closeForm} layout="vertical">
          <Form.Item name="actualCash" label="Efectivo contado ($)" rules={[{ required: true }]}>
            <InputNumber style={{ width: '100%' }} min={0} precision={2} prefix="$" />
          </Form.Item>
          <Form.Item name="notes" label="Notas de cierre">
            <input style={{ width: '100%', padding: '4px 8px', border: '1px solid #d9d9d9', borderRadius: 6 }} placeholder="Observaciones..." />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

function SessionDetail({ session }: { session: CashSessionDto }) {
  return (
    <Descriptions size="small" column={{ xs: 1, sm: 2, md: 3 }} style={{ padding: '8px 16px' }}>
      <Descriptions.Item label="Fondo inicial">{formatMoney(session.openingBalance)}</Descriptions.Item>
      <Descriptions.Item label="Efectivo (ventas)">{formatMoney(session.totalCash)}</Descriptions.Item>
      <Descriptions.Item label="Tarjeta">{formatMoney(session.totalCard)}</Descriptions.Item>
      <Descriptions.Item label="Transferencia">{formatMoney(session.totalTransfer)}</Descriptions.Item>
      <Descriptions.Item label="QR">{formatMoney(session.totalQr)}</Descriptions.Item>
      <Descriptions.Item label="Esperado en caja">{formatMoney(session.expectedCash)}</Descriptions.Item>
      {session.actualCash != null && (
        <Descriptions.Item label="Contado al cierre">{formatMoney(session.actualCash)}</Descriptions.Item>
      )}
      {session.cashDifference != null && (
        <Descriptions.Item label="Diferencia">
          <Text type={session.cashDifference === 0 ? 'success' : 'danger'}>
            {session.cashDifference > 0 ? '+' : ''}{formatMoney(session.cashDifference)}
          </Text>
        </Descriptions.Item>
      )}
      {session.closedAt && (
        <Descriptions.Item label="Cierre">{dayjs(session.closedAt).format('DD/MM/YYYY HH:mm')}</Descriptions.Item>
      )}
      {session.closeNotes && <Descriptions.Item label="Notas">{session.closeNotes}</Descriptions.Item>}
    </Descriptions>
  );
}
