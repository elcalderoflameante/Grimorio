import { useEffect, useMemo, useState } from 'react';
import { Layout, Menu, Dropdown, Avatar, Space, Drawer, Button, Breadcrumb, message } from 'antd';
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
  AppstoreOutlined,
  UsergroupAddOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  ShopOutlined
} from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { branchApi } from '../services/api';
import { formatError } from '../utils/errorHandler';
import type { BranchDto } from '../types';
import Welcome from '../components/Welcome/Welcome';
import EmployeeList from '../components/Employees/EmployeeList.tsx';
import PositionList from '../components/Positions/PositionList.tsx';
import UserList from '../components/Users/UserList.tsx';
import RoleList from '../components/Roles/RoleList.tsx';
import PermissionList from '../components/Permissions/PermissionList.tsx';
import Profile from '../components/Profile/Profile';
import {
  ScheduleConfigurationForm,
  WorkAreaList,
  WorkRoleList,
  MonthlySchedule,
  ShiftTemplateList,
} from '../components/Scheduling';
import { BranchConfigurationForm } from '../components/Branches/BranchConfigurationForm';
import type { MenuProps } from 'antd';

const { Header, Content, Sider } = Layout;

type MenuItem = Required<MenuProps>['items'][number];

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

export default function Dashboard() {
  const [selectedMenu, setSelectedMenu] = useState('welcome');
  const [drawerVisible, setDrawerVisible] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const [branch, setBranch] = useState<BranchDto | null>(null);
  const navigate = useNavigate();
  const { user, logout, hasPermission, branchId } = useAuth();

  useEffect(() => {
    if (!branchId) return;

    const loadBranch = async () => {
      try {
        const response = await branchApi.getCurrent();
        setBranch(response.data);
      } catch (error) {
        message.error(formatError(error));
      }
    };

    loadBranch();
  }, [branchId]);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const menuItems: MenuItem[] = [
    // Inicio
    {
      key: 'welcome',
      label: 'Inicio',
      icon: <HomeOutlined />,
    },
    // Admin visible si tiene acceso
    {
      key: 'admin',
      label: 'Administración',
      icon: <SettingOutlined />,
      children: [
        { key: 'users', label: 'Usuarios', icon: <UserOutlined /> },
        { key: 'roles', label: 'Roles', icon: <SafetyOutlined /> },
        { key: 'permissions', label: 'Permisos', icon: <KeyOutlined /> },
        { key: 'branch-config', label: 'Sucursal', icon: <ShopOutlined /> },
      ],
    },
    // RRHH solo visible si tiene permiso
    ...(hasPermission('RRHH.ViewEmployees') ? [
      {
        key: 'rrhh',
        label: 'RRHH',
        icon: <TeamOutlined />,
        children: [
          { key: 'employees', label: 'Empleados', icon: <IdcardOutlined /> },
          { key: 'positions', label: 'Posiciones', icon: <FolderOutlined /> },
          {
            key: 'scheduling',
            label: 'Horarios',
            icon: <CalendarOutlined />,
            children: [
              { key: 'monthly-shifts', label: 'Turnos', icon: <CalendarOutlined /> },
              { key: 'shift-templates', label: 'Plantillas', icon: <ToolOutlined /> },
              { key: 'schedule-config', label: 'Configuración', icon: <ToolOutlined /> },
              { key: 'work-areas', label: 'Áreas de Trabajo', icon: <AppstoreOutlined /> },
              { key: 'work-roles', label: 'Roles de Trabajo', icon: <UsergroupAddOutlined /> },
            ],
          },
        ],
      },
    ] : []),
  ];

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
        return <EmployeeList />;
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
      case 'shift-templates':
        return <ShiftTemplateList branchId={branchId || ''} />;
      case 'schedule-config':
          return <ScheduleConfigurationForm branchId={branchId || ''} />;
      case 'work-areas':
          return <WorkAreaList branchId={branchId || ''} />;
      case 'work-roles':
          return <WorkRoleList branchId={branchId || ''} />;
      default:
        return <Welcome />;
    }
  };

  const breadcrumbItems = useMemo(() => {
    const path = findBreadcrumbs(menuItems, selectedMenu);
    const titles = path.length > 0 ? path : ['Inicio'];
    return titles.map((title) => ({ title }));
  }, [menuItems, selectedMenu]);

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {/* Sider (menú lateral) */}
      <Sider
        trigger={null}
        collapsible
        collapsed={collapsed}
        breakpoint="lg"
        collapsedWidth={80}
        width={250}
        style={{
          overflow: 'hidden',
          height: '100vh',
          position: 'fixed',
          left: 0,
          top: 0,
          bottom: 0,
          display: 'flex',
          flexDirection: 'column',
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
            {!collapsed && <span>Grimorio</span>}
          </div>
          <Button
            type="text"
            icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={() => setCollapsed(!collapsed)}
            style={{
              fontSize: '16px',
              color: '#fff',
              width: 32,
              height: 32,
              flexShrink: 0,
            }}
          />
        </div>
        <div style={{ flex: 1, overflow: 'auto' }}>
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
      <Layout style={{ marginLeft: collapsed ? 80 : 250, transition: 'margin-left 0.2s' }}>
        {/* Header */}
        <Header
          style={{
            background: '#fff',
            padding: '0 24px',
            boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <Space>
            <ShopOutlined />
            <span>Sucursal: {branch?.name || '—'}</span>
          </Space>
          <Dropdown menu={{ items: userMenu }} placement="bottomRight">
            <Space style={{ cursor: 'pointer' }}>
              <Avatar icon={<UserOutlined />} />
              <span>{user?.firstName || 'Usuario'}</span>
            </Space>
          </Dropdown>
        </Header>

        {/* Contenido */}
        <Content
          style={{
            margin: '24px 16px',
            padding: 24,
            background: '#fff',
            borderRadius: '8px',
          }}
        >
          <Breadcrumb items={breadcrumbItems} style={{ marginBottom: 16 }} />
          {renderContent()}
        </Content>
      </Layout>

      {/* Drawer para menú móvil */}
      <Drawer
        title="Menú"
        placement="left"
        onClose={() => setDrawerVisible(false)}
        open={drawerVisible}
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
