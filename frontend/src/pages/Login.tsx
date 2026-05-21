import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Checkbox, ConfigProvider, Form, Input, message, theme as antdTheme } from 'antd';
import { FireFilled, LockOutlined, ThunderboltFilled, UserOutlined } from '@ant-design/icons';
import { jwtDecode } from 'jwt-decode';
import { authApi } from '../services/api';
import { useAuth } from '../context/useAuth';
import { formatError } from '../utils/errorHandler';
import ecfLogo from '../assets/ECF-Logo.png';
import './Login.css';

const PALETTE = {
  bg0: '#0e0d0a',
  bg1: '#1b1814',
  bg2: '#2a2520',
  surface: '#15130f',
  surfaceElev: '#1f1b15',
  border: 'rgba(180,140,80,0.22)',
  borderStrong: 'rgba(180,140,80,0.55)',
  gold: '#b88a3a',
  goldBright: '#d9a655',
  ember: '#ff6a1a',
  emberDeep: '#a02e08',
  cream: '#e8dcc0',
  text: '#e8dcc0',
  textMuted: 'rgba(232,220,192,0.6)',
  textFaint: 'rgba(232,220,192,0.38)',
  danger: '#e07050',
};

interface DecodedToken {
  BranchId?: string;
  [key: string]: unknown;
}

interface LoginFormValues {
  email: string;
  password: string;
  remember?: boolean;
}

function hexA(hex: string, alpha: number) {
  const h = hex.replace('#', '');
  const value = h.length === 3 ? h.split('').map((c) => c + c).join('') : h;
  const r = parseInt(value.slice(0, 2), 16);
  const g = parseInt(value.slice(2, 4), 16);
  const b = parseInt(value.slice(4, 6), 16);
  return `rgba(${r},${g},${b},${alpha})`;
}

function Embers() {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return undefined;

    const ctx = canvas.getContext('2d');
    if (!ctx) return undefined;

    let animation = 0;
    let running = true;
    const dpr = Math.min(window.devicePixelRatio || 1, 2);

    const resize = () => {
      canvas.width = canvas.clientWidth * dpr;
      canvas.height = canvas.clientHeight * dpr;
    };

    const spawn = () => ({
      x: Math.random() * canvas.width,
      y: canvas.height + Math.random() * canvas.height * 0.4,
      vx: (Math.random() - 0.5) * 0.25 * dpr,
      vy: -(0.2 + Math.random() * 0.6) * dpr,
      r: (0.6 + Math.random() * 1.8) * dpr,
      life: 0,
      maxLife: 220 + Math.random() * 420,
      flicker: Math.random() * Math.PI * 2,
    });

    resize();
    window.addEventListener('resize', resize);

    const particles = Array.from({ length: 60 }, spawn);

    const tick = () => {
      if (!running) return;
      ctx.clearRect(0, 0, canvas.width, canvas.height);

      for (const particle of particles) {
        particle.x += particle.vx;
        particle.y += particle.vy;
        particle.vy -= 0.002 * dpr;
        particle.vx += (Math.random() - 0.5) * 0.03 * dpr;
        particle.life += 1;
        particle.flicker += 0.1;

        const fade = 1 - particle.life / particle.maxLife;
        if (fade <= 0 || particle.y < -20) {
          Object.assign(particle, spawn(), { y: canvas.height + 10 });
          continue;
        }

        const flicker = 0.6 + 0.4 * Math.sin(particle.flicker);
        const alpha = Math.max(0, fade * flicker);
        const radius = particle.r;
        const glow = ctx.createRadialGradient(particle.x, particle.y, 0, particle.x, particle.y, radius * 6);

        glow.addColorStop(0, hexA(PALETTE.ember, alpha));
        glow.addColorStop(0.4, hexA(PALETTE.emberDeep, alpha * 0.5));
        glow.addColorStop(1, hexA(PALETTE.emberDeep, 0));

        ctx.fillStyle = glow;
        ctx.beginPath();
        ctx.arc(particle.x, particle.y, radius * 6, 0, Math.PI * 2);
        ctx.fill();

        ctx.fillStyle = hexA('#ffffff', alpha * 0.9);
        ctx.beginPath();
        ctx.arc(particle.x, particle.y, radius * 0.8, 0, Math.PI * 2);
        ctx.fill();
      }

      animation = requestAnimationFrame(tick);
    };

    tick();

    return () => {
      running = false;
      cancelAnimationFrame(animation);
      window.removeEventListener('resize', resize);
    };
  }, []);

  return <canvas ref={canvasRef} className="grim-login-embers" aria-hidden />;
}

function CornerOrnament({ className }: { className: string }) {
  return (
    <svg className={className} viewBox="0 0 80 80" fill="none" stroke="currentColor" strokeWidth="1.1" strokeLinecap="round" aria-hidden>
      <path d="M2 2 L40 2" />
      <path d="M2 2 L2 40" />
      <path d="M2 2 L14 14" opacity="0.6" />
      <circle cx="14" cy="14" r="1.6" fill="currentColor" stroke="none" />
      <path d="M40 2 Q 36 8 30 8 Q 34 12 30 16" opacity="0.7" />
      <path d="M2 40 Q 8 36 8 30 Q 12 34 16 30" opacity="0.7" />
      <path d="M22 22 Q 26 18 30 22 Q 26 26 22 22 Z" opacity="0.5" />
    </svg>
  );
}

function Flourish() {
  return (
    <div className="grim-login-flourish" aria-hidden>
      <span />
      <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
        <path d="M12 2 L13.5 9 L21 10.5 L13.5 12 L12 21 L10.5 12 L3 10.5 L10.5 9 Z" />
      </svg>
      <span />
    </div>
  );
}

export default function Login() {
  const [loading, setLoading] = useState(false);
  const [capsOn, setCapsOn] = useState(false);
  const [form] = Form.useForm<LoginFormValues>();
  const [messageApi, contextHolder] = message.useMessage();
  const navigate = useNavigate();
  const { login } = useAuth();

  const onFinish = async (values: LoginFormValues) => {
    setLoading(true);
    try {
      const response = await authApi.login(values.email, values.password);
      const { accessToken, userId, email, firstName, lastName } = response.data;
      const decoded = jwtDecode<DecodedToken>(accessToken);
      const branchId = decoded.BranchId || '';

      login({ userId, email, firstName, lastName }, accessToken, branchId);

      messageApi.success('El grimorio se abre');
      navigate('/dashboard');
    } catch (error: unknown) {
      messageApi.error(formatError(error));
    } finally {
      setLoading(false);
    }
  };

  const loginTheme = {
    algorithm: antdTheme.darkAlgorithm,
    token: {
      colorPrimary: PALETTE.gold,
      colorInfo: PALETTE.gold,
      colorBgBase: PALETTE.surface,
      colorBgContainer: 'rgba(255,255,255,0.04)',
      colorBgElevated: PALETTE.surfaceElev,
      colorBorder: PALETTE.border,
      colorError: PALETTE.danger,
      colorIcon: PALETTE.textMuted,
      colorIconHover: PALETTE.gold,
      colorText: PALETTE.text,
      colorTextBase: PALETTE.text,
      colorTextPlaceholder: PALETTE.textFaint,
      colorTextSecondary: PALETTE.textMuted,
      borderRadius: 8,
      borderRadiusLG: 10,
      controlHeight: 44,
      controlHeightLG: 48,
      fontFamily: '"Inter", ui-sans-serif, system-ui, -apple-system, sans-serif',
      fontSize: 14,
      lineWidth: 1,
    },
    components: {
      Button: {
        primaryShadow: `0 8px 24px ${hexA(PALETTE.ember, 0.35)}`,
      },
      Checkbox: {
        colorPrimary: PALETTE.gold,
      },
      Form: {
        labelColor: PALETTE.textMuted,
      },
      Input: {
        activeBorderColor: PALETTE.gold,
        activeShadow: `0 0 0 2px ${hexA(PALETTE.gold, 0.18)}`,
        hoverBorderColor: PALETTE.borderStrong,
      },
    },
  };

  return (
    <ConfigProvider theme={loginTheme}>
      {contextHolder}
      <div className="grim-login-root">
        <div className="grim-login-bg" />
        <div className="grim-login-vignette" />
        <div className="grim-login-stars" aria-hidden />
        <Embers />

        <div className="grim-login-stage">
          <aside className="grim-login-side">
            <div className="grim-login-side-inner">
              <img src={ecfLogo} alt="El Caldero Flameante" className="grim-login-side-logo" />
              <div className="grim-login-side-copy">
                <div className="grim-login-side-eyebrow">Hoy en la cocina</div>
              <h2 className="grim-login-side-title">Que el caldero hierva con tu turno.</h2>
              <p className="grim-login-side-body">
                Pedidos, inventario, recetas y operaciones del restaurante, reunidos en un solo libro.
              </p>
              <Flourish />
              <ul className="grim-login-side-list">
                <li><FireFilled /> Ventas, caja y mesas bajo control</li>
                <li><ThunderboltFilled /> Permisos listos para produccion</li>
                <li><span>★</span> Inventario conectado con recetas</li>
              </ul>
              </div>
            </div>
          </aside>

          <main className="grim-login-card-wrap">
            <section className="grim-login-card">
              <CornerOrnament className="grim-login-corner top-left" />
              <CornerOrnament className="grim-login-corner top-right" />
              <CornerOrnament className="grim-login-corner bottom-left" />
              <CornerOrnament className="grim-login-corner bottom-right" />

              <h1 className="grim-login-title">Bienvenido al Grimorio</h1>
              <p className="grim-login-subtitle">El Caldero Flameante · Sistema de gestion</p>

              <Flourish />

              <Form
                form={form}
                layout="vertical"
                size="large"
                requiredMark={false}
                onFinish={onFinish}
                initialValues={{ remember: true }}
                className="grim-login-form"
              >
                <Form.Item
                  name="email"
                  label="Usuario o correo"
                  rules={[
                    { required: true, message: 'Ingresa tu usuario o correo' },
                    { type: 'email', message: 'Email invalido' },
                  ]}
                >
                  <Input prefix={<UserOutlined />} placeholder="usuario@empresa.com" autoComplete="email" />
                </Form.Item>

                <Form.Item
                  name="password"
                  label={
                    <div className="grim-login-password-label">
                      <span>Contrasena secreta</span>
                      {capsOn && <span>Bloq Mayus activo</span>}
                    </div>
                  }
                  rules={[{ required: true, message: 'Ingresa tu contrasena' }]}
                >
                  <Input.Password
                    prefix={<LockOutlined />}
                    placeholder="Tu contrasena"
                    autoComplete="current-password"
                    onKeyUp={(event) => setCapsOn(event.getModifierState('CapsLock'))}
                    onKeyDown={(event) => setCapsOn(event.getModifierState('CapsLock'))}
                  />
                </Form.Item>

                <div className="grim-login-actions-row">
                  <Form.Item name="remember" valuePropName="checked" noStyle>
                    <Checkbox>Recordar este dispositivo</Checkbox>
                  </Form.Item>
                  <button
                    type="button"
                    className="grim-login-link"
                    onClick={() => messageApi.info('Contacta al administrador para recuperar el acceso.')}
                  >
                    Olvidaste tu contrasena?
                  </button>
                </div>

                <Form.Item className="grim-login-submit-item">
                  <Button
                    type="primary"
                    htmlType="submit"
                    block
                    loading={loading}
                    className="grim-login-submit"
                    icon={!loading ? <ThunderboltFilled /> : undefined}
                  >
                    Abrir el Grimorio
                  </Button>
                </Form.Item>
              </Form>

              <div className="grim-login-foot">Solo usuarios autorizados · El Caldero Flameante</div>
            </section>
          </main>
        </div>
      </div>
    </ConfigProvider>
  );
}
