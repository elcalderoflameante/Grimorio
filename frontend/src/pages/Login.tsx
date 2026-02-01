import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Form, Input, Button, Card, message, Spin } from 'antd';
import { UserOutlined, LockOutlined } from '@ant-design/icons';
import { authService } from '../services/api';
import { useAuth } from '../context/AuthContext';
import { jwtDecode } from 'jwt-decode';
import { formatError } from '../utils/errorHandler';

interface DecodedToken {
  BranchId?: string;
  branchId?: string;
  [key: string]: any;
}

interface LoginFormValues {
  email: string;
  password: string;
}

export default function Login() {
  const [loading, setLoading] = useState(false);
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const { login } = useAuth();

  const onFinish = async (values: LoginFormValues) => {
    setLoading(true);
    try {
      const response = await authService.login(values.email, values.password);
      const { accessToken, userId, email, firstName, lastName } = response.data;

      // Decodificar JWT para obtener claims (incluyendo branchId)
      const decoded = jwtDecode<DecodedToken>(accessToken);
      const branchId = decoded.BranchId || decoded.branchId || '';

      // Guardar en contexto y localStorage
      login({ userId, email, firstName, lastName }, accessToken, branchId);

      message.success('¡Bienvenido!');
      navigate('/dashboard');
    } catch (error: any) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{
      display: 'flex',
      justifyContent: 'center',
      alignItems: 'center',
      minHeight: '100vh',
      backgroundColor: '#f0f2f5',
    }}>
      <Card
        style={{
          maxWidth: 400,
          width: '100%',
          margin: '0 auto',
          boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
        }}
        
        title="Grimorio - El Caldero Flameante"
      >
        <Spin spinning={loading}>
          <Form
            form={form}
            onFinish={onFinish}
            layout="vertical"
          >
            <Form.Item
              label="Email"
              name="email"
              rules={[
                { required: true, message: 'El email es requerido' },
                { type: 'email', message: 'Email inválido' },
              ]}
            >
              <Input
                prefix={<UserOutlined />}
                placeholder="admin@eldoflameante.ec"
              />
            </Form.Item>

            <Form.Item
              label="Contraseña"
              name="password"
              rules={[
                { required: true, message: 'La contraseña es requerida' },
              ]}
            >
              <Input.Password
                prefix={<LockOutlined />}
                placeholder="Contraseña"
              />
            </Form.Item>

            <Form.Item>
              <Button
                type="primary"
                htmlType="submit"
                block
                size="large"
              >
                Iniciar Sesión
              </Button>
            </Form.Item>
          </Form>

          <div style={{ textAlign: 'center', marginTop: 16, color: '#999' }}>
            <small>admin@eldoflameante.ec / Admin123!</small>
          </div>
        </Spin>
      </Card>
    </div>
  );
}
