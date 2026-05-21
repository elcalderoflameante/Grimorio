import { useEffect, useMemo, useState } from 'react';
import { Layout, Menu, Dropdown, Avatar, Space, Drawer, Button, Breadcrumb, message, Grid, Badge, Alert } from 'antd';
import {
  UserOutlined,
  LogoutOutlined,
  HomeOutlined,
  SettingOutlined,
  TeamOutlined,
  SafetyOutlined,
  KeyOutlined,
  IdcardOutlined,
  FolderOutlined,
  CalendarOutlined,
  ToolOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  MenuOutlined,
  ShopOutlined,
  InboxOutlined,
  WarningOutlined,
  SwapOutlined,
  AppstoreOutlined,
  UnorderedListOutlined,
  ShoppingCartOutlined,
  ShoppingOutlined,
  DollarOutlined,
  BankOutlined,
  ContactsOutlined,
  FileTextOutlined,
  PercentageOutlined,
  FileImageOutlined,
} from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/useAuth';
import { PERMISSIONS } from '../constants/permissions';
import { branchApi } from '../services/api';
import { formatError } from '../utils/errorHandler';
import type { BranchDto } from '../types';
import Welcome from '../components/Welcome/Welcome';
import EmployeeList from '../components/Employees/EmployeeList.tsx';
import EmployeeDetail from '../components/Employees/EmployeeDetail';
import PositionList from '../components/Positions/PositionList.tsx';
import UserList from '../components/Users/UserList.tsx';
import RoleList from '../components/Roles/RoleList.tsx';
import PermissionList from '../components/Permissions/PermissionList.tsx';
import Profile from '../components/Profile/Profile';
import { PayrollSummary, PayrollConfigurationForm } from '../components/Payroll';
import {
  MonthlySchedule,
  SchedulingSettings,
} from '../components/Scheduling';
import TableServiceModule from '../components/POS/TableServiceModule';
import PosOrderModule from '../components/POS/PosOrderModule';
import StationsConfig from '../components/POS/StationsConfig';
import { BranchConfigurationForm } from '../components/Branches/BranchConfigurationForm';
import InventoryConfig from '../components/Inventory/InventoryConfig';
import MenuCategoriesList from '../components/Menu/MenuCategoriesList';
import MenuItemsList from '../components/Menu/MenuItemsList';
import ArticlesList from '../components/Inventory/ArticlesList';
import CurrentStock from '../components/Inventory/CurrentStock';
import StockMovements from '../components/Inventory/StockMovements';
import SuppliersList from '../components/Purchases/SuppliersList';
import PurchasesList from '../components/Purchases/PurchaseOrdersList';
import CustomersList from '../components/Billing/CustomersList';
import CashRegister from '../components/Billing/CashRegister';
import SalesHistory from '../components/Billing/SalesHistory';
import PaymentMethodsSettings from '../components/Billing/PaymentMethodsSettings';
import TaxConfig from '../components/Billing/TaxConfig';
import ElectronicInvoices from '../components/Billing/ElectronicInvoices';
import InvoiceTemplateEditor from '../components/Billing/InvoiceTemplateEditor';
import { inventoryApi } from '../services/api';
import type { StockAlertDto } from '../types';
import type { MenuProps } from 'antd';
import type { ReactNode } from 'react';

const { Header, Content, Sider } = Layout;
const { useBreakpoint } = Grid;

type MenuItem = Required<MenuProps>['items'][number];
type PermissionMenuItem = {
  key: string;
  label: ReactNode;
  icon?: ReactNode;
  permission?: string;
  children?: PermissionMenuItem[];
};

const getMenuLabel = (item: MenuItem): string => {
  if (item && typeof item === 'object' && 'label' in item) {
    const label = item.label;
    if (typeof label === 'string') return label;
  }
  if (item && typeof item === 'object' && 'key' in item) {
    return String(item.key);
  }
  return 'Inicio';
};

const findBreadcrumbs = (items: MenuItem[] = [], key: string): string[] => {
  for (const item of items) {
    if (!item || typeof item !== 'object' || !('key' in item)) continue;

    if (item.key === key) {
      return [getMenuLabel(item)];
    }

    if ('children' in item && Array.isArray(item.children)) {
      const childPath = findBreadcrumbs(item.children as MenuItem[], key);
      if (childPath.length > 0) {
        return [getMenuLabel(item), ...childPath];
      }
    }
  }

  return [];
};

const filterMenuItems = (
  items: PermissionMenuItem[],
  hasPermission: (permissionCode: string) => boolean,
): MenuItem[] => items
  .map((item) => {
    if (item.permission && !hasPermission(item.permission)) return null;

    const { permission: _permission, children: rawChildren, ...menuItem } = item;

    if (rawChildren && rawChildren.length > 0) {
      const children = filterMenuItems(rawChildren, hasPermission);
      if (children.length === 0) return null;
      return { ...menuItem, children };
    }

    return menuItem;
  })
  .filter(Boolean) as MenuItem[];

const getFirstMenuKey = (items: MenuItem[]): string | null => {
  for (const item of items) {
    if (!item || typeof item !== 'object' || !('key' in item)) continue;
    if ('children' in item && Array.isArray(item.children)) {
      const childKey = getFirstMenuKey(item.children as MenuItem[]);
      if (childKey) return childKey;
    }
    return String(item.key);
  }
  return null;
};

export default function Dashboard() {
  const [selectedMenu, setSelectedMenu] = useState('welcome');
  const [drawerVisible, setDrawerVisible] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const [branch, setBranch] = useState<BranchDto | null>(null);
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string | null>(null);
  const [alertasStock, setAlertasStock] = useState<StockAlertDto[]>([]);
  const navigate = useNavigate();
  const { user, logout, hasPermission, branchId } = useAuth();
  const screens = useBreakpoint();
  const isMobile = !screens.lg;
  const siderCollapsed = isMobile || collapsed;
  const isInventorySection = selectedMenu.startsWith('inv-');
  const canViewStock = hasPermission(PERMISSIONS.inventory.stockView);

  useEffect(() => {
    if (!branchId || !hasPermission(PERMISSIONS.admin.branchView)) return;

    const loadBranch = async () => {
      try {
        const response = await branchApi.getCurrent();
        setBranch(response.data);
      } catch (error) {
        message.error(formatError(error));
      }
    };

    loadBranch();
  }, [branchId, hasPermission]);

  useEffect(() => {
    if (!isInventorySection || !canViewStock) return;

    const loadAlertas = async () => {
      try {
        const res = await inventoryApi.getAlerts();
        setAlertasStock(res.data);
      } catch {
        // silencioso: las alertas son informativas
      }
    };
    loadAlertas();
    const interval = setInterval(loadAlertas, 5 * 60 * 1000);
    return () => clearInterval(interval);
  }, [isInventorySection, canViewStock]);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const handleViewEmployee = (employeeId: string) => {
    setSelectedEmployeeId(employeeId);
    setSelectedMenu('employee-detail');
  };

  const handleCreateEmployee = () => {
    setSelectedEmployeeId(null);
    setSelectedMenu('employee-detail');
  };

  const menuItems: MenuItem[] = useMemo(() => {
    const allItems: PermissionMenuItem[] = [
      {
        key: 'welcome',
        label: 'Inicio',
        icon: <HomeOutlined />,
      },
      {
        key: 'admin',
        label: 'Administración',
        icon: <SettingOutlined />,
        children: [
          { key: 'users', label: 'Usuarios', icon: <UserOutlined />, permission: PERMISSIONS.admin.usersView },
          { key: 'roles', label: 'Roles', icon: <SafetyOutlined />, permission: PERMISSIONS.admin.rolesView },
          { key: 'permissions', label: 'Permisos', icon: <KeyOutlined />, permission: PERMISSIONS.admin.permissionsView },
          { key: 'branch-config', label: 'Sucursal', icon: <ShopOutlined />, permission: PERMISSIONS.admin.branchUpdate },
        ],
      },
      {
        key: 'rrhh',
        label: 'RRHH',
        icon: <TeamOutlined />,
        children: [
          { key: 'employees', label: 'Empleados', icon: <IdcardOutlined />, permission: PERMISSIONS.rrhh.employeesView },
          { key: 'positions', label: 'Posiciones', icon: <FolderOutlined />, permission: PERMISSIONS.rrhh.positionsView },
          {
            key: 'scheduling',
            label: 'Horarios',
            icon: <CalendarOutlined />,
            permission: PERMISSIONS.rrhh.schedulingView,
            children: [
              { key: 'monthly-shifts', label: 'Turnos', icon: <CalendarOutlined /> },
              { key: 'scheduling-settings', label: 'Configuraciones', icon: <ToolOutlined /> },
            ],
          },
          {
            key: 'payroll',
            label: 'Nomina',
            icon: <ToolOutlined />,
            permission: PERMISSIONS.rrhh.payrollView,
            children: [
              { key: 'payroll-summary', label: 'Rol de pagos', icon: <IdcardOutlined /> },
              { key: 'payroll-config', label: 'Configuracion', icon: <SettingOutlined /> },
            ],
          },
        ],
      },
      {
        key: 'pos',
        label: 'POS',
        icon: <ShopOutlined />,
        children: [
          { key: 'pos-ordenes', label: 'Pedidos', icon: <ShoppingCartOutlined />, permission: PERMISSIONS.pos.ordersView },
          { key: 'pos-estaciones', label: 'Estaciones', icon: <ToolOutlined />, permission: PERMISSIONS.pos.stationsManage },
          { key: 'pos-table-service', label: 'Atención QR', icon: <ToolOutlined />, permission: PERMISSIONS.pos.tableRequestsView },
        ],
      },
      {
        key: 'menu',
        label: 'Menú',
        icon: <ShopOutlined />,
        children: [
          { key: 'menu-categorias', label: 'Categorías', icon: <AppstoreOutlined />, permission: PERMISSIONS.menu.categoriesView },
          { key: 'menu-items', label: 'Ítems y recetas', icon: <UnorderedListOutlined />, permission: PERMISSIONS.menu.itemsView },
        ],
      },
      {
        key: 'inventario',
        label: (
          <Space size={6}>
            Inventario
            {alertasStock.length > 0 && (
              <Badge count={alertasStock.length} size="small" />
            )}
          </Space>
        ),
        icon: <InboxOutlined />,
        children: [
          { key: 'inv-config', label: 'Configuración', icon: <SettingOutlined />, permission: PERMISSIONS.inventory.configView },
          { key: 'inv-articulos', label: 'Artículos', icon: <InboxOutlined />, permission: PERMISSIONS.inventory.articlesView },
          { key: 'inv-stock', label: 'Stock actual', icon: <WarningOutlined />, permission: PERMISSIONS.inventory.stockView },
          { key: 'inv-movimientos', label: 'Movimientos', icon: <SwapOutlined />, permission: PERMISSIONS.inventory.movementsView },
        ],
      },
      {
        key: 'purchases',
        label: 'Compras',
        icon: <ShoppingOutlined />,
        children: [
          { key: 'purchases-suppliers', label: 'Proveedores', icon: <TeamOutlined />, permission: PERMISSIONS.purchases.suppliersView },
          { key: 'purchases-orders', label: 'Compras', icon: <ShoppingCartOutlined />, permission: PERMISSIONS.purchases.ordersView },
        ],
      },
      {
        key: 'billing',
        label: 'Facturación',
        icon: <DollarOutlined />,
        children: [
          { key: 'billing-cash', label: 'Caja', icon: <BankOutlined />, permission: PERMISSIONS.billing.cashView },
          { key: 'billing-sales', label: 'Ventas', icon: <FileTextOutlined />, permission: PERMISSIONS.billing.cashView },
          { key: 'billing-customers', label: 'Clientes', icon: <ContactsOutlined />, permission: PERMISSIONS.billing.customersView },
          { key: 'billing-payment-methods', label: 'Medios de pago', icon: <DollarOutlined />, permission: PERMISSIONS.billing.paymentMethodsView },
          { key: 'billing-tax-config', label: 'Configuración fiscal', icon: <PercentageOutlined />, permission: PERMISSIONS.billing.taxView },
          { key: 'billing-electronic', label: 'Documentos electrónicos', icon: <FileTextOutlined />, permission: PERMISSIONS.billing.sriView },
          { key: 'billing-invoice-template', label: 'Plantilla de factura', icon: <FileImageOutlined />, permission: PERMISSIONS.billing.sriView },
        ],
      },
    ];

    return filterMenuItems(allItems, hasPermission);
  }, [hasPermission, alertasStock.length]);

  const allowedMenuKeys = useMemo(() => {
    const keys = new Set<string>();
    const collect = (items: MenuItem[]) => {
      for (const item of items) {
        if (!item || typeof item !== 'object' || !('key' in item)) continue;
        keys.add(String(item.key));
        if ('children' in item && Array.isArray(item.children)) {
          collect(item.children as MenuItem[]);
        }
      }
    };
    collect(menuItems);
    return keys;
  }, [menuItems]);

  useEffect(() => {
    if (selectedMenu === 'employee-detail') return;
    if (allowedMenuKeys.has(selectedMenu)) return;

    setSelectedMenu(getFirstMenuKey(menuItems) ?? 'welcome');
  }, [allowedMenuKeys, menuItems, selectedMenu]);

  const userMenu: MenuProps['items'] = [
    {
      key: 'profile',
      label: 'Mi Perfil',
      onClick: () => setSelectedMenu('profile'),
    },
    {
      type: 'divider',
    },
    {
      key: 'logout',
      label: 'Cerrar Sesión',
      icon: <LogoutOutlined />,
      onClick: handleLogout,
    },
  ];

  const renderContent = () => {
    switch (selectedMenu) {
      case 'welcome':
        return <Welcome />;
      case 'profile':
        return <Profile />;
      case 'employees':
        return (
          <EmployeeList
            onViewEmployee={handleViewEmployee}
            onCreateEmployee={handleCreateEmployee}
          />
        );
      case 'employee-detail':
        return (
          <EmployeeDetail
            employeeId={selectedEmployeeId}
            onSaved={() => setSelectedMenu('employees')}
            onCancel={() => setSelectedMenu('employees')}
          />
        );
      case 'positions':
        return <PositionList />;
      case 'users':
        return <UserList />;
      case 'roles':
        return <RoleList />;
      case 'permissions':
        return <PermissionList />;
      case 'branch-config':
        return <BranchConfigurationForm />;
      case 'monthly-shifts':
        return <MonthlySchedule />;
      case 'scheduling-settings':
        return <SchedulingSettings branchId={branchId || ''} />;
      case 'payroll-summary':
        return <PayrollSummary />;
      case 'payroll-config':
        return <PayrollConfigurationForm />;
      case 'pos-ordenes':
        return <PosOrderModule />;
case 'pos-estaciones':
        return <StationsConfig />;
      case 'pos-table-service':
        return <TableServiceModule />;
      case 'menu-categorias':
        return <MenuCategoriesList />;
      case 'menu-items':
        return <MenuItemsList />;
      case 'inv-stock':
        return <CurrentStock />;
      case 'inv-movimientos':
        return <StockMovements />;
      case 'inv-articulos':
        return <ArticlesList />;
      case 'inv-config':
        return <InventoryConfig />;
      case 'purchases-suppliers':
        return <SuppliersList />;
      case 'purchases-orders':
        return <PurchasesList />;
      case 'billing-cash':
        return <CashRegister />;
      case 'billing-sales':
        return <SalesHistory />;
      case 'billing-customers':
        return <CustomersList />;
      case 'billing-payment-methods':
        return <PaymentMethodsSettings />;
      case 'billing-tax-config':
        return <TaxConfig />;
      case 'billing-electronic':
        return <ElectronicInvoices />;
      case 'billing-invoice-template':
        return <InvoiceTemplateEditor />;
      default:
        return <Welcome />;
    }
  };

  const breadcrumbItems = useMemo(() => {
    if (selectedMenu === 'employee-detail') {
      return [
        { title: 'RRHH' },
        { title: 'Empleados' },
        { title: selectedEmployeeId ? 'Empleado' : 'Nuevo empleado' },
      ];
    }

    const path = findBreadcrumbs(menuItems, selectedMenu);
    const titles = path.length > 0 ? path : ['Inicio'];
    return titles.map((title) => ({ title }));
  }, [selectedMenu, menuItems, selectedEmployeeId]);

  return (
    <Layout style={{ minHeight: '100vh', width: '100%', overflowX: 'hidden' }}>
      {/* Sider (menú lateral) */}
      <Sider
        className="dashboard-sider"
        trigger={null}
        collapsible
        collapsed={siderCollapsed}
        breakpoint="lg"
        collapsedWidth={isMobile ? 0 : 80}
        width={250}
        style={{
          overflow: 'hidden',
          height: '100vh',
          position: isMobile ? 'absolute' : 'fixed',
          left: 0,
          top: 0,
          bottom: 0,
          display: 'flex',
          flexDirection: 'column',
          zIndex: 1000,
        }}
      >
        <div
          style={{
            height: 64,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '0 16px',
            color: '#fff',
            fontSize: 18,
            fontWeight: 'bold',
            flexShrink: 0,
            borderBottom: '1px solid rgba(255, 255, 255, 0.1)',
          }}
        >
          <div style={{ flex: 1, textAlign: 'center' }}>
            {!siderCollapsed && <span>Grimorio</span>}
          </div>
          <Button
            type="text"
            icon={siderCollapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={() => setCollapsed((prev) => !prev)}
            style={{
              fontSize: '16px',
              color: '#fff',
              width: 32,
              height: 32,
              flexShrink: 0,
            }}
          />
        </div>
        <div className="dashboard-sider-menu-scroll">
          <Menu
            theme="dark"
            mode="inline"
            selectedKeys={[selectedMenu]}
            items={menuItems}
            onClick={(e) => setSelectedMenu(e.key)}
            style={{ borderRight: 0 }}
          />
        </div>
      </Sider>

      {/* Layout principal */}
        <Layout
          style={{
            marginLeft: isMobile ? 0 : siderCollapsed ? 80 : 250,
            transition: 'margin-left 0.2s',
            minWidth: 0,
          }}
        >
        {/* Header */}
          <Header
          style={{
            background: '#fff',
            padding: isMobile ? '0 12px' : '0 24px',
            boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            flexWrap: 'wrap',
            rowGap: 8,
            height: 'auto',
            minHeight: 64,
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            {isMobile && (
              <Button
                type="text"
                icon={<MenuOutlined />}
                onClick={() => setDrawerVisible(true)}
              />
            )}
            <Breadcrumb items={breadcrumbItems} />
          </div>
          
          <div style={{ display: 'flex', alignItems: 'center', gap: isMobile ? '12px' : '24px' }}>
            {!isMobile && (
            <Space>
              <ShopOutlined />
              <span>Sucursal: {branch?.name || '—'}</span>
            </Space>
            )}
            <Dropdown menu={{ items: userMenu }} placement="bottomRight">
              <Space style={{ cursor: 'pointer' }}>
                <Avatar icon={<UserOutlined />} />
                <span>{user?.firstName || 'Usuario'}</span>
              </Space>
            </Dropdown>
          </div>
        </Header>

        {/* Contenido */}
        <Content
          style={{
            margin: isMobile ? '16px 8px' : '24px 16px',
            padding: isMobile ? 16 : 24,
            background: '#fff',
            borderRadius: '8px',
            overflowX: 'auto',
            maxWidth: '100%',
          }}
        >
          {isInventorySection && alertasStock.length > 0 && (
            <Alert
              type="warning"
              icon={<WarningOutlined />}
              showIcon
              style={{ marginBottom: 16, cursor: 'pointer' }}
              title={`${alertasStock.length} artículo${alertasStock.length > 1 ? 's' : ''} con stock bajo mínimo`}
              description={alertasStock.slice(0, 3).map(a => `${a.articleName}: ${a.currentStock}/${a.minStock} ${a.unitSymbol}`).join(' · ') + (alertasStock.length > 3 ? ' ...' : '')}
              onClick={() => setSelectedMenu('inv-stock')}
            />
          )}
          {renderContent()}
        </Content>
      </Layout>

      {/* Drawer para menú móvil */}
      <Drawer
        title="Menú"
        placement="left"
        onClose={() => setDrawerVisible(false)}
        open={drawerVisible}
        styles={{ body: { padding: 0, overflowY: 'auto' } }}
      >
        <Menu
          theme="light"
          mode="vertical"
          selectedKeys={[selectedMenu]}
          items={menuItems}
          onClick={(e) => {
            setSelectedMenu(e.key);
            setDrawerVisible(false);
          }}
        />
      </Drawer>
    </Layout>
  );
}
