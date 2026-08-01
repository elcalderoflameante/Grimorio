import { useCallback, useEffect, useState } from 'react';
import { App, Button, Card, Descriptions, Form, Input, Modal, Popconfirm, Space, Table, Tag, Typography } from 'antd';
import { CopyOutlined, DownloadOutlined, PlusOutlined, ReloadOutlined, StopOutlined, TabletOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { attendanceApi, type AttendanceKioskDto, type KioskRegistrationDto } from '../../services/attendanceApi';
import { formatError } from '../../utils/errorHandler';

interface RegisterValues { name: string; deviceIdentifier: string }

const attendanceApkUrl = import.meta.env.VITE_ATTENDANCE_APK_URL || '/downloads/grimorio-asistencia.apk';

export const KioskManagement = () => {
  const { message } = App.useApp();
  const [form] = Form.useForm<RegisterValues>();
  const [items, setItems] = useState<AttendanceKioskDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [registering, setRegistering] = useState(false);
  const [registerOpen, setRegisterOpen] = useState(false);
  const [credentials, setCredentials] = useState<KioskRegistrationDto | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const response = await attendanceApi.getKiosks();
      setItems(response.data);
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  }, [message]);

  useEffect(() => { void load(); }, [load]);

  const register = async (values: RegisterValues) => {
    setRegistering(true);
    try {
      const response = await attendanceApi.registerKiosk(values);
      setRegisterOpen(false);
      setCredentials(response.data);
      form.resetFields();
      await load();
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setRegistering(false);
    }
  };

  const revoke = async (id: string) => {
    try {
      await attendanceApi.revokeKiosk(id);
      message.success('Kiosco revocado');
      await load();
    } catch (error) {
      message.error(formatError(error));
    }
  };

  const copy = async (value: string) => {
    await navigator.clipboard.writeText(value);
    message.success('Copiado al portapapeles');
  };

  return (
    <Card
      title={<Space><TabletOutlined />Kioscos de asistencia</Space>}
      extra={<Space>
        <Button icon={<DownloadOutlined />} href={attendanceApkUrl} download>Descargar APK</Button>
        <Button icon={<ReloadOutlined />} onClick={() => void load()}>Actualizar</Button>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setRegisterOpen(true)}>Vincular kiosco</Button>
      </Space>}
    >
      <Table
        rowKey="id"
        loading={loading}
        dataSource={items}
        pagination={false}
        columns={[
          { title: 'Nombre', dataIndex: 'name' },
          { title: 'Identificador', dataIndex: 'deviceIdentifier', ellipsis: true },
          { title: 'Estado', dataIndex: 'status', render: (status: AttendanceKioskDto['status']) =>
            <Tag color={status === 'Active' ? 'green' : status === 'Revoked' ? 'red' : 'gold'}>{status}</Tag> },
          { title: 'Última conexión', dataIndex: 'lastSeenAtUtc', render: (value?: string) => value ? dayjs(value).format('DD/MM/YYYY HH:mm') : 'Nunca' },
          { title: 'Versión', dataIndex: 'appVersion', render: (value?: string) => value || '—' },
          { title: 'Acciones', render: (_: unknown, item: AttendanceKioskDto) => item.status === 'Active' && (
            <Popconfirm title="¿Revocar este kiosco?" description="El dispositivo dejará de poder realizar marcaciones." onConfirm={() => void revoke(item.id)}>
              <Button danger icon={<StopOutlined />}>Revocar</Button>
            </Popconfirm>
          )},
        ]}
      />

      <Modal title="Vincular kiosco" open={registerOpen} onCancel={() => setRegisterOpen(false)} footer={null} destroyOnHidden>
        <Form form={form} layout="vertical" onFinish={register}>
          <Form.Item name="name" label="Nombre del kiosco" rules={[{ required: true }, { max: 120 }]}>
            <Input placeholder="Entrada principal" />
          </Form.Item>
          <Form.Item name="deviceIdentifier" label="Identificador mostrado en la tablet" rules={[{ required: true }, { max: 200 }]}>
            <Input placeholder="Ej. ATT-7F2A..." />
          </Form.Item>
          <Button type="primary" htmlType="submit" loading={registering} block>Generar credenciales</Button>
        </Form>
      </Modal>

      <Modal title="Credenciales generadas" open={credentials !== null} onCancel={() => setCredentials(null)}
        footer={<Button type="primary" onClick={() => setCredentials(null)}>Entendido, ya las guardé</Button>} closable={false} maskClosable={false}>
        <Typography.Paragraph type="warning">
          La clave se muestra una sola vez. Ingrésala ahora en la tablet; después no podrá recuperarse.
        </Typography.Paragraph>
        {credentials && <Descriptions bordered column={1} size="small">
          <Descriptions.Item label="Kiosk ID"><Typography.Text code copyable>{credentials.kioskId}</Typography.Text></Descriptions.Item>
          <Descriptions.Item label="Clave">
            <Space direction="vertical" style={{ width: '100%' }}>
              <Typography.Text code style={{ wordBreak: 'break-all' }}>{credentials.apiKey}</Typography.Text>
              <Button icon={<CopyOutlined />} onClick={() => void copy(credentials.apiKey)}>Copiar clave</Button>
            </Space>
          </Descriptions.Item>
        </Descriptions>}
      </Modal>
    </Card>
  );
};
