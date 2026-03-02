import { useEffect, useState } from 'react';
import { Form, Button, InputNumber, Row, Col, message, Spin } from 'antd';
import { payrollApi } from '../../services/api';
import type { PayrollConfigurationDto, CreatePayrollConfigurationDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

export const PayrollConfigurationForm = () => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [initialLoading, setInitialLoading] = useState(true);
  const [configuration, setConfiguration] = useState<PayrollConfigurationDto | null>(null);

  useEffect(() => {
    const loadConfiguration = async () => {
      try {
        setInitialLoading(true);
        const response = await payrollApi.getConfiguration();
        if (response.data) {
          setConfiguration(response.data);
          form.setFieldsValue(response.data);
        }
      } catch (error) {
        message.error(formatError(error));
      } finally {
        setInitialLoading(false);
      }
    };

    loadConfiguration();
  }, [form]);

  const onFinish = async (values: CreatePayrollConfigurationDto) => {
    try {
      setLoading(true);
      const response = await payrollApi.updateConfiguration(values);
      setConfiguration(response.data);
      message.success('Configuracion de nomina guardada');
    } catch (error) {
      message.error(formatError(error));
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
        iessEmployeeRate: 9.45,
        iessEmployerRate: 11.45,
        incomeTaxRate: 0,
        overtimeRate50: 50,
        overtimeRate100: 100,
        decimoThirdRate: 8.33,
        decimoFourthRate: 8.33,
        reserveFundRate: 8.33,
        monthlyHours: 240,
      }}
    >
      <div style={{ marginBottom: '24px', padding: '16px', backgroundColor: '#f5f5f5', borderRadius: '4px' }}>
        <h3 style={{ marginTop: 0 }}>Aportes y retenciones</h3>
        <Row gutter={[16, 16]}>
          <Col xs={24} sm={12} lg={8}>
            <Form.Item label="IESS empleado (%)" name="iessEmployeeRate" rules={[{ required: true }]}
            >
              <InputNumber min={0} max={30} step={0.01} precision={2} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col xs={24} sm={12} lg={8}>
            <Form.Item label="IESS empleador (%)" name="iessEmployerRate" rules={[{ required: true }]}
            >
              <InputNumber min={0} max={30} step={0.01} precision={2} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col xs={24} sm={12} lg={8}>
            <Form.Item label="Impuesto a la renta (%)" name="incomeTaxRate" rules={[{ required: true }]}
            >
              <InputNumber min={0} max={30} step={0.01} precision={2} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>
      </div>

      <div style={{ marginBottom: '24px', padding: '16px', backgroundColor: '#f5f5f5', borderRadius: '4px' }}>
        <h3 style={{ marginTop: 0 }}>Horas extra y base de calculo</h3>
        <Row gutter={[16, 16]}>
          <Col xs={24} sm={12} lg={8}>
            <Form.Item label="Recargo 50% (%)" name="overtimeRate50" rules={[{ required: true }]}
            >
              <InputNumber min={0} max={200} step={0.1} precision={1} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col xs={24} sm={12} lg={8}>
            <Form.Item label="Recargo 100% (%)" name="overtimeRate100" rules={[{ required: true }]}
            >
              <InputNumber min={0} max={200} step={0.1} precision={1} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col xs={24} sm={12} lg={8}>
            <Form.Item label="Horas mensuales base" name="monthlyHours" rules={[{ required: true }]}
            >
              <InputNumber min={1} max={400} step={1} precision={0} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>
      </div>

      <div style={{ marginBottom: '24px', padding: '16px', backgroundColor: '#f5f5f5', borderRadius: '4px' }}>
        <h3 style={{ marginTop: 0 }}>Decimos y fondos</h3>
        <Row gutter={[16, 16]}>
          <Col xs={24} sm={12} lg={8}>
            <Form.Item label="Decimo tercero (%)" name="decimoThirdRate" rules={[{ required: true }]}
            >
              <InputNumber min={0} max={20} step={0.01} precision={2} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col xs={24} sm={12} lg={8}>
            <Form.Item label="Decimo cuarto (%)" name="decimoFourthRate" rules={[{ required: true }]}
            >
              <InputNumber min={0} max={20} step={0.01} precision={2} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col xs={24} sm={12} lg={8}>
            <Form.Item label="Fondos de reserva (%)" name="reserveFundRate" rules={[{ required: true }]}
            >
              <InputNumber min={0} max={20} step={0.01} precision={2} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>
      </div>

      <Form.Item>
        <Button type="primary" htmlType="submit" loading={loading} size="large" block>
          {configuration ? 'Actualizar configuracion' : 'Guardar configuracion'}
        </Button>
      </Form.Item>
    </Form>
  );
};
