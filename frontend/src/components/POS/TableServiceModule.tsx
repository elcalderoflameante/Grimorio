import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Button,
  Card,
  Col,
  Form,
  Grid,
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
import { useAuth } from '../../context/useAuth';
import { tableServiceApi } from '../../services/api';
import { formatError } from '../../utils/errorHandler';
import {
  QR_SERVER_BASE_URL,
  REQUEST_STATUS,
  REQUEST_STATUS_COLORS,
  REQUEST_STATUS_LABELS,
  REQUEST_TYPE_LABELS,
  REQUESTS_POLLING_INTERVAL_MS,
  TABLE_SERVICE_HUB_PATH,
} from '../../constants/tableService';
import type {
  CreateRestaurantTableDto,
  RestaurantTableDto,
  SetTableServiceRequestStatusDto,
  TableServiceRequestDto,
  TableServiceRequestStatus,
} from '../../types';

const { Text } = Typography;
const { useBreakpoint } = Grid;

const PUBLIC_APP_BASE_URL = (import.meta.env.VITE_PUBLIC_APP_URL as string | undefined)?.replace(/\/$/, '')
  || window.location.origin;


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

type TableRowLike = RestaurantTableDto & {
  Id?: string;
  BranchId?: string;
  Code?: string;
  Name?: string;
  Area?: string;
  Capacity?: number;
  PublicToken?: string;
  PublicUrl?: string;
  IsActive?: boolean;
};

const resolveTableId = (table: Partial<TableRowLike> | null | undefined): string => {
  if (!table) return '';
  const value = table.id ?? table.Id;
  return typeof value === 'string' ? value : '';
};

const normalizeTable = (raw: unknown): RestaurantTableDto => {
  const table = raw as Partial<TableRowLike>;
  return {
    id: resolveTableId(table),
    branchId: (table.branchId ?? table.BranchId ?? '') as string,
    code: (table.code ?? table.Code ?? '') as string,
    name: (table.name ?? table.Name ?? '') as string,
    area: (table.area ?? table.Area ?? undefined) as string | undefined,
    capacity: Number(table.capacity ?? table.Capacity ?? 0),
    publicToken: (table.publicToken ?? table.PublicToken ?? '') as string,
    publicUrl: (table.publicUrl ?? table.PublicUrl ?? '') as string,
    isActive: Boolean(table.isActive ?? table.IsActive ?? true),
  };
};

export default function TableServiceModule() {
  const { branchId, token } = useAuth();
  const screens = useBreakpoint();
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

  const buildTablePublicUrl = useCallback((publicToken: string) => {
    return `${PUBLIC_APP_BASE_URL}/mesa/${publicToken}`;
  }, []);

  const copyToClipboard = useCallback(async (text: string) => {
    if (!text) return false;

    // Prefer Clipboard API when available and allowed by browser context.
    if (navigator.clipboard && window.isSecureContext) {
      try {
        await navigator.clipboard.writeText(text);
        return true;
      } catch {
        // Fallback below.
      }
    }

    try {
      const textarea = document.createElement('textarea');
      textarea.value = text;
      textarea.style.position = 'fixed';
      textarea.style.left = '-9999px';
      textarea.style.top = '0';
      document.body.appendChild(textarea);
      textarea.focus();
      textarea.select();
      const copied = document.execCommand('copy');
      document.body.removeChild(textarea);
      return copied;
    } catch {
      return false;
    }
  }, []);

  const handleCopyUrl = useCallback(async (fullUrl: string) => {
    const copied = await copyToClipboard(fullUrl);
    if (copied) {
      message.success('URL copiada.');
      return;
    }
    message.error('No se pudo copiar la URL automáticamente.');
  }, [copyToClipboard]);

  const handlePrintQr = useCallback(() => {
    if (!qrPreview.table) return;
    const fullUrl = buildTablePublicUrl(qrPreview.table.publicToken);
    const qrUrl = `${QR_SERVER_BASE_URL}?size=420x420&data=${encodeURIComponent(fullUrl)}`;

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
  }, [buildTablePublicUrl, qrPreview.table]);

  const loadTables = useCallback(async () => {
    if (!branchId) return;
    const response = await tableServiceApi.getTables(branchId);
    const parsed = Array.isArray(response.data)
      ? response.data.map((item) => normalizeTable(item))
      : [];
    setTables(parsed);
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
    }, REQUESTS_POLLING_INTERVAL_MS);
    return () => clearInterval(timer);
  }, [loadRequests]);

  useEffect(() => {
    if (!token || !branchId) return;

    const apiUrlFromEnv = import.meta.env.VITE_API_URL as string | undefined;
    const hubUrl = apiUrlFromEnv
      ? `${apiUrlFromEnv.replace(/\/api\/?$/, '')}${TABLE_SERVICE_HUB_PATH}`
      : TABLE_SERVICE_HUB_PATH;
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    const onNewRequest = (payload: TableServiceRequestDto) => {
      const typeText = REQUEST_TYPE_LABELS[payload.type] ?? String(payload.type);
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
        description: `Estado: ${REQUEST_STATUS_LABELS[payload.status] ?? payload.status}`,
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
    const tableId = resolveTableId(table);
    if (!tableId) {
      message.error('No se pudo editar: la mesa no tiene id válido.');
      return;
    }
    setEditingTable({ ...table, id: tableId });
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
        const tableId = resolveTableId(editingTable);
        if (!tableId) {
          message.error('No se pudo actualizar: id de mesa inválido.');
          return;
        }
        await tableServiceApi.updateTable(tableId, {
          id: tableId,
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
      if (!id) {
        message.error('No se pudo eliminar: id de mesa inválido.');
        return;
      }
      await tableServiceApi.deleteTable(id);
      message.success('Mesa eliminada.');
      await loadTables();
    } catch (error) {
      message.error(formatError(error));
    }
  }, [loadTables]);

  const handleRegenerateToken = useCallback(async (id: string) => {
    try {
      if (!id) {
        message.error('No se pudo regenerar QR: id de mesa inválido.');
        return;
      }
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
        const fullUrl = buildTablePublicUrl(record.publicToken);
        const qrUrl = `${QR_SERVER_BASE_URL}?size=90x90&data=${encodeURIComponent(fullUrl)}`;
        return (
          <Space wrap>
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
            <Button size="small" onClick={() => void handleCopyUrl(fullUrl)}>
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
        <Space wrap>
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
  ], [buildTablePublicUrl, handleCopyUrl, handleDeleteTable, handleOpenEdit, handleRegenerateToken, openQrPreview]);

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
      render: (value: number) => REQUEST_TYPE_LABELS[value] || String(value),
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
      render: (value: number) => <Tag color={REQUEST_STATUS_COLORS[value] || 'default'}>{REQUEST_STATUS_LABELS[value] || value}</Tag>,
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
        <Space wrap>
          {row.status === REQUEST_STATUS.PENDING && <Button size="small" onClick={() => handleTakeRequest(row.id)}>Tomar</Button>}
          {row.status === REQUEST_STATUS.TAKEN && <Button size="small" onClick={() => handleSetRequestStatus(row.id, REQUEST_STATUS.IN_PROGRESS)}>En proceso</Button>}
          {(row.status === REQUEST_STATUS.TAKEN || row.status === REQUEST_STATUS.IN_PROGRESS) && (
            <Button size="small" type="primary" onClick={() => handleSetRequestStatus(row.id, REQUEST_STATUS.COMPLETED)}>
              Completar
            </Button>
          )}
          {(row.status === REQUEST_STATUS.PENDING || row.status === REQUEST_STATUS.TAKEN || row.status === REQUEST_STATUS.IN_PROGRESS) && (
            <Button size="small" danger onClick={() => handleSetRequestStatus(row.id, REQUEST_STATUS.CANCELLED)}>
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
                  <Col flex="auto"><Text type="secondary">Administra mesas y sus QR de atención.</Text></Col>
                  <Col>
                    <Button type="primary" icon={<PlusOutlined />} onClick={handleOpenCreate}>
                      Nueva mesa
                    </Button>
                  </Col>
                </Row>
                <Table
                  rowKey={(record) => resolveTableId(record) || `${record.code}-${record.name}`}
                  loading={loading}
                  columns={tableColumns}
                  dataSource={tables}
                  pagination={{ pageSize: 8 }}
                  scroll={{ x: 980 }}
                />
              </Space>
            ),
          },
          {
            key: 'requests',
            label: 'Solicitudes',
            children: (
              <Space direction="vertical" style={{ width: '100%' }}>
                <Row justify="space-between" align="middle">
                  <Col flex="auto">
                    <Text type="secondary">Solicitudes recibidas desde QR.</Text>
                  </Col>
                  <Col>
                    <Select
                      allowClear
                      placeholder="Filtrar estado"
                      style={{ width: screens.xs ? '100%' : 220, minWidth: 180 }}
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
                <Table
                  rowKey="id"
                  loading={loading}
                  columns={requestColumns}
                  dataSource={requests}
                  pagination={{ pageSize: 10 }}
                  scroll={{ x: 1100 }}
                />
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
                const fullUrl = buildTablePublicUrl(qrPreview.table.publicToken);
                void handleCopyUrl(fullUrl);
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
              src={`${QR_SERVER_BASE_URL}?size=420x420&data=${encodeURIComponent(buildTablePublicUrl(qrPreview.table.publicToken))}`}
              alt={`QR-Mesa-${qrPreview.table.code}`}
              style={{ width: 320, height: 320, maxWidth: '100%' }}
            />
            <Text copyable>{buildTablePublicUrl(qrPreview.table.publicToken)}</Text>
          </Space>
        )}
      </Modal>
    </Card>
  );
}
