import { useEffect, useState } from 'react';
import { Card, Form, Input, Button, Switch, Row, Col, message } from 'antd';
import { branchApi } from '../../services/api';
import type { BranchDto, UpdateBranchDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

export const BranchConfigurationForm = () => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [initialLoading, setInitialLoading] = useState(true);
  const [branch, setBranch] = useState<BranchDto | null>(null);

  useEffect(() => {
    const loadBranch = async () => {
      try {
        setInitialLoading(true);
        const response = await branchApi.getCurrent();
        setBranch(response.data);
        form.setFieldsValue(response.data);
      } catch (error) {
        message.error(formatError(error));
      } finally {
        setInitialLoading(false);
      }
    };

    loadBranch();
  }, [form]);

  const handleSave = async (values: UpdateBranchDto) => {
    try {
      setLoading(true);
      const response = await branchApi.updateCurrent(values);
      setBranch(response.data);
      form.setFieldsValue(response.data);
      message.success('Sucursal actualizada correctamente.');
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card title="Sucursal" loading={initialLoading}>
      <Form
        form={form}
        layout="vertical"
        onFinish={handleSave}
        initialValues={branch || undefined}
      >
        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item
              label="Nombre"
              name="name"
              rules={[{ required: true, message: 'Ingrese el nombre de la sucursal' }]}
            >
              <Input placeholder="Nombre de la sucursal" />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item
              label="Código"
              name="code"
              rules={[{ required: true, message: 'Ingrese el código de la sucursal' }]}
            >
              <Input placeholder="Código" />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item label="Teléfono" name="phone">
              <Input placeholder="Teléfono" />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item label="Email" name="email">
              <Input placeholder="Email" type="email" />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item label="Dirección" name="address">
          <Input placeholder="Dirección" />
        </Form.Item>

        <Form.Item label="Activa" name="isActive" valuePropName="checked">
          <Switch />
        </Form.Item>

        <Button type="primary" htmlType="submit" loading={loading}>
          Guardar cambios
        </Button>
      </Form>
    </Card>
  );
};
