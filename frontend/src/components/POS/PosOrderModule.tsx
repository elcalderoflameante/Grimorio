import { useState } from 'react';
import { Button, Modal, Space, Typography, message, List, Tag, Popconfirm } from 'antd';
import { ShoppingCartOutlined, CarOutlined, DollarOutlined, ReloadOutlined } from '@ant-design/icons';
import { useAuth } from '../../context/useAuth';
import type { OrderDto, RestaurantTableDto, OrderType } from '../../types';
import { posApi } from '../../services/api';
import TablesMap from './TablesMap';
import TakeOrder from './TakeOrder';
import PayOrderModal from '../Billing/PayOrderModal';

const { Title, Text } = Typography;

type View = 'map' | 'order' | 'orders-list';

const STATUS_COLORS: Record<string, string> = {
  Draft: 'default', Confirmed: 'processing', InPreparation: 'orange',
  Ready: 'cyan', Delivered: 'green', Cancelled: 'red',
};
const STATUS_LABELS: Record<string, string> = {
  Draft: 'Borrador', Confirmed: 'Confirmada', InPreparation: 'En preparación',
  Ready: 'Lista', Delivered: 'Entregada', Cancelled: 'Cancelada',
};

export default function PosOrderModule() {
  const { branchId } = useAuth();
  const [view, setView] = useState<View>('map');
  const [selectedTable, setSelectedTable] = useState<RestaurantTableDto | null>(null);
  const [orderType, setOrderType] = useState<OrderType>('DineIn');
  const [showTypeModal, setShowTypeModal] = useState(false);
  const [activeOrder, setActiveOrder] = useState<OrderDto | null>(null);
  const [ordersToPay, setOrdersToPay] = useState<OrderDto[]>([]);
  const [payingOrder, setPayingOrder] = useState<OrderDto | null>(null);
  const [loadingOrders, setLoadingOrders] = useState(false);

  if (!branchId) return <Text type="danger">Sin sucursal asignada</Text>;

  const handleSelectTable = (table: RestaurantTableDto) => {
    setSelectedTable(table);
    setOrderType('DineIn');
    setActiveOrder(null);
    setView('order');
  };

  const handleNewOrder = (type: OrderType) => {
    setSelectedTable(null);
    setOrderType(type);
    setActiveOrder(null);
    setShowTypeModal(false);
    setView('order');
  };

  const handleOrderConfirmed = (order: OrderDto) => {
    message.success(`Orden #${order.number} confirmada`);
    setView('map');
    setSelectedTable(null);
    setActiveOrder(null);
  };

  const handleClose = () => {
    setView('map');
    setSelectedTable(null);
    setActiveOrder(null);
  };

  const openPayList = async () => {
    setLoadingOrders(true);
    try {
      const r = await posApi.getOrders({ activeOnly: true });
      const unpaid = r.data.filter(o =>
        (o.status === 'Delivered' || o.status === 'Ready') && !o.paidAt
      );
      setOrdersToPay(unpaid);
      setView('orders-list');
    } catch {
      message.error('Error al cargar órdenes');
    } finally {
      setLoadingOrders(false);
    }
  };

  const handlePaid = () => {
    setPayingOrder(null);
    // refresh list
    openPayList();
  };

  return (
    <div style={{ height: '100%' }}>
      {view === 'map' && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
            <Title level={5} style={{ margin: 0 }}>Mapa de Mesas</Title>
            <Space>
              <Button icon={<DollarOutlined />} onClick={openPayList} loading={loadingOrders}>
                Cobrar
              </Button>
              <Button icon={<ShoppingCartOutlined />} onClick={() => setShowTypeModal(true)}>
                Nuevo pedido
              </Button>
            </Space>
          </div>
          <TablesMap branchId={branchId} onSelectTable={handleSelectTable} />
        </div>
      )}

      {view === 'order' && (
        <TakeOrder
          table={selectedTable ?? undefined}
          orderType={orderType}
          existingOrder={activeOrder ?? undefined}
          onClose={handleClose}
          onConfirm={handleOrderConfirmed}
        />
      )}

      {view === 'orders-list' && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
            <Title level={5} style={{ margin: 0 }}>Órdenes pendientes de cobro</Title>
            <Space>
              <Button icon={<ReloadOutlined />} onClick={openPayList} loading={loadingOrders}>Actualizar</Button>
              <Button onClick={() => setView('map')}>Volver al mapa</Button>
            </Space>
          </div>

          {ordersToPay.length === 0 ? (
            <Text type="secondary">No hay órdenes pendientes de cobro.</Text>
          ) : (
            <List
              dataSource={ordersToPay}
              renderItem={order => (
                <List.Item
                  actions={[
                    <Button
                      key="pay"
                      type="primary"
                      icon={<DollarOutlined />}
                      onClick={() => setPayingOrder(order)}
                    >
                      Cobrar
                    </Button>,
                  ]}
                >
                  <List.Item.Meta
                    title={
                      <Space>
                        <Text strong>Orden #{order.number}</Text>
                        <Tag color={STATUS_COLORS[order.status]}>{STATUS_LABELS[order.status] ?? order.status}</Tag>
                        {order.tableCode && <Tag>{order.tableCode}</Tag>}
                        {order.customerName && <Text type="secondary">{order.customerName}</Text>}
                      </Space>
                    }
                    description={`${order.totalItems} ítem(s) — Total: $${order.total.toFixed(2)}`}
                  />
                </List.Item>
              )}
            />
          )}
        </div>
      )}

      {/* Modal tipo de pedido */}
      <Modal
        title="Tipo de pedido"
        open={showTypeModal}
        onCancel={() => setShowTypeModal(false)}
        footer={null}
        width={320}
      >
        <Space direction="vertical" style={{ width: '100%' }} size={12}>
          <Button block size="large" icon={<ShoppingCartOutlined />} onClick={() => handleNewOrder('Takeout')}>
            Para llevar
          </Button>
          <Button block size="large" icon={<CarOutlined />} onClick={() => handleNewOrder('Delivery')}>
            Domicilio
          </Button>
        </Space>
      </Modal>

      {/* Modal de cobro */}
      {payingOrder && (
        <PayOrderModal
          order={payingOrder}
          open={!!payingOrder}
          onClose={() => setPayingOrder(null)}
          onPaid={handlePaid}
          branchId={branchId}
        />
      )}
    </div>
  );
}
