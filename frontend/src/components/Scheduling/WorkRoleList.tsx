import { useCallback, useEffect, useState } from 'react';
import { App as AntApp, Table, Button, Modal, Form, Input, Select, Space, Popconfirm } from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { workRoleApi, workAreaApi } from '../../services/api';
import type { WorkRoleDto, WorkAreaDto, CreateWorkRoleDto, UpdateWorkRoleDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

interface WorkRoleListProps {
  branchId: string;
}

export const WorkRoleList = ({ branchId }: WorkRoleListProps) => {
  const { message } = AntApp.useApp();

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
        await workRoleApi.update(editingRole.id, {
          ...values,
          id: editingRole.id,
        } as UpdateWorkRoleDto);
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
      title: 'Area',
      dataIndex: 'workAreaName',
      key: 'workAreaName',
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
            description="Estas seguro de eliminar este rol?"
            onConfirm={() => handleDelete(record.id)}
            okText="Si"
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
        pagination={{ defaultPageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
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
            label="Descripcion"
            name="description"
          >
            <Input.TextArea placeholder="Descripcion del rol" rows={2} />
          </Form.Item>

          <Form.Item
            label="Area de Trabajo"
            name="workAreaId"
            rules={[{ required: true, message: 'Selecciona un area' }]}
          >
            <Select placeholder="Selecciona un area" showSearch optionFilterProp="label">
              {areas.map((area) => (
                <Select.Option key={area.id} value={area.id} label={area.name}>
                  {area.name}
                </Select.Option>
              ))}
            </Select>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};
