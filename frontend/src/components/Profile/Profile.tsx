import { useCallback, useEffect, useState } from 'react';
import { Card, Form, Input, Button, message, Spin, Row, Col, Tag, Alert } from 'antd';
import { SaveOutlined, LockOutlined } from '@ant-design/icons';
import { useAuth } from '../../context/AuthContext';
import { userService } from '../../services/api';
import type { UserDto, UpdateUserDto } from '../../types';

interface ProfileFormValues {
  firstName: string;
  lastName: string;
  email: string;
}

interface ChangePasswordValues {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export default function Profile() {
  const { user } = useAuth();
  const [loading, setLoading] = useState(false);
  const [userData, setUserData] = useState<UserDto | null>(null);
  const [form] = Form.useForm<ProfileFormValues>();
  const [passwordForm] = Form.useForm<ChangePasswordValues>();
  const [showPasswordForm, setShowPasswordForm] = useState(false);

  const loadUserProfile = useCallback(async () => {
    if (!user?.userId) return;
    
    setLoading(true);
    try {
      const response = await userService.getById(user.userId);
      setUserData(response.data);
      form.setFieldsValue({
        firstName: response.data.firstName,
        lastName: response.data.lastName,
        email: response.data.email,
      });
    } catch (error) {
      message.error('Error al cargar el perfil');
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [user?.userId, form]);

  useEffect(() => {
    loadUserProfile();
  }, [loadUserProfile]);

  const handleUpdateProfile = useCallback(async (values: ProfileFormValues) => {
    if (!user?.userId) return;

    setLoading(true);
    try {
      const updateData: UpdateUserDto = {
        firstName: values.firstName,
        lastName: values.lastName,
        isActive: userData?.isActive ?? true,
      };

      await userService.update(user.userId, updateData);
      message.success('Perfil actualizado correctamente');
    } catch (error) {
      message.error('Error al actualizar el perfil');
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [user?.userId, userData?.isActive]);

  const handleChangePassword = useCallback(async (values: ChangePasswordValues) => {
    if (values.newPassword !== values.confirmPassword) {
      message.error('Las contraseñas no coinciden');
      return;
    }

    if (!user?.userId) return;

    setLoading(true);
    try {
      const response = await userService.changePassword(
        user.userId,
        values.currentPassword,
        values.newPassword
      );
      
      if (response.data.success) {
        message.success(response.data.message);
        passwordForm.resetFields();
        setShowPasswordForm(false);
      }
    } catch (error: unknown) {
      const axiosError = error as { response?: { data?: { message?: string } } };
      const errorMessage = axiosError.response?.data?.message || 'Error al cambiar la contraseña';
      message.error(errorMessage);
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [user?.userId, passwordForm]);

  if (!user) {
    return <Alert message="Usuario no autenticado" type="error" />;
  }

  return (
    <div style={{ maxWidth: 800 }}>
      <Row gutter={24}>
        {/* Información del Perfil */}
        <Col xs={24} sm={24} md={12}>
          <Card title="Mi Perfil" bordered={false}>
            <Spin spinning={loading}>
              <Form
                form={form}
                layout="vertical"
                onFinish={handleUpdateProfile}
              >
                <Form.Item
                  label="Nombre"
                  name="firstName"
                  rules={[{ required: true, message: 'El nombre es requerido' }]}
                >
                  <Input placeholder="Ingrese su nombre" />
                </Form.Item>

                <Form.Item
                  label="Apellido"
                  name="lastName"
                  rules={[{ required: true, message: 'El apellido es requerido' }]}
                >
                  <Input placeholder="Ingrese su apellido" />
                </Form.Item>

                <Form.Item
                  label="Email"
                  name="email"
                >
                  <Input disabled placeholder="Email" />
                </Form.Item>

                <Form.Item>
                  <Button
                    type="primary"
                    htmlType="submit"
                    icon={<SaveOutlined />}
                    loading={loading}
                  >
                    Guardar Cambios
                  </Button>
                </Form.Item>
              </Form>
            </Spin>
          </Card>
        </Col>

        {/* Información Adicional */}
        <Col xs={24} sm={24} md={12}>
          <Card title="Información Adicional" bordered={false}>
            <div style={{ marginBottom: 16 }}>
              <div style={{ marginBottom: 8 }}>
                <strong>Email:</strong>
              </div>
              <div>{userData?.email}</div>
            </div>

            <div style={{ marginBottom: 16 }}>
              <div style={{ marginBottom: 8 }}>
                <strong>Roles:</strong>
              </div>
              <div>
                {userData?.roles && userData.roles.length > 0 ? (
                  userData.roles.map((role) => (
                    <Tag key={role} color="blue" style={{ marginBottom: 4 }}>
                      {role}
                    </Tag>
                  ))
                ) : (
                  <span style={{ color: '#ccc' }}>Sin roles asignados</span>
                )}
              </div>
            </div>

            <div style={{ marginBottom: 16 }}>
              <div style={{ marginBottom: 8 }}>
                <strong>Estado:</strong>
              </div>
              <Tag color={userData?.isActive ? 'green' : 'red'}>
                {userData?.isActive ? 'Activo' : 'Inactivo'}
              </Tag>
            </div>

            <Button
              icon={<LockOutlined />}
              onClick={() => setShowPasswordForm(!showPasswordForm)}
              style={{ marginTop: 16 }}
            >
              {showPasswordForm ? 'Cancelar' : 'Cambiar Contraseña'}
            </Button>
          </Card>
        </Col>
      </Row>

      {/* Formulario de Cambio de Contraseña */}
      {showPasswordForm && (
        <Row gutter={24} style={{ marginTop: 24 }}>
          <Col xs={24} sm={24} md={12}>
            <Card title="Cambiar Contraseña" bordered={false}>
              <Form
                form={passwordForm}
                layout="vertical"
                onFinish={handleChangePassword}
              >
                <Form.Item
                  label="Contraseña Actual"
                  name="currentPassword"
                  rules={[{ required: true, message: 'La contraseña actual es requerida' }]}
                >
                  <Input.Password placeholder="Ingrese su contraseña actual" />
                </Form.Item>

                <Form.Item
                  label="Nueva Contraseña"
                  name="newPassword"
                  rules={[
                    { required: true, message: 'La nueva contraseña es requerida' },
                    { min: 8, message: 'La contraseña debe tener al menos 8 caracteres' },
                  ]}
                >
                  <Input.Password placeholder="Ingrese su nueva contraseña" />
                </Form.Item>

                <Form.Item
                  label="Confirmar Contraseña"
                  name="confirmPassword"
                  rules={[{ required: true, message: 'Debe confirmar la contraseña' }]}
                >
                  <Input.Password placeholder="Confirme su nueva contraseña" />
                </Form.Item>

                <Form.Item>
                  <Button
                    type="primary"
                    htmlType="submit"
                    loading={loading}
                  >
                    Cambiar Contraseña
                  </Button>
                </Form.Item>
              </Form>
            </Card>
          </Col>
        </Row>
      )}
    </div>
  );
}
