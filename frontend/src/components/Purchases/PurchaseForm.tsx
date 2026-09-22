import { useState, useEffect, useMemo } from 'react';
import { App as AntApp, Modal, Form, Select, DatePicker, Input, Button, Table, InputNumber,
  Space, Divider, Descriptions, Tag, Typography, Switch, Upload, Tabs, Alert } from 'antd';
import { ArrowLeftOutlined, PlusOutlined, DeleteOutlined, UploadOutlined, PaperClipOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import type {
  PurchaseDto, SupplierDto, PurchaseItemInputDto,
  InventoryArticleDto, WarehouseDto, TaxRateDto, MeasurementUnitDto, UnitConversionDto,
} from '../../types';
import { purchasesApi, inventoryApi, resolveMediaUrl, taxApi } from '../../services/api';
import { branchDateTimeUtcIso, branchStartOfDayUtcIso, formatBranchDate, formatBranchDateTime, toBranchDayjs } from '../../utils/branchTimeZone';
import './PurchaseForm.css';

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
  inventoryUnitSymbol?: string;
}

interface Fiscal {
  subtotal: number; discountTotal: number;
  taxableBase15: number; taxableBase0: number; taxableBaseExempt: number; taxableBaseNotSubject: number;
  iva15: number; ice: number; irbpnr: number; tip: number; total: number;
}

interface ImportedSupplierCandidate {
  taxId?: string;
  name?: string;
  commercialName?: string;
  address?: string;
}

function computeFiscal(items: ItemRow[], taxMap: Map<string, TaxRateDto>): Fiscal {
  let subtotal = 0, discountTotal = 0;
  let taxableBase15 = 0, taxableBase0 = 0, taxableBaseExempt = 0, taxableBaseNotSubject = 0, iva15 = 0;

  for (const item of items) {
    const qty = item.quantity || 0;
    const price = item.unitPrice || 0;
    const gross = qty * price;
    const exactDiscount = typeof item.discountAmount === 'number' ? item.discountAmount : undefined;
    const discAmt = Math.min(gross, Math.max(0, exactDiscount ?? Math.round(gross * ((item.discountPct || 0) / 100) * 100) / 100));
    const base = gross - discAmt;

    subtotal += gross;
    discountTotal += discAmt;

    const rate = item.taxRateId ? taxMap.get(item.taxRateId) : undefined;
    const taxAmt = rate ? Math.round(base * (rate.percentage / 100) * 100) / 100 : 0;

    if (!rate || rate.sriCode === '6' || rate.sriCode === '7') {
      taxableBaseExempt += base;
    } else if (rate.sriCode === '5') {
      taxableBaseNotSubject += base;
    } else if (rate.percentage > 0) {
      taxableBase15 += base;
      iva15 += taxAmt;
    } else {
      taxableBase0 += base;
    }
  }

  return {
    subtotal, discountTotal, taxableBase15, taxableBase0,
    taxableBaseExempt, taxableBaseNotSubject, iva15, ice: 0, irbpnr: 0, tip: 0,
    total: taxableBase15 + taxableBase0 + taxableBaseExempt + taxableBaseNotSubject + iva15,
  };
}

export default function PurchaseForm({ open, compra, proveedores, readOnly = false, onClose, onSaved }: Props) {
  const { message } = AntApp.useApp();

  const [form] = Form.useForm();
  const [articulos, setArticulos] = useState<InventoryArticleDto[]>([]);
  const [bodegas, setBodegas] = useState<WarehouseDto[]>([]);
  const [taxRates, setTaxRates] = useState<TaxRateDto[]>([]);
  const [units, setUnits] = useState<MeasurementUnitDto[]>([]);
  const [conversions, setConversions] = useState<UnitConversionDto[]>([]);
  const [items, setItems] = useState<ItemRow[]>([]);
  const [saving, setSaving] = useState(false);
  const [importingXml, setImportingXml] = useState(false);
  const [uploadingAttachment, setUploadingAttachment] = useState(false);
  const [creatingSupplier, setCreatingSupplier] = useState(false);
  const [createdSuppliers, setCreatedSuppliers] = useState<SupplierDto[]>([]);
  const [importedSupplierCandidate, setImportedSupplierCandidate] = useState<ImportedSupplierCandidate | null>(null);
  const iceValue = Form.useWatch('ice', form) ?? 0;
  const irbpnrValue = Form.useWatch('irbpnr', form) ?? 0;
  const tipValue = Form.useWatch('tip', form) ?? 0;

  const taxMap = useMemo(
    () => new Map(taxRates.map(r => [r.id, r])),
    [taxRates],
  );

  const availableSuppliers = useMemo(() => {
    const byId = new Map<string, SupplierDto>();
    [...proveedores, ...createdSuppliers].forEach(s => byId.set(s.id, s));
    return Array.from(byId.values());
  }, [proveedores, createdSuppliers]);

  const getArticleUnitOptions = (articleId?: string) => {
    const article = articulos.find(a => a.id === articleId);
    if (!article) return [];

    const unitIds = new Set<string>([article.baseUnitId]);
    conversions.forEach(conversion => {
      if (conversion.originUnitId === article.baseUnitId) unitIds.add(conversion.destinationUnitId);
      if (conversion.destinationUnitId === article.baseUnitId) unitIds.add(conversion.originUnitId);
    });

    return units
      .filter(unit => unitIds.has(unit.id))
      .map(unit => ({ value: unit.id, label: unit.symbol }));
  };

  const unitOptions = useMemo(
    () => units.map(unit => ({ value: unit.id, label: unit.symbol })),
    [units],
  );

  const fiscal = useMemo(() => {
    const result = computeFiscal(items, taxMap);
    const ice = Number(iceValue) || 0;
    const irbpnr = Number(irbpnrValue) || 0;
    const tip = Number(tipValue) || 0;
    return {
      ...result,
      ice,
      irbpnr,
      tip,
      total: result.total + ice + irbpnr + tip,
    };
  }, [items, taxMap, iceValue, irbpnrValue, tipValue]);

  useEffect(() => {
    const load = async () => {
      try {
        const [artRes, bodRes, taxRes, unitsRes, conversionsRes] = await Promise.all([
          inventoryApi.getArticles({ activeOnly: true }),
          inventoryApi.getWarehouses(),
          taxApi.getTaxRates(true),
          inventoryApi.getUnits(),
          inventoryApi.getConversions(),
        ]);
        setArticulos(artRes.data ?? []);
        setBodegas((bodRes.data ?? []).filter((b: WarehouseDto) => b.isActive));
        setTaxRates(taxRes.data ?? []);
        setUnits(unitsRes.data ?? []);
        setConversions(conversionsRes.data ?? []);
      } catch {
        message.error('Error al cargar catálogos');
      }
    };
    load();
  }, []);

  useEffect(() => {
    if (!open) return;
    setImportedSupplierCandidate(null);
    if (compra) {
      form.setFieldsValue({
        documentType: DOC_TYPE_VALUE[compra.documentType] ?? 1,
        documentNumber: compra.documentNumber,
        documentDate: toBranchDayjs(compra.documentDate) ?? dayjs(compra.documentDate),
        accessKey: compra.accessKey,
        authorizationNumber: compra.authorizationNumber,
        authorizationDate: compra.authorizationDate ? (toBranchDayjs(compra.authorizationDate) ?? dayjs(compra.authorizationDate)) : undefined,
        environment: compra.environment,
        emissionType: compra.emissionType,
        supplierCommercialName: compra.supplierCommercialName,
        supplierMatrixAddress: compra.supplierMatrixAddress,
        supplierBranchAddress: compra.supplierBranchAddress,
        supplierSpecialTaxpayerNumber: compra.supplierSpecialTaxpayerNumber,
        supplierObligatedAccounting: compra.supplierObligatedAccounting,
        paymentMethodSriCode: compra.paymentMethodSriCode,
        paymentMethodName: compra.paymentMethodName,
        paymentAmount: compra.paymentAmount,
        ice: compra.ice,
        irbpnr: compra.irbpnr,
        tip: compra.tip,
        xmlFileUrl: compra.xmlFileUrl,
        pdfFileUrl: compra.pdfFileUrl,
        supplierId: compra.supplierId,
        destinationWarehouseId: compra.destinationWarehouseId,
        notes: compra.notes,
      });
      setItems(compra.items.map(i => ({
        key: i.id,
        articleId: i.articleId,
        unitId: i.unitId,
        supplierMainCode: i.supplierMainCode,
        supplierAuxCode: i.supplierAuxCode,
        supplierDescription: i.supplierDescription,
        additionalDetail: i.additionalDetail,
        quantity: i.quantity,
        inventoryQuantity: i.inventoryQuantity ?? i.quantity,
        inventoryUnitId: i.inventoryUnitId ?? i.unitId,
        unitPrice: i.unitPrice,
        discountPct: i.discountPct,
        discountAmount: i.discountAmount,
        taxRateId: i.taxRateId,
        notes: i.notes,
        articleName: i.articleName,
        unitSymbol: i.unitSymbol,
        inventoryUnitSymbol: i.inventoryUnitSymbol ?? i.unitSymbol,
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
      key, articleId: '', unitId: '', quantity: 1,
      inventoryQuantity: 1, inventoryUnitId: '',
      unitPrice: 0, discountPct: 0,
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
          updated.inventoryUnitId = art.baseUnitId;
          updated.inventoryUnitSymbol = art.baseUnitSymbol;
          updated.inventoryQuantity = updated.inventoryQuantity || updated.quantity || 1;
          updated.articleName = art.name;
        }
      }
      if (field === 'unitId') {
        const unit = units.find(u => u.id === value);
        updated.unitSymbol = unit?.symbol;
      }
      if (field === 'inventoryUnitId') {
        const unit = units.find(u => u.id === value);
        updated.inventoryUnitSymbol = unit?.symbol;
      }
      return updated;
    }));
  };

  const removeItem = (key: string) => setItems(prev => prev.filter(i => i.key !== key));

  const findTaxRateId = (percentage: number) =>
    taxRates.find(t => Math.abs((t.percentage ?? 0) - percentage) < 0.01)?.id;

  const findArticleBySupplierCode = (mainCode?: string, auxCode?: string) => {
    const codes = [mainCode, auxCode].filter(Boolean).map(c => c!.trim().toLowerCase());
    if (codes.length === 0) return undefined;
    return articulos.find(a => a.internalCode && codes.includes(a.internalCode.trim().toLowerCase()));
  };

  const handleImportXml = async (file: File) => {
    setImportingXml(true);
    try {
      const data = new FormData();
      data.append('file', file);
      const res = await purchasesApi.importPurchaseXml(data);
      const imported = res.data;
      const supplier = availableSuppliers.find(p =>
        p.taxId?.trim() && imported.supplierTaxId?.trim() &&
        p.taxId.trim() === imported.supplierTaxId.trim());
      setImportedSupplierCandidate(supplier ? null : {
        taxId: imported.supplierTaxId,
        name: imported.supplierName,
        commercialName: imported.supplierCommercialName,
        address: imported.supplierBranchAddress || imported.supplierMatrixAddress,
      });

      form.setFieldsValue({
        documentType: imported.documentType || 1,
        documentNumber: imported.documentNumber,
        documentDate: imported.documentDate ? (toBranchDayjs(imported.documentDate) ?? dayjs(imported.documentDate)) : dayjs(),
        accessKey: imported.accessKey,
        authorizationNumber: imported.authorizationNumber,
        authorizationDate: imported.authorizationDate ? (toBranchDayjs(imported.authorizationDate) ?? dayjs(imported.authorizationDate)) : undefined,
        environment: imported.environment,
        emissionType: imported.emissionType,
        supplierId: supplier?.id,
        supplierCommercialName: imported.supplierCommercialName,
        supplierMatrixAddress: imported.supplierMatrixAddress,
        supplierBranchAddress: imported.supplierBranchAddress,
        supplierSpecialTaxpayerNumber: imported.supplierSpecialTaxpayerNumber,
        supplierObligatedAccounting: imported.supplierObligatedAccounting,
        paymentMethodSriCode: imported.paymentMethodSriCode,
        paymentMethodName: imported.paymentMethodName,
        paymentAmount: imported.paymentAmount,
        ice: imported.ice,
        irbpnr: imported.irbpnr,
        tip: imported.tip,
        xmlFileUrl: imported.xmlFileUrl,
      });

      const mappedItems = imported.items.map((item, index) => {
        const article = findArticleBySupplierCode(item.supplierMainCode, item.supplierAuxCode);
        return {
          key: `${Date.now()}-${index}`,
          articleId: article?.id ?? '',
          unitId: article?.baseUnitId ?? '',
          unitSymbol: article?.baseUnitSymbol,
          articleName: article?.name,
          supplierMainCode: item.supplierMainCode,
          supplierAuxCode: item.supplierAuxCode,
          supplierDescription: item.supplierDescription,
          additionalDetail: item.additionalDetail,
          quantity: item.quantity,
          inventoryQuantity: item.quantity,
          inventoryUnitId: article?.baseUnitId ?? '',
          inventoryUnitSymbol: article?.baseUnitSymbol,
          unitPrice: item.unitPrice,
          discountPct: 0,
          discountAmount: item.discountAmount || undefined,
          taxRateId: findTaxRateId(item.taxPercentage),
        };
      });
      setItems(mappedItems);

      const unmapped = mappedItems.filter(i => !i.articleId).length;
      if (unmapped > 0) {
        message.warning(`XML importado. Hay ${unmapped} item(s) por vincular con articulos internos.`);
      } else {
        message.success(supplier
          ? 'XML importado y proveedor vinculado'
          : 'XML importado. Revisa el proveedor detectado.');
      }
    } catch {
      message.error('No se pudo importar el XML de compra');
    } finally {
      setImportingXml(false);
    }
  };

  const handleUploadAttachment = async (file: File, fieldName: 'xmlFileUrl' | 'pdfFileUrl') => {
    setUploadingAttachment(true);
    try {
      const data = new FormData();
      data.append('file', file);
      const res = await purchasesApi.uploadPurchaseAttachment(data);
      form.setFieldValue(fieldName, res.data.fileUrl);
      message.success('Adjunto cargado');
    } catch {
      message.error('No se pudo cargar el adjunto');
    } finally {
      setUploadingAttachment(false);
    }
  };

  const handleCreateImportedSupplier = async () => {
    if (!importedSupplierCandidate?.name) {
      message.warning('La factura no contiene razon social del proveedor');
      return;
    }

    setCreatingSupplier(true);
    try {
      const res = await purchasesApi.createSupplier({
        name: importedSupplierCandidate.name,
        taxId: importedSupplierCandidate.taxId,
        address: importedSupplierCandidate.address,
        contactName: importedSupplierCandidate.commercialName,
      });
      setCreatedSuppliers(prev => [...prev, res.data]);
      form.setFieldValue('supplierId', res.data.id);
      setImportedSupplierCandidate(null);
      message.success('Proveedor creado y seleccionado');
    } catch {
      message.error('No se pudo crear el proveedor');
    } finally {
      setCreatingSupplier(false);
    }
  };

  const handleSave = async () => {
    const values = await form.validateFields();
    if (items.length === 0) { message.warning('Agrega al menos un ítem'); return; }
    if (items.some(i => !i.articleId || !i.unitId)) { message.warning('Completa todos los ítems'); return; }

    if (items.some(i => !i.inventoryQuantity || !i.inventoryUnitId)) {
      message.warning('Completa la cantidad y unidad de ingreso a inventario');
      return;
    }

    setSaving(true);
    try {
      const payload = {
        documentType: values.documentType,
        documentNumber: values.documentNumber || undefined,
        documentDate: branchStartOfDayUtcIso(values.documentDate) ?? values.documentDate.toISOString(),
        accessKey: values.accessKey || undefined,
        authorizationNumber: values.authorizationNumber || undefined,
        authorizationDate: branchDateTimeUtcIso(values.authorizationDate),
        environment: values.environment || undefined,
        emissionType: values.emissionType || undefined,
        supplierCommercialName: values.supplierCommercialName || undefined,
        supplierMatrixAddress: values.supplierMatrixAddress || undefined,
        supplierBranchAddress: values.supplierBranchAddress || undefined,
        supplierSpecialTaxpayerNumber: values.supplierSpecialTaxpayerNumber || undefined,
        supplierObligatedAccounting: values.supplierObligatedAccounting,
        paymentMethodSriCode: values.paymentMethodSriCode || undefined,
        paymentMethodName: values.paymentMethodName || undefined,
        paymentAmount: values.paymentAmount ?? undefined,
        ice: values.ice ?? undefined,
        irbpnr: values.irbpnr ?? undefined,
        tip: values.tip ?? undefined,
        xmlFileUrl: values.xmlFileUrl || undefined,
        pdfFileUrl: values.pdfFileUrl || undefined,
        supplierId: values.supplierId || undefined,
        notes: values.notes || undefined,
        destinationWarehouseId: values.destinationWarehouseId || undefined,
        items: items.map(i => ({
          articleId: i.articleId,
          unitId: i.unitId,
          supplierMainCode: i.supplierMainCode || undefined,
          supplierAuxCode: i.supplierAuxCode || undefined,
          supplierDescription: i.supplierDescription || undefined,
          additionalDetail: i.additionalDetail || undefined,
          quantity: i.quantity,
          inventoryQuantity: i.inventoryQuantity,
          inventoryUnitId: i.inventoryUnitId,
          unitPrice: i.unitPrice,
          discountPct: i.discountPct || 0,
          discountAmount: i.discountAmount ?? undefined,
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

  // Vista de solo lectura

  if (readOnly && compra) {
    const f = compra;
    return (
      <Modal
        open={open}
        onCancel={onClose}
        footer={<Button onClick={onClose}>Cerrar</Button>}
        title={`Compra — ${DOC_TYPE_LABEL[f.documentType] ?? f.documentType}${f.documentNumber ? ` N° ${f.documentNumber}` : ''}`}
        width={980}
        style={{ top: 24 }}
      >
        <Descriptions bordered size="small" column={2} style={{ marginBottom: 16 }}>
          <Descriptions.Item label="Estado">
            <Tag color={STATUS_COLOR[f.status]}>{f.status}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Tipo de documento">
            {DOC_TYPE_LABEL[f.documentType] ?? f.documentType}
          </Descriptions.Item>
          <Descriptions.Item label="Fecha del comprobante">
            {formatBranchDate(f.documentDate)}
          </Descriptions.Item>
          <Descriptions.Item label="Proveedor">{f.supplierName ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Clave de acceso" span={2}>{f.accessKey ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Autorizacion">{f.authorizationNumber ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Fecha autorizacion">
            {f.authorizationDate ? formatBranchDateTime(f.authorizationDate) : '—'}
          </Descriptions.Item>
          <Descriptions.Item label="Ambiente">{f.environment ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Emision">{f.emissionType ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Contribuyente especial">{f.supplierSpecialTaxpayerNumber ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Obligado contabilidad">
            {f.supplierObligatedAccounting == null ? '—' : f.supplierObligatedAccounting ? 'Si' : 'No'}
          </Descriptions.Item>
          <Descriptions.Item label="Forma de pago" span={2}>
            {[f.paymentMethodSriCode, f.paymentMethodName].filter(Boolean).join(' - ') || '—'}
            {f.paymentAmount != null ? ` / $${f.paymentAmount.toFixed(2)}` : ''}
          </Descriptions.Item>
          <Descriptions.Item label="XML">
            {f.xmlFileUrl ? <a href={resolveMediaUrl(f.xmlFileUrl)} target="_blank" rel="noreferrer">Ver XML</a> : '—'}
          </Descriptions.Item>
          <Descriptions.Item label="PDF">
            {f.pdfFileUrl ? <a href={resolveMediaUrl(f.pdfFileUrl)} target="_blank" rel="noreferrer">Ver PDF</a> : '—'}
          </Descriptions.Item>
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
            { title: 'Cod. prov.', dataIndex: 'supplierMainCode', render: (v?: string) => v ?? '—', width: 90 },
            { title: 'Cod. aux.', dataIndex: 'supplierAuxCode', render: (v?: string) => v ?? '—', width: 90 },
            { title: 'Desc. factura', dataIndex: 'supplierDescription', render: (v?: string) => v ?? '—', width: 140 },
            { title: 'Cód.', dataIndex: 'internalCode', render: (v?: string) => v ?? '—', width: 80 },
            { title: 'Unidad fact.', dataIndex: 'unitSymbol', width: 90 },
            { title: 'Cant. fact.', dataIndex: 'quantity', align: 'right', width: 90 },
            {
              title: 'Ingreso inv.', key: 'inventoryEntry', width: 110,
              render: (_: unknown, row: PurchaseDto['items'][number]) =>
                `${row.inventoryQuantity ?? row.quantity} ${row.inventoryUnitSymbol ?? row.unitSymbol}`,
            },
            { title: 'P. Unit.', dataIndex: 'unitPrice', align: 'right', width: 90, render: (v: number) => `$${v.toFixed(4)}` },
            { title: 'Desc. $', dataIndex: 'discountAmount', align: 'right', width: 80, render: (v: number) => v ? `$${v.toFixed(2)}` : '—' },
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
                ['Base no objeto IVA', f.taxableBaseNotSubject ?? 0],
                ['Base exenta IVA', f.taxableBaseExempt],
                ['IVA 15%', f.iva15],
                ['ICE', f.ice],
                ['IRBPNR', f.irbpnr ?? 0],
                ['Propina', f.tip ?? 0],
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

  // Formulario de creacion / edicion

  const totalsTable = (
    <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
      <table style={{ fontSize: 13, borderCollapse: 'collapse', minWidth: 320 }}>
        <tbody>
          {[
            ['Subtotal', fiscal.subtotal],
            ['(-) Descuentos', fiscal.discountTotal],
            ['Base imponible 15%', fiscal.taxableBase15],
            ['Base imponible 0%', fiscal.taxableBase0],
            ['Base no objeto IVA', fiscal.taxableBaseNotSubject],
            ['Base exenta IVA', fiscal.taxableBaseExempt],
            ['IVA 15%', fiscal.iva15],
            ['ICE', fiscal.ice],
            ['IRBPNR', fiscal.irbpnr],
            ['Propina', fiscal.tip],
          ].map(([label, val]) => (
            <tr key={label as string}>
              <td style={{ padding: '3px 18px 3px 0', color: '#666' }}>{label as string}</td>
              <td style={{ padding: '3px 0', textAlign: 'right' }}>${(val as number).toFixed(2)}</td>
            </tr>
          ))}
          <tr style={{ borderTop: '2px solid #000', fontWeight: 600 }}>
            <td style={{ padding: '6px 18px 0 0' }}>Valor total</td>
            <td style={{ padding: '6px 0 0', textAlign: 'right' }}>${fiscal.total.toFixed(2)}</td>
          </tr>
        </tbody>
      </table>
    </div>
  );

  const itemsTable = (
    <Table
      size="small"
      rowKey="key"
      dataSource={items}
      pagination={false}
      scroll={{ x: 1180, y: 'max(260px, calc(100dvh - 400px))' }}
      expandable={{
        expandedRowRender: (row: ItemRow) => (
          <div className="purchase-item-extra">
            <label>Cod. proveedor
              <Input value={row.supplierMainCode} onChange={e => updateItem(row.key, 'supplierMainCode', e.target.value)} />
            </label>
            <label>Cod. auxiliar
              <Input value={row.supplierAuxCode} onChange={e => updateItem(row.key, 'supplierAuxCode', e.target.value)} />
            </label>
            <label>Detalle adicional
              <Input value={row.additionalDetail} onChange={e => updateItem(row.key, 'additionalDetail', e.target.value)} />
            </label>
            <label>Descuento $
              <InputNumber min={0} step={0.01} precision={2} value={row.discountAmount} onChange={v => updateItem(row.key, 'discountAmount', v ?? undefined)} style={{ width: '100%' }} />
            </label>
            <label>Descuento %
              <InputNumber min={0} max={100} step={0.01} precision={2} value={row.discountPct || 0} disabled={typeof row.discountAmount === 'number'} onChange={v => updateItem(row.key, 'discountPct', v ?? 0)} style={{ width: '100%' }} />
            </label>
            <label>Observaciones
              <Input value={row.notes} onChange={e => updateItem(row.key, 'notes', e.target.value)} />
            </label>
          </div>
        ),
      }}
      columns={[
        {
          title: 'Articulo', key: 'articleId', width: 200,
          fixed: 'left' as const,
          render: (_: unknown, row: ItemRow) => (
            <Select
              style={{ width: '100%' }}
              value={row.articleId || undefined}
              onChange={v => updateItem(row.key, 'articleId', v)}
              showSearch
              options={articulos.map(a => ({ value: a.id, label: `${a.name}${a.internalCode ? ` (${a.internalCode})` : ''}` }))}
              filterOption={(input, opt) => (opt?.label as string ?? '').toLowerCase().includes(input.toLowerCase())}
              placeholder="Articulo"
            />
          ),
        },
        {
          title: 'Desc. factura', key: 'supplierDescription', width: 220,
          render: (_: unknown, row: ItemRow) => (
            <Input value={row.supplierDescription} onChange={e => updateItem(row.key, 'supplierDescription', e.target.value)} />
          ),
        },
        {
          title: 'Unidad fact.', key: 'unitId', width: 95,
          render: (_: unknown, row: ItemRow) => {
            return unitOptions.length > 0 ? (
              <Select
                style={{ width: '100%' }}
                value={row.unitId || undefined}
                onChange={v => updateItem(row.key, 'unitId', v)}
                options={unitOptions}
              />
            ) : <Text type="secondary">-</Text>;
          },
        },
        {
          title: 'Cant. fact.', key: 'quantity', width: 95,
          render: (_: unknown, row: ItemRow) => (
            <InputNumber min={0.001} step={1} value={row.quantity} onChange={v => updateItem(row.key, 'quantity', v ?? 1)} style={{ width: '100%' }} />
          ),
        },
        {
          title: 'Cant. inv.', key: 'inventoryQuantity', width: 95,
          render: (_: unknown, row: ItemRow) => (
            <InputNumber min={0.001} step={0.001} precision={4} value={row.inventoryQuantity} onChange={v => updateItem(row.key, 'inventoryQuantity', v ?? 1)} style={{ width: '100%' }} />
          ),
        },
        {
          title: 'Unidad inv.', key: 'inventoryUnitId', width: 95,
          render: (_: unknown, row: ItemRow) => {
            const unitOptions = getArticleUnitOptions(row.articleId);
            return unitOptions.length > 0 ? (
              <Select
                style={{ width: '100%' }}
                value={row.inventoryUnitId || undefined}
                onChange={v => updateItem(row.key, 'inventoryUnitId', v)}
                options={unitOptions}
              />
            ) : <Text type="secondary">-</Text>;
          },
        },
        {
          title: 'P. Unit.', key: 'unitPrice', width: 95,
          render: (_: unknown, row: ItemRow) => (
            <InputNumber min={0} step={0.01} precision={4} value={row.unitPrice} onChange={v => updateItem(row.key, 'unitPrice', v ?? 0)} prefix="$" style={{ width: '100%' }} />
          ),
        },
        {
          title: 'IVA', key: 'taxRateId', width: 100,
          render: (_: unknown, row: ItemRow) => (
            <Select allowClear style={{ width: '100%' }} value={row.taxRateId} onChange={v => updateItem(row.key, 'taxRateId', v)} options={taxRates.map(t => ({ value: t.id, label: t.name }))} placeholder="Sin IVA" />
          ),
        },
        {
          title: 'Total', key: 'total', width: 90, align: 'right',
          render: (_: unknown, row: ItemRow) => {
            const gross = (row.quantity || 0) * (row.unitPrice || 0);
            const exactDiscount = typeof row.discountAmount === 'number' ? row.discountAmount : undefined;
            const disc = Math.min(gross, Math.max(0, exactDiscount ?? Math.round(gross * ((row.discountPct || 0) / 100) * 100) / 100));
            const base = gross - disc;
            const rate = row.taxRateId ? taxMap.get(row.taxRateId) : undefined;
            const tax = rate ? Math.round(base * (rate.percentage / 100) * 100) / 100 : 0;
            return `$${(base + tax).toFixed(2)}`;
          },
        },
        {
          title: '', key: 'del', width: 48,
          fixed: 'right' as const,
          render: (_: unknown, row: ItemRow) => (
            <Button size="small" danger icon={<DeleteOutlined />} onClick={() => removeItem(row.key)} />
          ),
        },
      ]}
      footer={() => (
        <Button icon={<PlusOutlined />} onClick={addItem}>Agregar item</Button>
      )}
    />
  );

  const tabItems = [
    {
      key: 'document',
      label: 'Documento',
      children: (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, marginBottom: 12, flexWrap: 'wrap' }}>
            <Text type="secondary">{items.length} item(s) / Total ${fiscal.total.toFixed(2)}</Text>
            <Space wrap>
              <Upload accept=".xml" showUploadList={false} beforeUpload={file => { void handleImportXml(file); return false; }}>
                <Button icon={<UploadOutlined />} loading={importingXml}>Importar XML</Button>
              </Upload>
              <Upload accept=".pdf" showUploadList={false} beforeUpload={file => { void handleUploadAttachment(file, 'pdfFileUrl'); return false; }}>
                <Button icon={<PaperClipOutlined />} loading={uploadingAttachment}>Adjuntar PDF</Button>
              </Upload>
            </Space>
          </div>

          {importedSupplierCandidate && (
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 12 }}
              message="Proveedor no registrado"
              description={`${importedSupplierCandidate.name ?? 'Sin razon social'}${importedSupplierCandidate.taxId ? ` - RUC ${importedSupplierCandidate.taxId}` : ''}`}
              action={
                <Button size="small" type="primary" loading={creatingSupplier} onClick={handleCreateImportedSupplier}>
                  Crear proveedor
                </Button>
              }
            />
          )}

          <Divider plain>Datos principales</Divider>
          <div className="purchase-document-grid">
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
              <Select allowClear showSearch options={availableSuppliers.filter(p => p.isActive).map(p => ({ value: p.id, label: p.taxId ? `${p.name} (${p.taxId})` : p.name }))} filterOption={(input, opt) => (opt?.label as string ?? '').toLowerCase().includes(input.toLowerCase())} placeholder="Seleccionar proveedor" />
            </Form.Item>
            <Form.Item name="destinationWarehouseId" label="Bodega destino" rules={[{ required: true, message: 'Selecciona la bodega para actualizar el inventario' }]} tooltip="Indica donde se acreditara el stock de los articulos comprados">
              <Select options={bodegas.map(b => ({ value: b.id, label: b.name }))} placeholder="Seleccionar bodega" />
            </Form.Item>
            <Form.Item name="notes" label="Observaciones">
              <Input.TextArea rows={1} />
            </Form.Item>
          </div>

          <Divider plain>Datos SRI</Divider>
          <div className="purchase-document-grid">
            <Form.Item name="accessKey" label="Clave de acceso">
              <Input maxLength={49} placeholder="49 digitos" />
            </Form.Item>
            <Form.Item name="authorizationNumber" label="Autorizacion">
              <Input maxLength={60} />
            </Form.Item>
            <Form.Item name="authorizationDate" label="Fecha autorizacion">
              <DatePicker showTime style={{ width: '100%' }} format="DD/MM/YYYY HH:mm" />
            </Form.Item>
            <Form.Item name="environment" label="Ambiente">
              <Input maxLength={30} placeholder="PRODUCCION" />
            </Form.Item>
            <Form.Item name="emissionType" label="Emision">
              <Input maxLength={30} placeholder="NORMAL" />
            </Form.Item>
            <Form.Item name="supplierSpecialTaxpayerNumber" label="Contribuyente especial">
              <Input maxLength={30} />
            </Form.Item>
            <Form.Item name="supplierCommercialName" label="Nombre comercial">
              <Input maxLength={200} />
            </Form.Item>
            <Form.Item name="supplierObligatedAccounting" label="Obligado contabilidad" valuePropName="checked">
              <Switch checkedChildren="Si" unCheckedChildren="No" />
            </Form.Item>
            <Form.Item name="paymentMethodSriCode" label="Cod. forma pago">
              <Input maxLength={10} placeholder="19" />
            </Form.Item>
            <Form.Item name="paymentMethodName" label="Forma de pago" style={{ gridColumn: 'span 2' }}>
              <Input maxLength={120} placeholder="TARJETA DE CREDITO" />
            </Form.Item>
            <Form.Item name="paymentAmount" label="Valor pagado">
              <InputNumber min={0} step={0.01} precision={2} prefix="$" style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="supplierMatrixAddress" label="Direccion matriz" style={{ gridColumn: 'span 2' }}>
              <Input maxLength={400} />
            </Form.Item>
            <Form.Item name="supplierBranchAddress" label="Direccion sucursal">
              <Input maxLength={400} />
            </Form.Item>
          </div>
        </div>
      ),
    },
    { key: 'items', label: `Items (${items.length})`, children: itemsTable },
    {
      key: 'summary',
      label: 'Resumen',
      children: (
        <div className="purchase-summary-grid">
          <div>
            <Divider plain>Impuestos adicionales</Divider>
            <Form.Item name="ice" label="ICE">
              <InputNumber min={0} step={0.01} precision={2} prefix="$" style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="irbpnr" label="IRBPNR">
              <InputNumber min={0} step={0.01} precision={2} prefix="$" style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="tip" label="Propina">
              <InputNumber min={0} step={0.01} precision={2} prefix="$" style={{ width: '100%' }} />
            </Form.Item>
          </div>
          <div>
            <Divider plain>Totales</Divider>
            {totalsTable}
          </div>
        </div>
      ),
    },
  ];

  return (
    <div className="purchase-workspace">
      <Form form={form} layout="vertical" className="purchase-workspace-form">
        <header className="purchase-workspace-header">
          <Space>
            <Button icon={<ArrowLeftOutlined />} onClick={onClose} disabled={saving}>Volver</Button>
            <h2>{compra ? 'Editar compra' : 'Nueva compra'}</h2>
          </Space>
          <Text type="secondary">{items.length} items</Text>
        </header>
        <Form.Item name="xmlFileUrl" hidden><Input /></Form.Item>
        <Form.Item name="pdfFileUrl" hidden><Input /></Form.Item>
        <Tabs defaultActiveKey="items" items={tabItems} className="purchase-workspace-tabs" />
        <footer className="purchase-workspace-footer">
          <div>
            <Text type="secondary">Total de compra</Text>
            <Text strong className="purchase-workspace-total">${fiscal.total.toFixed(2)}</Text>
          </div>
          <Space>
            <Button onClick={onClose} disabled={saving}>Cancelar</Button>
            <Button type="primary" loading={saving} onClick={handleSave}>Guardar</Button>
          </Space>
        </footer>
      </Form>
    </div>
  );
}
