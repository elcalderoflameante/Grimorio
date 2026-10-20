import { useEffect, useRef, useState } from 'react';
import { App as AntApp, Table, Button, Space, Modal, Form, Input, Switch, Popconfirm, Tag, Tooltip } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, LockOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { roleApi, permissionApi } from '../../services/api';
import type { RoleDto, PermissionDto, CreateRoleDto, UpdateRoleDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import { resolveRolePermissionIds } from '../../utils/rolePermissions';
import { permissionPresentation } from '../../utils/permissionPresentation';
import PermissionPicker from './PermissionPicker';

interface RoleFormValues {
  name: string;
  description: string;
  isActive?: boolean;
}

interface AssignFormValues {
  permissionIds: string[];
}

export default function RoleList() {
  const { message } = AntApp.useApp();

  const { hasPermission } = useAuth();
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [permissions, setPermissions] = useState<PermissionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [assignModalVisible, setAssignModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [assigningRoleId, setAssigningRoleId] = useState<string | null>(null);
  const [assignLoading, setAssignLoading] = useState(false);
  const [assignSaving, setAssignSaving] = useState(false);
  const assignInFlight = useRef(false);
  const [assigningRoleName, setAssigningRoleName] = useState('');
  const [form] = Form.useForm<RoleFormValues>();
  const [assignForm] = Form.useForm<AssignFormValues>();
  const canCreate = hasPermission(PERMISSIONS.admin.rolesCreate);
  const canUpdate = hasPermission(PERMISSIONS.admin.rolesUpdate);
  const canDelete = hasPermission(PERMISSIONS.admin.rolesDelete);

  const loadRoles = async () => {
    setLoading(true);
    try {
      const response = await roleApi.getAll();
      setRoles(Array.isArray(response.data) ? response.data : []);
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  };

  const loadPermissions = async () => {
    try {
      const response = await permissionApi.getAll();
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
        await roleApi.update(editingId, updateData);
        message.success('Rol actualizado');
      } else {
        const createData: CreateRoleDto = {
          name: values.name,
          description: values.description,
        };
        await roleApi.create(createData);
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
    if (!assigningRoleId || assignInFlight.current) return;

    assignInFlight.current = true;
    setAssignSaving(true);
    try {
      const permissionIds = resolveRolePermissionIds(values.permissionIds, permissions);
      await roleApi.assignPermissions(assigningRoleId, permissionIds);
      message.success('Permisos asignados');
      setAssignModalVisible(false);
      assignForm.resetFields();
      setAssigningRoleId(null);
      loadRoles();
    } catch (error: unknown) {
      message.error(formatError(error));
    } finally {
      assignInFlight.current = false;
      setAssignSaving(false);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await roleApi.delete(id);
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

  const handleOpenAssign = async (role: RoleDto) => {
    setAssignLoading(true);
    try {
      const response = await permissionApi.getAll();
      const catalog = response.data;
      if (!Array.isArray(catalog)) throw new Error('No se pudo cargar el listado de permisos.');
      const permissionIds = resolveRolePermissionIds(role.permissions || [], catalog);
      setPermissions(catalog);
      setAssigningRoleId(role.id);
      setAssigningRoleName(role.name);
      assignForm.setFieldsValue({ permissionIds });
      setAssignModalVisible(true);
    } catch (error: unknown) {
      message.error(formatError(error));
    } finally {
      setAssignLoading(false);
    }
  };

  const columns: ColumnsType<RoleDto> = [
    { title: 'Nombre', dataIndex: 'name', key: 'name' },
    { title: 'Descripción', dataIndex: 'description', key: 'description' },
    {
      title: 'Permisos',
      key: 'permissions',
      render: (_, record) => (
        record.permissions && record.permissions.length > 0 ? (
          <Space size={[4, 4]} wrap>{Object.entries(record.permissions.reduce<Record<string, number>>((groups, code) => {
            const permission = permissions.find(p => p.code === code || p.id === code);
            const name = permission ? permissionPresentation(permission).module : 'Otros';
            groups[name] = (groups[name] ?? 0) + 1;
            return groups;
          }, {})).map(([name, count]) => <Tag key={name}>{name}: {count}</Tag>)}</Space>
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
          {canUpdate && <Tooltip title="Editar rol"><Button aria-label={`Editar rol ${record.name}`} icon={<EditOutlined />} onClick={() => handleEdit(record)} /></Tooltip>}
          {canUpdate && <Tooltip title="Asignar permisos"><Button aria-label={`Permisos de ${record.name}`} icon={<LockOutlined />} disabled={assignLoading} onClick={() => handleOpenAssign(record)} /></Tooltip>}
          {canDelete && <Popconfirm
            title="¿Eliminar rol?"
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
          Nuevo Rol
        </Button>}
      </div>

      <Table
        columns={columns}
        dataSource={roles}
        loading={loading}
        rowKey="id"
        scroll={{ x: 760 }}
        pagination={{ defaultPageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
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
        title={`Permisos del rol: ${assigningRoleName}`}
        forceRender
        width={920}
        styles={{ body: { maxHeight: '65vh', overflowY: 'auto', paddingRight: 8 } }}
        okText="Guardar permisos"
        cancelText="Cancelar"
        open={assignModalVisible}
        confirmLoading={assignSaving}
        closable={!assignSaving}
        maskClosable={!assignSaving}
        keyboard={!assignSaving}
        cancelButtonProps={{ disabled: assignSaving }}
        onOk={() => assignForm.submit()}
        onCancel={() => {
          if (assignSaving) return;
          setAssignModalVisible(false);
          setAssigningRoleId(null);
          assignForm.resetFields();
        }}
      >
        <Form form={assignForm} layout="vertical" onFinish={handleAssignPermissions}>
          <Form.Item
            name="permissionIds"
            rules={[{ required: true, message: 'Selecciona al menos un permiso' }]}
          >
            <PermissionPicker
              key={assigningRoleId}
              permissions={permissions}
              disabled={assignSaving}
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
