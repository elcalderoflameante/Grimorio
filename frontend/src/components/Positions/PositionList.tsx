import { useEffect, useState } from 'react';
import { Table, Button, Space, Modal, Form, Input, Switch, message, Popconfirm } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { positionService } from '../../services/api';
import type { PositionDto, CreatePositionDto, UpdatePositionDto } from '../../types';

interface PositionFormValues {
  name: string;
  description: string;
  isActive?: boolean;
}

export default function PositionList() {
  const [positions, setPositions] = useState<PositionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form] = Form.useForm<PositionFormValues>();

  // Cargar posiciones
  const loadPositions = async () => {
    setLoading(true);
    try {
      const response = await positionService.getAll();
      const data = response.data as any;
      const items = Array.isArray(data?.items) ? data.items : Array.isArray(data) ? data : [];
      setPositions(items);
    } catch (error) {
      message.error('Error al cargar posiciones');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadPositions();
  }, []);

  // Crear o actualizar
  const handleSave = async (values: PositionFormValues) => {
    try {
      if (editingId) {
        const updateData: UpdatePositionDto = {
          name: values.name,
          description: values.description,
          isActive: values.isActive ?? true,
        };
        await positionService.update(editingId, updateData);
        message.success('Posición actualizada');
      } else {
        const createData: CreatePositionDto = {
          name: values.name,
          description: values.description,
        };
        await positionService.create(createData);
        message.success('Posición creada');
      }

      setModalVisible(false);
      form.resetFields();
      setEditingId(null);
      loadPositions();
    } catch (error: any) {
      message.error(error.response?.data?.message || 'Error al guardar');
    }
  };

  // Eliminar
  const handleDelete = async (id: string) => {
    try {
      await positionService.delete(id);
      message.success('Posición eliminada');
      loadPositions();
    } catch (error) {
      message.error('Error al eliminar');
    }
  };

  // Editar
  const handleEdit = (position: PositionDto) => {
    setEditingId(position.id);
    form.setFieldsValue({
      name: position.name,
      description: position.description,
      isActive: position.isActive,
    });
    setModalVisible(true);
  };

  // Columnas de la tabla
  const columns: ColumnsType<PositionDto> = [
    {
      title: 'Nombre',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: 'Descripción',
      dataIndex: 'description',
      key: 'description',
    },
    {
      title: 'Activa',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (isActive: boolean) => isActive ? '✓' : '✗',
    },
    {
      title: 'Acciones',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <Button
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
          />
          <Popconfirm
            title="¿Eliminar posición?"
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
          Nueva Posición
        </Button>
      </div>

      <Table
        columns={columns}
        dataSource={positions}
        loading={loading}
        rowKey="id"
        pagination={false}
      />

      {/* Modal crear/editar */}
      <Modal
        title={editingId ? 'Editar Posición' : 'Nueva Posición'}
        open={modalVisible}
        onOk={() => form.submit()}
        onCancel={() => setModalVisible(false)}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSave}
        >
          <Form.Item
            label="Nombre"
            name="name"
            rules={[{ required: true, message: 'El nombre es requerido' }]}
          >
            <Input placeholder="ej: Mesero, Chef, Barista" />
          </Form.Item>

          <Form.Item
            label="Descripción"
            name="description"
            rules={[{ required: true, message: 'La descripción es requerida' }]}
          >
            <Input.TextArea rows={3} placeholder="Descripción del puesto" />
          </Form.Item>

          {editingId && (
            <Form.Item
              label="Activa"
              name="isActive"
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  );
}
