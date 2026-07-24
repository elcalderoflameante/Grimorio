import { useEffect, useState } from 'react';
import { App as AntApp, Table, Button, Space, Modal, Form, Input, Switch, Select, Popconfirm, Tag } from 'antd';
import { KeyOutlined, PlusOutlined, EditOutlined, DeleteOutlined, LockOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { userApi, roleApi } from '../../services/api';
import type { UserDto, RoleDto, CreateUserDto, UpdateUserDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
interface UserFormValues {
  firstName: string;
  lastName: string;
  email: string;
  password?: string;
  isActive?: boolean;
}

interface AssignFormValues {
  roleIds: string[];
}

interface KdsPinFormValues {
  pin?: string;
}

export default function UserList() {
  const { message } = AntApp.useApp();

  const { hasPermission } = useAuth();
  const [users, setUsers] = useState<UserDto[]>([]);
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [assignModalVisible, setAssignModalVisible] = useState(false);
  const [kdsPinModalVisible, setKdsPinModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [assigningUserId, setAssigningUserId] = useState<string | null>(null);
  const [kdsPinUser, setKdsPinUser] = useState<UserDto | null>(null);
  const [form] = Form.useForm<UserFormValues>();
  const [assignForm] = Form.useForm<AssignFormValues>();
  const [kdsPinForm] = Form.useForm<KdsPinFormValues>();
  const canCreate = hasPermission(PERMISSIONS.admin.usersCreate);
  const canUpdate = hasPermission(PERMISSIONS.admin.usersUpdate);
  const canDelete = hasPermission(PERMISSIONS.admin.usersDelete);

  const loadUsers = async () => {
    setLoading(true);
    try {
      const response = await userApi.getAll();
      setUsers(Array.isArray(response.data) ? response.data : []);
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  };

  const loadRoles = async () => {
    try {
      const response = await roleApi.getAll();
      setRoles(Array.isArray(response.data) ? response.data : []);
    } catch (error) {
      message.error(formatError(error));
    }
  };

  useEffect(() => {
    loadUsers();
    loadRoles();
  }, []);

  const handleSave = async (values: UserFormValues) => {
    try {
      if (editingId) {
        const updateData: UpdateUserDto = {
          firstName: values.firstName,
          lastName: values.lastName,
          isActive: values.isActive ?? true,
        };
        await userApi.update(editingId, updateData);
        message.success('Usuario actualizado');
      } else {
        const createData: CreateUserDto = {
          firstName: values.firstName,
          lastName: values.lastName,
          email: values.email,
          password: values.password || '',
        };
        await userApi.create(createData);
        message.success('Usuario creado');
      }

      setModalVisible(false);
      form.resetFields();
      setEditingId(null);
      loadUsers();
    } catch (error: unknown) {
      message.error(formatError(error));
    }
  };

  const handleAssignRoles = async (values: AssignFormValues) => {
    if (!assigningUserId) return;
    
    try {
      console.log('Asignando roles:', { userId: assigningUserId, roleIds: values.roleIds });
      await userApi.assignRoles(assigningUserId, values.roleIds);
      message.success('Roles asignados');
      setAssignModalVisible(false);
      assignForm.resetFields();
      setAssigningUserId(null);
      loadUsers();
    } catch (error: unknown) {
      console.error('Error al asignar roles:', error);
      message.error(formatError(error));
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await userApi.delete(id);
      message.success('Usuario eliminado');
      loadUsers();
    } catch (error) {
      message.error(formatError(error));
    }
  };

  const handleSetKdsPin = async (values: KdsPinFormValues) => {
    if (!kdsPinUser) return;

    try {
      await userApi.setKdsPin(kdsPinUser.id, values.pin?.trim());
      message.success(values.pin ? 'PIN de acceso actualizado' : 'PIN de acceso eliminado');
      setKdsPinModalVisible(false);
      setKdsPinUser(null);
      kdsPinForm.resetFields();
      loadUsers();
    } catch (error) {
      message.error(formatError(error));
    }
  };

  const handleOpenKdsPin = (user: UserDto) => {
    setKdsPinUser(user);
    kdsPinForm.resetFields();
    setKdsPinModalVisible(true);
  };

  const handleEdit = (user: UserDto) => {
    setEditingId(user.id);
    form.setFieldsValue({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      isActive: user.isActive,
    });
    setModalVisible(true);
  };

  const handleOpenAssign = (user: UserDto) => {
    setAssigningUserId(user.id);
    // Usar roleDetails si está disponible, sino roles vacíos
    const roleIds = user.roleDetails ? user.roleDetails.map(r => r.roleId) : [];
    assignForm.setFieldsValue({
      roleIds: roleIds,
    });
    setAssignModalVisible(true);
  };

  const columns: ColumnsType<UserDto> = [
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
      title: 'Roles',
      key: 'roles',
      render: (_, record) => (
        record.roles && record.roles.length > 0 ? (
          record.roles.map(r => <Tag key={r}>{r}</Tag>)
        ) : (
          <span style={{ color: '#ccc' }}>Sin roles</span>
        )
      ),
    },
    {
      title: 'PIN de acceso',
      dataIndex: 'hasKdsPin',
      key: 'hasKdsPin',
      render: (hasKdsPin: boolean) => (
        hasKdsPin ? <Tag color="green">Configurado</Tag> : <Tag>Sin PIN</Tag>
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
          {canUpdate && <Button icon={<EditOutlined />} onClick={() => handleEdit(record)} />}
          {canUpdate && <Button icon={<LockOutlined />} onClick={() => handleOpenAssign(record)} title="Asignar roles" />}
          {canUpdate && <Button icon={<KeyOutlined />} onClick={() => handleOpenKdsPin(record)} title="PIN de acceso" />}
          {canDelete && <Popconfirm
            title="¿Eliminar usuario?"
            onConfirm={() => handleDelete(record.id)}
            okText="Sí"
            cancelText="No"
          >
            <Button icon={<DeleteOutlined />} danger />
          </Popconfirm>}
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ marginBottom: 16 }}>
        {canCreate && <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => {
            setEditingId(null);
            form.resetFields();
            setModalVisible(true);
          }}
        >
          Nuevo Usuario
        </Button>}
      </div>

      <Table
        columns={columns}
        dataSource={users}
        loading={loading}
        rowKey="id"
        pagination={{ defaultPageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
      />

      <Modal
        title={editingId ? 'Editar Usuario' : 'Nuevo Usuario'}
        open={modalVisible}
        onOk={() => form.submit()}
        onCancel={() => setModalVisible(false)}
      >
        <Form form={form} layout="vertical" onFinish={handleSave}>
          <Form.Item
            label="Nombre"
            name="firstName"
            rules={[{ required: true }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            label="Apellido"
            name="lastName"
            rules={[{ required: true }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            label="Email"
            name="email"
            rules={[{ required: true, type: 'email' }]}
          >
            <Input disabled={!!editingId} />
          </Form.Item>

          {!editingId && (
            <Form.Item
              label="Contraseña"
              name="password"
              rules={[{ required: true }]}
            >
              <Input.Password />
            </Form.Item>
          )}

          {editingId && (
            <Form.Item label="Activo" name="isActive" valuePropName="checked">
              <Switch />
            </Form.Item>
          )}
        </Form>
      </Modal>

      <Modal
        title={`PIN de acceso${kdsPinUser ? ` - ${kdsPinUser.firstName} ${kdsPinUser.lastName}` : ''}`}
        open={kdsPinModalVisible}
        onOk={() => kdsPinForm.submit()}
        onCancel={() => {
          setKdsPinModalVisible(false);
          setKdsPinUser(null);
          kdsPinForm.resetFields();
        }}
        footer={[
          <Button
            key="clear"
            danger
            disabled={!kdsPinUser?.hasKdsPin}
            onClick={() => handleSetKdsPin({ pin: undefined })}
          >
            Quitar PIN
          </Button>,
          <Button
            key="cancel"
            onClick={() => {
              setKdsPinModalVisible(false);
              setKdsPinUser(null);
              kdsPinForm.resetFields();
            }}
          >
            Cancelar
          </Button>,
          <Button key="save" type="primary" onClick={() => kdsPinForm.submit()}>
            Guardar
          </Button>,
        ]}
      >
        <Form form={kdsPinForm} layout="vertical" onFinish={handleSetKdsPin}>
          <Form.Item
            label="PIN de 4 dígitos"
            name="pin"
            rules={[
              { required: true, message: 'Ingresa un PIN de 4 dígitos' },
              { pattern: /^\d{4}$/, message: 'El PIN debe tener exactamente 4 dígitos' },
            ]}
          >
            <Input.Password maxLength={4} inputMode="numeric" placeholder="0000" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Asignar Roles"
        open={assignModalVisible}
        onOk={() => assignForm.submit()}
        onCancel={() => {
          setAssignModalVisible(false);
          setAssigningUserId(null);
          assignForm.resetFields();
        }}
      >
        <Form form={assignForm} layout="vertical" onFinish={handleAssignRoles}>
          <Form.Item
            label="Roles"
            name="roleIds"
            rules={[{ required: true, message: 'Selecciona al menos un rol' }]}
          >
            <Select
              mode="multiple"
              placeholder="Selecciona roles"
              showSearch
              optionFilterProp="label"
              options={roles.map(r => ({
                label: r.name,
                value: r.id,
              }))}
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
