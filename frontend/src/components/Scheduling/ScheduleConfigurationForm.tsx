import { useEffect, useState } from 'react';
import { Form, Button, Card, Input, InputNumber, Row, Col, message } from 'antd';
import { scheduleConfigurationApi } from '../../services/api';
import type { ScheduleConfigurationDto, CreateScheduleConfigurationDto, UpdateScheduleConfigurationDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

interface ScheduleConfigurationFormProps {
  branchId: string;
  onConfigurationSaved?: (config: ScheduleConfigurationDto) => void;
}

export const ScheduleConfigurationForm = ({ 
  branchId, 
  onConfigurationSaved 
}: ScheduleConfigurationFormProps) => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [initialLoading, setInitialLoading] = useState(true);
  const [configuration, setConfiguration] = useState<ScheduleConfigurationDto | null>(null);

  // Cargar configuración existente
  useEffect(() => {
    const loadConfiguration = async () => {
      try {
        setInitialLoading(true);
        const response = await scheduleConfigurationApi.get(branchId);
        if (response.data) {
          setConfiguration(response.data);
          // Convertir propiedades de camelCase a snake_case para el formulario
          form.setFieldsValue({
            minHoursPerMonth: response.data.minHoursPerMonth,
            maxHoursPerMonth: response.data.maxHoursPerMonth,
            hoursMondayThursday: response.data.hoursMondayThursday,
            hoursFridaySaturday: response.data.hoursFridaySaturday,
            hoursSunday: response.data.hoursSunday,
            minStaffCocina: response.data.minStaffCocina,
            minStaffCaja: response.data.minStaffCaja,
            minStaffMesas: response.data.minStaffMesas,
            minStaffBar: response.data.minStaffBar,
            freeDayColor: response.data.freeDayColor || '#E8E8E8',
          });
        }
      } catch {
        console.log('No hay configuración previa, se creará una nueva');
      } finally {
        setInitialLoading(false);
      }
    };

    loadConfiguration();
  }, [branchId, form]);

  const onFinish = async (values: CreateScheduleConfigurationDto | UpdateScheduleConfigurationDto) => {
    try {
      setLoading(true);
      
      let result;
      if (configuration?.id) {
        // Actualizar configuración existente
        result = await scheduleConfigurationApi.update(configuration.id, values as UpdateScheduleConfigurationDto);
        message.success('Configuración actualizada correctamente');
      } else {
        // Crear nueva configuración
        result = await scheduleConfigurationApi.create(values as CreateScheduleConfigurationDto);
        message.success('Configuración creada correctamente');
      }

      setConfiguration(result.data);
      onConfigurationSaved?.(result.data);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card 
      title="Configuración de Horarios" 
      loading={initialLoading}
      style={{ marginBottom: '24px' }}
    >
      <Form
        form={form}
        layout="vertical"
        onFinish={onFinish}
        initialValues={{
          minHoursPerMonth: 160,
          maxHoursPerMonth: 220,
          hoursMondayThursday: 8.5,
          hoursFridaySaturday: 12.5,
          hoursSunday: 10,
          minStaffCocina: 2,
          minStaffCaja: 1,
          minStaffMesas: 3,
          minStaffBar: 1,
          freeDayColor: '#E8E8E8',
        }}
      >
        {/* Horas mensuales */}
        <div style={{ marginBottom: '24px', padding: '16px', backgroundColor: '#f5f5f5', borderRadius: '4px' }}>
          <h3 style={{ marginTop: 0 }}>📊 HORAS POR MES POR EMPLEADO</h3>
          <Row gutter={[16, 16]}>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Horas mínimas al mes"
                name="minHoursPerMonth"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <InputNumber 
                  min={0} 
                  max={500} 
                  step={0.5}
                  style={{ width: '100%' }}
                  placeholder="160"
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Horas máximas al mes"
                name="maxHoursPerMonth"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <InputNumber 
                  min={0} 
                  max={500} 
                  step={0.5}
                  style={{ width: '100%' }}
                  placeholder="220"
                />
              </Form.Item>
            </Col>
          </Row>
        </div>

        {/* Horarios diarios */}
        <div style={{ marginBottom: '24px', padding: '16px', backgroundColor: '#f5f5f5', borderRadius: '4px' }}>
          <h3 style={{ marginTop: 0 }}>⏰ HORAS DE TRABAJO DIARIAS</h3>
          <Row gutter={[16, 16]}>
            <Col xs={24} sm={8}>
              <Form.Item
                label="Lunes-Jueves (14:00-22:30)"
                name="hoursMondayThursday"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <InputNumber 
                  min={0} 
                  max={24} 
                  step={0.5}
                  style={{ width: '100%' }}
                  placeholder="8.5"
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={8}>
              <Form.Item
                label="Viernes-Sábado (11:30-23:30)"
                name="hoursFridaySaturday"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <InputNumber 
                  min={0} 
                  max={24} 
                  step={0.5}
                  style={{ width: '100%' }}
                  placeholder="12.5"
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={8}>
              <Form.Item
                label="Domingo (11:30-21:30)"
                name="hoursSunday"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <InputNumber 
                  min={0} 
                  max={24} 
                  step={0.5}
                  style={{ width: '100%' }}
                  placeholder="10"
                />
              </Form.Item>
            </Col>
          </Row>
        </div>

        {/* Staffing mínimo fines de semana */}
        <div style={{ marginBottom: '24px', padding: '16px', backgroundColor: '#f5f5f5', borderRadius: '4px' }}>
          <h3 style={{ marginTop: 0 }}>👥 PERSONAL MÍNIMO (FINES DE SEMANA)</h3>
          <Row gutter={[16, 16]}>
            <Col xs={24} sm={6}>
              <Form.Item
                label="Cocina"
                name="minStaffCocina"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <InputNumber 
                  min={1} 
                  max={20} 
                  step={1}
                  style={{ width: '100%' }}
                  placeholder="2"
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={6}>
              <Form.Item
                label="Caja"
                name="minStaffCaja"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <InputNumber 
                  min={1} 
                  max={20} 
                  step={1}
                  style={{ width: '100%' }}
                  placeholder="1"
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={6}>
              <Form.Item
                label="Mesas"
                name="minStaffMesas"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <InputNumber 
                  min={1} 
                  max={20} 
                  step={1}
                  style={{ width: '100%' }}
                  placeholder="3"
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={6}>
              <Form.Item
                label="Bar"
                name="minStaffBar"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <InputNumber 
                  min={1} 
                  max={20} 
                  step={1}
                  style={{ width: '100%' }}
                  placeholder="1"
                />
              </Form.Item>
            </Col>
          </Row>
        </div>

        {/* Visualización */}
        <div style={{ marginBottom: '24px', padding: '16px', backgroundColor: '#f5f5f5', borderRadius: '4px' }}>
          <h3 style={{ marginTop: 0 }}>🎨 VISUALIZACIÓN</h3>
          <Row gutter={[16, 16]}>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Color para empleados libres"
                name="freeDayColor"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <Input type="color" style={{ width: '100%', height: 40, padding: 4 }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <div style={{ marginTop: 30, color: '#666' }}>
                Este color se usará para resaltar a los empleados con día libre en el calendario semanal.
              </div>
            </Col>
          </Row>
        </div>

        <Form.Item>
          <Button 
            type="primary" 
            htmlType="submit" 
            loading={loading}
            size="large"
            block
          >
            {configuration ? 'Actualizar configuración' : 'Crear configuración'}
          </Button>
        </Form.Item>
      </Form>
    </Card>
  );
};
