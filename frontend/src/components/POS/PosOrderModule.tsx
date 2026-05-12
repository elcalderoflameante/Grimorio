import { useState, useEffect } from 'react';
import { Button, Modal, Space, Typography, message, Alert } from 'antd';
import { ShoppingCartOutlined, CarOutlined } from '@ant-design/icons';
import { useAuth } from '../../context/useAuth';
import type { OrderDto, RestaurantTableDto, OrderType } from '../../types';
import { cashApi } from '../../services/api';
import TablesMap from './TablesMap';
import TakeOrder from './TakeOrder';
import TableOrderView from './TableOrderView';

const { Title, Text } = Typography;

type View = 'map' | 'order' | 'table-detail';

export default function PosOrderModule() {
  const { branchId } = useAuth();
  const [view, setView] = useState<View>('map');
  const [selectedTable, setSelectedTable] = useState<RestaurantTableDto | null>(null);
  const [orderType, setOrderType] = useState<OrderType>('DineIn');
  const [showTypeModal, setShowTypeModal] = useState(false);
  const [activeOrder, setActiveOrder] = useState<OrderDto | null>(null);
  const [hasSession, setHasSession] = useState<boolean | null>(null);
  const [detailOrderId, setDetailOrderId] = useState<string | null>(null);
  const [tablesRefreshKey, setTablesRefreshKey] = useState(0);

  useEffect(() => {
    cashApi.getActiveSession()
      .then(() => setHasSession(true))
      .catch(() => setHasSession(false));
  }, []);

  if (!branchId) return <Text type="danger">Sin sucursal asignada</Text>;

  const handleSelectTable = (table: RestaurantTableDto) => {
    if (!hasSession) {
      message.warning('Debes abrir la caja antes de tomar pedidos');
      return;
    }
    setSelectedTable(table);
    if (table.currentStatus === 'Occupied' && table.currentOrderId) {
      setDetailOrderId(table.currentOrderId);
      setView('table-detail');
    } else {
      setOrderType('DineIn');
      setActiveOrder(null);
      setView('order');
    }
  };

  const handleNewOrder = (type: OrderType) => {
    if (!hasSession) {
      message.warning('Debes abrir la caja antes de tomar pedidos');
      setShowTypeModal(false);
      return;
    }
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
    setDetailOrderId(null);
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
            <Button icon={<ShoppingCartOutlined />} onClick={() => setShowTypeModal(true)}>
              Nuevo pedido
            </Button>
          </div>
          <TablesMap branchId={branchId} onSelectTable={handleSelectTable} refreshKey={tablesRefreshKey} />
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

      {view === 'table-detail' && selectedTable && detailOrderId && (
        <TableOrderView
          orderId={detailOrderId}
          table={selectedTable}
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
