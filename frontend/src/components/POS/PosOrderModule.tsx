import { useState } from 'react';
import { Button, Modal, Space, Typography, message } from 'antd';
import { ShoppingCartOutlined, CarOutlined } from '@ant-design/icons';
import { useAuth } from '../../context/useAuth';
import type { OrderDto, RestaurantTableDto, OrderType } from '../../types';
import TablesMap from './TablesMap';
import TakeOrder from './TakeOrder';

const { Title, Text } = Typography;

type View = 'map' | 'order';

export default function PosOrderModule() {
  const { branchId } = useAuth();
  const [view, setView] = useState<View>('map');
  const [selectedTable, setSelectedTable] = useState<RestaurantTableDto | null>(null);
  const [orderType, setOrderType] = useState<OrderType>('DineIn');
  const [showTypeModal, setShowTypeModal] = useState(false);
  const [activeOrder, setActiveOrder] = useState<OrderDto | null>(null);

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

  return (
    <div style={{ height: '100%' }}>
      {view === 'map' && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
            <Title level={5} style={{ margin: 0 }}>Mapa de Mesas</Title>
            <Space>
              <Button
                icon={<ShoppingCartOutlined />}
                onClick={() => { setShowTypeModal(true); }}
              >
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

      <Modal
        title="Tipo de pedido"
        open={showTypeModal}
        onCancel={() => setShowTypeModal(false)}
        footer={null}
        width={320}
      >
        <Space direction="vertical" style={{ width: '100%' }} size={12}>
          <Button
            block
            size="large"
            icon={<ShoppingCartOutlined />}
            onClick={() => handleNewOrder('Takeout')}
          >
            Para llevar
          </Button>
          <Button
            block
            size="large"
            icon={<CarOutlined />}
            onClick={() => handleNewOrder('Delivery')}
          >
            Domicilio
          </Button>
        </Space>
      </Modal>
    </div>
  );
}
