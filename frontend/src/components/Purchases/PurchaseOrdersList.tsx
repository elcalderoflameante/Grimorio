import { useState, useEffect, useCallback } from 'react';
import {
  Table, Button, Tag, Space, Popconfirm, message, Select, Tooltip,
} from 'antd';
import {
  PlusOutlined, EyeOutlined, SendOutlined, CheckOutlined,
  CloseOutlined, DeleteOutlined, EditOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import type { PurchaseOrderDto, SupplierDto, PurchaseOrderStatus } from '../../types';
import { purchasesApi } from '../../services/api';
import PurchaseOrderForm from './PurchaseOrderForm';
import PurchaseReception from './PurchaseReception';

const ESTADO_COLOR: Record<PurchaseOrderStatus, string> = {
  Draft: 'default',
  Sent: 'blue',
  Received: 'green',
  Cancelled: 'red',
};

export default function PurchaseOrdersList() {
  const [ordenes, setOrdenes] = useState<PurchaseOrderDto[]>([]);
  const [proveedores, setProveedores] = useState<SupplierDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [filtroEstado, setFiltroEstado] = useState<string | undefined>();
  const [filtroProveedor, setFiltroProveedor] = useState<string | undefined>();

  const [formOpen, setFormOpen] = useState(false);
  const [editingOrden, setEditingOrden] = useState<PurchaseOrderDto | null>(null);
  const [viewingOrden, setViewingOrden] = useState<PurchaseOrderDto | null>(null);
  const [recibiendo, setRecibiendo] = useState<PurchaseOrderDto | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [orRes, pvRes] = await Promise.all([
        purchasesApi.getOrders({ status: filtroEstado, supplierId: filtroProveedor }),
        purchasesApi.getSuppliers(),
      ]);
      setOrdenes(orRes.data);
      setProveedores(pvRes.data);
    } finally {
      setLoading(false);
    }
  }, [filtroEstado, filtroProveedor]);

  useEffect(() => { load(); }, [load]);

  const handleEnviar = async (id: string) => {
    try {
      await purchasesApi.sendOrder(id);
      message.success('Orden enviada al proveedor');
      load();
    } catch {
      message.error('Error al enviar orden');
    }
  };

  const handleCancelar = async (id: string) => {
    try {
      await purchasesApi.cancelOrder(id);
      message.success('Orden cancelada');
      load();
    } catch {
      message.error('Error al cancelar orden');
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await purchasesApi.deleteOrden(id);
      message.success('Orden eliminada');
      load();
    } catch {
      message.error('Error al eliminar orden');
    }
  };

  const columns = [
    { title: 'N° Orden', dataIndex: 'orderNumber', key: 'orderNumber', width: 130 },
    {
      title: 'Estado',
      dataIndex: 'status',
      key: 'status',
      width: 110,
      render: (e: PurchaseOrderStatus) => <Tag color={ESTADO_COLOR[e]}>{e}</Tag>,
    },
    { title: 'Proveedor', dataIndex: 'supplierName', key: 'supplierName' },
    {
      title: 'Emisión',
      dataIndex: 'issuedAt',
      key: 'issuedAt',
      width: 110,
      render: (d: string) => dayjs(d).format('DD/MM/YY'),
    },
    {
      title: 'Esperada',
      dataIndex: 'expectedAt',
      key: 'expectedAt',
      width: 110,
      render: (d?: string) => d ? dayjs(d).format('DD/MM/YY') : '—',
    },
    { title: 'Ítems', dataIndex: 'totalItems', key: 'totalItems', align: 'right' as const, width: 70 },
    {
      title: 'Total',
      dataIndex: 'total',
      key: 'total',
      align: 'right' as const,
      width: 110,
      render: (v: number) => `$${v.toFixed(2)}`,
    },
    { title: 'Bodega', dataIndex: 'warehouseName', key: 'warehouseName', render: (v?: string) => v ?? '—' },
    {
      title: '',
      key: 'actions',
      width: 160,
      render: (_: unknown, r: PurchaseOrderDto) => (
        <Space size={4}>
          <Tooltip title="Ver detalle">
            <Button size="small" icon={<EyeOutlined />} onClick={() => setViewingOrden(r)} />
          </Tooltip>
          {r.status === 'Draft' && (
            <>
              <Tooltip title="Editar">
                <Button size="small" icon={<EditOutlined />} onClick={() => { setEditingOrden(r); setFormOpen(true); }} />
              </Tooltip>
              <Tooltip title="Enviar">
                <Popconfirm title="¿Enviar esta orden al proveedor?" onConfirm={() => handleEnviar(r.id)} okText="Sí" cancelText="No">
                  <Button size="small" type="primary" icon={<SendOutlined />} />
                </Popconfirm>
              </Tooltip>
              <Tooltip title="Eliminar">
                <Popconfirm title="¿Eliminar orden?" onConfirm={() => handleDelete(r.id)} okText="Sí" cancelText="No">
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Tooltip>
            </>
          )}
          {r.status === 'Sent' && (
            <>
              <Tooltip title="Recibir mercadería">
                <Button size="small" type="primary" icon={<CheckOutlined />} onClick={() => setRecibiendo(r)} />
              </Tooltip>
              <Tooltip title="Cancelar">
                <Popconfirm title="¿Cancelar esta orden?" onConfirm={() => handleCancelar(r.id)} okText="Sí" cancelText="No">
                  <Button size="small" danger icon={<CloseOutlined />} />
                </Popconfirm>
              </Tooltip>
            </>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16, flexWrap: 'wrap', gap: 8 }}>
        <h2 style={{ margin: 0 }}>Órdenes de compra</h2>
        <Space wrap>
          <Select
            placeholder="Filtrar por estado"
            allowClear
            style={{ width: 160 }}
            value={filtroEstado}
            onChange={setFiltroEstado}
            options={[
              { value: 'Draft', label: 'Draft' },
              { value: 'Sent', label: 'Sent' },
              { value: 'Received', label: 'Received' },
              { value: 'Cancelled', label: 'Cancelled' },
            ]}
          />
          <Select
            placeholder="Filtrar por proveedor"
            allowClear
            style={{ width: 200 }}
            value={filtroProveedor}
            onChange={setFiltroProveedor}
            options={proveedores.map(p => ({ value: p.id, label: p.name }))}
            showSearch
            filterOption={(input, opt) => (opt?.label as string ?? '').toLowerCase().includes(input.toLowerCase())}
          />
          <Button type="primary" icon={<PlusOutlined />} onClick={() => { setEditingOrden(null); setFormOpen(true); }}>
            Nueva orden
          </Button>
        </Space>
      </div>

      <Table
        columns={columns}
        dataSource={ordenes}
        rowKey="id"
        loading={loading}
        pagination={{ pageSize: 20 }}
        size="small"
      />

      {/* Ver detalle (solo lectura) */}
      {viewingOrden && (
        <PurchaseOrderForm
          open={true}
          readOnly
          orden={viewingOrden}
          proveedores={proveedores}
          onClose={() => setViewingOrden(null)}
          onSaved={() => { setViewingOrden(null); load(); }}
        />
      )}

      {/* Crear / Editar */}
      {formOpen && (
        <PurchaseOrderForm
          open={formOpen}
          orden={editingOrden}
          proveedores={proveedores}
          onClose={() => setFormOpen(false)}
          onSaved={() => { setFormOpen(false); load(); }}
        />
      )}

      {/* Recibir mercadería */}
      {recibiendo && (
        <PurchaseReception
          open={true}
          orden={recibiendo}
          onClose={() => setRecibiendo(null)}
          onSaved={() => { setRecibiendo(null); load(); }}
        />
      )}
    </div>
  );
}

