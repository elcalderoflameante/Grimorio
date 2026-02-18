import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { Button, Form, Input, Modal, Table, Space, Popconfirm, message, DatePicker, Card, Empty, Row, Col, TimePicker, InputNumber, Select } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import dayjs, { Dayjs } from 'dayjs';
import type { ColumnsType } from 'antd/es/table';
import type { SpecialDateDto, CreateSpecialDateDto, UpdateSpecialDateDto } from '../../types/SpecialDate';
import type { SpecialDateTemplateDto, CreateSpecialDateTemplateDto, UpdateSpecialDateTemplateDto, WorkAreaDto, WorkRoleDto } from '../../types';
import { specialDateApi } from '../../services/specialDateApi';
import { specialDateTemplateApi, workAreaApi, workRoleApi } from '../../services/api';

interface SpecialDateListProps {
  branchId: string;
}

interface SpecialDateFormValues {
  date: Dayjs;
  name: string;
  description?: string;
}

interface TemplateFormValues {
  startTime: Dayjs;
  endTime: Dayjs;
  breakMinutes?: number;
  lunchMinutes?: number;
  workAreaId: string;
  workRoleId: string;
  requiredCount: number;
  notes?: string;
}

const timeFormat = 'HH:mm';

const timeSpanToMinutes = (timeSpan?: string): number | null => {
  if (!timeSpan) return null;
  const match = timeSpan.match(/(\d+):(\d+):(\d+)/);
  if (!match) return null;
  return parseInt(match[1]) * 60 + parseInt(match[2]);
};

const minutesToTimeSpan = (minutes?: number): string | undefined => {
  if (!minutes) return undefined;
  const hours = Math.floor(minutes / 60);
  const mins = minutes % 60;
  return `${hours.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}:00`;
};

const dayOfWeekNames = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];

export const SpecialDateListWithTemplates: React.FC<SpecialDateListProps> = ({ branchId }) => {
  const [dateForm] = Form.useForm<SpecialDateFormValues>();
  const [templateForm] = Form.useForm<TemplateFormValues>();
  
  const [loading, setLoading] = useState(false);
  const [specialDates, setSpecialDates] = useState<SpecialDateDto[]>([]);
  const [templates, setTemplates] = useState<Record<string, SpecialDateTemplateDto[]>>({});
  const [workAreas, setWorkAreas] = useState<WorkAreaDto[]>([]);
  const [workRoles, setWorkRoles] = useState<WorkRoleDto[]>([]);
  
  const [isDateModalVisible, setIsDateModalVisible] = useState(false);
  const [isTemplateModalVisible, setIsTemplateModalVisible] = useState(false);
  const [editingDate, setEditingDate] = useState<SpecialDateDto | null>(null);
  const [editingTemplate, setEditingTemplate] = useState<SpecialDateTemplateDto | null>(null);
  const [selectedSpecialDateId, setSelectedSpecialDateId] = useState<string | null>(null);
  const [expandedRowKeys, setExpandedRowKeys] = useState<string[]>([]);

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

  const loadTemplatesForDate = useCallback(async (specialDateId: string) => {
    try {
      const response = await specialDateTemplateApi.getBySpecialDateId(specialDateId);
      setTemplates(prev => ({
        ...prev,
        [specialDateId]: response.data || []
      }));
    } catch (error) {
      message.error('Error al cargar plantillas');
      console.error(error);
    }
  }, []);

  const loadWorkAreasAndRoles = useCallback(async () => {
    if (!branchId) return;
    try {
      const [areasResponse, rolesResponse] = await Promise.all([
        workAreaApi.getAll(branchId),
        workRoleApi.getAll(branchId)
      ]);
      setWorkAreas(areasResponse.data || []);
      setWorkRoles(rolesResponse.data || []);
    } catch (error) {
      message.error('Error al cargar áreas y roles');
      console.error(error);
    }
  }, [branchId]);

  useEffect(() => {
    loadSpecialDates();
    loadWorkAreasAndRoles();
  }, [branchId, loadSpecialDates, loadWorkAreasAndRoles]);

  const handleOpenDateModal = useCallback((date?: SpecialDateDto) => {
    if (date) {
      setEditingDate(date);
      dateForm.setFieldsValue({
        date: dayjs(date.date),
        name: date.name,
        description: date.description,
      });
    } else {
      setEditingDate(null);
      dateForm.resetFields();
    }
    setIsDateModalVisible(true);
  }, [dateForm]);

  const handleCloseDateModal = () => {
    setIsDateModalVisible(false);
    setEditingDate(null);
    dateForm.resetFields();
  };

  const handleSubmitDate = async (values: SpecialDateFormValues) => {
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

      handleCloseDateModal();
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

  const handleDeleteDate = useCallback(async (id: string) => {
    try {
      setLoading(true);
      await specialDateApi.delete(id);
      message.success('Día especial y sus plantillas eliminados');
      await loadSpecialDates();
      setTemplates(prev => {
        const newTemplates = { ...prev };
        delete newTemplates[id];
        return newTemplates;
      });
    } catch (error) {
      message.error('Error al eliminar el día especial');
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [loadSpecialDates]);

  const handleOpenTemplateModal = useCallback((specialDateId: string, template?: SpecialDateTemplateDto) => {
    setSelectedSpecialDateId(specialDateId);
    
    if (template) {
      setEditingTemplate(template);
      const startParts = template.startTime.split(':');
      const endParts = template.endTime.split(':');
      
      templateForm.setFieldsValue({
        startTime: dayjs().hour(parseInt(startParts[0])).minute(parseInt(startParts[1])),
        endTime: dayjs().hour(parseInt(endParts[0])).minute(parseInt(endParts[1])),
        breakMinutes: timeSpanToMinutes(template.breakDuration) || undefined,
        lunchMinutes: timeSpanToMinutes(template.lunchDuration) || undefined,
        workAreaId: template.workAreaId,
        workRoleId: template.workRoleId,
        requiredCount: template.requiredCount,
        notes: template.notes,
      });
    } else {
      setEditingTemplate(null);
      templateForm.resetFields();
    }
    
    setIsTemplateModalVisible(true);
  }, [templateForm]);

  const handleCloseTemplateModal = () => {
    setIsTemplateModalVisible(false);
    setEditingTemplate(null);
    setSelectedSpecialDateId(null);
    templateForm.resetFields();
  };

  const handleSubmitTemplate = async (values: TemplateFormValues) => {
    if (!selectedSpecialDateId) {
      message.error('Error: No se seleccionó día especial');
      return;
    }

    try {
      setLoading(true);
      
      const basePayload = {
        startTime: values.startTime.format('HH:mm:ss'),
        endTime: values.endTime.format('HH:mm:ss'),
        breakDuration: minutesToTimeSpan(values.breakMinutes),
        lunchDuration: minutesToTimeSpan(values.lunchMinutes),
        requiredCount: values.requiredCount,
        notes: values.notes?.trim(),
      };

      if (editingTemplate) {
        await specialDateTemplateApi.update(editingTemplate.id, basePayload as UpdateSpecialDateTemplateDto);
        message.success('Plantilla actualizada');
      } else {
        const createPayload: CreateSpecialDateTemplateDto = {
          ...basePayload,
          specialDateId: selectedSpecialDateId,
          workAreaId: values.workAreaId,
          workRoleId: values.workRoleId,
        };
        await specialDateTemplateApi.create(createPayload);
        message.success('Plantilla creada');
      }

      handleCloseTemplateModal();
      await loadTemplatesForDate(selectedSpecialDateId);
    } catch (error) {
      message.error('Error al guardar la plantilla');
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteTemplate = useCallback(async (specialDateId: string, templateId: string) => {
    try {
      setLoading(true);
      await specialDateTemplateApi.delete(templateId);
      message.success('Plantilla eliminada');
      await loadTemplatesForDate(specialDateId);
    } catch (error) {
      message.error('Error al eliminar la plantilla');
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [loadTemplatesForDate]);

  const handleExpand = (expanded: boolean, record: SpecialDateDto) => {
    if (expanded) {
      setExpandedRowKeys(prev => [...prev, record.id]);
      if (!templates[record.id]) {
        loadTemplatesForDate(record.id);
      }
    } else {
      setExpandedRowKeys(prev => prev.filter(key => key !== record.id));
    }
  };

  const templateColumns: ColumnsType<SpecialDateTemplateDto> = useMemo(() => [
    {
      title: 'Horario',
      key: 'schedule',
      render: (_, record) => `${record.startTime.substring(0, 5)} - ${record.endTime.substring(0, 5)}`,
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
      width: 100,
    },
    {
      title: 'Descanso (min)',
      key: 'breakDuration',
      render: (_, record) => timeSpanToMinutes(record.breakDuration) ?? '-',
      width: 120,
    },
    {
      title: 'Almuerzo (min)',
      key: 'lunchDuration',
      render: (_, record) => timeSpanToMinutes(record.lunchDuration) ?? '-',
      width: 120,
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
          <Button 
            icon={<EditOutlined />} 
            size="small"
            onClick={() => handleOpenTemplateModal(record.specialDateId, record)} 
          />
          <Popconfirm
            title="¿Eliminar plantilla?"
            onConfirm={() => handleDeleteTemplate(record.specialDateId, record.id)}
            okText="Sí"
            cancelText="No"
          >
            <Button icon={<DeleteOutlined />} danger size="small" />
          </Popconfirm>
        </Space>
      ),
      width: 120,
    },
  ], [handleDeleteTemplate, handleOpenTemplateModal]);

  const expandedRowRender = (record: SpecialDateDto) => {
    const dateTemplates = templates[record.id] || [];
    
    return (
      <div style={{ padding: '16px 24px', background: '#fafafa' }}>
        <div style={{ marginBottom: 12 }}>
          <Button 
            type="primary" 
            size="small"
            icon={<PlusOutlined />}
            onClick={() => handleOpenTemplateModal(record.id)}
          >
            Nueva Plantilla para {record.name}
          </Button>
        </div>
        
        {dateTemplates.length === 0 ? (
          <Empty 
            description={`No hay plantillas definidas para ${record.name}`}
            image={Empty.PRESENTED_IMAGE_SIMPLE}
          />
        ) : (
          <Table
            columns={templateColumns}
            dataSource={dateTemplates}
            rowKey="id"
            size="small"
            pagination={false}
          />
        )}
      </div>
    );
  };

  const dateColumns: ColumnsType<SpecialDateDto> = useMemo(() => [
    {
      title: 'Fecha',
      key: 'date',
      render: (_, record) => {
        const date = dayjs(record.date);
        const dow = date.day();
        return `${date.format('DD/MM/YYYY')} (${dayOfWeekNames[dow]})`;
      },
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
      title: 'Plantillas',
      key: 'templateCount',
      render: (_, record) => {
        const count = templates[record.id]?.length || 0;
        return count > 0 ? `${count} plantilla${count !== 1 ? 's' : ''}` : 'Sin plantillas';
      },
      width: 130,
    },
    {
      title: 'Acciones',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <Button icon={<EditOutlined />} size="small" onClick={() => handleOpenDateModal(record)} />
          <Popconfirm
            title="¿Eliminar día especial?"
            description={`Se eliminará ${record.name} y todas sus plantillas`}
            onConfirm={() => handleDeleteDate(record.id)}
            okText="Sí"
            cancelText="No"
          >
            <Button icon={<DeleteOutlined />} danger size="small" />
          </Popconfirm>
        </Space>
      ),
      width: 120,
    },
  ], [templates, handleDeleteDate, handleOpenDateModal]);

  if (!branchId) {
    return <Empty description="Debe seleccionar una sucursal" />;
  }

  return (
    <Card title="Días Especiales y sus Plantillas" size="small">
      <div style={{ marginBottom: 16 }}>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => handleOpenDateModal()}
          loading={loading}
        >
          Nuevo Día Especial
        </Button>
      </div>

      <Table
        columns={dateColumns}
        dataSource={specialDates}
        rowKey="id"
        loading={loading}
        pagination={{ pageSize: 20 }}
        locale={{ emptyText: 'No hay días especiales' }}
        expandable={{
          expandedRowRender,
          expandedRowKeys,
          onExpand: handleExpand,
        }}
      />

      {/* Modal para días especiales */}
      <Modal
        title={editingDate ? 'Editar Día Especial' : 'Nuevo Día Especial'}
        open={isDateModalVisible}
        onCancel={handleCloseDateModal}
        footer={null}
        width={600}
      >
        <Form
          form={dateForm}
          layout="vertical"
          onFinish={handleSubmitDate}
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
              <Button onClick={handleCloseDateModal}>Cancelar</Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* Modal para plantillas */}
      <Modal
        title={editingTemplate ? 'Editar Plantilla' : 'Nueva Plantilla'}
        open={isTemplateModalVisible}
        onCancel={handleCloseTemplateModal}
        footer={null}
        width={700}
      >
        <Form
          form={templateForm}
          layout="vertical"
          onFinish={handleSubmitTemplate}
        >
          <Row gutter={16}>
            <Col xs={24} sm={8}>
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

          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit" loading={loading}>
                {editingTemplate ? 'Actualizar' : 'Crear'}
              </Button>
              <Button onClick={handleCloseTemplateModal}>Cancelar</Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
};

export default SpecialDateListWithTemplates;
