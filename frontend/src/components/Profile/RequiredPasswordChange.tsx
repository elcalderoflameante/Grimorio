import { useState } from 'react';
import { App, Button, Form, Input, Typography } from 'antd';
import { LockOutlined, LogoutOutlined, SaveOutlined } from '@ant-design/icons';
import { useAuth } from '../../context/useAuth';
import { userApi } from '../../services/api';
import { formatError } from '../../utils/errorHandler';

export default function RequiredPasswordChange() {
  const { user, logout } = useAuth();
  const { message } = App.useApp();
  const [saving, setSaving] = useState(false);

  const save = async (values: { currentPassword: string; newPassword: string }) => {
    if (!user || saving) return;
    setSaving(true);
    try {
      await userApi.changePassword(user.userId, values.currentPassword, values.newPassword);
      message.success('Contrasena actualizada. Inicia sesion con tu nueva contrasena.');
      logout();
    } catch (error) { message.error(formatError(error)); }
    finally { setSaving(false); }
  };

  return (
    <main style={{ maxWidth: 440, margin: '64px auto', padding: 24 }}>
      <Typography.Title level={3}>Cambiar contrasena temporal</Typography.Title>
      <Typography.Paragraph type="secondary">{user?.email}</Typography.Paragraph>
      <Form layout="vertical" onFinish={save} disabled={saving}>
        <Form.Item name="currentPassword" label="Contrasena temporal" rules={[{ required: true }]}>
          <Input.Password prefix={<LockOutlined />} autoComplete="current-password" />
        </Form.Item>
        <Form.Item name="newPassword" label="Nueva contrasena" rules={[{ required: true }, { min: 8, message: 'Minimo 8 caracteres' }, { max: 72 }]}>
          <Input.Password autoComplete="new-password" />
        </Form.Item>
        <Form.Item name="confirmPassword" label="Confirmar nueva contrasena" dependencies={['newPassword']} rules={[
          { required: true },
          ({ getFieldValue }) => ({ validator: (_, value) => !value || value === getFieldValue('newPassword')
            ? Promise.resolve() : Promise.reject(new Error('Las contrasenas no coinciden')) }),
        ]}>
          <Input.Password autoComplete="new-password" />
        </Form.Item>
        <Button htmlType="submit" type="primary" block loading={saving} icon={<SaveOutlined />} aria-label="Guardar contrasena">Guardar contrasena</Button>
      </Form>
      <Button style={{ marginTop: 12 }} icon={<LogoutOutlined />} disabled={saving} onClick={logout}>Cerrar sesion</Button>
    </main>
  );
}
