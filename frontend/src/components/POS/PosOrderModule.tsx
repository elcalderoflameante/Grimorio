import { useState, useEffect } from 'react';
import { Button, Modal, Space, Typography, message, Alert } from 'antd';
import { ShoppingCartOutlined, CarOutlined } from '@ant-design/icons';
import { useAuth } from '../../context/useAuth';
import type { OrderDto, RestaurantTableDto, OrderType } from '../../types';
import { cashApi, posApi } from '../../services/api';
import TablesMap from './TablesMap';
import TakeOrder from './TakeOrder';
import TableOrderView from './TableOrderView';
import { PERMISSIONS } from '../../constants/permissions';

const { Title, Text } = Typography;

type View = 'map' | 'order' | 'table-detail';

export default function PosOrderModule() {
  const { branchId, hasPermission } = useAuth();
  const [view, setView] = useState<View>('map');
  const [selectedTable, setSelectedTable] = useState<RestaurantTableDto | null>(null);
  const [orderType, setOrderType] = useState<OrderType>('DineIn');
  const [showTypeModal, setShowTypeModal] = useState(false);
  const [activeOrder, setActiveOrder] = useState<OrderDto | null>(null);
  const [hasSession, setHasSession] = useState<boolean | null>(null);
  const [detailOrderId, setDetailOrderId] = useState<string | null>(null);
  const [tablesRefreshKey, setTablesRefreshKey] = useState(0);
  const [directSaleMode, setDirectSaleMode] = useState(false);
  const canCreateOrders = hasPermission(PERMISSIONS.pos.ordersCreate);
  const canDirectSale = hasPermission(PERMISSIONS.pos.directSaleCreate) && hasPermission(PERMISSIONS.billing.cashCharge);

  useEffect(() => {
    cashApi.getActiveSession()
      .then(() => setHasSession(true))
      .catch(() => setHasSession(false));
  }, []);

  if (!branchId) return <Text type="danger">Sin sucursal asignada</Text>;

  const handleSelectTable = async (table: RestaurantTableDto) => {
    if (!hasSession) {
      message.warning('Debes abrir la caja antes de tomar pedidos');
      return;
    }
    setSelectedTable(table);
    setDirectSaleMode(false);
    if (table.currentStatus === 'Draft' && table.currentOrderId) {
      try {
        const order = (await posApi.getOrden(table.currentOrderId)).data;
        setOrderType('DineIn');
        setActiveOrder(order);
        setView('order');
      } catch (e) {
        message.error('No se pudo cargar el borrador de la mesa');
      }
      return;
    }
    if (table.currentStatus === 'Occupied' && table.currentOrderId) {
      setDetailOrderId(table.currentOrderId);
      setView('table-detail');
    } else {
      if (!canCreateOrders) {
        message.warning('No tienes permiso para crear pedidos');
        return;
      }
      setOrderType('DineIn');
      setActiveOrder(null);
      setView('order');
    }
  };

  const handleNewOrder = (type: OrderType) => {
    if (!canCreateOrders) {
      message.warning('No tienes permiso para crear pedidos');
      setShowTypeModal(false);
      return;
    }
    if (!hasSession) {
      message.warning('Debes abrir la caja antes de tomar pedidos');
      setShowTypeModal(false);
      return;
    }
    setSelectedTable(null);
    setOrderType(type);
    setActiveOrder(null);
    setDirectSaleMode(false);
    setShowTypeModal(false);
    setView('order');
  };

  const handleDirectSale = () => {
    if (!canDirectSale) {
      message.warning('No tienes permiso para crear ventas directas');
      return;
    }
    if (!hasSession) {
      message.warning('Debes abrir la caja antes de realizar ventas directas');
      return;
    }
    setSelectedTable(null);
    setOrderType('Takeout');
    setActiveOrder(null);
    setDetailOrderId(null);
    setDirectSaleMode(true);
    setView('order');
  };

  const handleOrderConfirmed = (order: OrderDto) => {
    if (directSaleMode) {
      setDetailOrderId(order.id);
      setSelectedTable(null);
      setActiveOrder(null);
      setDirectSaleMode(false);
      setView('table-detail');
      refreshTables();
      return;
    }
    message.success(`Orden #${order.number} confirmada`);
    setView('map');
    setSelectedTable(null);
    setActiveOrder(null);
    setDirectSaleMode(false);
    refreshTables();
  };

  const handleClose = () => {
    setView('map');
    setSelectedTable(null);
    setActiveOrder(null);
    setDetailOrderId(null);
    setDirectSaleMode(false);
    refreshTables();
  };

  const refreshTables = () => setTablesRefreshKey(k => k + 1);

  return (
    <div style={{ height: '100%' }}>
      {view === 'map' && (
        <div>
          {hasSession === false && (
            <Alert
              title="Caja no abierta"
              description="Para tomar pedidos debes abrir la caja del día primero."
              type="warning"
              showIcon
              style={{ marginBottom: 16 }}
            />
          )}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
            <Title level={5} style={{ margin: 0 }}>Mapa de Mesas</Title>
            <Space>
              {canDirectSale && (
                <Button type="primary" icon={<ShoppingCartOutlined />} onClick={handleDirectSale}>
                  Venta directa
                </Button>
              )}
              {canCreateOrders && (
                <Button icon={<ShoppingCartOutlined />} onClick={() => setShowTypeModal(true)}>
                  Nuevo pedido
                </Button>
              )}
            </Space>
          </div>
          <TablesMap branchId={branchId} onSelectTable={handleSelectTable} refreshKey={tablesRefreshKey} />
        </div>
      )}

      {view === 'order' && (
        <TakeOrder
          table={selectedTable ?? undefined}
          orderType={orderType}
          existingOrder={activeOrder ?? undefined}
          directSale={directSaleMode}
          onClose={handleClose}
          onConfirm={handleOrderConfirmed}
        />
      )}

      {view === 'table-detail' && detailOrderId && (
        <TableOrderView
          orderId={detailOrderId}
          table={selectedTable ?? undefined}
          branchId={branchId}
          onClose={handleClose}
          onTableUpdated={refreshTables}
        />
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

    </div>
  );
}
