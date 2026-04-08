import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Row,
  Select,
  Space,
  Table,
  Tabs,
  Tag,
  Typography,
  notification,
  message,
} from 'antd';
import { EditOutlined, PlusOutlined, ReloadOutlined, DeleteOutlined } from '@ant-design/icons';
import * as signalR from '@microsoft/signalr';
import { useAuth } from '../../context/AuthContext';
import { tableServiceApi } from '../../services/api';
import { formatError } from '../../utils/errorHandler';
import type {
  CreateRestaurantTableDto,
  RestaurantTableDto,
  SetTableServiceRequestStatusDto,
  TableServiceRequestDto,
  TableServiceRequestStatus,
} from '../../types';

const { Text } = Typography;

const PUBLIC_APP_BASE_URL = (import.meta.env.VITE_PUBLIC_APP_URL as string | undefined)?.replace(/\/$/, '')
  || window.location.origin;

const requestTypeLabel: Record<number, string> = {
  1: 'Servilletas',
  2: 'Sal',
  3: 'Salsa de tomate',
  4: 'Mayonesa',
  5: 'Aji',
  6: 'Contenedor',
  7: 'Cuenta',
  8: 'Llamar mesero',
  99: 'Personalizado',
};

const statusLabel: Record<number, string> = {
  1: 'Pendiente',
  2: 'Tomada',
  3: 'En proceso',
  4: 'Completada',
  5: 'Cancelada',
};

const statusColor: Record<number, string> = {
  1: 'gold',
  2: 'blue',
  3: 'purple',
  4: 'green',
  5: 'red',
};

interface TableFormValues {
  code: string;
  name: string;
  area?: string;
  capacity: number;
  isActive?: boolean;
}

interface QrPreviewState {
  open: boolean;
  table: RestaurantTableDto | null;
}

export default function TableServiceModule() {
  const { branchId, token } = useAuth();
  const [loading, setLoading] = useState(false);
  const [tables, setTables] = useState<RestaurantTableDto[]>([]);
  const [requests, setRequests] = useState<TableServiceRequestDto[]>([]);
  const [statusFilter, setStatusFilter] = useState<TableServiceRequestStatus | undefined>(undefined);
  const [tableModalOpen, setTableModalOpen] = useState(false);
  const [editingTable, setEditingTable] = useState<RestaurantTableDto | null>(null);
  const [qrPreview, setQrPreview] = useState<QrPreviewState>({ open: false, table: null });
  const [tableForm] = Form.useForm<TableFormValues>();

  const openQrPreview = useCallback((table: RestaurantTableDto) => {
    setQrPreview({ open: true, table });
  }, []);

  const closeQrPreview = useCallback(() => {
    setQrPreview({ open: false, table: null });
  }, []);

  const handlePrintQr = useCallback(() => {
    if (!qrPreview.table) return;
    const fullUrl = `${PUBLIC_APP_BASE_URL}/mesa/${qrPreview.table.publicToken}`;
    const qrUrl = `https://api.qrserver.com/v1/create-qr-code/?size=420x420&data=${encodeURIComponent(fullUrl)}`;

    const printWindow = window.open('', '_blank', 'width=900,height=700');
    if (!printWindow) {
      message.warning('No se pudo abrir la ventana de impresión.');
      return;
    }

    printWindow.document.write(`
      <html>
        <head>
          <title>QR Mesa ${qrPreview.table.code}</title>
          <style>
            body { font-family: Arial, sans-serif; padding: 24px; text-align: center; }
            h1 { margin: 0 0 8px; font-size: 22px; }
            p { margin: 0 0 12px; color: #555; }
            img { width: 360px; height: 360px; }
            .meta { margin-top: 12px; font-size: 14px; color: #333; }
          </style>
        </head>
        <body>
          <h1>Mesa ${qrPreview.table.code} - ${qrPreview.table.name}</h1>
          <p>${qrPreview.table.area || 'Área general'}</p>
          <img src="${qrUrl}" alt="QR Mesa ${qrPreview.table.code}" />
          <div class="meta">${fullUrl}</div>
        </body>
      </html>
    `);
    printWindow.document.close();
    printWindow.focus();
    printWindow.print();
  }, [qrPreview.table]);

  const loadTables = useCallback(async () => {
    if (!branchId) return;
    const response = await tableServiceApi.getTables(branchId);
    setTables(Array.isArray(response.data) ? response.data : []);
  }, [branchId]);

  const loadRequests = useCallback(async () => {
    const response = await tableServiceApi.getRequests(statusFilter);
    setRequests(Array.isArray(response.data) ? response.data : []);
  }, [statusFilter]);

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      await Promise.all([loadTables(), loadRequests()]);
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  }, [loadTables, loadRequests]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  useEffect(() => {
    const timer = setInterval(() => {
      loadRequests().catch(() => {});
    }, 10000);
    return () => clearInterval(timer);
  }, [loadRequests]);

  useEffect(() => {
    if (!token || !branchId) return;

    const apiUrlFromEnv = import.meta.env.VITE_API_URL as string | undefined;
    const hubUrl = apiUrlFromEnv
      ? `${apiUrlFromEnv.replace(/\/api\/?$/, '')}/hubs/table-service`
      : '/hubs/table-service';
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    const onNewRequest = (payload: TableServiceRequestDto) => {
      const typeText = requestTypeLabel[payload.type] ?? String(payload.type);
      notification.info({
        message: `Nueva solicitud: ${payload.tableCode} - ${payload.tableName}`,
        description: payload.customMessage?.trim()
          ? `${typeText} · ${payload.customMessage}`
          : typeText,
        placement: 'topRight',
      });
      loadRequests().catch(() => {});
    };

    const onRequestUpdated = (payload: TableServiceRequestDto) => {
      notification.open({
        message: `Solicitud actualizada: ${payload.tableCode} - ${payload.tableName}`,
        description: `Estado: ${statusLabel[payload.status] ?? payload.status}`,
        placement: 'topRight',
      });
      loadRequests().catch(() => {});
    };

    connection.on('tableService:new-request', onNewRequest);
    connection.on('tableService:request-updated', onRequestUpdated);

    connection.start().catch((error) => {
      console.error('SignalR connection error', error);
    });

    return () => {
      connection.off('tableService:new-request', onNewRequest);
      connection.off('tableService:request-updated', onRequestUpdated);
      connection.stop().catch(() => {});
    };
  }, [token, branchId, loadRequests]);

  const handleOpenCreate = () => {
    setEditingTable(null);
    tableForm.setFieldsValue({ capacity: 4, isActive: true });
    setTableModalOpen(true);
  };

  const handleOpenEdit = useCallback((table: RestaurantTableDto) => {
    setEditingTable(table);
    tableForm.setFieldsValue({
      code: table.code,
      name: table.name,
      area: table.area,
      capacity: table.capacity,
      isActive: table.isActive,
    });
    setTableModalOpen(true);
  }, [tableForm]);

  const handleSaveTable = async (values: TableFormValues) => {
    try {
      if (editingTable) {
        await tableServiceApi.updateTable(editingTable.id, {
          code: values.code,
          name: values.name,
          area: values.area,
          capacity: values.capacity,
          isActive: values.isActive ?? true,
        });
        message.success('Mesa actualizada.');
      } else {
        const payload: CreateRestaurantTableDto = {
          code: values.code,
          name: values.name,
          area: values.area,
          capacity: values.capacity,
        };
        await tableServiceApi.createTable(payload);
        message.success('Mesa creada.');
      }
      setTableModalOpen(false);
      tableForm.resetFields();
      await loadTables();
    } catch (error) {
      message.error(formatError(error));
    }
  };

  const handleDeleteTable = useCallback(async (id: string) => {
    try {
      await tableServiceApi.deleteTable(id);
      message.success('Mesa eliminada.');
      await loadTables();
    } catch (error) {
      message.error(formatError(error));
    }
  }, [loadTables]);

  const handleRegenerateToken = useCallback(async (id: string) => {
    try {
      await tableServiceApi.regenerateTableToken(id);
      message.success('Token QR regenerado.');
      await loadTables();
    } catch (error) {
      message.error(formatError(error));
    }
  }, [loadTables]);

  const handleSetRequestStatus = useCallback(async (requestId: string, status: TableServiceRequestStatus) => {
    try {
      const body: SetTableServiceRequestStatusDto = { status };
      await tableServiceApi.setRequestStatus(requestId, body);
      await loadRequests();
      message.success('Solicitud actualizada.');
    } catch (error) {
      message.error(formatError(error));
    }
  }, [loadRequests]);

  const handleTakeRequest = useCallback(async (requestId: string) => {
    try {
      await tableServiceApi.takeRequest(requestId);
      await loadRequests();
      message.success('Solicitud tomada.');
    } catch (error) {
      message.error(formatError(error));
    }
  }, [loadRequests]);

  const tableColumns = useMemo(() => [
    {
      title: 'Código',
      dataIndex: 'code',
      key: 'code',
      width: 90,
    },
    {
      title: 'Mesa',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: 'Área',
      dataIndex: 'area',
      key: 'area',
      render: (value?: string) => value || '-',
    },
    {
      title: 'Capacidad',
      dataIndex: 'capacity',
      key: 'capacity',
      width: 90,
    },
    {
      title: 'Estado',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 100,
      render: (active: boolean) => <Tag color={active ? 'green' : 'red'}>{active ? 'Activa' : 'Inactiva'}</Tag>,
    },
    {
      title: 'QR',
      key: 'qr',
      render: (_: unknown, record: RestaurantTableDto) => {
        const fullUrl = `${PUBLIC_APP_BASE_URL}/mesa/${record.publicToken}`;
        const qrUrl = `https://api.qrserver.com/v1/create-qr-code/?size=90x90&data=${encodeURIComponent(fullUrl)}`;
        return (
          <Space>
            <img
              src={qrUrl}
              alt={`QR-${record.code}`}
              width={42}
              height={42}
              style={{ cursor: 'pointer', borderRadius: 4 }}
              onClick={() => openQrPreview(record)}
              title="Ver QR en grande"
            />
            <Button size="small" onClick={() => openQrPreview(record)}>
              Ver QR
            </Button>
            <Button size="small" onClick={() => navigator.clipboard.writeText(fullUrl)}>
              Copiar URL
            </Button>
          </Space>
        );
      },
    },
    {
      title: 'Acciones',
      key: 'actions',
      width: 230,
      render: (_: unknown, record: RestaurantTableDto) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => handleOpenEdit(record)} />
          <Button size="small" onClick={() => handleRegenerateToken(record.id)}>
            Regenerar QR
          </Button>
          <Popconfirm
            title="¿Eliminar mesa?"
            onConfirm={() => handleDeleteTable(record.id)}
            okText="Sí"
            cancelText="No"
          >
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    },
  ], [handleDeleteTable, handleOpenEdit, handleRegenerateToken, openQrPreview]);

  const requestColumns = useMemo(() => [
    {
      title: 'Hora',
      dataIndex: 'requestedAt',
      key: 'requestedAt',
      width: 85,
      render: (value: string) => new Date(value).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    },
    {
      title: 'Mesa',
      key: 'table',
      render: (_: unknown, row: TableServiceRequestDto) => `${row.tableCode} - ${row.tableName}`,
    },
    {
      title: 'Tipo',
      dataIndex: 'type',
      key: 'type',
      render: (value: number) => requestTypeLabel[value] || String(value),
    },
    {
      title: 'Detalle',
      dataIndex: 'customMessage',
      key: 'customMessage',
      render: (value?: string) => value || '-',
    },
    {
      title: 'Estado',
      dataIndex: 'status',
      key: 'status',
      width: 120,
      render: (value: number) => <Tag color={statusColor[value] || 'default'}>{statusLabel[value] || value}</Tag>,
    },
    {
      title: 'Responsable',
      dataIndex: 'takenByName',
      key: 'takenByName',
      render: (value?: string) => value || '-',
    },
    {
      title: 'Acciones',
      key: 'actions',
      width: 280,
      render: (_: unknown, row: TableServiceRequestDto) => (
        <Space>
          {row.status === 1 && <Button size="small" onClick={() => handleTakeRequest(row.id)}>Tomar</Button>}
          {row.status === 2 && <Button size="small" onClick={() => handleSetRequestStatus(row.id, 3)}>En proceso</Button>}
          {(row.status === 2 || row.status === 3) && (
            <Button size="small" type="primary" onClick={() => handleSetRequestStatus(row.id, 4)}>
              Completar
            </Button>
          )}
          {(row.status === 1 || row.status === 2 || row.status === 3) && (
            <Button size="small" danger onClick={() => handleSetRequestStatus(row.id, 5)}>
              Cancelar
            </Button>
          )}
        </Space>
      ),
    },
  ], [handleSetRequestStatus, handleTakeRequest]);

  return (
    <Card
      title="Atención por QR"
      extra={<Button icon={<ReloadOutlined />} onClick={loadData} loading={loading}>Recargar</Button>}
      size="small"
    >
      <Tabs
        items={[
          {
            key: 'tables',
            label: 'Mesas y QR',
            children: (
              <Space direction="vertical" style={{ width: '100%' }}>
                <Row justify="space-between" align="middle">
                  <Col><Text type="secondary">Administra mesas y sus QR de atención.</Text></Col>
                  <Col><Button type="primary" icon={<PlusOutlined />} onClick={handleOpenCreate}>Nueva mesa</Button></Col>
                </Row>
                <Table rowKey="id" loading={loading} columns={tableColumns} dataSource={tables} pagination={{ pageSize: 8 }} />
              </Space>
            ),
          },
          {
            key: 'requests',
            label: 'Solicitudes',
            children: (
              <Space direction="vertical" style={{ width: '100%' }}>
                <Row justify="space-between" align="middle">
                  <Col>
                    <Text type="secondary">Solicitudes recibidas desde QR.</Text>
                  </Col>
                  <Col>
                    <Select
                      allowClear
                      placeholder="Filtrar estado"
                      style={{ width: 180 }}
                      value={statusFilter}
                      onChange={(value) => setStatusFilter(value)}
                      options={[
                        { label: 'Pendiente', value: 1 },
                        { label: 'Tomada', value: 2 },
                        { label: 'En proceso', value: 3 },
                        { label: 'Completada', value: 4 },
                        { label: 'Cancelada', value: 5 },
                      ]}
                    />
                  </Col>
                </Row>
                <Table rowKey="id" loading={loading} columns={requestColumns} dataSource={requests} pagination={{ pageSize: 10 }} />
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editingTable ? 'Editar Mesa' : 'Nueva Mesa'}
        open={tableModalOpen}
        onCancel={() => setTableModalOpen(false)}
        onOk={() => tableForm.submit()}
        okText="Guardar"
        cancelText="Cancelar"
      >
        <Form form={tableForm} layout="vertical" onFinish={handleSaveTable}>
          <Form.Item label="Código" name="code" rules={[{ required: true, message: 'Ingrese código' }]}>
            <Input maxLength={40} />
          </Form.Item>
          <Form.Item label="Nombre" name="name" rules={[{ required: true, message: 'Ingrese nombre' }]}>
            <Input maxLength={120} />
          </Form.Item>
          <Form.Item label="Área" name="area">
            <Input maxLength={120} />
          </Form.Item>
          <Form.Item label="Capacidad" name="capacity" rules={[{ required: true, message: 'Ingrese capacidad' }]}>
            <InputNumber min={1} max={30} style={{ width: '100%' }} />
          </Form.Item>
          {editingTable && (
            <Form.Item label="Estado" name="isActive">
              <Select
                options={[
                  { label: 'Activa', value: true },
                  { label: 'Inactiva', value: false },
                ]}
              />
            </Form.Item>
          )}
        </Form>
      </Modal>

      <Modal
        title={qrPreview.table ? `QR Mesa ${qrPreview.table.code} - ${qrPreview.table.name}` : 'QR de mesa'}
        open={qrPreview.open}
        onCancel={closeQrPreview}
        footer={
          <Space>
            <Button
              onClick={() => {
                if (!qrPreview.table) return;
                const fullUrl = `${PUBLIC_APP_BASE_URL}/mesa/${qrPreview.table.publicToken}`;
                navigator.clipboard.writeText(fullUrl);
                message.success('URL copiada.');
              }}
            >
              Copiar URL
            </Button>
            <Button onClick={handlePrintQr}>Imprimir</Button>
            <Button type="primary" onClick={closeQrPreview}>Cerrar</Button>
          </Space>
        }
      >
        {qrPreview.table && (
          <Space direction="vertical" style={{ width: '100%', alignItems: 'center' }}>
            <Text type="secondary">{qrPreview.table.area || 'Área general'}</Text>
            <img
              src={`https://api.qrserver.com/v1/create-qr-code/?size=420x420&data=${encodeURIComponent(`${PUBLIC_APP_BASE_URL}/mesa/${qrPreview.table.publicToken}`)}`}
              alt={`QR-Mesa-${qrPreview.table.code}`}
              style={{ width: 320, height: 320, maxWidth: '100%' }}
            />
            <Text copyable>{`${PUBLIC_APP_BASE_URL}/mesa/${qrPreview.table.publicToken}`}</Text>
          </Space>
        )}
      </Modal>
    </Card>
  );
}
