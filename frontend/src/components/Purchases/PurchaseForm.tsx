import { useState, useEffect, useMemo } from 'react';
import {
  Modal, Form, Select, DatePicker, Input, Button, Table, InputNumber,
  Space, Divider, Descriptions, Tag, message, Typography,
} from 'antd';
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import type {
  PurchaseDto, SupplierDto, PurchaseItemInputDto,
  InventoryArticleDto, WarehouseDto, TaxRateDto,
} from '../../types';
import { purchasesApi, inventoryApi, taxApi } from '../../services/api';

const { Text } = Typography;

const DOC_TYPE_OPTIONS = [
  { value: 1, label: 'Factura' },
  { value: 2, label: 'Nota de Venta' },
  { value: 3, label: 'Comprobante' },
  { value: 4, label: 'Liquidación de Compra' },
  { value: 5, label: 'Otro' },
];

const DOC_TYPE_VALUE: Record<string, number> = {
  Factura: 1, NotaDeVenta: 2, Comprobante: 3, LiquidacionCompra: 4, Otro: 5,
};

const DOC_TYPE_LABEL: Record<string, string> = {
  Factura: 'Factura', NotaDeVenta: 'Nota de Venta', Comprobante: 'Comprobante',
  LiquidacionCompra: 'Liquidación de Compra', Otro: 'Otro',
};

const STATUS_COLOR: Record<string, string> = { Registrada: 'green', Anulada: 'red' };

interface Props {
  open: boolean;
  compra: PurchaseDto | null;
  proveedores: SupplierDto[];
  readOnly?: boolean;
  onClose: () => void;
  onSaved: () => void;
}

interface ItemRow extends PurchaseItemInputDto {
  key: string;
  articleName?: string;
  unitSymbol?: string;
}

interface Fiscal {
  subtotal: number; discountTotal: number;
  taxableBase15: number; taxableBase0: number; taxableBaseExempt: number;
  iva15: number; ice: number; total: number;
}

function computeFiscal(items: ItemRow[], taxMap: Map<string, TaxRateDto>): Fiscal {
  let subtotal = 0, discountTotal = 0;
  let taxableBase15 = 0, taxableBase0 = 0, taxableBaseExempt = 0, iva15 = 0;

  for (const item of items) {
    const qty = item.quantity || 0;
    const price = item.unitPrice || 0;
    const gross = qty * price;
    const discAmt = Math.round(gross * ((item.discountPct || 0) / 100) * 100) / 100;
    const base = gross - discAmt;

    subtotal += gross;
    discountTotal += discAmt;

    const rate = item.taxRateId ? taxMap.get(item.taxRateId) : undefined;
    const taxAmt = rate ? Math.round(base * (rate.percentage / 100) * 100) / 100 : 0;

    if (!rate || rate.sriCode === '5' || rate.sriCode === '6' || rate.sriCode === '7') {
      taxableBaseExempt += base;
    } else if (rate.percentage > 0) {
      taxableBase15 += base;
      iva15 += taxAmt;
    } else {
      taxableBase0 += base;
    }
  }

  return {
    subtotal, discountTotal, taxableBase15, taxableBase0,
    taxableBaseExempt, iva15, ice: 0,
    total: taxableBase15 + taxableBase0 + taxableBaseExempt + iva15,
  };
}

export default function PurchaseForm({ open, compra, proveedores, readOnly = false, onClose, onSaved }: Props) {
  const [form] = Form.useForm();
  const [articulos, setArticulos] = useState<InventoryArticleDto[]>([]);
  const [bodegas, setBodegas] = useState<WarehouseDto[]>([]);
  const [taxRates, setTaxRates] = useState<TaxRateDto[]>([]);
  const [items, setItems] = useState<ItemRow[]>([]);
  const [saving, setSaving] = useState(false);

  const taxMap = useMemo(
    () => new Map(taxRates.map(r => [r.id, r])),
    [taxRates],
  );

  const fiscal = useMemo(() => computeFiscal(items, taxMap), [items, taxMap]);

  useEffect(() => {
    const load = async () => {
      try {
        const [artRes, bodRes, taxRes] = await Promise.all([
          inventoryApi.getArticles({ activeOnly: true }),
          inventoryApi.getWarehouses(),
          taxApi.getTaxRates(true),
        ]);
        setArticulos(artRes.data ?? []);
        setBodegas((bodRes.data ?? []).filter((b: WarehouseDto) => b.isActive));
        setTaxRates(taxRes.data ?? []);
      } catch {
        message.error('Error al cargar catálogos');
      }
    };
    load();
  }, []);

  useEffect(() => {
    if (!open) return;
    if (compra) {
      form.setFieldsValue({
        documentType: DOC_TYPE_VALUE[compra.documentType] ?? 1,
        documentNumber: compra.documentNumber,
        documentDate: dayjs(compra.documentDate),
        supplierId: compra.supplierId,
        destinationWarehouseId: compra.destinationWarehouseId,
        notes: compra.notes,
      });
      setItems(compra.items.map(i => ({
        key: i.id,
        articleId: i.articleId,
        unitId: i.unitId,
        quantity: i.quantity,
        unitPrice: i.unitPrice,
        discountPct: i.discountPct,
        taxRateId: i.taxRateId,
        notes: i.notes,
        articleName: i.articleName,
        unitSymbol: i.unitSymbol,
      })));
    } else {
      form.resetFields();
      form.setFieldsValue({ documentType: 1, documentDate: dayjs() });
      setItems([]);
    }
  }, [open, compra, form]);

  const addItem = () => {
    const key = crypto.randomUUID?.() ?? `item-${Math.random().toString(36).slice(2)}-${Date.now()}`;
    setItems(prev => [...prev, {
      key, articleId: '', unitId: '', quantity: 1, unitPrice: 0, discountPct: 0,
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

  const handleSave = async () => {
    const values = await form.validateFields();
    if (items.length === 0) { message.warning('Agrega al menos un ítem'); return; }
    if (items.some(i => !i.articleId || !i.unitId)) { message.warning('Completa todos los ítems'); return; }

    setSaving(true);
    try {
      const payload = {
        documentType: values.documentType,
        documentNumber: values.documentNumber || undefined,
        documentDate: values.documentDate.toISOString(),
        supplierId: values.supplierId || undefined,
        notes: values.notes || undefined,
        destinationWarehouseId: values.destinationWarehouseId || undefined,
        items: items.map(i => ({
          articleId: i.articleId,
          unitId: i.unitId,
          quantity: i.quantity,
          unitPrice: i.unitPrice,
          discountPct: i.discountPct || 0,
          taxRateId: i.taxRateId || undefined,
          notes: i.notes || undefined,
        })),
      };

      if (compra) {
        await purchasesApi.updatePurchase(compra.id, payload);
        message.success('Compra actualizada');
      } else {
        await purchasesApi.createPurchase(payload);
        message.success('Compra registrada');
      }
      onSaved();
    } catch {
      message.error('Error al guardar la compra');
    } finally {
      setSaving(false);
    }
  };

  // ── Vista de sólo lectura ──────────────────────────────────────────────────

  if (readOnly && compra) {
    const f = compra;
    return (
      <Modal
        open={open}
        onCancel={onClose}
        footer={<Button onClick={onClose}>Cerrar</Button>}
        title={`Compra — ${DOC_TYPE_LABEL[f.documentType] ?? f.documentType}${f.documentNumber ? ` N° ${f.documentNumber}` : ''}`}
        width={750}
      >
        <Descriptions bordered size="small" column={2} style={{ marginBottom: 16 }}>
          <Descriptions.Item label="Estado">
            <Tag color={STATUS_COLOR[f.status]}>{f.status}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Tipo de documento">
            {DOC_TYPE_LABEL[f.documentType] ?? f.documentType}
          </Descriptions.Item>
          <Descriptions.Item label="Fecha del comprobante">
            {dayjs(f.documentDate).format('DD/MM/YYYY')}
          </Descriptions.Item>
          <Descriptions.Item label="Proveedor">{f.supplierName ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Bodega destino">{f.warehouseName ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Observaciones">{f.notes ?? '—'}</Descriptions.Item>
        </Descriptions>

        <Table
          size="small"
          rowKey="id"
          dataSource={f.items}
          pagination={false}
          columns={[
            { title: 'Artículo', dataIndex: 'articleName' },
            { title: 'Cód.', dataIndex: 'internalCode', render: (v?: string) => v ?? '—', width: 80 },
            { title: 'Unidad', dataIndex: 'unitSymbol', width: 70 },
            { title: 'Cantidad', dataIndex: 'quantity', align: 'right', width: 85 },
            { title: 'P. Unit.', dataIndex: 'unitPrice', align: 'right', width: 90, render: (v: number) => `$${v.toFixed(4)}` },
            { title: 'Desc. %', dataIndex: 'discountPct', align: 'right', width: 75, render: (v: number) => v ? `${v}%` : '—' },
            { title: 'IVA', dataIndex: 'taxRateName', width: 80, render: (v?: string) => v ?? '—' },
            { title: 'Total', dataIndex: 'totalPrice', align: 'right', width: 90, render: (v: number) => `$${v.toFixed(2)}` },
          ]}
        />

        <div style={{ marginTop: 16, display: 'flex', justifyContent: 'flex-end' }}>
          <table style={{ fontSize: 13, borderCollapse: 'collapse', minWidth: 280 }}>
            <tbody>
              {[
                ['Subtotal', f.subtotal],
                ['(-) Descuentos', f.discountTotal],
                ['Base imponible 15%', f.taxableBase15],
                ['Base imponible 0%', f.taxableBase0],
                ['Base no objeto/exenta', f.taxableBaseExempt],
                ['IVA 15%', f.iva15],
                ['ICE', f.ice],
              ].map(([label, val]) => (
                <tr key={label as string}>
                  <td style={{ padding: '2px 16px 2px 0', color: '#666' }}>{label as string}</td>
                  <td style={{ padding: '2px 0', textAlign: 'right' }}>${(val as number).toFixed(2)}</td>
                </tr>
              ))}
              <tr style={{ borderTop: '2px solid #000', fontWeight: 600 }}>
                <td style={{ padding: '4px 16px 0 0' }}>Valor total</td>
                <td style={{ padding: '4px 0', textAlign: 'right' }}>${f.total.toFixed(2)}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </Modal>
    );
  }

  // ── Formulario de creación / edición ──────────────────────────────────────

  return (
    <Modal
      open={open}
      onCancel={onClose}
      onOk={handleSave}
      okText="Guardar"
      cancelText="Cancelar"
      confirmLoading={saving}
      title={compra ? 'Editar compra' : 'Registrar compra'}
      width={860}
    >
      <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '0 16px' }}>
          <Form.Item name="documentType" label="Tipo de documento" rules={[{ required: true }]}>
            <Select options={DOC_TYPE_OPTIONS} />
          </Form.Item>
          <Form.Item name="documentNumber" label="N° comprobante">
            <Input placeholder="Ej: 001-001-000000123" />
          </Form.Item>
          <Form.Item name="documentDate" label="Fecha del comprobante" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
          </Form.Item>
          <Form.Item name="supplierId" label="Proveedor">
            <Select
              allowClear
              showSearch
              options={proveedores.filter(p => p.isActive).map(p => ({ value: p.id, label: p.name }))}
              filterOption={(input, opt) => (opt?.label as string ?? '').toLowerCase().includes(input.toLowerCase())}
              placeholder="Seleccionar proveedor (opcional)"
            />
          </Form.Item>
          <Form.Item
            name="destinationWarehouseId"
            label="Bodega destino"
            rules={[{ required: true, message: 'Selecciona la bodega para actualizar el inventario' }]}
            tooltip="Indica dónde se acreditará el stock de los artículos comprados"
          >
            <Select
              options={bodegas.map(b => ({ value: b.id, label: b.name }))}
              placeholder="Seleccionar bodega"
            />
          </Form.Item>
          <Form.Item name="notes" label="Observaciones">
            <Input.TextArea rows={1} />
          </Form.Item>
        </div>
      </Form>

      <Divider>Ítems</Divider>

      <Table
        size="small"
        rowKey="key"
        dataSource={items}
        pagination={false}
        scroll={{ x: 820 }}
        columns={[
          {
            title: 'Artículo', key: 'articleId', width: 200,
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
            title: 'Unidad', key: 'unitId', width: 90,
            render: (_: unknown, row: ItemRow) => {
              const art = articulos.find(a => a.id === row.articleId);
              return art ? (
                <Select
                  style={{ width: '100%' }}
                  value={row.unitId || undefined}
                  onChange={v => updateItem(row.key, 'unitId', v)}
                  options={[{ value: art.baseUnitId, label: art.baseUnitSymbol }]}
                />
              ) : <Text type="secondary">—</Text>;
            },
          },
          {
            title: 'Cantidad', key: 'quantity', width: 90,
            render: (_: unknown, row: ItemRow) => (
              <InputNumber
                min={0.001} step={1} value={row.quantity}
                onChange={v => updateItem(row.key, 'quantity', v ?? 1)}
                style={{ width: '100%' }}
              />
            ),
          },
          {
            title: 'P. Unit.', key: 'unitPrice', width: 100,
            render: (_: unknown, row: ItemRow) => (
              <InputNumber
                min={0} step={0.01} precision={4} value={row.unitPrice}
                onChange={v => updateItem(row.key, 'unitPrice', v ?? 0)}
                prefix="$" style={{ width: '100%' }}
              />
            ),
          },
          {
            title: 'Desc. %', key: 'discountPct', width: 80,
            render: (_: unknown, row: ItemRow) => (
              <InputNumber
                min={0} max={100} step={0.01} precision={2}
                value={row.discountPct || 0}
                onChange={v => updateItem(row.key, 'discountPct', v ?? 0)}
                style={{ width: '100%' }}
              />
            ),
          },
          {
            title: 'IVA', key: 'taxRateId', width: 110,
            render: (_: unknown, row: ItemRow) => (
              <Select
                allowClear
                style={{ width: '100%' }}
                value={row.taxRateId}
                onChange={v => updateItem(row.key, 'taxRateId', v)}
                options={taxRates.map(t => ({ value: t.id, label: t.name }))}
                placeholder="Sin IVA"
              />
            ),
          },
          {
            title: 'Total', key: 'total', width: 90, align: 'right',
            render: (_: unknown, row: ItemRow) => {
              const gross = (row.quantity || 0) * (row.unitPrice || 0);
              const disc = Math.round(gross * ((row.discountPct || 0) / 100) * 100) / 100;
              const base = gross - disc;
              const rate = row.taxRateId ? taxMap.get(row.taxRateId) : undefined;
              const tax = rate ? Math.round(base * (rate.percentage / 100) * 100) / 100 : 0;
              return `$${(base + tax).toFixed(2)}`;
            },
          },
          {
            title: '', key: 'del', width: 40,
            render: (_: unknown, row: ItemRow) => (
              <Button size="small" danger icon={<DeleteOutlined />} onClick={() => removeItem(row.key)} />
            ),
          },
        ]}
        footer={() => (
          <Space style={{ justifyContent: 'space-between', width: '100%', display: 'flex' }}>
            <Button icon={<PlusOutlined />} onClick={addItem} size="small">Agregar ítem</Button>
          </Space>
        )}
      />

      {/* Resumen fiscal */}
      {items.length > 0 && (
        <div style={{ marginTop: 12, display: 'flex', justifyContent: 'flex-end' }}>
          <table style={{ fontSize: 13, borderCollapse: 'collapse', minWidth: 280 }}>
            <tbody>
              {[
                ['Subtotal', fiscal.subtotal],
                ['(-) Descuentos', fiscal.discountTotal],
                ['Base imponible 15%', fiscal.taxableBase15],
                ['Base imponible 0%', fiscal.taxableBase0],
                ['Base no objeto/exenta', fiscal.taxableBaseExempt],
                ['IVA 15%', fiscal.iva15],
                ['ICE', fiscal.ice],
              ].map(([label, val]) => (
                <tr key={label as string}>
                  <td style={{ padding: '2px 16px 2px 0', color: '#666' }}>{label as string}</td>
                  <td style={{ padding: '2px 0', textAlign: 'right' }}>${(val as number).toFixed(2)}</td>
                </tr>
              ))}
              <tr style={{ borderTop: '2px solid #000', fontWeight: 600 }}>
                <td style={{ padding: '4px 16px 0 0' }}>Valor total</td>
                <td style={{ padding: '4px 0', textAlign: 'right' }}>${fiscal.total.toFixed(2)}</td>
              </tr>
            </tbody>
          </table>
        </div>
      )}
    </Modal>
  );
}
