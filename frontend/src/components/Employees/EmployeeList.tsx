import { useEffect, useState } from 'react';
import { Table, Button, Space, Modal, Form, Input, Select, DatePicker, message, Popconfirm, Drawer, Row, Col, InputNumber, Switch } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, TeamOutlined, CalendarOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { employeeService, positionService } from '../../services/api';
import { useAuth } from '../../context/AuthContext';
import { EmployeeWorkRoleAssignment, EmployeeAvailabilityForm } from '../Scheduling';
import { ContractType, type EmployeeDto, type PositionDto, type CreateEmployeeDto, type UpdateEmployeeDto, type ContractTypeValue } from '../../types';
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
  contractType: ContractTypeValue;
  weeklyMinHours: number;
  weeklyMaxHours: number;
  freeDaysPerMonth?: number;
  isActive?: boolean;
}

const contractTypeOptions = [
  { value: ContractType.FullTime, label: 'Tiempo completo' },
  { value: ContractType.PartTime, label: 'Tiempo parcial' },
  { value: ContractType.Temporary, label: 'Temporal' },
  { value: ContractType.Seasonal, label: 'Temporada' },
];

const extractPositionItems = (value: unknown): PositionDto[] => {
  if (Array.isArray(value)) {
    return value as PositionDto[];
  }

  if (value && typeof value === 'object') {
    const items = (value as { items?: unknown }).items;
    if (Array.isArray(items)) {
      return items as PositionDto[];
    }
  }

  return [];
};

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

  // Cargar posiciones (para el select)
  const loadPositions = async () => {
    try {
      const response = await positionService.getAll();
      setPositions(extractPositionItems(response.data));
    } catch (error) {
      message.error('Error al cargar posiciones');
    }
  };

  useEffect(() => {
    loadEmployees();
    loadPositions();
  }, [showInactive]);

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
          isActive: values.isActive ?? true,
          terminationDate: undefined,
          contractType: values.contractType,
          weeklyMinHours: values.weeklyMinHours,
          weeklyMaxHours: values.weeklyMaxHours,
          freeDaysPerMonth: values.freeDaysPerMonth || 6,
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
          contractType: values.contractType,
          weeklyMinHours: values.weeklyMinHours,
          weeklyMaxHours: values.weeklyMaxHours,
          freeDaysPerMonth: values.freeDaysPerMonth || 6,
        };
        await employeeService.create(createData);
        message.success('Empleado creado');
      }

      setModalVisible(false);
      form.resetFields();
      setEditingId(null);
      loadEmployees();
    } catch (error: unknown) {
      const errorMessage = (error as { response?: { data?: { message?: string } } }).response?.data?.message;
      message.error(errorMessage || 'Error al guardar');
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
      contractType: employee.contractType,
      weeklyMinHours: employee.weeklyMinHours,
      weeklyMaxHours: employee.weeklyMaxHours,
      freeDaysPerMonth: employee.freeDaysPerMonth,
      isActive: employee.isActive,
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
              form.setFieldsValue({
                contractType: ContractType.FullTime,
                weeklyMinHours: 40,
                weeklyMaxHours: 40,
                freeDaysPerMonth: 6,
              });
              setModalVisible(true);
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
            <Col xs={24} sm={12} lg={8}>
              <Form.Item
                label="Nombre"
                name="firstName"
                rules={[{ required: true }]}
              >
                <Input />
              </Form.Item>
            </Col>

            <Col xs={24} sm={12} lg={8}>
              <Form.Item
                label="Apellido"
                name="lastName"
                rules={[{ required: true }]}
              >
                <Input />
              </Form.Item>
            </Col>

            <Col xs={24} sm={12} lg={8}>
              <Form.Item
                label="Email"
                name="email"
                rules={[{ required: true, type: 'email' }]}
              >
                <Input />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} sm={12} lg={8}>
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

            <Col xs={24} sm={12} lg={8}>
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

            <Col xs={24} sm={12} lg={8}>
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
            <Col xs={24} sm={12} lg={8}>
              <Form.Item
                label="Fecha de Contratación"
                name="hireDate"
                rules={[{ required: true }]}
              >
                <DatePicker style={{ width: '100%' }} />
              </Form.Item>
            </Col>

            <Col xs={24} sm={12} lg={8}>
              <Form.Item
                label="Días libres al mes"
                name="freeDaysPerMonth"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <Input type="number" min={1} max={30} placeholder="6" />
              </Form.Item>
            </Col>

            {editingId && (
              <Col xs={24} sm={12} lg={8}>
                <Form.Item label="Activo" name="isActive" valuePropName="checked">
                  <Switch />
                </Form.Item>
              </Col>
            )}
          </Row>

          <Row gutter={16}>
            <Col xs={24} sm={12} lg={8}>
              <Form.Item
                label="Tipo de contrato"
                name="contractType"
                rules={[{ required: true, message: 'Selecciona un tipo de contrato' }]}
              >
                <Select
                  placeholder="Selecciona un tipo"
                  options={contractTypeOptions}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} lg={8}>
              <Form.Item
                label="Horas mínimas por semana"
                name="weeklyMinHours"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <InputNumber<number>
                  min={0}
                  max={80}
                  step={1}
                  precision={0}
                  style={{ width: '100%' }}
                  placeholder="40"
                />
              </Form.Item>
            </Col>

            <Col xs={24} sm={12} lg={8}>
              <Form.Item
                label="Horas máximas por semana"
                name="weeklyMaxHours"
                rules={[
                  { required: true, message: 'Campo obligatorio' },
                  ({ getFieldValue }) => ({
                    validator(_, value) {
                      const minValue = getFieldValue('weeklyMinHours');
                      if (value == null || minValue == null || value >= minValue) {
                        return Promise.resolve();
                      }
                      return Promise.reject('Debe ser mayor o igual a las horas mínimas');
                    },
                  }),
                ]}
              >
                <InputNumber<number>
                  min={0}
                  max={80}
                  step={1}
                  precision={0}
                  style={{ width: '100%' }}
                  placeholder="40"
                />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Modal>

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
