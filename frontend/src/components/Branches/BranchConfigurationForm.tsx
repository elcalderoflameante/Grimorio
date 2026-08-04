import { useEffect, useState } from 'react';
import { App as AntApp, Card, Form, Input, Button, Switch, Row, Col, Divider, Select } from 'antd';
import { branchApi } from '../../services/api';
import { LocationMap } from './LocationMap';
import type { BranchDto, UpdateBranchDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { DEFAULT_BRANCH_TIME_ZONE, setBranchTimeZone } from '../../utils/branchTimeZone';

const timeZoneOptions = [
  { value: DEFAULT_BRANCH_TIME_ZONE, label: 'Ecuador - America/Guayaquil (UTC-05:00)' },
  { value: 'America/Bogota', label: 'Colombia - America/Bogota (UTC-05:00)' },
  { value: 'America/Lima', label: 'Peru - America/Lima (UTC-05:00)' },
  { value: 'America/Panama', label: 'Panama - America/Panama (UTC-05:00)' },
  { value: 'America/New_York', label: 'Estados Unidos Este - America/New_York' },
  { value: 'America/Chicago', label: 'Estados Unidos Centro - America/Chicago' },
  { value: 'America/Denver', label: 'Estados Unidos Montana - America/Denver' },
  { value: 'America/Los_Angeles', label: 'Estados Unidos Pacifico - America/Los_Angeles' },
  { value: 'Europe/Madrid', label: 'Espana - Europe/Madrid' },
];

export const BranchConfigurationForm = () => {
  const { message } = AntApp.useApp();

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
        setBranchTimeZone(response.data.timeZoneId);
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

  const handleLocationChange = (lat: number, lng: number, address?: string) => {
    setLatitude(lat);
    setLongitude(lng);
    form.setFieldsValue({ 
      latitude: lat, 
      longitude: lng,
      ...(address && { address })
    });
  };

  const handleSave = async (values: UpdateBranchDto) => {
    try {
      setLoading(true);
      const dataToSave = {
        ...values,
        timeZoneId: values.timeZoneId || DEFAULT_BRANCH_TIME_ZONE,
        latitude,
        longitude,
      };
      const response = await branchApi.updateCurrent(dataToSave);
      setBranch(response.data);
      form.setFieldsValue(response.data);
      setBranchTimeZone(response.data.timeZoneId);
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
            <Form.Item
              label="RUC"
              name="identificationNumber"
              rules={[{ required: true, message: 'Ingrese el RUC' }]}
            >
              <Input placeholder="RUC" />
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

        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item
              label="Zona horaria"
              name="timeZoneId"
              rules={[{ required: true, message: 'Seleccione la zona horaria de la sucursal' }]}
            >
              <Select
                showSearch
                options={timeZoneOptions}
                placeholder="Seleccione la zona horaria"
                optionFilterProp="label"
              />
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
