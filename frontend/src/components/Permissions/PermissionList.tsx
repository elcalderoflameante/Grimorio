import { useEffect, useState } from 'react';
import { Table, Button, Space, Modal, Form, Input, Switch, message, Popconfirm } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { permissionApi } from '../../services/api';
import type { PermissionDto, CreatePermissionDto, UpdatePermissionDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

interface PermissionFormValues {
  code: string;
  description: string;
  isActive?: boolean;
}

export default function PermissionList() {
  const { hasPermission } = useAuth();
  const [permissions, setPermissions] = useState<PermissionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form] = Form.useForm<PermissionFormValues>();
  const canManage = hasPermission(PERMISSIONS.admin.permissionsManage);

  const loadPermissions = async () => {
    setLoading(true);
    try {
      const response = await permissionApi.getAll();
      setPermissions(Array.isArray(response.data) ? response.data : []);
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadPermissions();
  }, []);

  const handleSave = async (values: PermissionFormValues) => {
    try {
      if (editingId) {
        const updateData: UpdatePermissionDto = {
          description: values.description,
          isActive: values.isActive ?? true,
        };
        await permissionApi.update(editingId, updateData);
        message.success('Permiso actualizado');
      } else {
        const createData: CreatePermissionDto = {
          code: values.code,
          description: values.description,
        };
        await permissionApi.create(createData);
        message.success('Permiso creado');
      }

      setModalVisible(false);
      form.resetFields();
      setEditingId(null);
      loadPermissions();
    } catch (error: unknown) {
      message.error(formatError(error));
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await permissionApi.delete(id);
      message.success('Permiso eliminado');
      loadPermissions();
    } catch (error) {
      message.error(formatError(error));
    }
  };

  const handleEdit = (permission: PermissionDto) => {
    setEditingId(permission.id);
    form.setFieldsValue({
      code: permission.code,
      description: permission.description,
      isActive: permission.isActive,
    });
    setModalVisible(true);
  };

  const columns: ColumnsType<PermissionDto> = [
    { title: 'Código', dataIndex: 'code', key: 'code' },
    { title: 'Descripción', dataIndex: 'description', key: 'description' },
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
          {canManage && <Button icon={<EditOutlined />} onClick={() => handleEdit(record)} />}
          {canManage && <Popconfirm
            title="¿Eliminar permiso?"
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
        {canManage && <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => {
            setEditingId(null);
            form.resetFields();
            setModalVisible(true);
          }}
        >
          Nuevo Permiso
        </Button>}
      </div>

      <Table
        columns={columns}
        dataSource={permissions}
        loading={loading}
        rowKey="id"
        pagination={{ defaultPageSize: 15, showSizeChanger: true, pageSizeOptions: ['15', '30', '60'] }}
      />

      <Modal
        title={editingId ? 'Editar Permiso' : 'Nuevo Permiso'}
        open={modalVisible}
        onOk={() => form.submit()}
        onCancel={() => setModalVisible(false)}
      >
        <Form form={form} layout="vertical" onFinish={handleSave}>
          <Form.Item
            label="Código"
            name="code"
            rules={[{ required: true, message: 'El código es requerido' }]}
          >
            <Input disabled={!!editingId} placeholder="ej: Admin.Users.Update" />
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
    </div>
  );
}
