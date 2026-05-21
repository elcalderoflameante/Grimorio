import { useCallback, useEffect, useState } from 'react';
import dayjs from 'dayjs';
import {
  Table, Button, Modal, Form, Input, Switch, InputNumber,
  Space, Tag, Popconfirm, Typography, message, Card, Divider,
  Select, Row, Col, Alert, Upload, Spin, Tooltip,
} from 'antd';
import {
  PlusOutlined, EditOutlined, DeleteOutlined, PercentageOutlined,
  BankOutlined, SafetyCertificateOutlined, CloudUploadOutlined,
  WifiOutlined, CheckCircleOutlined, CloseCircleOutlined, WarningOutlined,
  MailOutlined, SendOutlined,
} from '@ant-design/icons';
import type { UploadFile } from 'antd';
import type { TaxRateDto, UpsertTaxRateDto, BranchTaxConfigDto, SriCertificateStatusDto, SmtpConfigDto } from '../../types';
import { taxApi, sriApi } from '../../services/api';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Text } = Typography;

// Catálogo codigoPorcentaje SRI Ecuador — actualizar si el SRI modifica las tarifas
const SRI_CODES = [
  { value: '4',  label: 'IVA 15% — código 4 (vigente desde abr 2024)', pct: 15 },
  { value: '10', label: 'IVA 13% — código 10 (ene-abr 2024)',           pct: 13 },
  { value: '8',  label: 'IVA 8% — código 8 (feriados)',                 pct: 8  },
  { value: '2',  label: 'IVA 12% — código 2 (hasta dic 2023)',          pct: 12 },
  { value: '0',  label: 'IVA 0% — código 0',                            pct: 0  },
  { value: '6',  label: 'Exento de IVA — código 6',                     pct: 0  },
  { value: '5',  label: 'No objeto de IVA — código 5',                  pct: 0  },
];

export default function TaxConfig() {
  const { hasPermission } = useAuth();
  const canManageTax = hasPermission(PERMISSIONS.billing.taxManage);
  const canManageSri = hasPermission(PERMISSIONS.billing.sriManage);
  // ── Tarifas de IVA ──────────────────────────────────────────────────────────
  const [rates, setRates]             = useState<TaxRateDto[]>([]);
  const [loadingRates, setLoadingRates] = useState(true);
  const [rateModalOpen, setRateModalOpen] = useState(false);
  const [editingRate, setEditingRate] = useState<TaxRateDto | null>(null);
  const [savingRate, setSavingRate]   = useState(false);
  const [rateForm] = Form.useForm();

  // ── Config fiscal ────────────────────────────────────────────────────────────
  const [loadingConfig, setLoadingConfig] = useState(true);
  const [savingConfig, setSavingConfig]   = useState(false);
  const [configForm] = Form.useForm<BranchTaxConfigDto>();
  const ambienteWatch = Form.useWatch('ambiente', configForm);

  // ── Certificado .p12 ─────────────────────────────────────────────────────────
  const [certStatus, setCertStatus]     = useState<SriCertificateStatusDto | null>(null);
  const [uploadModal, setUploadModal]   = useState(false);
  const [uploadFile, setUploadFile]     = useState<UploadFile | null>(null);
  const [uploadPassword, setUploadPassword] = useState('');
  const [uploading, setUploading]       = useState(false);

  // ── Ping ─────────────────────────────────────────────────────────────────────
  const [pingResult, setPingResult] = useState<{ success: boolean; ambiente: string; error?: string } | null>(null);
  const [pinging, setPinging]       = useState(false);

  // ── SMTP ──────────────────────────────────────────────────────────────────────
  const [smtpConfig, setSmtpConfig]     = useState<SmtpConfigDto | null>(null);
  const [savingSmtp, setSavingSmtp]     = useState(false);
  const [testingSmtp, setTestingSmtp]   = useState(false);
  const [smtpTestResult, setSmtpTestResult] = useState<{ success: boolean; message: string } | null>(null);
  const [testEmail, setTestEmail]       = useState('');
  const [smtpForm] = Form.useForm();

  // ── Cargar datos ─────────────────────────────────────────────────────────────

  const loadRates = useCallback(async () => {
    setLoadingRates(true);
    try {
      const r = await taxApi.getTaxRates();
      setRates(r.data);
    } catch {
      message.error('Error al cargar tarifas de IVA');
    } finally {
      setLoadingRates(false);
    }
  }, []);

  const loadConfig = useCallback(async () => {
    setLoadingConfig(true);
    try {
      const [cfg, cert, smtp] = await Promise.all([
        sriApi.getConfig(),
        sriApi.getCertificateStatus(),
        sriApi.getSmtpConfig().catch(() => null),
      ]);
      if (cfg.data) configForm.setFieldsValue(cfg.data);
      setCertStatus(cert.data);
      if (smtp?.data) {
        setSmtpConfig(smtp.data);
        smtpForm.setFieldsValue({ ...smtp.data, password: undefined });
      }
    } catch {
      // primera vez sin config → formulario vacío
    } finally {
      setLoadingConfig(false);
    }
  }, [configForm, smtpForm]);

  useEffect(() => {
    loadRates();
    loadConfig();
  }, [loadConfig, loadRates]);

  // ── Tarifas ──────────────────────────────────────────────────────────────────

  const openCreateRate = () => {
    setEditingRate(null);
    rateForm.resetFields();
    rateForm.setFieldsValue({ isDefault: false, isActive: true, sriCode: '4', percentage: 15 });
    setRateModalOpen(true);
  };

  const openEditRate = (r: TaxRateDto) => {
    setEditingRate(r);
    rateForm.setFieldsValue(r);
    setRateModalOpen(true);
  };

  const handleSaveRate = async () => {
    const values = await rateForm.validateFields() as UpsertTaxRateDto;
    setSavingRate(true);
    try {
      if (editingRate) {
        await taxApi.updateTaxRate(editingRate.id, values);
        message.success('Tarifa actualizada');
      } else {
        await taxApi.createTaxRate(values);
        message.success('Tarifa creada');
      }
      setRateModalOpen(false);
      loadRates();
    } catch {
      message.error('Error al guardar tarifa');
    } finally {
      setSavingRate(false);
    }
  };

  const handleDeleteRate = async (id: string) => {
    try {
      await taxApi.deleteTaxRate(id);
      message.success('Tarifa eliminada');
      loadRates();
    } catch {
      message.error('Error al eliminar tarifa');
    }
  };

  // ── Config fiscal ─────────────────────────────────────────────────────────────

  const handleSaveConfig = async () => {
    const values = await configForm.validateFields();
    setSavingConfig(true);
    try {
      await sriApi.upsertConfig(values);
      message.success('Configuración fiscal guardada');
      loadConfig();
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setSavingConfig(false);
    }
  };

  // ── SMTP ──────────────────────────────────────────────────────────────────────

  const handleSaveSmtp = async () => {
    const values = await smtpForm.validateFields();
    setSavingSmtp(true);
    try {
      const r = await sriApi.upsertSmtpConfig(values);
      setSmtpConfig(r.data);
      smtpForm.setFieldsValue({ ...r.data, password: undefined });
      message.success('Configuración de correo guardada');
    } catch {
      message.error('Error al guardar configuración de correo');
    } finally {
      setSavingSmtp(false);
    }
  };

  const handleTestSmtp = async () => {
    if (!testEmail.trim()) { message.warning('Ingresa el correo de destino para la prueba'); return; }
    setTestingSmtp(true);
    setSmtpTestResult(null);
    try {
      const r = await sriApi.testSmtpConnection(testEmail);
      setSmtpTestResult(r.data);
    } catch {
      setSmtpTestResult({ success: false, message: 'No se pudo conectar al servidor SMTP' });
    } finally {
      setTestingSmtp(false);
    }
  };

  // ── Ping ──────────────────────────────────────────────────────────────────────

  const ping = async () => {
    const ambiente = configForm.getFieldValue('ambiente') ?? '1';
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

  // ── Certificado ───────────────────────────────────────────────────────────────

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

  // ── Columnas de tarifas ───────────────────────────────────────────────────────

  const rateColumns = [
    { title: 'Nombre', dataIndex: 'name', key: 'name' },
    { title: 'Porcentaje', dataIndex: 'percentage', key: 'percentage', render: (v: number) => `${v}%` },
    {
      title: 'Código SRI',
      dataIndex: 'sriCode',
      key: 'sriCode',
      render: (v: string) => <Tag>{v}</Tag>,
    },
    {
      title: 'Por defecto',
      dataIndex: 'isDefault',
      key: 'isDefault',
      render: (v: boolean) => v ? <Tag color="blue">Sí</Tag> : null,
    },
    {
      title: 'Estado',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (v: boolean) => <Tag color={v ? 'green' : 'red'}>{v ? 'Activo' : 'Inactivo'}</Tag>,
    },
    ...(canManageTax ? [{
      title: 'Acciones',
      key: 'actions',
      render: (_: unknown, record: TaxRateDto) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEditRate(record)} />
          <Popconfirm title="¿Eliminar tarifa?" onConfirm={() => handleDeleteRate(record.id)} okText="Sí" cancelText="No">
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    }] : []),
  ];

  // ── Render ────────────────────────────────────────────────────────────────────

  return (
    <div style={{ padding: 24, maxWidth: 960 }}>
      <Spin spinning={loadingConfig}>
        <Space direction="vertical" size="large" style={{ width: '100%' }}>

          {/* ── Datos del emisor ── */}
          <Card title={<Space><BankOutlined /><Text strong>Datos fiscales (SRI Ecuador)</Text></Space>}>
            {!loadingConfig && (
              <Form
                form={configForm}
                layout="vertical"
                initialValues={{ codigoEstablecimiento: '001', puntoEmision: '001', ambiente: '1', secuencialInicial: 1 }}
              >
                {ambienteWatch === '2' && (
                  <Alert
                    type="warning"
                    showIcon
                    message="Modo Producción activo — los comprobantes emitidos tendrán validez legal ante el SRI."
                    style={{ marginBottom: 16 }}
                  />
                )}
                <Row gutter={16}>
                  <Col xs={24} md={12}>
                    <Form.Item name="ruc" label="RUC" rules={[{ required: true, len: 13, message: 'El RUC debe tener 13 dígitos' }]}>
                      <Input maxLength={13} placeholder="0123456789001" />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={12}>
                    <Form.Item name="ambiente" label="Ambiente SRI" rules={[{ required: true }]}>
                      <Select options={[{ value: '1', label: 'Pruebas (SRI)' }, { value: '2', label: 'Producción' }]} />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={12}>
                    <Form.Item name="razonSocial" label="Razón social" rules={[{ required: true }]}>
                      <Input maxLength={300} />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={12}>
                    <Form.Item name="nombreComercial" label="Nombre comercial">
                      <Input maxLength={300} />
                    </Form.Item>
                  </Col>
                  <Col xs={24}>
                    <Form.Item name="direccion" label="Dirección matriz" rules={[{ required: true }]}>
                      <Input maxLength={300} />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={6}>
                    <Form.Item name="codigoEstablecimiento" label="Establecimiento" rules={[{ required: true, len: 3 }]}>
                      <Input maxLength={3} placeholder="001" />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={6}>
                    <Form.Item name="puntoEmision" label="Punto de emisión" rules={[{ required: true, len: 3 }]}>
                      <Input maxLength={3} placeholder="001" />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={6}>
                    <Form.Item
                      name="secuencialInicial"
                      label="Secuencial inicial"
                      tooltip="Número desde el que empezará la facturación. Cambiar este valor o el punto de emisión reinicia el contador."
                      rules={[{ required: true, type: 'number', min: 1, message: 'Mínimo 1' }]}
                    >
                      <InputNumber min={1} style={{ width: '100%' }} />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={6}>
                    <Form.Item name="contribuyenteEspecial" label="Nº Contribuyente especial" tooltip="Dejar vacío si no aplica">
                      <Input maxLength={20} placeholder="— no aplica —" />
                    </Form.Item>
                  </Col>
                  <Col xs={24}>
                    <Form.Item name="obligadoContabilidad" label="Obligado a llevar contabilidad" valuePropName="checked">
                      <Switch />
                    </Form.Item>
                  </Col>
                </Row>

                <Divider />

                <Space>
                  {canManageSri && <Button type="primary" onClick={handleSaveConfig} loading={savingConfig}>
                    Guardar configuración fiscal
                  </Button>}
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
            )}
          </Card>

          {/* ── Certificado .p12 ── */}
          <Card
            title={<Space><SafetyCertificateOutlined /><Text strong>Certificado de firma electrónica (.p12)</Text></Space>}
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
                          {dayjs(certStatus.expiresAt).format('DD/MM/YYYY')}
                        </Text>
                      </div>
                    </div>
                  )}
                  {certStatus.uploadedAt && (
                    <div>
                      <Text type="secondary" style={{ fontSize: 12 }}>Cargado el</Text>
                      <div><Text>{dayjs(certStatus.uploadedAt).format('DD/MM/YYYY')}</Text></div>
                    </div>
                  )}
                </div>
                <Divider style={{ margin: '12px 0' }} />
                {canManageSri && <Space>
                  <Button icon={<CloudUploadOutlined />} onClick={() => setUploadModal(true)}>
                    Reemplazar certificado
                  </Button>
                  <Popconfirm title="¿Eliminar el certificado?" onConfirm={deleteCert} okText="Sí" cancelText="No">
                    <Button danger icon={<DeleteOutlined />}>Eliminar certificado</Button>
                  </Popconfirm>
                </Space>}
              </Space>
            ) : (
              <Space direction="vertical">
                <Text type="secondary">
                  No hay certificado cargado. Para emitir facturas electrónicas debes cargar
                  el archivo .p12 entregado por el BCE (Banco Central del Ecuador).
                </Text>
                {canManageSri && <Button type="primary" icon={<CloudUploadOutlined />} onClick={() => setUploadModal(true)}>
                  Cargar certificado .p12
                </Button>}
              </Space>
            )}
          </Card>

          {/* ── Configuración SMTP ── */}
          <Card title={<Space><MailOutlined /><Text strong>Envío de facturas por correo (SMTP)</Text></Space>}>
            <Form
              form={smtpForm}
              layout="vertical"
              initialValues={{ port: 587, enableSsl: true, isActive: true }}
            >
              <Row gutter={16}>
                <Col xs={24} md={14}>
                  <Form.Item name="host" label="Servidor SMTP" rules={[{ required: true }]}>
                    <Input placeholder="smtp.gmail.com" />
                  </Form.Item>
                </Col>
                <Col xs={24} md={4}>
                  <Form.Item name="port" label="Puerto" rules={[{ required: true }]}>
                    <InputNumber min={1} max={65535} style={{ width: '100%' }} />
                  </Form.Item>
                </Col>
                <Col xs={24} md={6}>
                  <Form.Item name="enableSsl" label="Seguridad" valuePropName="checked">
                    <Switch checkedChildren="TLS/SSL" unCheckedChildren="Sin cifrado" />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item name="username" label="Usuario" rules={[{ required: true }]}>
                    <Input placeholder="correo@empresa.com" />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    name="password"
                    label={smtpConfig?.hasPassword ? 'Contraseña (dejar vacío para no cambiar)' : 'Contraseña'}
                    rules={smtpConfig?.hasPassword ? [] : [{ required: true }]}
                  >
                    <Input.Password placeholder={smtpConfig?.hasPassword ? '••••••••' : 'Contraseña SMTP'} />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item name="fromEmail" label="Correo remitente" rules={[{ required: true, type: 'email' }]}>
                    <Input placeholder="facturas@empresa.com" />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item name="fromName" label="Nombre remitente" rules={[{ required: true }]}>
                    <Input placeholder="El Caldero Flameante" />
                  </Form.Item>
                </Col>
                <Col xs={24}>
                  <Form.Item name="isActive" label="Envío activo" valuePropName="checked">
                    <Switch />
                  </Form.Item>
                </Col>
              </Row>

              <Divider />

              <Space wrap>
                {canManageSri && <Button type="primary" icon={<MailOutlined />} onClick={handleSaveSmtp} loading={savingSmtp}>
                  Guardar configuración de correo
                </Button>}
                <Input
                  placeholder="correo@prueba.com"
                  value={testEmail}
                  onChange={e => setTestEmail(e.target.value)}
                  style={{ width: 220 }}
                />
                <Button icon={<SendOutlined />} onClick={handleTestSmtp} loading={testingSmtp} disabled={!smtpConfig}>
                  Enviar correo de prueba
                </Button>
              </Space>

              {smtpTestResult && (
                <Alert
                  style={{ marginTop: 12 }}
                  type={smtpTestResult.success ? 'success' : 'error'}
                  icon={smtpTestResult.success ? <CheckCircleOutlined /> : <CloseCircleOutlined />}
                  showIcon
                  message={smtpTestResult.message}
                />
              )}
            </Form>
          </Card>

          {/* ── Tarifas de IVA ── */}
          <Card
            title={<Space><PercentageOutlined /><Text strong>Tarifas de IVA</Text></Space>}
            extra={canManageTax ? <Button type="primary" icon={<PlusOutlined />} onClick={openCreateRate}>Nueva tarifa</Button> : null}
          >
            <Table
              dataSource={rates}
              columns={rateColumns}
              rowKey="id"
              loading={loadingRates}
              pagination={{ defaultPageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
              size="small"
            />
          </Card>

        </Space>
      </Spin>

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

      {/* ── Modal tarifa ── */}
      <Modal
        title={editingRate ? 'Editar tarifa de IVA' : 'Nueva tarifa de IVA'}
        open={rateModalOpen}
        onOk={handleSaveRate}
        onCancel={() => setRateModalOpen(false)}
        confirmLoading={savingRate}
        okText="Guardar"
        cancelText="Cancelar"
      >
        <Form form={rateForm} layout="vertical">
          <Form.Item name="name" label="Nombre" rules={[{ required: true }]}>
            <Input placeholder="Ej: IVA 15%" />
          </Form.Item>
          <Form.Item name="sriCode" label="Código SRI (codigoPorcentaje)" rules={[{ required: true }]}>
            <Select
              options={SRI_CODES}
              onChange={(val) => {
                const found = SRI_CODES.find(c => c.value === val);
                if (found) rateForm.setFieldValue('percentage', found.pct);
              }}
            />
          </Form.Item>
          <Form.Item name="percentage" label="Porcentaje (%)" rules={[{ required: true }]}>
            <InputNumber min={0} max={100} step={0.01} style={{ width: '100%' }} addonAfter="%" />
          </Form.Item>
          <Space>
            <Form.Item name="isDefault" valuePropName="checked" label="Por defecto">
              <Switch />
            </Form.Item>
            <Form.Item name="isActive" valuePropName="checked" label="Activo">
              <Switch />
            </Form.Item>
          </Space>
        </Form>
      </Modal>
    </div>
  );
}
