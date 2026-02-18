import { useEffect, useState } from 'react';
import { Form, Button, Input, InputNumber, Row, Col, message, Spin } from 'antd';
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
            hoursPerDay: response.data.hoursPerDay,
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

  if (initialLoading) {
    return <Spin size="large" style={{ display: 'block', textAlign: 'center', padding: '50px' }} />;
  }

  return (
    <Form
      form={form}
      layout="vertical"
      onFinish={onFinish}
      initialValues={{
        hoursPerDay: 8,
        freeDayColor: '#E8E8E8',
      }}
    >
        {/* Nota sobre horas semanales */}
        <div style={{ marginBottom: '24px', padding: '16px', backgroundColor: '#fff7e6', borderRadius: '4px', border: '1px solid #ffc069' }}>
          <h3 style={{ marginTop: 0, marginBottom: '8px' }}>📌 Cambio en configuración de horas</h3>
          <p style={{ marginBottom: 0, color: '#666' }}>
            Las horas ahora se configuran por empleado (horas mínimas y máximas semanales) en lugar de ser globales por sucursal.
            Cada empleado tiene asignado su ContractType (Tiempo completo, Tiempo parcial, etc.) con las horas semanales correspondientes.
          </p>
        </div>

        {/* Horas diarias */}
        <div style={{ marginBottom: '24px', padding: '16px', backgroundColor: '#f5f5f5', borderRadius: '4px' }}>
          <h3 style={{ marginTop: 0 }}>⏰ HORAS DE REFERENCIA DIARIA</h3>
          <Row gutter={[16, 16]}>
            <Col xs={24} sm={12}>
              <Form.Item
                label="Horas de trabajo por día (referencia)"
                name="hoursPerDay"
                rules={[{ required: true, message: 'Campo obligatorio' }]}
              >
                <InputNumber 
                  min={1} 
                  max={12} 
                  step={0.5}
                  style={{ width: '100%' }}
                  placeholder="8"
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <div style={{ marginTop: 30, color: '#666' }}>
                Valor referencial para cálculos (no se usa en validación de horas).
              </div>
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
  );
};
