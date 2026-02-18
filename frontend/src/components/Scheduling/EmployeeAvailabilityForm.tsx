import { useCallback, useEffect, useState } from 'react';
import { Table, Button, Modal, Form, DatePicker, Input, message, Popconfirm, Divider, Radio } from 'antd';
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import { employeeAvailabilityApi } from '../../services/api';
import type { EmployeeAvailabilityDto, EmployeeDto, CreateEmployeeAvailabilityDto } from '../../types';
import dayjs from 'dayjs';
import { formatError } from '../../utils/errorHandler';

interface EmployeeAvailabilityFormProps {
  employee: EmployeeDto;
  onClose: () => void;
}

export const EmployeeAvailabilityForm = ({ employee, onClose }: EmployeeAvailabilityFormProps) => {
  const [availability, setAvailability] = useState<EmployeeAvailabilityDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [dateType, setDateType] = useState<'single' | 'range'>('single');
  const [form] = Form.useForm();

  type AvailabilityFormValues = {
    unavailableDate?: dayjs.Dayjs;
    dateRange?: [dayjs.Dayjs, dayjs.Dayjs];
    reason?: string;
  };

  const loadAvailability = useCallback(async () => {
    try {
      setLoading(true);
      const response = await employeeAvailabilityApi.getByEmployee(employee.id);
      console.log('[EmployeeAvailability] Disponibilidades cargadas:', response.data);
      setAvailability(response.data);
    } catch (error: unknown) {
      message.error(formatError(error));
      console.error('[EmployeeAvailability] Error cargando:', error);
    } finally {
      setLoading(false);
    }
  }, [employee.id]);

  useEffect(() => {
    loadAvailability();
  }, [loadAvailability]);

  const handleOpenModal = () => {
    form.resetFields();
    setDateType('single');
    setModalVisible(true);
  };

  const handleSubmit = async (values: AvailabilityFormValues) => {
    try {
      setLoading(true);
      
      if (dateType === 'single') {
        // Agregar una sola fecha
        if (!values.unavailableDate) {
          message.error('Selecciona una fecha válida.');
          return;
        }
        await employeeAvailabilityApi.add({
          employeeId: employee.id,
          unavailableDate: values.unavailableDate.format('YYYY-MM-DD'),
          reason: values.reason,
        } as CreateEmployeeAvailabilityDto);
        message.success('Fecha de indisponibilidad agregada correctamente');
      } else {
        // Agregar rango de fechas
        const startDate = values.dateRange?.[0];
        const endDate = values.dateRange?.[1];

        if (!startDate || !endDate) {
          message.error('Selecciona un rango de fechas válido.');
          return;
        }
        
        // Generar todas las fechas en el rango
        let currentDate = startDate.clone();
        const addRequests: Array<Promise<unknown>> = [];
        
        while (currentDate.isBefore(endDate) || currentDate.isSame(endDate, 'day')) {
          addRequests.push(
            employeeAvailabilityApi.add({
              employeeId: employee.id,
              unavailableDate: currentDate.format('YYYY-MM-DD'),
              reason: values.reason,
            } as CreateEmployeeAvailabilityDto)
          );
          currentDate = currentDate.add(1, 'day');
        }
        
        await Promise.all(addRequests);
        message.success(`${addRequests.length} fechas de indisponibilidad agregadas correctamente`);
      }
      
      setModalVisible(false);
      loadAvailability();
    } catch (error: unknown) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      setLoading(true);
      console.log('[EmployeeAvailability] Eliminando disponibilidad con ID:', id);
      await employeeAvailabilityApi.remove(employee.id, id);
      message.success('Fecha de indisponibilidad eliminada correctamente');
      loadAvailability();
    } catch (error: unknown) {
      const err = error as { response?: { status?: number }; config?: { url?: string; method?: string } };
      console.error('[EmployeeAvailability] Error eliminando:', {
        id,
        status: err?.response?.status,
        url: err?.config?.url,
        method: err?.config?.method,
      });
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  };

  const columns = [
    {
      title: 'Fecha',
      dataIndex: 'unavailableDate',
      key: 'unavailableDate',
      render: (date: string) => dayjs(date).format('DD/MM/YYYY'),
      width: 120,
    },
    {
      title: 'Razón',
      dataIndex: 'reason',
      key: 'reason',
    },
    {
      title: 'Acciones',
      key: 'actions',
      width: 80,
      render: (_text: unknown, record: EmployeeAvailabilityDto) => (
        <Popconfirm
          title="Eliminar"
          description="¿Estás seguro de eliminar esta fecha?"
          onConfirm={() => handleDelete(record.id)}
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
      <Divider>Disponibilidad de {employee.firstName} {employee.lastName}</Divider>

      <div style={{ marginBottom: 16 }}>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={handleOpenModal}
        >
          Agregar Indisponibilidad
        </Button>
        <Button
          style={{ marginLeft: 8 }}
          onClick={onClose}
        >
          Cerrar
        </Button>
      </div>

      <Table
        dataSource={availability}
        columns={columns}
        loading={loading}
        rowKey="id"
        pagination={false}
        size="small"
      />

      <Modal
        title={`Agregar Indisponibilidad - ${employee.firstName} ${employee.lastName}`}
        open={modalVisible}
        onOk={() => form.submit()}
        onCancel={() => setModalVisible(false)}
        confirmLoading={loading}
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit}>
          <Form.Item label="Tipo de Selección" required>
            <Radio.Group 
              value={dateType} 
              onChange={(e) => {
                setDateType(e.target.value);
                form.resetFields(['unavailableDate', 'dateRange']);
              }}
            >
              <Radio value="single">Fecha Individual</Radio>
              <Radio value="range">Rango de Fechas</Radio>
            </Radio.Group>
          </Form.Item>

          {dateType === 'single' ? (
            <Form.Item
              label="Fecha de Indisponibilidad"
              name="unavailableDate"
              rules={[{ required: true, message: 'Campo obligatorio' }]}
            >
              <DatePicker 
                style={{ width: '100%' }}
                format="DD/MM/YYYY"
                placeholder="Selecciona una fecha"
              />
            </Form.Item>
          ) : (
            <Form.Item
              label="Rango de Fechas"
              name="dateRange"
              rules={[{ required: true, message: 'Campo obligatorio' }]}
            >
              <DatePicker.RangePicker 
                style={{ width: '100%' }}
                format="DD/MM/YYYY"
                placeholder={['Fecha inicio', 'Fecha fin']}
              />
            </Form.Item>
          )}

          <Form.Item
            label="Razón"
            name="reason"
          >
            <Input.TextArea 
              placeholder="ej: Enfermedad, Vacaciones, Permiso, etc."
              rows={2}
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};
