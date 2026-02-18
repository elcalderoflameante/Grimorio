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
      title: 'Prioridad',
      dataIndex: 'priority',
      key: 'priority',
      width: 100,
      render: (priority: number) => {
        const priorityLabels = ['Principal', '2do', '3ro'];
        const priorityColors: { [key: number]: string } = {
          1: '#52c41a',
          2: '#faad14',
          3: '#f5222d',
        };
        return (
          <span style={{
            color: priorityColors[priority] || '#666',
            fontWeight: priority === 1 ? 'bold' : 'normal',
          }}>
            {priorityLabels[priority - 1] || `P${priority}`}
          </span>
        );
      },
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
            label="Selecciona los roles que puede desempeñar (máximo 3)"
            name="workRoleIds"
            rules={[
              {
                required: true,
                message: 'Debes asignar al menos un rol',
              },
              {
                validator: (_, value) => {
                  if (value && value.length > 3) {
                    return Promise.reject('Máximo 3 roles permitidos');
                  }
                  return Promise.resolve();
                },
              },
            ]}
          >
            <Select
              mode="multiple"
              placeholder="Selecciona uno o más roles (máximo 3)"
              optionLabelProp="label"
              showSearch
              optionFilterProp="label"
              maxTagCount="responsive"
              notFoundContent="Sin roles disponibles"
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
          <div style={{ 
            padding: '8px 12px', 
            backgroundColor: '#f0f2f5', 
            borderRadius: '4px',
            fontSize: '12px',
            color: '#666',
            marginTop: '-8px'
          }}>
            <strong>Nota:</strong> El orden de selección determina la prioridad. El primer rol será el principal (mayor prioridad).
          </div>
        </Form>
      </Modal>
    </div>
  );
};
