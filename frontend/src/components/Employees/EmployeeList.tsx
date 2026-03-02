import { useEffect, useState } from 'react';
import { Table, Button, Space, message, Popconfirm, Drawer, Switch } from 'antd';
import { PlusOutlined, DeleteOutlined, TeamOutlined, CalendarOutlined, EyeOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { employeeService } from '../../services/api';
import { useAuth } from '../../context/AuthContext';
import { EmployeeWorkRoleAssignment, EmployeeAvailabilityForm } from '../Scheduling';
import { type EmployeeDto } from '../../types';

interface EmployeeListProps {
  onViewEmployee?: (employeeId: string) => void;
  onCreateEmployee?: () => void;
}

export default function EmployeeList({ onViewEmployee, onCreateEmployee }: EmployeeListProps) {
  const [employees, setEmployees] = useState<EmployeeDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [pagination, setPagination] = useState({ pageNumber: 1, pageSize: 10, total: 0 });
  const { hasPermission } = useAuth();
  const [rolesDrawerVisible, setRolesDrawerVisible] = useState(false);
  const [availabilityDrawerVisible, setAvailabilityDrawerVisible] = useState(false);
  const [selectedEmployee, setSelectedEmployee] = useState<EmployeeDto | null>(null);
  const [showInactive, setShowInactive] = useState(false);

  // Cargar empleados
  const loadEmployees = async (pageNumber = 1, pageSize = 10) => {
    setLoading(true);
    try {
      const response = await employeeService.getAll(pageNumber, pageSize, !showInactive);
      const data = response.data;
      
      setEmployees(Array.isArray(data) ? data : []);
      setPagination({
        pageNumber: pageNumber,
        pageSize: pageSize,
        total: Array.isArray(data) ? data.length : 0,
      });
    } catch (error) {
      message.error('Error al cargar empleados');
      console.error('Error al cargar empleados:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadEmployees();
  }, [showInactive]);

  // Eliminar
  const handleDelete = async (id: string) => {
    try {
      await employeeService.delete(id);
      message.success('Empleado eliminado');
      loadEmployees();
    } catch (error) {
      message.error('Error al eliminar');
    }
  };

  // Columnas de la tabla
  const columns: ColumnsType<EmployeeDto> = [
    {
      title: 'Nombre',
      key: 'name',
      render: (_, record) => `${record.firstName} ${record.lastName}`,
    },
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
    },
    {
      title: 'Posición',
      dataIndex: 'positionName',
      key: 'positionName',
    },
    {
      title: 'Activo',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (isActive: boolean) => (isActive ? '✓' : '✗'),
    },
    {
      title: 'Cédula',
      dataIndex: 'identificationNumber',
      key: 'identificationNumber',
    },
    {
      title: 'Acciones',
      key: 'actions',
      render: (_, record) => (
        <Space>
          {onViewEmployee && (
            <Button
              icon={<EyeOutlined />}
              onClick={() => onViewEmployee(record.id)}
              title="Ver"
            />
          )}
          <Button
            icon={<TeamOutlined />}
            title="Asignar Roles"
            onClick={() => {
              setSelectedEmployee(record);
              setRolesDrawerVisible(true);
            }}
          />
          <Button
            icon={<CalendarOutlined />}
            title="Disponibilidad"
            onClick={() => {
              setSelectedEmployee(record);
              setAvailabilityDrawerVisible(true);
            }}
          />
          {hasPermission('RRHH.DeleteEmployees') && (
            <Popconfirm
              title="¿Eliminar empleado?"
              onConfirm={() => handleDelete(record.id)}
              okText="Sí"
              cancelText="No"
            >
              <Button icon={<DeleteOutlined />} danger />
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ marginBottom: 16 }}>
        {hasPermission('RRHH.CreateEmployees') && (
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              onCreateEmployee?.();
            }}
          >
            Nuevo Empleado
          </Button>
        )}
        <span style={{ marginLeft: 16 }}>
          <Switch
            checked={showInactive}
            onChange={setShowInactive}
          />{' '}
          Mostrar inactivos
        </span>
      </div>

      <Table
        columns={columns}
        dataSource={employees}
        loading={loading}
        rowKey="id"
        pagination={{
          current: pagination.pageNumber,
          pageSize: pagination.pageSize,
          total: pagination.total,
          onChange: (page, size) => loadEmployees(page, size),
        }}
      />

      {/* Drawer para asignar roles */}
      <Drawer
        title="Asignar Roles"
        placement="right"
        size="large"
        onClose={() => setRolesDrawerVisible(false)}
        open={rolesDrawerVisible}
      >
        {selectedEmployee && (
          <EmployeeWorkRoleAssignment
            employee={selectedEmployee}
            onClose={() => setRolesDrawerVisible(false)}
          />
        )}
      </Drawer>

      {/* Drawer para disponibilidad */}
      <Drawer
        title="Disponibilidad"
        placement="right"
        size="large"
        onClose={() => setAvailabilityDrawerVisible(false)}
        open={availabilityDrawerVisible}
      >
        {selectedEmployee && (
          <EmployeeAvailabilityForm
            employee={selectedEmployee}
            onClose={() => setAvailabilityDrawerVisible(false)}
          />
        )}
      </Drawer>
    </div>
  );
}
