import React, { useState, useEffect, useRef, useCallback } from 'react';
import { App as AntApp, Card, Tabs, Button, Switch, Input, ColorPicker, Upload, Spin, Tooltip, Typography, Divider, Space, Tag, Alert } from 'antd';
import { DragOutlined, EyeOutlined, EyeInvisibleOutlined,
  SaveOutlined, FileImageOutlined, MailOutlined,
  FilePdfOutlined, ReloadOutlined, DeleteOutlined } from '@ant-design/icons';
import {
  DndContext, closestCenter, PointerSensor, useSensor, useSensors, type DragEndEvent,
} from '@dnd-kit/core';
import {
  SortableContext, verticalListSortingStrategy,
  useSortable, arrayMove,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import type { InvoiceTemplateDto, PdfBlock, EmailBlock } from '../../types';
import { sriApi } from '../../services/api';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Text } = Typography;
const { TextArea } = Input;

// ── Variables disponibles ─────────────────────────────────────────────────────

const EMAIL_VARS = [
  { key: '{nombreCliente}', label: 'Nombre del cliente' },
  { key: '{numeroFactura}', label: 'Número de factura' },
  { key: '{razonSocial}', label: 'Razón social' },
  { key: '{importeTotal}', label: 'Importe total' },
];

// ── Bloque sortable ───────────────────────────────────────────────────────────

interface SortableBlockProps {
  id: string;
  label: string;
  visible: boolean;
  expanded: boolean;
  onToggleVisible: () => void;
  onToggleExpanded: () => void;
  children?: React.ReactNode;
}

function SortableBlock({ id, label, visible, expanded, onToggleVisible, onToggleExpanded, children }: SortableBlockProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id });

  const style: React.CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
    marginBottom: 8,
  };

  return (
    <div ref={setNodeRef} style={style}>
      <Card
        size="small"
        style={{ border: `1px solid ${visible ? '#d9d9d9' : '#f0f0f0'}`, background: visible ? '#fff' : '#fafafa' }}
        styles={{ body: { padding: '8px 12px' } }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span
            {...attributes}
            {...listeners}
            style={{ cursor: 'grab', color: '#bbb', fontSize: 16, lineHeight: 1, touchAction: 'none' }}
          >
            <DragOutlined />
          </span>
          <Text
            style={{ flex: 1, cursor: 'pointer', color: visible ? '#000' : '#bbb', fontWeight: 500 }}
            onClick={onToggleExpanded}
          >
            {label}
          </Text>
          <Tooltip title={visible ? 'Ocultar bloque' : 'Mostrar bloque'}>
            <Switch
              size="small"
              checked={visible}
              onChange={onToggleVisible}
              checkedChildren={<EyeOutlined />}
              unCheckedChildren={<EyeInvisibleOutlined />}
            />
          </Tooltip>
        </div>
        {expanded && visible && children && (
          <div style={{ marginTop: 12, paddingTop: 12, borderTop: '1px solid #f0f0f0' }}>
            {children}
          </div>
        )}
      </Card>
    </div>
  );
}

// ── Renderizado del preview de correo (client-side) ───────────────────────────

function renderEmailPreview(template: InvoiceTemplateDto): string {
  const blocks = template.emailBlocks;
  const razonSocial = 'Mi Empresa S.A.';
  const toName = 'Cliente Ejemplo';
  const numeroFactura = '001-001-000000001';
  const importeTotal = 99.14;

  let html = `<!DOCTYPE html><html lang="es"><head><meta charset="UTF-8"></head>
<body style="margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif;">
<table width="100%" cellpadding="0" cellspacing="0" style="background:#f5f5f5;padding:32px 0;">
<tr><td align="center">
<table width="600" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08);">`;

  for (const block of blocks.filter(b => b.visible)) {
    const esc = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    const subst = (s: string) => s
      .replace('{nombreCliente}', toName)
      .replace('{numeroFactura}', numeroFactura)
      .replace('{razonSocial}', razonSocial)
      .replace('{importeTotal}', String(importeTotal));

    switch (block.type) {
      case 'header': {
        const bg = block.bgColor || '#1677ff';
        const title = block.title || razonSocial;
        const sub = block.subtitle || 'Factura Electrónica';
        html += `<tr><td style="background:${bg};padding:24px 32px;"><h1 style="margin:0;color:#fff;font-size:22px;">${esc(title)}</h1><p style="margin:4px 0 0;color:rgba(255,255,255,.75);font-size:13px;">${esc(sub)}</p></td></tr>`;
        break;
      }
      case 'greeting':
        html += `<tr><td style="padding:24px 32px 0;"><p style="margin:0;color:#333;font-size:15px;">${esc(subst(block.text || 'Estimado/a {nombreCliente},'))}</p></td></tr>`;
        break;
      case 'message':
        html += `<tr><td style="padding:12px 32px;"><p style="margin:0;color:#555;font-size:14px;line-height:1.6;">${esc(subst(block.text || ''))}</p></td></tr>`;
        break;
      case 'invoice_summary':
        html += `<tr><td style="padding:12px 32px;"><table width="100%" cellpadding="12" cellspacing="0" style="background:#f9f9f9;border-radius:6px;"><tr><td style="color:#888;font-size:13px;">Número de factura</td><td style="color:#222;font-size:14px;font-weight:bold;text-align:right;">${esc(numeroFactura)}</td></tr><tr style="border-top:1px solid #eee;"><td style="color:#888;font-size:13px;">Importe total</td><td style="font-size:16px;font-weight:bold;text-align:right;color:#1677ff;">$${importeTotal.toFixed(2)}</td></tr></table></td></tr>`;
        break;
      case 'legal_note':
        html += `<tr><td style="padding:8px 32px;"><p style="margin:0;color:#888;font-size:12px;line-height:1.6;">${esc(subst(block.text || ''))}</p></td></tr>`;
        break;
      case 'footer':
        html += `<tr><td style="background:#f9f9f9;padding:16px 32px;text-align:center;"><p style="margin:0;color:#bbb;font-size:11px;">${esc(block.text || '')}</p></td></tr>`;
        break;
    }
  }

  html += '</table></td></tr></table></body></html>';
  return html;
}

// ── Componente principal ──────────────────────────────────────────────────────

const DEFAULT_TEMPLATE: InvoiceTemplateDto = {
  primaryColor: '#1677ff',
  accentColor: '#e6f4ff',
  pdfBlocks: [
    { id: 'header', type: 'header', visible: true, label: 'Encabezado', primaryColor: '#1677ff', showLogo: true, showEmail: false, showPhone: false, showAddress: false, showAuxCode: false, showDiscount: true, showZeroLines: true },
    { id: 'customer', type: 'customer', visible: true, label: 'Datos del comprador', primaryColor: '#1677ff', showLogo: false, showEmail: true, showPhone: false, showAddress: true, showAuxCode: false, showDiscount: false, showZeroLines: false },
    { id: 'items', type: 'items', visible: true, label: 'Detalle de productos', primaryColor: '#1677ff', showLogo: false, showEmail: false, showPhone: false, showAddress: false, showAuxCode: false, showDiscount: true, showZeroLines: false },
    { id: 'payments', type: 'payments', visible: true, label: 'Forma de pago', primaryColor: '#1677ff', showLogo: false, showEmail: false, showPhone: false, showAddress: false, showAuxCode: false, showDiscount: false, showZeroLines: false },
    { id: 'totals', type: 'totals', visible: true, label: 'Totales', primaryColor: '#1677ff', showLogo: false, showEmail: false, showPhone: false, showAddress: false, showAuxCode: false, showDiscount: false, showZeroLines: true },
    { id: 'footer', type: 'footer', visible: true, label: 'Pie de página', primaryColor: '#1677ff', showLogo: false, showEmail: false, showPhone: false, showAddress: false, showAuxCode: false, showDiscount: false, showZeroLines: false, customText: '¡Gracias por su compra!' },
  ],
  emailSubject: 'Factura Electrónica {numeroFactura} — {razonSocial}',
  emailBlocks: [
    { id: 'header', type: 'header', visible: true, label: 'Encabezado', bgColor: '#1677ff', title: 'Factura Electrónica', subtitle: 'Documento autorizado por el SRI Ecuador' },
    { id: 'greeting', type: 'greeting', visible: true, label: 'Saludo', bgColor: '#1677ff', text: 'Estimado/a {nombreCliente},' },
    { id: 'message', type: 'message', visible: true, label: 'Mensaje principal', bgColor: '#1677ff', text: 'Adjunto encontrará el RIDE (Representación Impresa del Documento Electrónico) de su factura autorizada por el SRI Ecuador.' },
    { id: 'invoice_summary', type: 'invoice_summary', visible: true, label: 'Resumen de factura', bgColor: '#1677ff' },
    { id: 'legal_note', type: 'legal_note', visible: true, label: 'Nota legal', bgColor: '#1677ff', text: 'Este documento tiene validez legal ante el Servicio de Rentas Internas (SRI) del Ecuador.' },
    { id: 'footer', type: 'footer', visible: true, label: 'Pie de correo', bgColor: '#1677ff', text: 'Generado por Grimorio' },
  ],
};

export default function InvoiceTemplateEditor() {
  const { message } = AntApp.useApp();

  const { hasPermission } = useAuth();
  const [template, setTemplate] = useState<InvoiceTemplateDto>(DEFAULT_TEMPLATE);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [generatingPdf, setGeneratingPdf] = useState(false);
  const [pdfUrl, setPdfUrl] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'pdf' | 'email'>('pdf');
  const [expandedBlock, setExpandedBlock] = useState<string | null>(null);
  const prevPdfUrl = useRef<string | null>(null);
  const canManageSri = hasPermission(PERMISSIONS.billing.sriManage);

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  useEffect(() => {
    sriApi.getInvoiceTemplate()
      .then(r => {
        const data = r.data;
        if (data.pdfBlocks?.length) setTemplate(data);
        else setTemplate(DEFAULT_TEMPLATE);
      })
      .catch(() => setTemplate(DEFAULT_TEMPLATE))
      .finally(() => setLoading(false));
  }, []);

  // Limpiar blob URLs previas al actualizar
  useEffect(() => {
    return () => { if (prevPdfUrl.current) URL.revokeObjectURL(prevPdfUrl.current); };
  }, []);

  const handleSave = async () => {
    setSaving(true);
    try {
      const r = await sriApi.upsertInvoiceTemplate(template);
      setTemplate(r.data);
      message.success('Plantilla guardada correctamente');
    } catch {
      message.error('Error al guardar la plantilla');
    } finally {
      setSaving(false);
    }
  };

  const handlePreviewPdf = useCallback(async () => {
    setGeneratingPdf(true);
    try {
      const r = await sriApi.previewInvoicePdf(template);
      if (prevPdfUrl.current) URL.revokeObjectURL(prevPdfUrl.current);
      const url = URL.createObjectURL(r.data);
      prevPdfUrl.current = url;
      setPdfUrl(url);
    } catch {
      message.error('Error al generar el preview');
    } finally {
      setGeneratingPdf(false);
    }
  }, [template]);

  // ── Handlers de bloques PDF ───────────────────────────────────────────────

  const updatePdfBlock = (id: string, patch: Partial<PdfBlock>) => {
    setTemplate(t => ({
      ...t,
      pdfBlocks: t.pdfBlocks.map(b => b.id === id ? { ...b, ...patch } : b),
    }));
  };

  const handlePdfDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    setTemplate(t => {
      const oldIndex = t.pdfBlocks.findIndex(b => b.id === active.id);
      const newIndex = t.pdfBlocks.findIndex(b => b.id === over.id);
      return { ...t, pdfBlocks: arrayMove(t.pdfBlocks, oldIndex, newIndex) };
    });
  };

  // ── Handlers de bloques Email ─────────────────────────────────────────────

  const updateEmailBlock = (id: string, patch: Partial<EmailBlock>) => {
    setTemplate(t => ({
      ...t,
      emailBlocks: t.emailBlocks.map(b => b.id === id ? { ...b, ...patch } : b),
    }));
  };

  const handleEmailDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    setTemplate(t => {
      const oldIndex = t.emailBlocks.findIndex(b => b.id === active.id);
      const newIndex = t.emailBlocks.findIndex(b => b.id === over.id);
      return { ...t, emailBlocks: arrayMove(t.emailBlocks, oldIndex, newIndex) };
    });
  };

  // ── Settings de bloques PDF ───────────────────────────────────────────────

  const renderPdfBlockSettings = (block: PdfBlock) => {
    switch (block.type) {
      case 'header':
        return (
          <Space orientation="vertical" style={{ width: '100%' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Text type="secondary" style={{ minWidth: 90, fontSize: 12 }}>Color principal</Text>
              <ColorPicker
                value={block.primaryColor}
                onChange={c => updatePdfBlock(block.id, { primaryColor: c.toHexString() })}
                size="small"
              />
              <Text style={{ fontSize: 12 }}>{block.primaryColor}</Text>
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Text type="secondary" style={{ minWidth: 90, fontSize: 12 }}>Mostrar logo</Text>
              <Switch size="small" checked={block.showLogo} onChange={v => updatePdfBlock(block.id, { showLogo: v })} />
            </div>
          </Space>
        );
      case 'customer':
        return (
          <Space orientation="vertical">
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Switch size="small" checked={block.showEmail} onChange={v => updatePdfBlock(block.id, { showEmail: v })} />
              <Text style={{ fontSize: 12 }}>Mostrar email</Text>
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Switch size="small" checked={block.showAddress} onChange={v => updatePdfBlock(block.id, { showAddress: v })} />
              <Text style={{ fontSize: 12 }}>Mostrar dirección</Text>
            </div>
          </Space>
        );
      case 'items':
        return (
          <Space orientation="vertical">
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Switch size="small" checked={block.showAuxCode} onChange={v => updatePdfBlock(block.id, { showAuxCode: v })} />
              <Text style={{ fontSize: 12 }}>Mostrar código auxiliar</Text>
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Switch size="small" checked={block.showDiscount} onChange={v => updatePdfBlock(block.id, { showDiscount: v })} />
              <Text style={{ fontSize: 12 }}>Mostrar descuento</Text>
            </div>
          </Space>
        );
      case 'totals':
        return (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <Switch size="small" checked={block.showZeroLines} onChange={v => updatePdfBlock(block.id, { showZeroLines: v })} />
            <Text style={{ fontSize: 12 }}>Mostrar líneas en cero (IVA 12%, 5%, etc.)</Text>
          </div>
        );
      case 'footer':
        return (
          <Input
            placeholder="Texto del pie de página"
            value={block.customText ?? ''}
            onChange={e => updatePdfBlock(block.id, { customText: e.target.value })}
            size="small"
          />
        );
      default:
        return null;
    }
  };

  // ── Settings de bloques Email ─────────────────────────────────────────────

  const renderEmailBlockSettings = (block: EmailBlock) => {
    const insertVar = (varKey: string, field: 'text' | 'title' | 'subtitle') => {
      const current = (block as unknown as Record<string, unknown>)[field] as string | undefined ?? '';
      updateEmailBlock(block.id, { [field]: current + varKey } as Partial<EmailBlock>);
    };

    switch (block.type) {
      case 'header':
        return (
          <Space orientation="vertical" style={{ width: '100%' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Text type="secondary" style={{ minWidth: 70, fontSize: 12 }}>Color fondo</Text>
              <ColorPicker
                value={block.bgColor}
                onChange={c => updateEmailBlock(block.id, { bgColor: c.toHexString() })}
                size="small"
              />
            </div>
            <Input
              placeholder="Título del encabezado"
              value={block.title ?? ''}
              onChange={e => updateEmailBlock(block.id, { title: e.target.value })}
              size="small"
              addonBefore={<Text style={{ fontSize: 11 }}>Título</Text>}
            />
            <Input
              placeholder="Subtítulo"
              value={block.subtitle ?? ''}
              onChange={e => updateEmailBlock(block.id, { subtitle: e.target.value })}
              size="small"
              addonBefore={<Text style={{ fontSize: 11 }}>Subtítulo</Text>}
            />
          </Space>
        );
      case 'greeting':
      case 'message':
      case 'legal_note':
      case 'footer':
        return (
          <Space orientation="vertical" style={{ width: '100%' }}>
            <TextArea
              placeholder="Texto del bloque"
              value={block.text ?? ''}
              onChange={e => updateEmailBlock(block.id, { text: e.target.value })}
              rows={block.type === 'message' ? 3 : 2}
              style={{ fontSize: 12 }}
            />
            {block.type !== 'footer' && (
              <div>
                <Text type="secondary" style={{ fontSize: 11 }}>Variables: </Text>
                {EMAIL_VARS.map(v => (
                  <Tag
                    key={v.key}
                    style={{ cursor: 'pointer', fontSize: 10, marginBottom: 2 }}
                    onClick={() => insertVar(v.key, 'text')}
                  >
                    {v.key}
                  </Tag>
                ))}
              </div>
            )}
          </Space>
        );
      default:
        return <Text type="secondary" style={{ fontSize: 12 }}>Este bloque no tiene configuración adicional.</Text>;
    }
  };

  // ── Logo upload ───────────────────────────────────────────────────────────

  const handleLogoUpload = (file: File) => {
    const reader = new FileReader();
    reader.onload = e => {
      setTemplate(t => ({ ...t, logoBase64: e.target?.result as string }));
    };
    reader.readAsDataURL(file);
    return false; // evita upload automático de antd
  };

  // ── Email preview HTML ────────────────────────────────────────────────────

  const emailPreviewHtml = renderEmailPreview(template);

  if (loading) return <div style={{ display: 'flex', justifyContent: 'center', padding: 60 }}><Spin size="large" /></div>;

  return (
    <div style={{ display: 'flex', gap: 16, height: 'calc(100vh - 120px)', minHeight: 600 }}>

      {/* ── Panel izquierdo: configuración ── */}
      <div style={{ width: 360, display: 'flex', flexDirection: 'column', gap: 12, overflowY: 'auto', flexShrink: 0 }}>

        {/* Acciones */}
        <Card size="small">
          <Space>
            {canManageSri && <Button type="primary" icon={<SaveOutlined />} loading={saving} onClick={handleSave}>
              Guardar plantilla
            </Button>}
            {activeTab === 'pdf' && (
              <Button icon={<ReloadOutlined />} loading={generatingPdf} onClick={handlePreviewPdf}>
                Actualizar preview PDF
              </Button>
            )}
          </Space>
        </Card>

        {/* Logo y colores globales */}
        <Card size="small" title="Configuración global">
          <Space orientation="vertical" style={{ width: '100%' }}>
            <div>
              <Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 6 }}>Logo de la empresa</Text>
              {template.logoBase64 ? (
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <img
                    src={template.logoBase64}
                    alt="Logo"
                    style={{ height: 48, maxWidth: 160, objectFit: 'contain', border: '1px solid #f0f0f0', borderRadius: 4, padding: 4 }}
                  />
                  {canManageSri && <Button
                    size="small"
                    danger
                    icon={<DeleteOutlined />}
                    onClick={() => setTemplate(t => ({ ...t, logoBase64: undefined }))}
                  />}
                </div>
              ) : (
                canManageSri && (
                <Upload
                  accept="image/png,image/jpeg,image/svg+xml"
                  showUploadList={false}
                  beforeUpload={handleLogoUpload}
                  maxCount={1}
                >
                  <Button size="small" icon={<FileImageOutlined />}>Subir logo (PNG/JPG/SVG)</Button>
                </Upload>
                )
              )}
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Text type="secondary" style={{ minWidth: 80, fontSize: 12 }}>Color primario</Text>
              <ColorPicker
                value={template.primaryColor}
                onChange={c => {
                  const hex = c.toHexString();
                  setTemplate(t => ({
                    ...t,
                    primaryColor: hex,
                    pdfBlocks: t.pdfBlocks.map(b => b.type === 'header' ? { ...b, primaryColor: hex } : b),
                    emailBlocks: t.emailBlocks.map(b => b.type === 'header' ? { ...b, bgColor: hex } : b),
                  }));
                }}
                size="small"
              />
              <Text style={{ fontSize: 12 }}>{template.primaryColor}</Text>
            </div>
          </Space>
        </Card>

        {/* Tabs PDF / Email */}
        <Card size="small" styles={{ body: { padding: 0 } }}>
          <Tabs
            activeKey={activeTab}
            onChange={k => setActiveTab(k as 'pdf' | 'email')}
            size="small"
            style={{ padding: '0 12px' }}
            items={[
              {
                key: 'pdf',
                label: <span><FilePdfOutlined /> PDF de factura</span>,
                children: (
                  <div style={{ padding: '0 0 8px' }}>
                    <Text type="secondary" style={{ fontSize: 11, display: 'block', marginBottom: 8 }}>
                      Arrastra los bloques para reordenarlos
                    </Text>
                    <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handlePdfDragEnd}>
                      <SortableContext items={template.pdfBlocks.map(b => b.id)} strategy={verticalListSortingStrategy}>
                        {template.pdfBlocks.map(block => (
                          <SortableBlock
                            key={block.id}
                            id={block.id}
                            label={block.label}
                            visible={block.visible}
                            expanded={expandedBlock === `pdf-${block.id}`}
                            onToggleVisible={() => updatePdfBlock(block.id, { visible: !block.visible })}
                            onToggleExpanded={() => setExpandedBlock(prev => prev === `pdf-${block.id}` ? null : `pdf-${block.id}`)}
                          >
                            {renderPdfBlockSettings(block)}
                          </SortableBlock>
                        ))}
                      </SortableContext>
                    </DndContext>
                  </div>
                ),
              },
              {
                key: 'email',
                label: <span><MailOutlined /> Correo electrónico</span>,
                children: (
                  <div style={{ padding: '0 0 8px' }}>
                    <div style={{ marginBottom: 12 }}>
                      <Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 4 }}>Asunto del correo</Text>
                      <Input
                        value={template.emailSubject}
                        onChange={e => setTemplate(t => ({ ...t, emailSubject: e.target.value }))}
                        size="small"
                        placeholder="Asunto del correo"
                      />
                      <div style={{ marginTop: 4 }}>
                        {EMAIL_VARS.slice(0, 2).map(v => (
                          <Tag
                            key={v.key}
                            style={{ cursor: 'pointer', fontSize: 10 }}
                            onClick={() => setTemplate(t => ({ ...t, emailSubject: t.emailSubject + v.key }))}
                          >
                            {v.key}
                          </Tag>
                        ))}
                      </div>
                    </div>
                    <Divider style={{ margin: '8px 0' }} />
                    <Text type="secondary" style={{ fontSize: 11, display: 'block', marginBottom: 8 }}>
                      Bloques del correo (arrastra para reordenar)
                    </Text>
                    <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleEmailDragEnd}>
                      <SortableContext items={template.emailBlocks.map(b => b.id)} strategy={verticalListSortingStrategy}>
                        {template.emailBlocks.map(block => (
                          <SortableBlock
                            key={block.id}
                            id={block.id}
                            label={block.label}
                            visible={block.visible}
                            expanded={expandedBlock === `email-${block.id}`}
                            onToggleVisible={() => updateEmailBlock(block.id, { visible: !block.visible })}
                            onToggleExpanded={() => setExpandedBlock(prev => prev === `email-${block.id}` ? null : `email-${block.id}`)}
                          >
                            {renderEmailBlockSettings(block)}
                          </SortableBlock>
                        ))}
                      </SortableContext>
                    </DndContext>
                  </div>
                ),
              },
            ]}
          />
        </Card>
      </div>

      {/* ── Panel derecho: preview ── */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <Card
          size="small"
          title={activeTab === 'pdf' ? 'Preview del PDF' : 'Preview del correo'}
          style={{ flex: 1, display: 'flex', flexDirection: 'column' }}
          styles={{ body: { flex: 1, padding: 0, display: 'flex', flexDirection: 'column' } }}
          extra={
            activeTab === 'pdf' && !pdfUrl && (
              <Button size="small" type="primary" icon={<FilePdfOutlined />} loading={generatingPdf} onClick={handlePreviewPdf}>
                Generar preview
              </Button>
            )
          }
        >
          {activeTab === 'pdf' ? (
            pdfUrl ? (
              <iframe
                src={pdfUrl}
                style={{ flex: 1, width: '100%', border: 'none', minHeight: 500 }}
                title="Preview factura PDF"
              />
            ) : (
              <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', flexDirection: 'column', gap: 16, color: '#bbb' }}>
                <FilePdfOutlined style={{ fontSize: 48 }} />
                <Text type="secondary">Haz clic en "Generar preview" para ver cómo quedará el PDF de la factura</Text>
                <Button type="primary" loading={generatingPdf} onClick={handlePreviewPdf} icon={<FilePdfOutlined />}>
                  Generar preview
                </Button>
              </div>
            )
          ) : (
            <iframe
              srcDoc={emailPreviewHtml}
              style={{ flex: 1, width: '100%', border: 'none', minHeight: 500 }}
              title="Preview correo"
              sandbox="allow-same-origin"
            />
          )}
        </Card>
        {activeTab === 'email' && (
          <Alert
            message="El preview del correo se actualiza en tiempo real. Los datos del cliente son de muestra."
            type="info"
            showIcon
            style={{ marginTop: 8 }}
          />
        )}
      </div>
    </div>
  );
}
