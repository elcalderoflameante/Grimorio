import { useEffect, useState } from 'react';
import { Card, Form, Input, Button, Switch, Row, Col, message, Divider } from 'antd';
import { branchApi } from '../../services/api';
import { LocationMap } from './LocationMap';
import type { BranchDto, UpdateBranchDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

export const BranchConfigurationForm = () => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [initialLoading, setInitialLoading] = useState(true);
  const [branch, setBranch] = useState<BranchDto | null>(null);
  const [latitude, setLatitude] = useState<number | undefined>();
  const [longitude, setLongitude] = useState<number | undefined>();

  useEffect(() => {
    const loadBranch = async () => {
      try {
        setInitialLoading(true);
        const response = await branchApi.getCurrent();
        setBranch(response.data);
        form.setFieldsValue(response.data);
        setLatitude(response.data.latitude);
        setLongitude(response.data.longitude);
      } catch (error) {
        message.error(formatError(error));
      } finally {
        setInitialLoading(false);
      }
    };

    loadBranch();
  }, [form]);

  const handleLocationChange = (lat: number, lng: number) => {
    setLatitude(lat);
    setLongitude(lng);
    form.setFieldsValue({ latitude: lat, longitude: lng });
  };

  const handleSave = async (values: UpdateBranchDto) => {
    try {
      setLoading(true);
      const dataToSave = {
        ...values,
        latitude,
        longitude,
      };
      const response = await branchApi.updateCurrent(dataToSave);
      setBranch(response.data);
      form.setFieldsValue(response.data);
      setLatitude(response.data.latitude);
      setLongitude(response.data.longitude);
      message.success('Sucursal actualizada correctamente.');
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card title="Configuración de Sucursal" loading={initialLoading}>
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

        <Divider>Ubicación Geográfica</Divider>

        <Row gutter={16} style={{ marginBottom: 16 }}>
          <Col xs={24} md={12}>
            <Form.Item label="Latitud" name="latitude">
              <Input
                type="number"
                placeholder="Latitud"
                step="0.000001"
                onChange={(e) => {
                  const val = e.target.value ? parseFloat(e.target.value) : undefined;
                  setLatitude(val);
                }}
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item label="Longitud" name="longitude">
              <Input
                type="number"
                placeholder="Longitud"
                step="0.000001"
                onChange={(e) => {
                  const val = e.target.value ? parseFloat(e.target.value) : undefined;
                  setLongitude(val);
                }}
              />
            </Form.Item>
          </Col>
        </Row>

        <div style={{ marginBottom: 16 }}>
          <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>
            Haz clic en el mapa para marcar la ubicación
          </label>
          <LocationMap
            latitude={latitude}
            longitude={longitude}
            onLocationChange={handleLocationChange}
            height="300px"
          />
        </div>

        <Button type="primary" htmlType="submit" loading={loading}>
          Guardar cambios
        </Button>
      </Form>
    </Card>
  );
};
