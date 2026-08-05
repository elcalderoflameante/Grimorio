import { useEffect, useState } from 'react';
import {
  App as AntApp,
  Button,
  Card,
  Col,
  Divider,
  Form,
  Input,
  Row,
  Select,
  Space,
  Switch,
  Upload,
} from 'antd';
import { DeleteOutlined, UploadOutlined } from '@ant-design/icons';
import { branchApi, resolveMediaUrl } from '../../services/api';
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

const buildLogoPreviewUrl = (url?: string | null, refresh = false): string | undefined => {
  const resolved = resolveMediaUrl(url);
  if (!resolved || !refresh || /^(data:|blob:)/i.test(resolved)) return resolved;

  return `${resolved}${resolved.includes('?') ? '&' : '?'}v=${Date.now()}`;
};

export const BranchConfigurationForm = () => {
  const { message } = AntApp.useApp();

  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [initialLoading, setInitialLoading] = useState(true);
  const [branch, setBranch] = useState<BranchDto | null>(null);
  const [latitude, setLatitude] = useState<number | undefined>();
  const [longitude, setLongitude] = useState<number | undefined>();
  const [logoLoading, setLogoLoading] = useState(false);
  const [logoPreviewUrl, setLogoPreviewUrl] = useState<string | undefined>();
  const [logoLoadFailed, setLogoLoadFailed] = useState(false);

  const applyBranch = (nextBranch: BranchDto, refreshLogo = false) => {
    setBranch(nextBranch);
    form.setFieldsValue(nextBranch);
    setLatitude(nextBranch.latitude);
    setLongitude(nextBranch.longitude);
    setLogoPreviewUrl(buildLogoPreviewUrl(nextBranch.logoUrl, refreshLogo));
    setLogoLoadFailed(false);
  };

  useEffect(() => {
    const loadBranch = async () => {
      try {
        setInitialLoading(true);
        const response = await branchApi.getCurrent();
        applyBranch(response.data);
        setBranchTimeZone(response.data.timeZoneId);
      } catch (error) {
        message.error(formatError(error));
      } finally {
        setInitialLoading(false);
      }
    };

    loadBranch();
  }, [form, message]);

  const handleLocationChange = (lat: number, lng: number, address?: string) => {
    setLatitude(lat);
    setLongitude(lng);
    form.setFieldsValue({
      latitude: lat,
      longitude: lng,
      ...(address && { address }),
    });
  };

  const handleSave = async (values: UpdateBranchDto) => {
    try {
      setLoading(true);
      const dataToSave = {
        ...values,
        timeZoneId: values.timeZoneId || DEFAULT_BRANCH_TIME_ZONE,
        logoUrl: branch?.logoUrl,
        latitude,
        longitude,
      };
      const response = await branchApi.updateCurrent(dataToSave);
      applyBranch(response.data);
      setBranchTimeZone(response.data.timeZoneId);
      message.success('Sucursal actualizada correctamente.');
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  };

  const handleLogoUpload = async (file: File) => {
    try {
      setLogoLoading(true);
      const response = await branchApi.uploadLogo(file);
      applyBranch(response.data, true);
      message.success('Logo actualizado correctamente.');
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLogoLoading(false);
    }
  };

  const handleLogoDelete = async () => {
    try {
      setLogoLoading(true);
      const response = await branchApi.deleteLogo();
      applyBranch(response.data);
      message.success('Logo eliminado correctamente.');
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLogoLoading(false);
    }
  };

  return (
    <Card title="Configuracion de Sucursal" loading={initialLoading}>
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
              label="Codigo"
              name="code"
              rules={[{ required: true, message: 'Ingrese el codigo de la sucursal' }]}
            >
              <Input placeholder="Codigo" />
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
            <Form.Item label="Telefono" name="phone">
              <Input placeholder="Telefono" />
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

        <Divider>Imagen de Sucursal</Divider>

        <Row gutter={16} align="middle" style={{ marginBottom: 16 }}>
          <Col xs={24} md={8}>
            <div
              style={{
                width: 160,
                height: 96,
                border: '1px solid #d9d9d9',
                borderRadius: 8,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                overflow: 'hidden',
                background: '#fafafa',
              }}
            >
              {logoPreviewUrl && !logoLoadFailed ? (
                <img
                  src={logoPreviewUrl}
                  alt="Logo de sucursal"
                  style={{ maxWidth: 150, maxHeight: 86, objectFit: 'contain' }}
                  onError={() => setLogoLoadFailed(true)}
                />
              ) : (
                <span style={{ color: '#8c8c8c' }}>
                  {branch?.logoUrl ? 'No se pudo cargar' : 'Sin logo'}
                </span>
              )}
            </div>
          </Col>
          <Col xs={24} md={16}>
            <Space wrap>
              <Upload
                accept=".jpg,.jpeg,.png,.webp"
                showUploadList={false}
                beforeUpload={(file) => {
                  void handleLogoUpload(file);
                  return false;
                }}
                disabled={logoLoading}
              >
                <Button icon={<UploadOutlined />} loading={logoLoading}>
                  Subir logo
                </Button>
              </Upload>
              <Button
                danger
                icon={<DeleteOutlined />}
                disabled={!branch?.logoUrl || logoLoading}
                onClick={handleLogoDelete}
              >
                Quitar logo
              </Button>
            </Space>
          </Col>
        </Row>

        <Form.Item label="Direccion" name="address">
          <Input placeholder="Direccion" />
        </Form.Item>

        <Form.Item label="Activa" name="isActive" valuePropName="checked">
          <Switch />
        </Form.Item>

        <Divider>Ubicacion Geografica</Divider>

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
            Haz clic en el mapa para marcar la ubicacion
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
