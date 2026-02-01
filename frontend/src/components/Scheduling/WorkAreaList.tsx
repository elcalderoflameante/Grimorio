import { useCallback, useEffect, useState } from 'react';
import { Table, Button, Modal, Form, Input, InputNumber, Space, message, Popconfirm } from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { workAreaApi } from '../../services/api';
import type { WorkAreaDto, CreateWorkAreaDto, UpdateWorkAreaDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

interface WorkAreaListProps {
  branchId: string;
}

export const WorkAreaList = ({ branchId }: WorkAreaListProps) => {
  const [areas, setAreas] = useState<WorkAreaDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingArea, setEditingArea] = useState<WorkAreaDto | null>(null);
  const [form] = Form.useForm();

  const loadAreas = useCallback(async () => {
    try {
      setLoading(true);
      const response = await workAreaApi.getAll(branchId);
      setAreas(response.data);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [branchId]);

  useEffect(() => {
    loadAreas();
  }, [loadAreas]);

  const handleOpenModal = (area?: WorkAreaDto) => {
    if (area) {
      setEditingArea(area);
      form.setFieldsValue({
        name: area.name,
        description: area.description,
        color: area.color,
        displayOrder: area.displayOrder,
      });
    } else {
      setEditingArea(null);
      form.resetFields();
    }
    setModalVisible(true);
  };

  const handleSubmit = async (values: CreateWorkAreaDto | UpdateWorkAreaDto) => {
    try {
      setLoading(true);
      
      const colorValue = typeof values.color === 'string' ? values.color : values.color;

      if (editingArea) {
        await workAreaApi.update(editingArea.id, {
          name: values.name,
          description: values.description,
          color: colorValue,
          displayOrder: values.displayOrder,
        } as UpdateWorkAreaDto);
        message.success('Área actualizada');
      } else {
        await workAreaApi.create({
          name: values.name,
          description: values.description,
          color: colorValue,
          displayOrder: values.displayOrder,
        } as CreateWorkAreaDto);
        message.success('Área creada');
      }

      setModalVisible(false);
      loadAreas();
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
      await workAreaApi.delete(id);
      message.success('Área eliminada');
      loadAreas();
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const columns = [
    {
      title: 'Área',
      dataIndex: 'name',
      key: 'name',
      render: (text: string, record: WorkAreaDto) => (
        <Space>
          <div
            style={{
              width: 20,
              height: 20,
              borderRadius: 4,
              backgroundColor: record.color,
            }}
          />
          <span>{text}</span>
        </Space>
      ),
    },
    {
      title: 'Descripción',
      dataIndex: 'description',
      key: 'description',
    },
    {
      title: 'Orden',
      dataIndex: 'displayOrder',
      key: 'displayOrder',
      width: 80,
    },
    {
      title: 'Acciones',
      key: 'actions',
      width: 120,
      render: (_text: unknown, record: WorkAreaDto) => (
        <Space>
          <Button
            type="primary"
            size="small"
            icon={<EditOutlined />}
            onClick={() => handleOpenModal(record)}
          />
          <Popconfirm
            title="Eliminar"
            description="¿Estás seguro de eliminar esta área?"
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
          Nueva Área
        </Button>
      </div>

      <Table
        dataSource={areas}
        columns={columns}
        loading={loading}
        rowKey="id"
        pagination={false}
      />

      <Modal
        title={editingArea ? 'Editar Área' : 'Nueva Área'}
        open={modalVisible}
        onOk={() => form.submit()}
        onCancel={() => setModalVisible(false)}
        confirmLoading={loading}
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit}>
          <Form.Item
            label="Nombre del Área"
            name="name"
            rules={[{ required: true, message: 'Campo obligatorio' }]}
          >
            <Input placeholder="ej: Cocina, Caja, Mesas" />
          </Form.Item>

          <Form.Item
            label="Descripción"
            name="description"
          >
            <Input.TextArea placeholder="Descripción del área" rows={3} />
          </Form.Item>

          <Form.Item
            label="Color"
            name="color"
            initialValue="#808080"
          >
            <Input type="color" />
          </Form.Item>

          <Form.Item
            label="Orden de visualización"
            name="displayOrder"
            rules={[{ required: true, message: 'Campo obligatorio' }]}
            initialValue={0}
          >
            <InputNumber min={0} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};
