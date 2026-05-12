import { useEffect, useState } from 'react';
import {
  Form, Input, Select, Switch, Button, Card, Alert, Tag, Space,
  Typography, Divider, Upload, Modal, Spin, message, Tooltip,
} from 'antd';
import {
  SafetyCertificateOutlined, CloudUploadOutlined, DeleteOutlined,
  CheckCircleOutlined, CloseCircleOutlined, WifiOutlined, WarningOutlined,
} from '@ant-design/icons';
import type { UploadFile } from 'antd';
import { sriApi } from '../../services/api';
import type { BranchTaxConfigDto, SriCertificateStatusDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Title, Text } = Typography;

export default function SriConfig() {
  const [form] = Form.useForm<BranchTaxConfigDto>();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [certStatus, setCertStatus] = useState<SriCertificateStatusDto | null>(null);
  const [pingResult, setPingResult] = useState<{ success: boolean; ambiente: string; error?: string } | null>(null);
  const [pinging, setPinging] = useState(false);

  const [uploadModal, setUploadModal] = useState(false);
  const [uploadFile, setUploadFile] = useState<UploadFile | null>(null);
  const [uploadPassword, setUploadPassword] = useState('');
  const [uploading, setUploading] = useState(false);

  const ambienteWatch = Form.useWatch('ambiente', form);

  const load = async () => {
    setLoading(true);
    try {
      const [cfg, cert] = await Promise.all([sriApi.getConfig(), sriApi.getCertificateStatus()]);
      if (cfg.data) form.setFieldsValue(cfg.data);
      setCertStatus(cert.data);
    } catch {
      // primera vez sin config → formulario vacío
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const save = async () => {
    const values = await form.validateFields();
    setSaving(true);
    try {
      await sriApi.upsertConfig(values);
      message.success('Configuración guardada correctamente');
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setSaving(false);
    }
  };

  const ping = async () => {
    const ambiente = form.getFieldValue('ambiente') ?? '1';
    setPinging(true);
    setPingResult(null);
    try {
      const r = await sriApi.ping(ambiente);
      setPingResult(r.data);
    } catch {
      setPingResult({ success: false, ambiente: ambiente === '2' ? 'Producción' : 'Pruebas', error: 'No se pudo conectar' });
    } finally {
      setPinging(false);
    }
  };

  const uploadCert = async () => {
    if (!uploadFile?.originFileObj) { message.warning('Selecciona un archivo .p12'); return; }
    if (!uploadPassword.trim()) { message.warning('Ingresa la contraseña del certificado'); return; }
    setUploading(true);
    try {
      const r = await sriApi.uploadCertificate(uploadFile.originFileObj as File, uploadPassword);
      setCertStatus(r.data);
      message.success('Certificado cargado y validado correctamente');
      setUploadModal(false);
      setUploadFile(null);
      setUploadPassword('');
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setUploading(false);
    }
  };

  const deleteCert = async () => {
    try {
      await sriApi.deleteCertificate();
      setCertStatus({ hasCertificate: false });
      message.success('Certificado eliminado');
    } catch (e) {
      message.error(formatError(e));
    }
  };

  const certTag = () => {
    if (!certStatus?.hasCertificate) return <Tag color="default">Sin certificado</Tag>;
    if (certStatus.isExpired) return <Tag color="error" icon={<WarningOutlined />}>Vencido</Tag>;
    return <Tag color="success" icon={<CheckCircleOutlined />}>Activo</Tag>;
  };

  return (
    <Spin spinning={loading}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>

        {/* ── Datos del emisor ── */}
        <Card title={<Space><SafetyCertificateOutlined /> Datos del emisor (SRI Ecuador)</Space>}>
          <Form form={form} layout="vertical" initialValues={{ codigoEstablecimiento: '001', puntoEmision: '001', ambiente: '1' }}>
            <Space style={{ width: '100%' }} size="middle">
              <Form.Item name="ruc" label="RUC" rules={[{ required: true, len: 13, message: 'El RUC debe tener 13 dígitos' }]} style={{ flex: 1 }}>
                <Input maxLength={13} placeholder="0123456789001" />
              </Form.Item>
              <Form.Item name="ambiente" label="Ambiente" rules={[{ required: true }]} style={{ width: 160 }}>
                <Select options={[{ value: '1', label: 'Pruebas (SRI)' }, { value: '2', label: 'Producción' }]} />
              </Form.Item>
            </Space>

            {ambienteWatch === '2' && (
              <Alert
                type="warning"
                showIcon
                message="Modo Producción activo — los comprobantes emitidos tendrán validez legal ante el SRI."
                style={{ marginBottom: 16 }}
              />
            )}

            <Form.Item name="razonSocial" label="Razón Social" rules={[{ required: true }]}>
              <Input maxLength={300} />
            </Form.Item>
            <Form.Item name="nombreComercial" label="Nombre Comercial">
              <Input maxLength={300} />
            </Form.Item>
            <Form.Item name="direccion" label="Dirección Matriz" rules={[{ required: true }]}>
              <Input maxLength={300} />
            </Form.Item>

            <Space style={{ width: '100%' }} size="middle">
              <Form.Item name="codigoEstablecimiento" label="Establecimiento" rules={[{ required: true, len: 3 }]} style={{ width: 130 }}>
                <Input maxLength={3} placeholder="001" />
              </Form.Item>
              <Form.Item name="puntoEmision" label="Punto de Emisión" rules={[{ required: true, len: 3 }]} style={{ width: 130 }}>
                <Input maxLength={3} placeholder="001" />
              </Form.Item>
              <Form.Item name="contribuyenteEspecial" label="Nº Contribuyente Especial" tooltip="Dejar vacío si no aplica" style={{ width: 200 }}>
                <Input maxLength={20} placeholder="— no aplica —" />
              </Form.Item>
              <Form.Item name="obligadoContabilidad" label="Obligado a llevar contabilidad" valuePropName="checked" style={{ paddingTop: 6 }}>
                <Switch />
              </Form.Item>
            </Space>

            <Space>
              <Button type="primary" onClick={save} loading={saving}>Guardar configuración</Button>
              <Tooltip title="Verifica la conectividad con los servidores del SRI">
                <Button icon={<WifiOutlined />} onClick={ping} loading={pinging}>
                  Probar conexión SRI
                </Button>
              </Tooltip>
            </Space>

            {pingResult && (
              <Alert
                style={{ marginTop: 12 }}
                type={pingResult.success ? 'success' : 'error'}
                icon={pingResult.success ? <CheckCircleOutlined /> : <CloseCircleOutlined />}
                showIcon
                message={
                  pingResult.success
                    ? `Conexión exitosa con el SRI (${pingResult.ambiente})`
                    : `Sin conexión con el SRI (${pingResult.ambiente}): ${pingResult.error ?? 'error desconocido'}`
                }
              />
            )}
          </Form>
        </Card>

        {/* ── Certificado .p12 ── */}
        <Card
          title={<Space><SafetyCertificateOutlined /> Certificado de Firma Electrónica (.p12)</Space>}
          extra={certTag()}
        >
          {certStatus?.hasCertificate ? (
            <Space direction="vertical" style={{ width: '100%' }}>
              <div style={{ display: 'flex', gap: 24, flexWrap: 'wrap' }}>
                <div>
                  <Text type="secondary" style={{ fontSize: 12 }}>Archivo</Text>
                  <div><Text strong>{certStatus.fileName}</Text></div>
                </div>
                {certStatus.expiresAt && (
                  <div>
                    <Text type="secondary" style={{ fontSize: 12 }}>Vence el</Text>
                    <div>
                      <Text strong type={certStatus.isExpired ? 'danger' : undefined}>
                        {new Date(certStatus.expiresAt).toLocaleDateString('es-EC')}
                      </Text>
                    </div>
                  </div>
                )}
                {certStatus.uploadedAt && (
                  <div>
                    <Text type="secondary" style={{ fontSize: 12 }}>Cargado el</Text>
                    <div><Text>{new Date(certStatus.uploadedAt).toLocaleDateString('es-EC')}</Text></div>
                  </div>
                )}
              </div>
              <Divider style={{ margin: '12px 0' }} />
              <Space>
                <Button icon={<CloudUploadOutlined />} onClick={() => setUploadModal(true)}>
                  Reemplazar certificado
                </Button>
                <Button danger icon={<DeleteOutlined />} onClick={deleteCert}>
                  Eliminar certificado
                </Button>
              </Space>
            </Space>
          ) : (
            <Space direction="vertical">
              <Text type="secondary">
                No hay certificado cargado. Para emitir facturas electrónicas debes cargar
                el archivo .p12 entregado por el BCE (Banco Central del Ecuador).
              </Text>
              <Button type="primary" icon={<CloudUploadOutlined />} onClick={() => setUploadModal(true)}>
                Cargar certificado .p12
              </Button>
            </Space>
          )}
        </Card>

      </Space>

      {/* ── Modal carga .p12 ── */}
      <Modal
        title="Cargar certificado de firma electrónica"
        open={uploadModal}
        onOk={uploadCert}
        onCancel={() => { setUploadModal(false); setUploadFile(null); setUploadPassword(''); }}
        okText="Cargar y validar"
        confirmLoading={uploading}
      >
        <Space direction="vertical" style={{ width: '100%' }} size="middle">
          <Alert
            type="info"
            showIcon
            message="El archivo .p12 y su contraseña se almacenan cifrados en la base de datos. Nunca se transmiten en texto plano."
          />
          <Upload
            accept=".p12"
            maxCount={1}
            beforeUpload={() => false}
            onChange={({ fileList }) => setUploadFile(fileList[fileList.length - 1] ?? null)}
            fileList={uploadFile ? [uploadFile] : []}
          >
            <Button icon={<CloudUploadOutlined />}>Seleccionar archivo .p12</Button>
          </Upload>
          <div>
            <Text strong>Contraseña del certificado</Text>
            <Input.Password
              value={uploadPassword}
              onChange={e => setUploadPassword(e.target.value)}
              placeholder="Contraseña del archivo .p12"
              style={{ marginTop: 4 }}
            />
          </div>
        </Space>
      </Modal>
    </Spin>
  );
}
