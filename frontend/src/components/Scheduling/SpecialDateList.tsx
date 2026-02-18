import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { Button, Form, Input, Modal, Table, Space, Popconfirm, message, DatePicker, Card, Empty } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import dayjs, { Dayjs } from 'dayjs';
import type { ColumnsType } from 'antd/es/table';
import type { SpecialDateDto, CreateSpecialDateDto, UpdateSpecialDateDto } from '../../types/SpecialDate';
import { specialDateApi } from '../../services/specialDateApi';

interface SpecialDateListProps {
  branchId: string;
}

interface SpecialDateFormValues {
  date: Dayjs;
  name: string;
  description?: string;
}

export const SpecialDateList: React.FC<SpecialDateListProps> = ({ branchId }) => {
  const [form] = Form.useForm<SpecialDateFormValues>();
  const [loading, setLoading] = useState(false);
  const [specialDates, setSpecialDates] = useState<SpecialDateDto[]>([]);
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [editingDate, setEditingDate] = useState<SpecialDateDto | null>(null);

  const loadSpecialDates = useCallback(async () => {
    if (!branchId) return;
    
    try {
      setLoading(true);
      const response = await specialDateApi.getAll(branchId);
      setSpecialDates(response.data || []);
    } catch (error) {
      message.error('Error al cargar los días especiales');
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [branchId]);

  useEffect(() => {
    loadSpecialDates();
  }, [branchId, loadSpecialDates]);

  const handleOpenModal = useCallback((date?: SpecialDateDto) => {
    if (date) {
      setEditingDate(date);
      form.setFieldsValue({
        date: dayjs(date.date),
        name: date.name,
        description: date.description,
      });
    } else {
      setEditingDate(null);
      form.resetFields();
    }
    setIsModalVisible(true);
  }, [form]);

  const handleCloseModal = () => {
    setIsModalVisible(false);
    setEditingDate(null);
    form.resetFields();
  };

  const handleSubmit = async (values: SpecialDateFormValues) => {
    if (!branchId) {
      message.error('Debe seleccionar una sucursal');
      return;
    }

    try {
      setLoading(true);
      const payload = {
        branchId,
        date: values.date.toISOString(),
        name: values.name.trim(),
        description: values.description?.trim(),
      };

      if (editingDate) {
        await specialDateApi.update(editingDate.id, payload as UpdateSpecialDateDto);
        message.success('Día especial actualizado');
      } else {
        await specialDateApi.create(payload as CreateSpecialDateDto);
        message.success('Día especial creado');
      }

      handleCloseModal();
      await loadSpecialDates();
    } catch (error: unknown) {
      const axiosError = error as { response?: { status: number } };
      if (axiosError.response?.status === 409) {
        message.error('Ya existe un día especial en esta fecha para esta sucursal');
      } else {
        message.error('Error al guardar el día especial');
      }
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = useCallback(async (id: string) => {
    try {
      setLoading(true);
      await specialDateApi.delete(id);
      message.success('Día especial eliminado');
      await loadSpecialDates();
    } catch (error) {
      message.error('Error al eliminar el día especial');
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [loadSpecialDates]);

  const columns: ColumnsType<SpecialDateDto> = useMemo(() => [
    {
      title: 'Fecha',
      key: 'date',
      render: (_, record) => dayjs(record.date).format('DD/MM/YYYY'),
      sorter: (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime(),
    },
    {
      title: 'Nombre',
      dataIndex: 'name',
      key: 'name',
      sorter: (a, b) => a.name.localeCompare(b.name),
    },
    {
      title: 'Descripción',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: 'Acciones',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <Button icon={<EditOutlined />} onClick={() => handleOpenModal(record)} />
          <Popconfirm
            title="¿Eliminar día especial?"
            description={`Se eliminará ${record.name} del ${dayjs(record.date).format('DD/MM/YYYY')}`}
            onConfirm={() => handleDelete(record.id)}
            okText="Sí"
            cancelText="No"
          >
            <Button icon={<DeleteOutlined />} danger />
          </Popconfirm>
        </Space>
      ),
      width: 120,
    },
  ], [handleDelete, handleOpenModal]);

  if (!branchId) {
    return <Empty description="Debe seleccionar una sucursal" />;
  }

  return (
    <Card title="Días Especiales" size="small">
      <div style={{ marginBottom: 16 }}>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => handleOpenModal()}
          loading={loading}
        >
          Nuevo Día Especial
        </Button>
      </div>

      <Table
        columns={columns}
        dataSource={specialDates}
        rowKey="id"
        loading={loading}
        pagination={{ pageSize: 20 }}
        locale={{ emptyText: 'No hay días especiales' }}
      />

      <Modal
        title={editingDate ? 'Editar Día Especial' : 'Nuevo Día Especial'}
        open={isModalVisible}
        onCancel={handleCloseModal}
        footer={null}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
        >
          <Form.Item
            label="Fecha"
            name="date"
            rules={[{ required: true, message: 'La fecha es requerida' }]}
          >
            <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item
            label="Nombre (ej: Valentine, Carnaval, Navidad)"
            name="name"
            rules={[
              { required: true, message: 'El nombre es requerido' },
              { max: 100, message: 'Máximo 100 caracteres' },
            ]}
          >
            <Input placeholder="Valentine" />
          </Form.Item>

          <Form.Item
            label="Descripción (opcional)"
            name="description"
            rules={[{ max: 500, message: 'Máximo 500 caracteres' }]}
          >
            <Input.TextArea
              placeholder="Descripción del día especial..."
              rows={3}
            />
          </Form.Item>

          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit" loading={loading}>
                {editingDate ? 'Actualizar' : 'Crear'}
              </Button>
              <Button onClick={handleCloseModal}>Cancelar</Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
};

export default SpecialDateList;
