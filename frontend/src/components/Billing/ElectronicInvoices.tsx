import { useState, useEffect, useCallback } from 'react';
import { App as AntApp, Table, Tag, Space, Typography, Button, DatePicker, Select, Tooltip, Modal, Descriptions, Alert, Spin } from 'antd';
import { ReloadOutlined, FilePdfOutlined, FileTextOutlined,
  RedoOutlined, ThunderboltOutlined, CodeOutlined } from '@ant-design/icons';
import type { ElectronicDocumentDto } from '../../types';
import { sriApi } from '../../services/api';
import dayjs, { type Dayjs } from 'dayjs';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;

function formatXml(xml: string): string {
  try {
    const parser = new DOMParser();
    const doc = parser.parseFromString(xml, 'application/xml');
    if (doc.querySelector('parsererror')) return xml;
    const serializer = new XMLSerializer();
    const raw = serializer.serializeToString(doc);
    // Indentar manualmente
    let indent = 0;
    return raw
      .replace(/>\s*</g, '><')
      .replace(/(<[^/][^>]*[^/]>|<[^/][^>]*[^>]>)(?!.*<\/)/g, '$1')
      .split(/(<[^>]+>)/)
      .filter(Boolean)
      .map(token => {
        if (/^<\//.test(token)) indent = Math.max(0, indent - 1);
        const line = '  '.repeat(indent) + token;
        if (/^<[^/][^>]*[^/]>$/.test(token) && !/^<[^>]+\/>$/.test(token)) indent++;
        return line;
      })
      .join('\n');
  } catch {
    return xml;
  }
}

const STATUS_LABELS: Record<string, { label: string; color: string }> = {
  Pending:    { label: 'Pendiente',   color: 'default' },
  Sent:       { label: 'Enviado',     color: 'processing' },
  Authorized: { label: 'Autorizado',  color: 'success' },
  Rejected:   { label: 'Rechazado',   color: 'error' },
  Cancelled:  { label: 'Anulado',     color: 'warning' },
};

const fmt = (v: number) => `$${v.toFixed(2)}`;

function DocDetail({ doc }: { doc: ElectronicDocumentDto }) {
  return (
    <Descriptions size="small" column={{ xs: 1, sm: 2 }} style={{ padding: '8px 16px' }}>
      <Descriptions.Item label="Clave de acceso">
        <Text code style={{ fontSize: 10 }}>{doc.claveAcceso}</Text>
      </Descriptions.Item>
      {doc.numeroAutorizacion && (
        <Descriptions.Item label="N° Autorización">
          <Text code style={{ fontSize: 10 }}>{doc.numeroAutorizacion}</Text>
        </Descriptions.Item>
      )}
      {doc.fechaAutorizacion && (
        <Descriptions.Item label="Fecha autorización">
          {dayjs(doc.fechaAutorizacion).format('DD/MM/YYYY HH:mm:ss')}
        </Descriptions.Item>
      )}
      <Descriptions.Item label="Subtotal sin IVA">{fmt(doc.totalSinImpuestos)}</Descriptions.Item>
      <Descriptions.Item label="IVA">{fmt(doc.totalIva)}</Descriptions.Item>
      <Descriptions.Item label="Total">{fmt(doc.importeTotal)}</Descriptions.Item>
      <Descriptions.Item label="Ambiente">
        <Tag color={doc.environment === '2' ? 'red' : 'orange'}>
          {doc.environment === '2' ? 'Producción' : 'Pruebas'}
        </Tag>
      </Descriptions.Item>
      <Descriptions.Item label="Reintentos">{doc.retryCount}</Descriptions.Item>
      {doc.errorMessage && (
        <Descriptions.Item label="Error" span={2}>
          <Alert type="error" title={doc.errorMessage} style={{ padding: '2px 8px' }} />
        </Descriptions.Item>
      )}
    </Descriptions>
  );
}

export default function ElectronicInvoices() {
  const { message } = AntApp.useApp();

  const { hasPermission } = useAuth();
  const [docs, setDocs] = useState<ElectronicDocumentDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [retrying, setRetrying] = useState<string | null>(null);
  const [dateRange, setDateRange] = useState<[Dayjs | null, Dayjs | null]>([
    dayjs().startOf('month'),
    dayjs().endOf('day'),
  ]);
  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined);
  const [detailDoc, setDetailDoc] = useState<ElectronicDocumentDto | null>(null);
  const [xmlModal, setXmlModal] = useState<{ doc: ElectronicDocumentDto; content: string } | null>(null);
  const [xmlLoading, setXmlLoading] = useState<string | null>(null);
  const canGenerateSri = hasPermission(PERMISSIONS.billing.sriGenerate);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [from, to] = dateRange;
      const res = await sriApi.getDocuments({
        desde: from?.toISOString(),
        hasta: to?.toISOString(),
        estado: statusFilter,
        pageSize: 200,
      });
      setDocs(res.data);
    } catch {
      message.error('Error al cargar documentos electrónicos');
    } finally {
      setLoading(false);
    }
  }, [dateRange, statusFilter]);

  useEffect(() => { load(); }, [load]);

  const handleRetry = async (doc: ElectronicDocumentDto) => {
    setRetrying(doc.id);
    try {
      const res = await sriApi.retryInvoice(doc.id);
      message.success(`Factura ${res.data.numeroFactura} procesada: ${STATUS_LABELS[res.data.status]?.label ?? res.data.status}`);
      load();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: string } })?.response?.data ?? 'Error al reintentar';
      message.error(String(msg));
    } finally {
      setRetrying(null);
    }
  };

  const handleViewXml = async (doc: ElectronicDocumentDto) => {
    setXmlLoading(doc.id);
    try {
      const res = await sriApi.getRespuestaSriText(doc.id);
      setXmlModal({ doc, content: formatXml(res.data) });
    } catch {
      message.error('No se pudo cargar la respuesta del SRI');
    } finally {
      setXmlLoading(null);
    }
  };

  const handleDownload = (url: string, filename: string) => {
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.target = '_blank';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Documentos electrónicos</Title>
        <Space wrap>
          <RangePicker
            value={dateRange}
            onChange={(v) => setDateRange(v ? [v[0], v[1]] : [null, null])}
            format="DD/MM/YYYY"
            allowClear={false}
          />
          <Select
            placeholder="Estado"
            allowClear
            value={statusFilter}
            onChange={setStatusFilter}
            style={{ width: 130 }}
            options={Object.entries(STATUS_LABELS).map(([k, v]) => ({ value: k, label: v.label }))}
          />
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Actualizar</Button>
        </Space>
      </div>

      <Table
        size="small"
        dataSource={docs}
        rowKey="id"
        loading={loading}
        pagination={{ defaultPageSize: 25, showSizeChanger: true, pageSizeOptions: ['10', '25', '50', '100'] }}
        expandable={{ expandedRowRender: (r) => <DocDetail doc={r} /> }}
        columns={[
          {
            title: 'Emitida',
            dataIndex: 'createdAt',
            width: 150,
            render: (v: string) => dayjs(v).format('DD/MM/YYYY HH:mm'),
          },
          {
            title: 'N° Factura',
            dataIndex: 'numeroFactura',
            width: 140,
            render: (v: string) => <Text code>{v}</Text>,
          },
          {
            title: 'Estado',
            dataIndex: 'status',
            width: 110,
            render: (v: string) => {
              const s = STATUS_LABELS[v] ?? { label: v, color: 'default' };
              return <Tag color={s.color}>{s.label}</Tag>;
            },
          },
          {
            title: 'Total',
            dataIndex: 'importeTotal',
            align: 'right',
            width: 90,
            render: (v: number) => <Text strong>{fmt(v)}</Text>,
          },
          {
            title: 'Ambiente',
            dataIndex: 'environment',
            width: 90,
            render: (v: string) => (
              <Tag color={v === '2' ? 'red' : 'orange'}>{v === '2' ? 'Producción' : 'Pruebas'}</Tag>
            ),
          },
          {
            title: 'Error',
            dataIndex: 'errorMessage',
            ellipsis: true,
            render: (v?: string) => v
              ? <Tooltip title={v}><Text type="danger" style={{ fontSize: 12 }}>{v}</Text></Tooltip>
              : null,
          },
          {
            title: 'Acciones',
            width: 140,
            render: (_: unknown, r: ElectronicDocumentDto) => (
              <Space size={4}>
                {r.hasRide && (
                  <Tooltip title="Descargar RIDE (PDF)">
                    <Button
                      size="small"
                      icon={<FilePdfOutlined />}
                      onClick={() => handleDownload(
                        sriApi.downloadRideUrl(r.id),
                        `RIDE-${r.numeroFactura}.pdf`
                      )}
                    />
                  </Tooltip>
                )}
                {r.hasXml && (
                  <Tooltip title="Descargar XML">
                    <Button
                      size="small"
                      icon={<FileTextOutlined />}
                      onClick={() => handleDownload(
                        sriApi.downloadXmlUrl(r.id),
                        `FE-${r.numeroFactura}.xml`
                      )}
                    />
                  </Tooltip>
                )}
                {r.hasXmlResponse && (
                  <Tooltip title="Ver respuesta del SRI">
                    <Button
                      size="small"
                      icon={<CodeOutlined />}
                      loading={xmlLoading === r.id}
                      onClick={() => handleViewXml(r)}
                    />
                  </Tooltip>
                )}
                {canGenerateSri && (r.status === 'Rejected' || r.status === 'Sent' || r.status === 'Pending') && (
                  <Tooltip title="Reintentar envío al SRI">
                    <Button
                      size="small"
                      icon={<RedoOutlined />}
                      loading={retrying === r.id}
                      onClick={() => handleRetry(r)}
                    />
                  </Tooltip>
                )}
              </Space>
            ),
          },
        ]}
      />

      <Modal
        open={!!detailDoc}
        onCancel={() => setDetailDoc(null)}
        footer={null}
        title={`Detalle — ${detailDoc?.numeroFactura}`}
        width={640}
      >
        {detailDoc && <DocDetail doc={detailDoc} />}
      </Modal>

      <Modal
        open={!!xmlModal}
        onCancel={() => setXmlModal(null)}
        title={
          <Space>
            <CodeOutlined />
            <span>Respuesta SRI — {xmlModal?.doc.numeroFactura}</span>
            <Tag color={STATUS_LABELS[xmlModal?.doc.status ?? '']?.color}>
              {STATUS_LABELS[xmlModal?.doc.status ?? '']?.label}
            </Tag>
          </Space>
        }
        width={820}
        footer={
          <Button
            icon={<FileTextOutlined />}
            onClick={() => xmlModal && handleDownload(
              sriApi.downloadRespuestaSriUrl(xmlModal.doc.id),
              `RespuestaSRI-${xmlModal.doc.numeroFactura}.xml`
            )}
          >
            Descargar XML
          </Button>
        }
      >
        {xmlLoading ? (
          <div style={{ textAlign: 'center', padding: 32 }}><Spin /></div>
        ) : (
          <pre style={{
            background: '#1e1e1e',
            color: '#d4d4d4',
            padding: 16,
            borderRadius: 6,
            fontSize: 11,
            lineHeight: 1.5,
            maxHeight: 520,
            overflowY: 'auto',
            whiteSpace: 'pre-wrap',
            wordBreak: 'break-all',
            margin: 0,
          }}>
            {xmlModal?.content}
          </pre>
        )}
      </Modal>
    </div>
  );
}

// Botón standalone para usar desde SalesHistory u otros componentes
interface GenerateInvoiceButtonProps {
  orderPaymentId: string;
  documentType: string;
  electronicDocumentId?: string;
  electronicDocumentStatus?: string;
  onSuccess?: (doc: ElectronicDocumentDto) => void;
}

export function GenerateInvoiceButton({
  orderPaymentId,
  documentType,
  electronicDocumentId,
  electronicDocumentStatus,
  onSuccess,
}: GenerateInvoiceButtonProps) {
  const { message } = AntApp.useApp();
  const { hasPermission } = useAuth();
  const [loading, setLoading] = useState(false);
  const canGenerateSri = hasPermission(PERMISSIONS.billing.sriGenerate);

  if (documentType !== 'Factura' || !canGenerateSri) return null;

  // Factura ya autorizada — no se puede modificar
  if (electronicDocumentStatus === 'Authorized') {
    const s = STATUS_LABELS['Authorized'];
    return <Tag color={s.color}>{s.label}</Tag>;
  }

  // Factura existente pero no autorizada — reintentar con el mismo documento
  if (electronicDocumentId && electronicDocumentStatus) {
    const s = STATUS_LABELS[electronicDocumentStatus] ?? { label: electronicDocumentStatus, color: 'default' };
    const handleRetry = async () => {
      setLoading(true);
      try {
        const res = await sriApi.retryInvoice(electronicDocumentId);
        const newStatus = STATUS_LABELS[res.data.status]?.label ?? res.data.status;
        message.success(`Factura ${res.data.numeroFactura}: ${newStatus}`);
        onSuccess?.(res.data);
      } catch (err: unknown) {
        const msg = (err as { response?: { data?: string } })?.response?.data ?? 'Error al reintentar';
        message.error(String(msg));
      } finally {
        setLoading(false);
      }
    };

    return (
      <Space size={4}>
        <Tag color={s.color}>{s.label}</Tag>
        <Tooltip title="Reintentar envío al SRI">
          <Button size="small" icon={<RedoOutlined />} loading={loading} onClick={handleRetry} />
        </Tooltip>
      </Space>
    );
  }

  // Sin documento — generar por primera vez
  const handleGenerate = async () => {
    setLoading(true);
    try {
      const res = await sriApi.generateInvoice(orderPaymentId);
      const status = STATUS_LABELS[res.data.status]?.label ?? res.data.status;
      message.success(`Factura ${res.data.numeroFactura}: ${status}`);
      onSuccess?.(res.data);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: string } })?.response?.data ?? 'Error al generar factura';
      message.error(String(msg));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Tooltip title="Generar factura electrónica SRI">
      <Button size="small" icon={<ThunderboltOutlined />} loading={loading} onClick={handleGenerate}>
        Factura electrónica
      </Button>
    </Tooltip>
  );
}
