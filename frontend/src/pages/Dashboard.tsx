import { useState } from 'react';
import { Layout, Menu, Button, Dropdown, Avatar, Space, Drawer } from 'antd';
import { UserOutlined, LogoutOutlined, MenuOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import EmployeeList from '../components/Employees/EmployeeList.tsx';
import PositionList from '../components/Positions/PositionList.tsx';
import UserList from '../components/Users/UserList.tsx';
import RoleList from '../components/Roles/RoleList.tsx';
import PermissionList from '../components/Permissions/PermissionList.tsx';
import { ScheduleConfigurationForm } from '../components/Scheduling';
import type { MenuProps } from 'antd';

const { Header, Content, Sider } = Layout;

type MenuItem = Required<MenuProps>['items'][number];

export default function Dashboard() {
  const [selectedMenu, setSelectedMenu] = useState('employees');
  const [drawerVisible, setDrawerVisible] = useState(false);
  const navigate = useNavigate();
  const { user, logout, hasPermission } = useAuth();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const menuItems: MenuItem[] = [
    // RRHH solo visible si tiene permiso
    ...(hasPermission('RRHH.ViewEmployees') ? [
      {
        key: 'rrhh',
        label: 'RRHH',
        children: [
          { key: 'employees', label: 'Empleados' },
          { key: 'positions', label: 'Posiciones' },
        ],
      },
    ] : []),
    // Scheduling
    {
      key: 'scheduling',
      label: 'Horarios',
      children: [
        { key: 'schedule-config', label: 'Configuración' },
      ],
    },
    // Admin visible si tiene acceso
    {
      key: 'admin',
      label: 'Administración',
      children: [
        { key: 'users', label: 'Usuarios' },
        { key: 'roles', label: 'Roles' },
        { key: 'permissions', label: 'Permisos' },
      ],
    },
  ];

  const userMenu: MenuProps['items'] = [
    {
      key: 'profile',
      label: `${user?.firstName || 'Usuario'}`,
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
      case 'schedule-config':
        return <ScheduleConfigurationForm branchId={user?.branchId || ''} />;
      default:
        return <EmployeeList />;
    }
  };

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {/* Sider (menú lateral) */}
      <Sider
        trigger={null}
        collapsible
        collapsed={false}
        breakpoint="lg"
        collapsedWidth={0}
      >
        <div
          style={{
            height: 64,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#fff',
            fontSize: 18,
            fontWeight: 'bold',
          }}
        >
          Grimorio
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[selectedMenu]}
          items={menuItems}
          onClick={(e) => setSelectedMenu(e.key)}
        />
      </Sider>

      {/* Layout principal */}
      <Layout>
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
          <Button
            type="text"
            size="large"
            icon={<MenuOutlined />}
            onClick={() => setDrawerVisible(true)}
            style={{ display: 'none' }}
          />

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
