import { useCallback, useEffect, useState } from 'react';
import { Alert, App, Button, Descriptions, Input, Space, Spin, Tag, Tooltip, Typography } from 'antd';
import { CheckCircleOutlined, CopyOutlined, ExportOutlined, ReloadOutlined } from '@ant-design/icons';
import { loyaltyWhatsAppApi } from '../../services/loyaltyWhatsAppApi';
import type { LoyaltyWhatsAppSetup as Setup } from '../../services/loyaltyWhatsAppApi';
import { formatError } from '../../utils/errorHandler';

const labels: Record<string, string> = {
  Mode: 'Modo de conexion', PublicBranchId: 'Sucursal del programa', PhoneNumberId: 'ID del numero real en Meta',
  TestPhoneNumberId: 'ID del numero de prueba', BusinessAccountId: 'ID de la cuenta de WhatsApp (WABA)',
  BusinessPhone: 'Telefono esperado', AccessToken: 'Token de acceso', AuthenticationTemplate: 'Plantilla de autenticacion',
  TemplateLanguage: 'Idioma de plantilla', CodeHashKey: 'Clave privada de verificacion', AppSecret: 'Secreto de la app de Meta',
  WebhookVerifyToken: 'Token de verificacion del webhook', AllowedTestPhones: 'Telefonos autorizados para pruebas',
  PublicAppUrl: 'Dominio publico HTTPS', ErpAppUrl: 'Dominio HTTPS del ERP', GraphVersion: 'Version de Graph API',
  BusinessAppCoexistenceConfirmed: 'Confirmacion de coexistencia con WhatsApp Business', DailySendLimit: 'Limite diario de codigos',
};
export default function LoyaltyWhatsAppSetup() {
  const { message } = App.useApp();
  const [data, setData] = useState<Setup>();
  const [loading, setLoading] = useState(true);
  const [checking, setChecking] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<{ success: boolean; message: string }>();
  const load = useCallback(async () => {
    setLoading(true); setError(''); setResult(undefined);
    try { setData((await loyaltyWhatsAppApi.setup()).data); }
    catch (e) { setError(formatError(e)); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { void load(); }, [load]);
  async function check() {
    setChecking(true); setResult(undefined);
    try { setResult((await loyaltyWhatsAppApi.check()).data); }
    catch (e) { setResult({ success: false, message: formatError(e) }); }
    finally { setChecking(false); }
  }
  function link(label: string, value?: string) {
    return <div style={{ marginTop: 12 }} key={label}>
      <Typography.Text>{label}</Typography.Text>
      <Space.Compact style={{ width: '100%', marginTop: 4 }}>
        <Input aria-label={label} readOnly value={value ?? ''} placeholder="Pendiente de configurar" onFocus={e => e.target.select()} />
        <Tooltip title={`Copiar ${label.toLowerCase()}`}><Button icon={<CopyOutlined />} aria-label={`Copiar ${label.toLowerCase()}`} disabled={!value} onClick={() => {
          if (!navigator.clipboard || !value) { message.info('Selecciona el enlace para copiarlo'); return; }
          void navigator.clipboard.writeText(value).then(() => message.success('Enlace copiado')).catch(() => message.error('No se pudo copiar'));
        }} /></Tooltip>
        <Tooltip title={`Abrir ${label.toLowerCase()}`}><Button icon={<ExportOutlined />} aria-label={`Abrir ${label.toLowerCase()}`}
          href={value} disabled={!value} target="_blank" rel="noopener noreferrer" /></Tooltip>
      </Space.Compact>
    </div>;
  }
  return <section aria-label="Conexion de WhatsApp" style={{ borderBottom: '1px solid #ddd', paddingBottom: 24, marginBottom: 24 }}>
    <div style={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'center', gap: 12, marginBottom: 16 }}>
      <Typography.Title level={5} style={{ margin: 0 }}>Conexion de WhatsApp</Typography.Title>
      <Space wrap>
        <Tooltip title="Actualizar conexion"><Button icon={<ReloadOutlined />} aria-label="Actualizar conexion" loading={loading} onClick={() => void load()} /></Tooltip>
        <Button icon={<CheckCircleOutlined />} loading={checking} disabled={!data?.canCheckConnection || loading} onClick={() => void check()}>Comprobar Meta</Button>
      </Space>
    </div>
    {error && <Alert type="error" showIcon title={error} />}
    <Spin spinning={loading}>
      {data && <>
        <Space wrap style={{ marginBottom: 12 }}>
          <Tag color={data.ready ? 'green' : 'gold'}>{data.ready ? 'Envios habilitados por configuracion' : 'Pendiente de habilitacion'}</Tag>
          <Tag>{data.mode === 'Production' ? 'Produccion' : 'Pruebas'}</Tag>
          <Tag color={data.restricted ? 'blue' : 'green'}>{data.restricted ? 'Solo telefonos autorizados' : 'Afiliacion publica'}</Tag>
        </Space>
        <Descriptions size="small" column={{ xs: 1, sm: 2 }} items={[
          { key: 'phone', label: 'Numero esperado', children: data.businessPhone || 'Pendiente' },
          { key: 'limit', label: 'Maximo de solicitudes / 24 h', children: data.dailySendLimit },
          { key: 'template', label: 'Plantilla', children: <span style={{ overflowWrap: 'anywhere' }}>{data.authenticationTemplate || 'Pendiente'} ({data.templateLanguage})</span> },
          { key: 'program', label: 'Programa', children: data.programActive ? 'Activo' : 'Inactivo' },
        ]} />
        {data.missing.length > 0 && <Alert type="warning" showIcon title="Configuracion pendiente" style={{ marginTop: 12 }}
          description={<ul style={{ margin: '8px 0', paddingLeft: 20 }}>{data.missing.map(key => <li key={key}><Tooltip title={`LoyaltyWhatsApp__${key}`}>{labels[key] ?? key}</Tooltip></li>)}</ul>} />}
        {result && <Alert type={result.success ? 'success' : 'error'} showIcon title={result.message} style={{ marginTop: 12 }} />}
        {link('URL del webhook', data.webhookUrl)}
        {link('Enlace de afiliacion', data.enrollmentUrl)}
        {link('Politica de privacidad', data.privacyUrl)}
        {link('Eliminacion de datos', data.deletionUrl)}
        <Space wrap style={{ marginTop: 16 }}>
          <Button href={data.developerUrl} target="_blank" rel="noopener noreferrer" icon={<ExportOutlined />}>App de Meta</Button>
          <Button href={data.managerUrl} target="_blank" rel="noopener noreferrer" icon={<ExportOutlined />}>Administrador de WhatsApp</Button>
        </Space>
      </>}
    </Spin>
  </section>;
}
