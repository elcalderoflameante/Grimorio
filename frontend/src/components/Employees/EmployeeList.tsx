import { useEffect, useState } from 'react';
import { Table, Button, Space, Modal, Form, Input, Select, DatePicker, message, Popconfirm, Drawer, Row, Col } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, TeamOutlined, CalendarOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { employeeService, positionService } from '../../services/api';
import { useAuth } from '../../context/AuthContext';
import { EmployeeWorkRoleAssignment, EmployeeAvailabilityForm } from '../Scheduling';
import type { EmployeeDto, PositionDto, CreateEmployeeDto, UpdateEmployeeDto } from '../../types';
import dayjs, { type Dayjs } from 'dayjs';
import { isValidEcuadorCedula, isValidEcuadorCell } from '../../utils/ecuadorValidators';

interface EmployeeFormValues {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  identificationNumber: string;
  positionId: string;
  hireDate: Dayjs;
}

export default function EmployeeList() {
  const [employees, setEmployees] = useState<EmployeeDto[]>([]);
  const [positions, setPositions] = useState<PositionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [pagination, setPagination] = useState({ pageNumber: 1, pageSize: 10, total: 0 });
  const [modalVisible, setModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form] = Form.useForm<EmployeeFormValues>();
  const { hasPermission } = useAuth();
  const [rolesDrawerVisible, setRolesDrawerVisible] = useState(false);
  const [availabilityDrawerVisible, setAvailabilityDrawerVisible] = useState(false);
  const [selectedEmployee, setSelectedEmployee] = useState<EmployeeDto | null>(null);

  // Cargar empleados
  const loadEmployees = async (pageNumber = 1, pageSize = 10) => {
    setLoading(true);
    try {
      const response = await employeeService.getAll(pageNumber, pageSize);
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

  // Cargar posiciones (para el select)
  const loadPositions = async () => {
    try {
      const response = await positionService.getAll();
      const data = response.data;
      const items = Array.isArray((data as any).items) ? (data as any).items : Array.isArray(data) ? data : [];
      setPositions(items);
    } catch (error) {
      message.error('Error al cargar posiciones');
    }
  };

  useEffect(() => {
    loadEmployees();
    loadPositions();
  }, []);

  // Crear o actualizar
  const handleSave = async (values: EmployeeFormValues) => {
    try {
      if (editingId) {
        const updateData: UpdateEmployeeDto = {
          firstName: values.firstName,
          lastName: values.lastName,
          email: values.email,
          phone: values.phone || '',
          identificationNumber: values.identificationNumber,
          positionId: values.positionId,
          isActive: true,
          terminationDate: undefined,
        };
        await employeeService.update(editingId, updateData);
        message.success('Empleado actualizado');
      } else {
        const createData: CreateEmployeeDto = {
          firstName: values.firstName,
          lastName: values.lastName,
          email: values.email,
          phone: values.phone || '',
          identificationNumber: values.identificationNumber,
          positionId: values.positionId,
          hireDate: values.hireDate.toISOString(),
        };
        await employeeService.create(createData);
        message.success('Empleado creado');
      }

      setModalVisible(false);
      form.resetFields();
      setEditingId(null);
      loadEmployees();
    } catch (error: any) {
      message.error(error.response?.data?.message || 'Error al guardar');
    }
  };

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

  // Editar
  const handleEdit = (employee: EmployeeDto) => {
    setEditingId(employee.id);
    form.setFieldsValue({
      firstName: employee.firstName,
      lastName: employee.lastName,
      email: employee.email,
      phone: employee.phone,
      identificationNumber: employee.identificationNumber,
      positionId: employee.positionId,
      hireDate: dayjs(employee.hireDate),
    });
    setModalVisible(true);
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
      title: 'Cédula',
      dataIndex: 'identificationNumber',
      key: 'identificationNumber',
    },
    {
      title: 'Acciones',
      key: 'actions',
      render: (_, record) => (
        <Space>
          {hasPermission('RRHH.UpdateEmployees') && (
            <Button
              icon={<EditOutlined />}
              onClick={() => handleEdit(record)}
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
              setEditingId(null);
              form.resetFields();
              setModalVisible(true);
            }}
          >
            Nuevo Empleado
          </Button>
        )}
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

      {/* Modal crear/editar */}
      <Modal
        title={editingId ? 'Editar Empleado' : 'Nuevo Empleado'}
        open={modalVisible}
        onOk={() => form.submit()}
        onCancel={() => setModalVisible(false)}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSave}
        >
          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Nombre"
                name="firstName"
                rules={[{ required: true }]}
              >
                <Input />
              </Form.Item>
            </Col>

            <Col xs={24} sm={12}>
              <Form.Item
                label="Apellido"
                name="lastName"
                rules={[{ required: true }]}
              >
                <Input />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Email"
                name="email"
                rules={[{ required: true, type: 'email' }]}
              >
                <Input />
              </Form.Item>
            </Col>

            <Col xs={24} sm={12}>
              <Form.Item
                label="Teléfono"
                name="phone"
                rules={[
                  {
                    validator: (_, value) => {
                      if (!value) return Promise.resolve(); // opcional
                      return isValidEcuadorCell(value)
                        ? Promise.resolve()
                        : Promise.reject('Teléfono celular inválido (Ecuador, debe iniciar con 09)');
                    },
                  },
                ]}
              >
                <Input placeholder="09xxxxxxxx" />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Cédula"
                name="identificationNumber"
                rules={[
                  { required: true, message: 'La cédula es requerida' },
                  {
                    validator: (_, value) => {
                      if (!value) return Promise.reject('La cédula es requerida');
                      return isValidEcuadorCedula(value)
                        ? Promise.resolve()
                        : Promise.reject('Cédula inválida (Ecuador)');
                    },
                  },
                ]}
              >
                <Input />
              </Form.Item>
            </Col>

            <Col xs={24} sm={12}>
              <Form.Item
                label="Posición"
                name="positionId"
                rules={[{ required: true }]}
              >
                <Select
                  placeholder="Selecciona una posición"
                  showSearch
                  optionFilterProp="label"
                  options={positions.map(p => ({
                    label: p.name || p.description || p.id,
                    value: p.id,
                  }))}
                />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Fecha de Contratación"
                name="hireDate"
                rules={[{ required: true }]}
              >
                <DatePicker style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Modal>

      {/* Drawer para asignar roles */}
      <Drawer
        title="Asignar Roles"
        placement="right"
        width={800}
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
        width={800}
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
