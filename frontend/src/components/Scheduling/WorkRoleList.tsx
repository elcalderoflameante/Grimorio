import { useCallback, useEffect, useState } from 'react';
import { Table, Button, Modal, Form, Input, InputNumber, Select, Space, message, Popconfirm } from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { workRoleApi, workAreaApi } from '../../services/api';
import type { WorkRoleDto, WorkAreaDto, CreateWorkRoleDto, UpdateWorkRoleDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

interface WorkRoleListProps {
  branchId: string;
}

export const WorkRoleList = ({ branchId }: WorkRoleListProps) => {
  const [roles, setRoles] = useState<WorkRoleDto[]>([]);
  const [areas, setAreas] = useState<WorkAreaDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingRole, setEditingRole] = useState<WorkRoleDto | null>(null);
  const [form] = Form.useForm();

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      const [rolesResponse, areasResponse] = await Promise.all([
        workRoleApi.getAll(),
        workAreaApi.getAll(branchId),
      ]);
      setRoles(rolesResponse.data);
      setAreas(areasResponse.data);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [branchId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleOpenModal = (role?: WorkRoleDto) => {
    if (role) {
      setEditingRole(role);
      form.setFieldsValue({
        name: role.name,
        description: role.description,
        workAreaId: role.workAreaId,
        freeDaysPerMonth: role.freeDaysPerMonth,
        dailyHoursTarget: role.dailyHoursTarget,
      });
    } else {
      setEditingRole(null);
      form.resetFields();
    }
    setModalVisible(true);
  };

  const handleSubmit = async (values: CreateWorkRoleDto | UpdateWorkRoleDto) => {
    try {
      setLoading(true);

      if (editingRole) {
        await workRoleApi.update(editingRole.id, values as UpdateWorkRoleDto);
        message.success('Rol actualizado');
      } else {
        await workRoleApi.create(values as CreateWorkRoleDto);
        message.success('Rol creado');
      }

      setModalVisible(false);
      loadData();
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      setLoading(true);
      await workRoleApi.delete(id);
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
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: 'Área',
      dataIndex: 'workAreaName',
      key: 'workAreaName',
    },
    {
      title: 'Días Libres/mes',
      dataIndex: 'freeDaysPerMonth',
      key: 'freeDaysPerMonth',
      width: 120,
    },
    {
      title: 'Horas Diarias',
      dataIndex: 'dailyHoursTarget',
      key: 'dailyHoursTarget',
      width: 120,
    },
    {
      title: 'Acciones',
      key: 'actions',
      width: 120,
      render: (_text: unknown, record: WorkRoleDto) => (
        <Space>
          <Button
            type="primary"
            size="small"
            icon={<EditOutlined />}
            onClick={() => handleOpenModal(record)}
          />
          <Popconfirm
            title="Eliminar"
            description="¿Estás seguro de eliminar este rol?"
            onConfirm={() => handleDelete(record.id)}
            okText="Sí"
            cancelText="No"
          >
            <Button type="primary" danger size="small" icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ marginBottom: 16 }}>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => handleOpenModal()}
        >
          Nuevo Rol
        </Button>
      </div>

      <Table
        dataSource={roles}
        columns={columns}
        loading={loading}
        rowKey="id"
        pagination={false}
      />

      <Modal
        title={editingRole ? 'Editar Rol' : 'Nuevo Rol'}
        open={modalVisible}
        onOk={() => form.submit()}
        onCancel={() => setModalVisible(false)}
        confirmLoading={loading}
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit}>
          <Form.Item
            label="Nombre del Rol"
            name="name"
            rules={[{ required: true, message: 'Campo obligatorio' }]}
          >
            <Input placeholder="ej: Parrillero, Mesera, Ayudante" />
          </Form.Item>

          <Form.Item
            label="Descripción"
            name="description"
          >
            <Input.TextArea placeholder="Descripción del rol" rows={2} />
          </Form.Item>

          <Form.Item
            label="Área de Trabajo"
            name="workAreaId"
            rules={[{ required: true, message: 'Selecciona un área' }]}
          >
            <Select placeholder="Selecciona un área" showSearch optionFilterProp="label">
              {areas.map((area) => (
                <Select.Option key={area.id} value={area.id} label={area.name}>
                  {area.name}
                </Select.Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item
            label="Días Libres por Mes"
            name="freeDaysPerMonth"
            rules={[{ required: true, message: 'Campo obligatorio' }]}
            initialValue={6}
          >
            <InputNumber min={0} max={31} />
          </Form.Item>

          <Form.Item
            label="Horas Objetivo por Día"
            name="dailyHoursTarget"
            rules={[{ required: true, message: 'Campo obligatorio' }]}
            initialValue={8}
          >
            <InputNumber min={0} max={24} step={0.5} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};
