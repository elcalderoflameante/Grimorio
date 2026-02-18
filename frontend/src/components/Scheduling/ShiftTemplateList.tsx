import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Form, Input, InputNumber, Modal, Select, Space, Table, TimePicker, message, Popconfirm, Row, Col, Collapse, Empty } from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import dayjs, { Dayjs } from 'dayjs';
import type { ColumnsType } from 'antd/es/table';
import { shiftTemplateApi, workAreaApi, workRoleApi } from '../../services/api';
import type {
  ShiftTemplateDto,
  CreateShiftTemplateDto,
  UpdateShiftTemplateDto,
  WorkAreaDto,
  WorkRoleDto,
} from '../../types';
import { formatError } from '../../utils/errorHandler';

interface ShiftTemplateListProps {
  branchId: string;
}

interface ShiftTemplateFormValues {
  dayOfWeek: number;
  startTime: Dayjs;
  endTime: Dayjs;
  breakMinutes?: number;
  lunchMinutes?: number;
  workAreaId: string;
  workRoleId: string;
  requiredCount: number;
  notes?: string;
}

const dayOptions = [
  { value: 0, label: 'Domingo' },
  { value: 1, label: 'Lunes' },
  { value: 2, label: 'Martes' },
  { value: 3, label: 'Miércoles' },
  { value: 4, label: 'Jueves' },
  { value: 5, label: 'Viernes' },
  { value: 6, label: 'Sábado' },
];

const timeFormat = 'HH:mm';

const timeSpanToMinutes = (value?: string) => {
  if (!value) return undefined;
  const parts = value.split(':').map(Number);
  if (parts.length < 2) return undefined;
  const hours = parts[0] || 0;
  const minutes = parts[1] || 0;
  return hours * 60 + minutes;
};

const minutesToTimeSpan = (value?: number) => {
  if (!value || value <= 0) return undefined;
  const hours = Math.floor(value / 60);
  const minutes = value % 60;
  return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:00`;
};

const parseTime = (value: string) => dayjs(`1970-01-01T${value}`);

export const ShiftTemplateList = ({ branchId }: ShiftTemplateListProps) => {
  const [templates, setTemplates] = useState<ShiftTemplateDto[]>([]);
  const [workAreas, setWorkAreas] = useState<WorkAreaDto[]>([]);
  const [workRoles, setWorkRoles] = useState<WorkRoleDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingTemplate, setEditingTemplate] = useState<ShiftTemplateDto | null>(null);
  const [form] = Form.useForm<ShiftTemplateFormValues>();

  const loadTemplates = useCallback(async () => {
    try {
      setLoading(true);
      const response = await shiftTemplateApi.getAll(branchId);
      setTemplates(Array.isArray(response.data) ? response.data : []);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [branchId]);

  const loadWorkAreas = useCallback(async () => {
    try {
      const response = await workAreaApi.getAll(branchId);
      setWorkAreas(Array.isArray(response.data) ? response.data : []);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    }
  }, [branchId]);

  const loadWorkRoles = useCallback(async (workAreaId?: string) => {
    if (!workAreaId) {
      setWorkRoles([]);
      return;
    }
    try {
      const response = await workRoleApi.getAll(workAreaId);
      setWorkRoles(Array.isArray(response.data) ? response.data : []);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    }
  }, []);

  useEffect(() => {
    loadTemplates();
    loadWorkAreas();
  }, [loadTemplates, loadWorkAreas]);

  const handleOpenModal = (template?: ShiftTemplateDto) => {
    if (template) {
      setEditingTemplate(template);
      form.setFieldsValue({
        dayOfWeek: template.dayOfWeek,
        startTime: parseTime(template.startTime),
        endTime: parseTime(template.endTime),
        breakMinutes: timeSpanToMinutes(template.breakDuration),
        lunchMinutes: timeSpanToMinutes(template.lunchDuration),
        workAreaId: template.workAreaId,
        workRoleId: template.workRoleId,
        requiredCount: template.requiredCount,
        notes: template.notes,
      });
      loadWorkRoles(template.workAreaId);
    } else {
      setEditingTemplate(null);
      form.resetFields();
      setWorkRoles([]);
    }
    setModalVisible(true);
  };

  const handleWorkAreaChange = (workAreaId: string) => {
    form.setFieldValue('workRoleId', undefined);
    loadWorkRoles(workAreaId);
  };

  const handleSubmit = async (values: ShiftTemplateFormValues) => {
    try {
      setLoading(true);
      const basePayload = {
        dayOfWeek: values.dayOfWeek,
        startTime: values.startTime.format('HH:mm:ss'),
        endTime: values.endTime.format('HH:mm:ss'),
        breakDuration: minutesToTimeSpan(values.breakMinutes),
        lunchDuration: minutesToTimeSpan(values.lunchMinutes),
        requiredCount: values.requiredCount,
        notes: values.notes,
      };

      if (editingTemplate) {
        const updateData: UpdateShiftTemplateDto = {
          ...basePayload,
        };
        await shiftTemplateApi.update(editingTemplate.id, updateData);
        message.success('Plantilla actualizada');
      } else {
        const createData: CreateShiftTemplateDto = {
          ...basePayload,
          workAreaId: values.workAreaId,
          workRoleId: values.workRoleId,
        };
        await shiftTemplateApi.create(createData);
        message.success('Plantilla creada');
      }

      setModalVisible(false);
      form.resetFields();
      setEditingTemplate(null);
      loadTemplates();
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
      await shiftTemplateApi.delete(id);
      message.success('Plantilla eliminada');
      loadTemplates();
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const columns: ColumnsType<ShiftTemplateDto> = useMemo(() => [
    {
      title: 'Día',
      dataIndex: 'dayOfWeek',
      key: 'dayOfWeek',
      render: (value: number) => dayOptions.find(d => d.value === value)?.label || value,
    },
    {
      title: 'Horario',
      key: 'time',
      render: (_, record) => `${record.startTime} - ${record.endTime}`,
    },
    {
      title: 'Área',
      dataIndex: 'workAreaName',
      key: 'workAreaName',
    },
    {
      title: 'Rol',
      dataIndex: 'workRoleName',
      key: 'workRoleName',
    },
    {
      title: 'Cantidad',
      dataIndex: 'requiredCount',
      key: 'requiredCount',
    },
    {
      title: 'Descanso (min)',
      key: 'breakDuration',
      render: (_, record) => timeSpanToMinutes(record.breakDuration) ?? '-'
    },
    {
      title: 'Almuerzo (min)',
      key: 'lunchDuration',
      render: (_, record) => timeSpanToMinutes(record.lunchDuration) ?? '-'
    },
    {
      title: 'Notas',
      dataIndex: 'notes',
      key: 'notes',
      ellipsis: true,
    },
    {
      title: 'Acciones',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <Button icon={<EditOutlined />} onClick={() => handleOpenModal(record)} />
          <Popconfirm
            title="¿Eliminar plantilla?"
            onConfirm={() => handleDelete(record.id)}
            okText="Sí"
            cancelText="No"
          >
            <Button icon={<DeleteOutlined />} danger />
          </Popconfirm>
        </Space>
      ),
    },
  ], [loadTemplates]);

  const groupedTemplates = useMemo(() => {
    return dayOptions.map((day) => {
      const dayTemplates = templates.filter(t => t.dayOfWeek === day.value);
      return {
        key: String(day.value),
        label: `${day.label} (${dayTemplates.length})`,
        templates: dayTemplates,
      };
    });
  }, [templates]);

  const columnsWithoutDay = useMemo(() => columns.filter(col => col.key !== 'dayOfWeek'), [columns]);

  return (
    <div>
      <div style={{ marginBottom: 16 }}>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => handleOpenModal()}>
          Nueva Plantilla
        </Button>
      </div>

      <Collapse
        accordion
        items={groupedTemplates.map((group) => ({
          key: group.key,
          label: group.label,
          children: group.templates.length > 0 ? (
            <Table
              columns={columnsWithoutDay}
              dataSource={group.templates}
              rowKey="id"
              loading={loading}
              pagination={false}
              size="small"
            />
          ) : (
            <Empty description="Sin plantillas" />
          ),
        }))}
      />

      <Modal
        title={editingTemplate ? 'Editar Plantilla' : 'Nueva Plantilla'}
        open={modalVisible}
        onCancel={() => setModalVisible(false)}
        onOk={() => form.submit()}
        okText="Guardar"
        cancelText="Cancelar"
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit}>
          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Día de la semana"
                name="dayOfWeek"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <Select options={dayOptions} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Cantidad requerida"
                name="requiredCount"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <InputNumber min={1} max={50} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Área de trabajo"
                name="workAreaId"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <Select
                  placeholder="Seleccione un área"
                  options={workAreas.map(area => ({ label: area.name, value: area.id }))}
                  onChange={handleWorkAreaChange}
                  disabled={!!editingTemplate}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Rol de trabajo"
                name="workRoleId"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <Select
                  placeholder="Seleccione un rol"
                  options={workRoles.map(role => ({ label: role.name, value: role.id }))}
                  disabled={!!editingTemplate}
                />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Hora inicio"
                name="startTime"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <TimePicker format={timeFormat} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Hora fin"
                name="endTime"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <TimePicker format={timeFormat} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item label="Descanso (minutos)" name="breakMinutes">
                <InputNumber min={0} max={180} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item label="Almuerzo (minutos)" name="lunchMinutes">
                <InputNumber min={0} max={240} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item label="Notas" name="notes">
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};
