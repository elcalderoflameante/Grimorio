import { useState, useEffect } from 'react';
import {
  Modal, Form, Select, DatePicker, Input, Button, Table, InputNumber,
  Space, Divider, Descriptions, Tag, message,
} from 'antd';
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import type {
  PurchaseOrderDto, SupplierDto, PurchaseOrderItemInputDto,
  InventoryArticleDto, WarehouseDto, PurchaseOrderStatus,
} from '../../types';
import { purchasesApi, inventoryApi } from '../../services/api';

interface Props {
  open: boolean;
  orden: PurchaseOrderDto | null;
  proveedores: SupplierDto[];
  readOnly?: boolean;
  onClose: () => void;
  onSaved: () => void;
}

interface ItemRow extends PurchaseOrderItemInputDto {
  key: string;
  articleName?: string;
  unitSymbol?: string;
}

const ESTADO_COLOR: Record<PurchaseOrderStatus, string> = {
  Draft: 'default', Sent: 'blue', Received: 'green', Cancelled: 'red',
};

export default function PurchaseOrderForm({ open, orden, proveedores, readOnly = false, onClose, onSaved }: Props) {
  const [form] = Form.useForm();
  const [articulos, setArticulos] = useState<InventoryArticleDto[]>([]);
  const [bodegas, setBodegas] = useState<WarehouseDto[]>([]);
  const [items, setItems] = useState<ItemRow[]>([]);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const loadCatalogos = async () => {
      const [artRes, bodRes] = await Promise.all([
        inventoryApi.getArticles({ activeOnly: true }),
        inventoryApi.getWarehouses(),
      ]);
      setArticulos(artRes.data);
      setBodegas(bodRes.data.filter((b: WarehouseDto) => b.isActive));
    };
    loadCatalogos();
  }, []);

  useEffect(() => {
    if (!open) return;
    if (orden) {
      form.setFieldsValue({
        supplierId: orden.supplierId,
        expectedAt: orden.expectedAt ? dayjs(orden.expectedAt) : null,
        notes: orden.notes,
        destinationWarehouseId: orden.destinationWarehouseId,
      });
      setItems(orden.items.map(i => ({
        key: i.id,
        articleId: i.articleId,
        unitId: i.unitId,
        quantityOrdered: i.quantityOrdered,
        unitPrice: i.unitPrice,
        notes: i.notes,
        articleName: i.articleName,
        unitSymbol: i.unitSymbol,
      })));
    } else {
      form.resetFields();
      setItems([]);
    }
  }, [open, orden, form]);

  const addItem = () => {
    setItems(prev => [...prev, {
      key: crypto.randomUUID(),
      articleId: '',
      unitId: '',
      quantityOrdered: 1,
      unitPrice: 0,
    }]);
  };

  const updateItem = (key: string, field: keyof ItemRow, value: unknown) => {
    setItems(prev => prev.map(i => {
      if (i.key !== key) return i;
      const updated = { ...i, [field]: value };
      if (field === 'articleId') {
        const art = articulos.find(a => a.id === value);
        if (art) {
          updated.unitId = art.baseUnitId;
          updated.unitSymbol = art.baseUnitSymbol;
          updated.articleName = art.name;
        }
      }
      return updated;
    }));
  };

  const removeItem = (key: string) => setItems(prev => prev.filter(i => i.key !== key));

  const total = items.reduce((s, i) => s + (i.quantityOrdered || 0) * (i.unitPrice || 0), 0);

  const handleSave = async () => {
    const values = await form.validateFields();
    if (items.length === 0) { message.warning('Agrega al menos un ítem'); return; }
    if (items.some(i => !i.articleId || !i.unitId)) { message.warning('Completa todos los ítems'); return; }

    setSaving(true);
    try {
      const payload = {
        supplierId: values.supplierId,
        expectedAt: values.expectedAt?.toISOString(),
        notes: values.notes,
        destinationWarehouseId: values.destinationWarehouseId,
        items: items.map(i => ({
          articleId: i.articleId,
          unitId: i.unitId,
          quantityOrdered: i.quantityOrdered,
          unitPrice: i.unitPrice,
          notes: i.notes,
        })),
      };

      if (orden) {
        await purchasesApi.updateOrden(orden.id, payload);
        message.success('Orden actualizada');
      } else {
        await purchasesApi.createOrden(payload);
        message.success('Orden creada');
      }
      onSaved();
    } catch {
      message.error('Error al guardar la orden');
    } finally {
      setSaving(false);
    }
  };

  // Read-only view
  if (readOnly && orden) {
    return (
      <Modal
        open={open}
        onCancel={onClose}
        footer={<Button onClick={onClose}>Cerrar</Button>}
        title={`Orden ${orden.orderNumber}`}
        width={700}
      >
        <Descriptions bordered size="small" column={2} style={{ marginBottom: 16 }}>
          <Descriptions.Item label="Estado">
            <Tag color={ESTADO_COLOR[orden.status as PurchaseOrderStatus]}>{orden.status}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Proveedor">{orden.supplierName}</Descriptions.Item>
          <Descriptions.Item label="Emisión">{dayjs(orden.issuedAt).format('DD/MM/YYYY')}</Descriptions.Item>
          <Descriptions.Item label="Esperada">{orden.expectedAt ? dayjs(orden.expectedAt).format('DD/MM/YYYY') : '—'}</Descriptions.Item>
          {orden.receivedAt && (
            <Descriptions.Item label="Recepción" span={2}>{dayjs(orden.receivedAt).format('DD/MM/YYYY HH:mm')}</Descriptions.Item>
          )}
          <Descriptions.Item label="Bodega destino">{orden.warehouseName ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Observaciones">{orden.notes ?? '—'}</Descriptions.Item>
        </Descriptions>

        <Table
          size="small"
          rowKey="id"
          dataSource={orden.items}
          pagination={false}
          columns={[
            { title: 'Artículo', dataIndex: 'articleName' },
            { title: 'Código', dataIndex: 'internalCode', render: (v?: string) => v ?? '—' },
            { title: 'Unidad', dataIndex: 'unitSymbol', width: 80 },
            { title: 'Pedido', dataIndex: 'quantityOrdered', align: 'right', width: 90 },
            { title: 'Recibido', dataIndex: 'quantityReceived', align: 'right', width: 90 },
            { title: 'P. Unit.', dataIndex: 'unitPrice', align: 'right', width: 100, render: (v: number) => `$${v.toFixed(4)}` },
            { title: 'Total', dataIndex: 'totalPrice', align: 'right', width: 100, render: (v: number) => `$${v.toFixed(2)}` },
          ]}
          summary={() => (
            <Table.Summary.Row>
              <Table.Summary.Cell index={0} colSpan={6} align="right"><strong>Total</strong></Table.Summary.Cell>
              <Table.Summary.Cell index={1} align="right"><strong>${orden.total.toFixed(2)}</strong></Table.Summary.Cell>
            </Table.Summary.Row>
          )}
        />
      </Modal>
    );
  }

  // Edit / Create form
  const unidadesDeArticulo = (articleId: string) => {
    const art = articulos.find(a => a.id === articleId);
    if (!art) return [];
    return [{ id: art.baseUnitId, symbol: art.baseUnitSymbol, name: art.baseUnitName }];
  };

  return (
    <Modal
      open={open}
      onCancel={onClose}
      onOk={handleSave}
      okText="Guardar"
      cancelText="Cancelar"
      confirmLoading={saving}
      title={orden ? `Editar orden ${orden.orderNumber}` : 'Nueva orden de compra'}
      width={800}
    >
      <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 16px' }}>
          <Form.Item name="supplierId" label="Proveedor" rules={[{ required: true, message: 'Requerido' }]}>
            <Select
              showSearch
              options={proveedores.filter(p => p.isActive).map(p => ({ value: p.id, label: p.name }))}
              filterOption={(input, opt) => (opt?.label as string ?? '').toLowerCase().includes(input.toLowerCase())}
              placeholder="Seleccionar proveedor"
            />
          </Form.Item>
          <Form.Item name="expectedAt" label="Fecha esperada de entrega">
            <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
          </Form.Item>
          <Form.Item name="destinationWarehouseId" label="Bodega destino">
            <Select
              allowClear
              options={bodegas.map(b => ({ value: b.id, label: b.name }))}
              placeholder="Seleccionar bodega"
            />
          </Form.Item>
          <Form.Item name="notes" label="Observaciones">
            <Input.TextArea rows={2} />
          </Form.Item>
        </div>
      </Form>

      <Divider>Ítems</Divider>

      <Table
        size="small"
        rowKey="key"
        dataSource={items}
        pagination={false}
        columns={[
          {
            title: 'Artículo',
            key: 'articleId',
            render: (_: unknown, row: ItemRow) => (
              <Select
                style={{ width: '100%' }}
                value={row.articleId || undefined}
                onChange={v => updateItem(row.key, 'articleId', v)}
                showSearch
                options={articulos.map(a => ({ value: a.id, label: `${a.name}${a.internalCode ? ` (${a.internalCode})` : ''}` }))}
                filterOption={(input, opt) => (opt?.label as string ?? '').toLowerCase().includes(input.toLowerCase())}
                placeholder="Artículo"
              />
            ),
          },
          {
            title: 'Unidad',
            key: 'unitId',
            width: 110,
            render: (_: unknown, row: ItemRow) => (
              <Select
                style={{ width: '100%' }}
                value={row.unitId || undefined}
                onChange={v => updateItem(row.key, 'unitId', v)}
                options={unidadesDeArticulo(row.articleId).map(u => ({ value: u.id, label: u.symbol }))}
                placeholder="Unidad"
              />
            ),
          },
          {
            title: 'Cantidad',
            key: 'quantityOrdered',
            width: 100,
            render: (_: unknown, row: ItemRow) => (
              <InputNumber
                min={0.001}
                step={1}
                value={row.quantityOrdered}
                onChange={v => updateItem(row.key, 'quantityOrdered', v ?? 1)}
                style={{ width: '100%' }}
              />
            ),
          },
          {
            title: 'P. Unit.',
            key: 'unitPrice',
            width: 110,
            render: (_: unknown, row: ItemRow) => (
              <InputNumber
                min={0}
                step={0.01}
                precision={4}
                value={row.unitPrice}
                onChange={v => updateItem(row.key, 'unitPrice', v ?? 0)}
                prefix="$"
                style={{ width: '100%' }}
              />
            ),
          },
          {
            title: 'Total',
            key: 'total',
            width: 100,
            align: 'right',
            render: (_: unknown, row: ItemRow) => `$${((row.quantityOrdered || 0) * (row.unitPrice || 0)).toFixed(2)}`,
          },
          {
            title: '',
            key: 'del',
            width: 40,
            render: (_: unknown, row: ItemRow) => (
              <Button size="small" danger icon={<DeleteOutlined />} onClick={() => removeItem(row.key)} />
            ),
          },
        ]}
        footer={() => (
          <Space style={{ justifyContent: 'space-between', width: '100%', display: 'flex' }}>
            <Button icon={<PlusOutlined />} onClick={addItem} size="small">Agregar ítem</Button>
            <strong>Total: ${total.toFixed(2)}</strong>
          </Space>
        )}
      />
    </Modal>
  );
}
