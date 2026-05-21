import { useState, useEffect, useCallback } from 'react';
import {
  Table, Button, Tag, Space, Popconfirm, message, Select, DatePicker,
  Tooltip,
} from 'antd';
import {
  PlusOutlined, EyeOutlined, EditOutlined, StopOutlined, DeleteOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import type { PurchaseDto, SupplierDto } from '../../types';
import { purchasesApi } from '../../services/api';
import PurchaseForm from './PurchaseForm';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { RangePicker } = DatePicker;

const DOC_TYPE_LABEL: Record<string, string> = {
  Factura: 'Factura', NotaDeVenta: 'Nota de Venta', Comprobante: 'Comprobante',
  LiquidacionCompra: 'Liquidación de Compra', Otro: 'Otro',
};

const STATUS_COLOR: Record<string, string> = { Registrada: 'green', Anulada: 'red' };

export default function PurchasesList() {
  const { hasPermission } = useAuth();
  const [compras, setCompras] = useState<PurchaseDto[]>([]);
  const [proveedores, setProveedores] = useState<SupplierDto[]>([]);
  const [loading, setLoading] = useState(false);

  const [filtroEstado, setFiltroEstado] = useState<string | undefined>();
  const [filtroProveedor, setFiltroProveedor] = useState<string | undefined>();
  const [filtroFechas, setFiltroFechas] = useState<[dayjs.Dayjs, dayjs.Dayjs] | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [editando, setEditando] = useState<PurchaseDto | null>(null);
  const [viendo, setViendo] = useState<PurchaseDto | null>(null);
  const canCreate = hasPermission(PERMISSIONS.purchases.ordersCreate);
  const canUpdate = hasPermission(PERMISSIONS.purchases.ordersUpdate);
  const canCancel = hasPermission(PERMISSIONS.purchases.ordersCancel);
  const canDelete = hasPermission(PERMISSIONS.purchases.ordersDelete);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [cRes, pvRes] = await Promise.all([
        purchasesApi.getPurchases({
          status: filtroEstado,
          supplierId: filtroProveedor,
          dateFrom: filtroFechas ? filtroFechas[0].startOf('day').toISOString() : undefined,
          dateTo: filtroFechas ? filtroFechas[1].endOf('day').toISOString() : undefined,
        }),
        purchasesApi.getSuppliers(),
      ]);
      setCompras(cRes.data ?? []);
      setProveedores(pvRes.data ?? []);
    } catch {
      message.error('Error al cargar compras');
    } finally {
      setLoading(false);
    }
  }, [filtroEstado, filtroProveedor, filtroFechas]);

  useEffect(() => { load(); }, [load]);

  const handleAnular = async (id: string) => {
    try {
      await purchasesApi.anularPurchase(id);
      message.success('Compra anulada — el stock fue revertido');
      load();
    } catch {
      message.error('Error al anular la compra');
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await purchasesApi.deletePurchase(id);
      message.success('Compra eliminada');
      load();
    } catch {
      message.error('Error al eliminar la compra');
    }
  };

  const columns = [
    {
      title: 'Fecha',
      dataIndex: 'documentDate',
      key: 'documentDate',
      width: 100,
      render: (d: string) => dayjs(d).format('DD/MM/YY'),
    },
    {
      title: 'Tipo',
      dataIndex: 'documentType',
      key: 'documentType',
      width: 130,
      render: (v: string) => DOC_TYPE_LABEL[v] ?? v,
    },
    {
      title: 'N° Comprobante',
      dataIndex: 'documentNumber',
      key: 'documentNumber',
      render: (v?: string) => v ?? '—',
    },
    { title: 'Proveedor', dataIndex: 'supplierName', key: 'supplierName', render: (v?: string) => v ?? <em style={{ color: '#aaa' }}>Sin proveedor</em> },
    {
      title: 'Estado',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (s: string) => <Tag color={STATUS_COLOR[s] ?? 'default'}>{s}</Tag>,
    },
    { title: 'Ítems', dataIndex: 'totalItems', key: 'totalItems', align: 'right' as const, width: 65 },
    {
      title: 'Total',
      dataIndex: 'total',
      key: 'total',
      align: 'right' as const,
      width: 100,
      render: (v: number) => `$${(v ?? 0).toFixed(2)}`,
    },
    { title: 'Bodega', dataIndex: 'warehouseName', key: 'warehouseName', render: (v?: string) => v ?? '—' },
    {
      title: '',
      key: 'actions',
      width: 130,
      render: (_: unknown, r: PurchaseDto) => (
        <Space size={4}>
          <Tooltip title="Ver detalle">
            <Button size="small" icon={<EyeOutlined />} onClick={() => setViendo(r)} />
          </Tooltip>
          {r.status === 'Registrada' && (
            <>
              {canUpdate && <Tooltip title="Editar">
                <Button size="small" icon={<EditOutlined />} onClick={() => { setEditando(r); setFormOpen(true); }} />
              </Tooltip>}
              {canCancel && <Tooltip title="Anular">
                <Popconfirm
                  title="¿Anular esta compra?"
                  description="Se revertirá el stock agregado al momento de la compra."
                  onConfirm={() => handleAnular(r.id)}
                  okText="Sí, anular"
                  cancelText="No"
                >
                  <Button size="small" danger icon={<StopOutlined />} />
                </Popconfirm>
              </Tooltip>}
            </>
          )}
          {canDelete && r.status === 'Anulada' && (
            <Tooltip title="Eliminar">
              <Popconfirm title="¿Eliminar esta compra?" onConfirm={() => handleDelete(r.id)} okText="Sí" cancelText="No">
                <Button size="small" danger icon={<DeleteOutlined />} />
              </Popconfirm>
            </Tooltip>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16, flexWrap: 'wrap', gap: 8 }}>
        <h2 style={{ margin: 0 }}>Compras</h2>
        <Space wrap>
          <Select
            placeholder="Estado"
            allowClear
            style={{ width: 130 }}
            value={filtroEstado}
            onChange={setFiltroEstado}
            options={[
              { value: 'Registrada', label: 'Registrada' },
              { value: 'Anulada', label: 'Anulada' },
            ]}
          />
          <Select
            placeholder="Proveedor"
            allowClear
            showSearch
            style={{ width: 190 }}
            value={filtroProveedor}
            onChange={setFiltroProveedor}
            options={proveedores.map(p => ({ value: p.id, label: p.name }))}
            filterOption={(input, opt) => (opt?.label as string ?? '').toLowerCase().includes(input.toLowerCase())}
          />
          <RangePicker
            format="DD/MM/YYYY"
            value={filtroFechas}
            onChange={v => setFiltroFechas(v as [dayjs.Dayjs, dayjs.Dayjs] | null)}
            style={{ width: 230 }}
          />
          {canCreate && <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => { setEditando(null); setFormOpen(true); }}
          >
            Nueva compra
          </Button>}
        </Space>
      </div>

      <Table
        columns={columns}
        dataSource={compras}
        rowKey="id"
        loading={loading}
        pagination={{ defaultPageSize: 20, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
        size="small"
      />

      {/* Ver detalle */}
      {viendo && (
        <PurchaseForm
          open={true}
          readOnly
          compra={viendo}
          proveedores={proveedores}
          onClose={() => setViendo(null)}
          onSaved={() => { setViendo(null); load(); }}
        />
      )}

      {/* Crear / Editar */}
      {formOpen && (
        <PurchaseForm
          open={formOpen}
          compra={editando}
          proveedores={proveedores}
          onClose={() => setFormOpen(false)}
          onSaved={() => { setFormOpen(false); load(); }}
        />
      )}
    </div>
  );
}
