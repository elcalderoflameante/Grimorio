import { useCallback, useEffect, useState } from 'react';
import { Table, Button, Modal, Form, Select, message, Popconfirm, Divider } from 'antd';
import { DeleteOutlined, EditOutlined } from '@ant-design/icons';
import { employeeWorkRoleApi, workRoleApi } from '../../services/api';
import { formatError } from '../../utils/errorHandler';
import type { EmployeeWorkRoleDto, WorkRoleDto, EmployeeDto, AssignWorkRolesDto } from '../../types';

interface EmployeeWorkRoleAssignmentProps {
  employee: EmployeeDto;
  onClose: () => void;
}

export const EmployeeWorkRoleAssignment = ({ employee, onClose }: EmployeeWorkRoleAssignmentProps) => {
  const [workRoles, setWorkRoles] = useState<WorkRoleDto[]>([]);
  const [employeeRoles, setEmployeeRoles] = useState<EmployeeWorkRoleDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [selectedRoleIds, setSelectedRoleIds] = useState<string[]>([]);
  const [form] = Form.useForm();

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      const [rolesResponse, employeeRolesResponse] = await Promise.all([
        workRoleApi.getAll(),
        employeeWorkRoleApi.getByEmployee(employee.id),
      ]);
      setWorkRoles(rolesResponse.data);
      setEmployeeRoles(employeeRolesResponse.data);
      setSelectedRoleIds(employeeRolesResponse.data.map(r => r.workRoleId));
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [employee.id]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleOpenModal = () => {
    form.setFieldsValue({
      workRoleIds: selectedRoleIds,
    });
    setModalVisible(true);
  };

  const handleSubmit = async (values: { workRoleIds?: string[] }) => {
    try {
      setLoading(true);
      await employeeWorkRoleApi.assign({
        employeeId: employee.id,
        workRoleIds: values.workRoleIds || [],
      } as AssignWorkRolesDto);
      message.success('Roles asignados correctamente');
      setModalVisible(false);
      loadData();
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (workRoleId: string) => {
    try {
      setLoading(true);
      await employeeWorkRoleApi.remove(employee.id, workRoleId);
      message.success('Rol eliminado');
      loadData();
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const columns = [
    {
      title: 'Rol',
      dataIndex: 'workRoleName',
      key: 'workRoleName',
    },
    {
      title: 'Principal',
      dataIndex: 'isPrimary',
      key: 'isPrimary',
      render: (isPrimary: boolean) => isPrimary ? '✓' : '—',
    },
    {
      title: 'Prioridad',
      dataIndex: 'priority',
      key: 'priority',
    },
    {
      title: 'Acciones',
      key: 'actions',
      render: (_text: unknown, record: EmployeeWorkRoleDto) => (
        <Popconfirm
          title="Eliminar"
          description="¿Estás seguro de eliminar este rol?"
          onConfirm={() => handleDelete(record.workRoleId)}
          okText="Sí"
          cancelText="No"
        >
          <Button type="primary" danger size="small" icon={<DeleteOutlined />} />
        </Popconfirm>
      ),
    },
  ];

  return (
    <div>
      <Divider>Roles de {employee.firstName} {employee.lastName}</Divider>

      <div style={{ marginBottom: 16 }}>
        <Button
          type="primary"
          icon={<EditOutlined />}
          onClick={handleOpenModal}
        >
          Modificar Roles
        </Button>
        <Button
          style={{ marginLeft: 8 }}
          onClick={onClose}
        >
          Cerrar
        </Button>
      </div>

      <Table
        dataSource={employeeRoles}
        columns={columns}
        loading={loading}
        rowKey="id"
        pagination={false}
        size="small"
      />

      <Modal
        title={`Asignar Roles a ${employee.firstName} ${employee.lastName}`}
        open={modalVisible}
        onOk={() => form.submit()}
        onCancel={() => setModalVisible(false)}
        confirmLoading={loading}
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit}>
          <Form.Item
            label="Selecciona los roles que puede desempeñar"
            name="workRoleIds"
          >
            <Select
              mode="multiple"
              placeholder="Selecciona uno o más roles"
              optionLabelProp="label"
              showSearch
              optionFilterProp="label"
            >
              {workRoles.map((role) => (
                <Select.Option key={role.id} value={role.id} label={role.name}>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <span>{role.name}</span>
                    <span style={{ color: '#999', fontSize: '12px' }}>
                      {role.freeDaysPerMonth} días libres
                    </span>
                  </div>
                </Select.Option>
              ))}
            </Select>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};
