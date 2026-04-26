import { Tabs } from 'antd';
import { UnorderedListOutlined, AppstoreOutlined, ShopOutlined } from '@ant-design/icons';
import UnitsList from './UnitsList';
import CategoriesList from './CategoriesList';
import WarehousesList from './WarehousesList';

export default function InventoryConfig() {
  return (
    <Tabs
      defaultActiveKey="unidades"
      type="card"
      items={[
        {
          key: 'unidades',
          label: <><UnorderedListOutlined /> Unidades de medida</>,
          children: <UnitsList />,
        },
        {
          key: 'categorias',
          label: <><AppstoreOutlined /> Categorías</>,
          children: <CategoriesList />,
        },
        {
          key: 'bodegas',
          label: <><ShopOutlined /> Bodegas</>,
          children: <WarehousesList />,
        },
      ]}
    />
  );
}
