import { useEffect, useState } from 'react';
import { Alert, Button, Checkbox, Col, Form, Input, InputNumber, Modal, Row, Segmented, Space, Spin, Tag, Typography } from 'antd';
import { ArrowLeftOutlined, CheckOutlined, ReloadOutlined, WhatsAppOutlined } from '@ant-design/icons';
import { Link, useNavigate } from 'react-router-dom';
import { publicLoyaltyApi, publicLoyaltyError } from '../services/publicLoyaltyApi';
import type { LoyaltyChallenge, PublicEnrollmentProfile, PublicLoyaltyInfo } from '../services/publicLoyaltyApi';

type Profile = PublicEnrollmentProfile & { phone: string; code?: string };
type Pending = LoyaltyChallenge & { phone: string; profile: PublicEnrollmentProfile; retryAt: number };

export default function PublicLoyaltyEnrollment() {
  const navigate = useNavigate();
  const [form] = Form.useForm<Profile>();
  const [info, setInfo] = useState<PublicLoyaltyInfo>();
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [mode, setMode] = useState<'Enroll' | 'Recover'>('Enroll');
  const [pending, setPending] = useState<Pending>();
  const [termsOpen, setTermsOpen] = useState(false);
  const [now, setNow] = useState(Date.now());
  useEffect(() => {
    let current = true;
    void publicLoyaltyApi.info().then(r => { if (current) setInfo(r.data); })
      .catch(e => { if (current) setError(publicLoyaltyError(e)); })
      .finally(() => { if (current) setLoading(false); });
    return () => { current = false; };
  }, []);
  useEffect(() => {
    if (!pending) return;
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, [pending]);

  async function request(phone: string, profile: PublicEnrollmentProfile) {
    setBusy(true); setError('');
    try {
      const { data } = await publicLoyaltyApi.request(phone, mode);
      setPending({ ...data, phone, profile, retryAt: Date.now() + data.retryAfterSeconds * 1000 });
      form.setFieldValue('code', ''); setNow(Date.now());
    } catch (e) { setError(publicLoyaltyError(e)); }
    finally { setBusy(false); }
  }
  async function submit(values: Profile) {
    if (!pending) {
      const profile: PublicEnrollmentProfile = mode === 'Enroll' ? {
        name: values.name, email: values.email, birthDay: values.birthDay, birthMonth: values.birthMonth,
        acceptsProgram: values.acceptsProgram, marketingConsent: values.marketingConsent ?? false,
      } : {};
      await request(values.phone, profile); return;
    }
    setBusy(true); setError('');
    try {
      const { data } = await publicLoyaltyApi.verify(pending.challengeId, values.code ?? '', pending.profile, info!.termsVersion);
      navigate(`/fidelizacion/${encodeURIComponent(data.publicToken)}`, { replace: true });
    } catch (e) { setError(publicLoyaltyError(e)); }
    finally { setBusy(false); }
  }
  const retrySeconds = pending ? Math.max(0, Math.ceil((pending.retryAt - now) / 1000)) : 0;
  const expired = pending && Date.parse(pending.expiresAt) <= now;
  return <main style={{ maxWidth: 520, margin: '0 auto', padding: '24px 20px 40px', minHeight: '100vh' }}>
    <header style={{ textAlign: 'center', marginBottom: 24 }}>
      <img src="/restaurant/logo.png" width={112} height={79} alt="El Caldero Flameante" style={{ objectFit: 'contain' }} />
      <Typography.Title level={3}>EL CALDERO FLAMEANTE</Typography.Title>
      <Typography.Title level={4}>{info?.programName ?? 'Recompensas'}</Typography.Title>
    </header>
    {loading ? <Spin /> : !info?.enabled ? <Alert showIcon type="info" title="Afiliacion en linea no disponible" description={error || 'Puedes consultar por tu tarjeta en caja.'} /> : <>
      {info.testMode && <Tag color="blue" style={{ marginBottom: 16 }}>Prueba limitada a telefonos autorizados</Tag>}
      {!pending && <Segmented<'Enroll' | 'Recover'> block value={mode} disabled={busy} options={[
        { label: 'Afiliarme', value: 'Enroll' }, { label: 'Recuperar tarjeta', value: 'Recover' },
      ]} onChange={value => { setMode(value); setError(''); form.resetFields(); }} style={{ marginBottom: 24 }} />}
      {error && <Alert role="alert" type="error" showIcon title={error} style={{ marginBottom: 16 }} />}
      {pending && <Typography.Paragraph>Codigo enviado por WhatsApp a <strong>{pending.phone}</strong>.</Typography.Paragraph>}
      {expired && <Alert type="warning" showIcon title="El codigo vencio. Solicita uno nuevo." style={{ marginBottom: 16 }} />}
      <Form form={form} layout="vertical" onFinish={submit} disabled={busy} requiredMark="optional">
        {!pending ? <>
          <Form.Item name="phone" label="Celular / WhatsApp" rules={[{ required: true, message: 'Ingresa tu celular.' }, { max: 40 }]}>
            <Input autoComplete="tel" inputMode="tel" placeholder="09XXXXXXXX o +593..." maxLength={40} />
          </Form.Item>
          {mode === 'Enroll' && <>
            <Form.Item name="name" label="Nombre" rules={[{ required: true, whitespace: true, message: 'Ingresa tu nombre.' }, { max: 200 }]}>
              <Input autoComplete="name" maxLength={200} />
            </Form.Item>
            <Form.Item name="email" label="Correo electronico" rules={[{ type: 'email', message: 'Revisa el correo.' }, { max: 254 }]}>
              <Input autoComplete="email" inputMode="email" maxLength={254} />
            </Form.Item>
            <Row gutter={12}>
              <Col span={12}><Form.Item name="birthDay" label="Dia de cumpleanos" dependencies={['birthMonth']} rules={[{ validator: async (_, day) => {
                const month = form.getFieldValue('birthMonth') as number | undefined;
                if ((day != null) !== (month != null) || (day != null && month != null && day > new Date(2000, month, 0).getDate()))
                  throw new Error('Revisa el dia y mes.');
              } }]}><InputNumber min={1} max={31} precision={0} style={{ width: '100%' }} /></Form.Item></Col>
              <Col span={12}><Form.Item name="birthMonth" label="Mes de cumpleanos"><InputNumber min={1} max={12} precision={0} style={{ width: '100%' }} /></Form.Item></Col>
            </Row>
            <Form.Item name="acceptsProgram" valuePropName="checked" rules={[{ validator: async (_, value) => { if (!value) throw new Error('Acepta las condiciones para participar.'); } }]}>
              <Checkbox>Acepto las condiciones del programa de recompensas.</Checkbox>
            </Form.Item>
            <Button type="link" onClick={() => setTermsOpen(true)} style={{ padding: 0, marginBottom: 16 }}>Ver condiciones</Button>
            <Form.Item name="marketingConsent" valuePropName="checked"><Checkbox>Deseo recibir promociones y novedades por WhatsApp o correo.</Checkbox></Form.Item>
          </>}
        </> : <Form.Item name="code" label="Codigo de verificacion" rules={[{ required: true, pattern: /^\d{6}$/, message: 'Ingresa los 6 digitos.' }]}>
          <Input autoFocus autoComplete="one-time-code" inputMode="numeric" maxLength={6} style={{ fontSize: 22, textAlign: 'center' }} />
        </Form.Item>}
        <Button block type="primary" htmlType="submit" loading={busy} disabled={Boolean(expired)} icon={pending ? <CheckOutlined /> : <WhatsAppOutlined />}>
          {pending ? 'Verificar y abrir mi tarjeta' : 'Recibir codigo por WhatsApp'}
        </Button>
      </Form>
      {pending && <Space orientation="vertical" style={{ marginTop: 16, width: '100%' }}>
        <Button block icon={<ReloadOutlined />} disabled={busy || retrySeconds > 0} onClick={() => void request(pending.phone, pending.profile)}>
          {retrySeconds > 0 ? `Reenviar en ${retrySeconds} s` : 'Reenviar codigo'}
        </Button>
        <Button block type="text" icon={<ArrowLeftOutlined />} disabled={busy} onClick={() => { setPending(undefined); setError(''); }}>Cambiar datos</Button>
      </Space>}
    </>}
    <p style={{ marginTop: 24 }}><Link to="/fidelizacion/privacidad">Privacidad</Link>{' | '}<Link to="/fidelizacion/eliminar-datos">Eliminacion de datos</Link></p>
    <Modal title="Condiciones del programa" open={termsOpen} footer={null} onCancel={() => setTermsOpen(false)}>
      <p>Una visita por dia con un consumo pagado minimo de ${info?.minimumSpend.toFixed(2)} {info?.includeTax ? 'incluido IVA' : 'sin IVA'}, despues de descuentos y excluyendo los productos no participantes. Afiliarse no suma visitas.</p>
      {info?.rewards.map(r => <p key={r.visitNumber}>Visita {r.visitNumber}{r.repeatEvery ? ` y cada ${r.repeatEvery} visitas posteriores` : ''}: {r.name}.</p>)}
      <p>Los premios estaran disponibles desde el siguiente dia y tendran una vigencia de {info?.rewardValidityDays} dias. No son canjeables por dinero.</p>
      <p>Usamos tu nombre y telefono para administrar tu tarjeta y verificar el acceso. El correo y cumpleanos son opcionales. Recibir publicidad es opcional y puedes retirar esa autorizacion en caja. Puedes solicitar alli la actualizacion o eliminacion de tus datos.</p>
    </Modal>
  </main>;
}
