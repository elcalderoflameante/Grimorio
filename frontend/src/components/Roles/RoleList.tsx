import { useEffect, useState } from 'react';
import { Table, Button, Space, Modal, Form, Input, Switch, Select, message, Popconfirm, Tag } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, LockOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { roleService, permissionService } from '../../services/api';
import type { RoleDto, PermissionDto, CreateRoleDto, UpdateRoleDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

interface RoleFormValues {
  name: string;
  description: string;
  isActive?: boolean;
}

interface AssignFormValues {
  permissionIds: string[];
}

export default function RoleList() {
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [permissions, setPermissions] = useState<PermissionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [assignModalVisible, setAssignModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [assigningRoleId, setAssigningRoleId] = useState<string | null>(null);
  const [form] = Form.useForm<RoleFormValues>();
  const [assignForm] = Form.useForm<AssignFormValues>();

  const loadRoles = async () => {
    setLoading(true);
    try {
      const response = await roleService.getAll();
      setRoles(Array.isArray(response.data) ? response.data : []);
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  };

  const loadPermissions = async () => {
    try {
      const response = await permissionService.getAll();
      setPermissions(Array.isArray(response.data) ? response.data : []);
    } catch (error) {
      message.error(formatError(error));
    }
  };

  useEffect(() => {
    loadRoles();
    loadPermissions();
  }, []);

  const handleSave = async (values: RoleFormValues) => {
    try {
      if (editingId) {
        const updateData: UpdateRoleDto = {
          name: values.name,
          description: values.description,
          isActive: values.isActive ?? true,
        };
        await roleService.update(editingId, updateData);
        message.success('Rol actualizado');
      } else {
        const createData: CreateRoleDto = {
          name: values.name,
          description: values.description,
        };
        await roleService.create(createData);
        message.success('Rol creado');
      }

      setModalVisible(false);
      form.resetFields();
      setEditingId(null);
      loadRoles();
    } catch (error: unknown) {
      message.error(formatError(error));
    }
  };

  const handleAssignPermissions = async (values: AssignFormValues) => {
    if (!assigningRoleId) return;

    try {
      await roleService.assignPermissions(assigningRoleId, values.permissionIds);
      message.success('Permisos asignados');
      setAssignModalVisible(false);
      assignForm.resetFields();
      setAssigningRoleId(null);
      loadRoles();
    } catch (error: unknown) {
      message.error(formatError(error));
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await roleService.delete(id);
      message.success('Rol eliminado');
      loadRoles();
    } catch (error) {
      message.error(formatError(error));
    }
  };

  const handleEdit = (role: RoleDto) => {
    setEditingId(role.id);
    form.setFieldsValue({
      name: role.name,
      description: role.description,
      isActive: role.isActive,
    });
    setModalVisible(true);
  };

  const handleOpenAssign = (role: RoleDto) => {
    setAssigningRoleId(role.id);
    assignForm.setFieldsValue({
      permissionIds: role.permissions || [],
    });
    setAssignModalVisible(true);
  };

  const columns: ColumnsType<RoleDto> = [
    { title: 'Nombre', dataIndex: 'name', key: 'name' },
    { title: 'Descripción', dataIndex: 'description', key: 'description' },
    {
      title: 'Permisos',
      key: 'permissions',
      render: (_, record) => (
        record.permissions && record.permissions.length > 0 ? (
          record.permissions.map(p => <Tag key={p}>{p}</Tag>)
        ) : (
          <span style={{ color: '#ccc' }}>Sin permisos</span>
        )
      ),
    },
    {
      title: 'Activo',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (isActive: boolean) => (isActive ? '✓' : '✗'),
    },
    {
      title: 'Acciones',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <Button icon={<EditOutlined />} onClick={() => handleEdit(record)} />
          <Button icon={<LockOutlined />} onClick={() => handleOpenAssign(record)} title="Asignar permisos" />
          <Popconfirm
            title="¿Eliminar rol?"
            onConfirm={() => handleDelete(record.id)}
            okText="Sí"
            cancelText="No"
          >
            <Button icon={<DeleteOutlined />} danger />
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
          onClick={() => {
            setEditingId(null);
            form.resetFields();
            setModalVisible(true);
          }}
        >
          Nuevo Rol
        </Button>
      </div>

      <Table
        columns={columns}
        dataSource={roles}
        loading={loading}
        rowKey="id"
        pagination={false}
      />

      <Modal
        title={editingId ? 'Editar Rol' : 'Nuevo Rol'}
        open={modalVisible}
        onOk={() => form.submit()}
        onCancel={() => setModalVisible(false)}
      >
        <Form form={form} layout="vertical" onFinish={handleSave}>
          <Form.Item
            label="Nombre"
            name="name"
            rules={[{ required: true, message: 'El nombre es requerido' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            label="Descripción"
            name="description"
            rules={[{ required: true, message: 'La descripción es requerida' }]}
          >
            <Input.TextArea rows={3} />
          </Form.Item>

          {editingId && (
            <Form.Item label="Activo" name="isActive" valuePropName="checked">
              <Switch />
            </Form.Item>
          )}
        </Form>
      </Modal>

      <Modal
        title="Asignar Permisos"
        open={assignModalVisible}
        onOk={() => assignForm.submit()}
        onCancel={() => {
          setAssignModalVisible(false);
          setAssigningRoleId(null);
          assignForm.resetFields();
        }}
      >
        <Form form={assignForm} layout="vertical" onFinish={handleAssignPermissions}>
          <Form.Item
            label="Permisos"
            name="permissionIds"
            rules={[{ required: true, message: 'Selecciona al menos un permiso' }]}
          >
            <Select
              mode="multiple"
              placeholder="Selecciona permisos"
              showSearch
              optionFilterProp="label"
              options={permissions.map(p => ({
                label: `${p.code} - ${p.description}`,
                value: p.id,
              }))}
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
