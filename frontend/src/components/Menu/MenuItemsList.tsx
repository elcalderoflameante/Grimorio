import { useEffect, useMemo, useRef, useState } from 'react';
import { App as AntApp, Table, Button, Modal, Form, Input, InputNumber, Select, Switch,
  Dropdown, Space, Typography, Tag, Tooltip, Alert, Upload, Image } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, UnorderedListOutlined, ControlOutlined, UploadOutlined, FileTextOutlined, ReloadOutlined, ClearOutlined, MoreOutlined } from '@ant-design/icons';
import { menuApi, posApi, resolveMediaUrl, taxApi } from '../../services/api';
import type { MenuItemDto, MenuCategoryDto, CreateMenuItemDto, UpdateMenuItemDto, WorkStationDto, TaxRateDto, MenuItemSaleVisibility } from '../../types';
import type { UploadFile } from 'antd/es/upload/interface';
import { formatError } from '../../utils/errorHandler';
import RecipeEditor from './RecipeEditor';
import ModifierEditor from './ModifierEditor';
import PreparationEditor from './PreparationEditor';
import ComboComponentsEditor from './ComboComponentsEditor';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Title } = Typography;

interface ItemFilters {
  search?: string;
  categoryId?: string;
  stationId?: string;
  isActive?: boolean;
  availableForSale?: boolean;
  saleVisibility?: MenuItemSaleVisibility;
}

const normalizeSearch = (value: string) => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();

export default function MenuItemsList() {
  const { message, modal: dialogs } = AntApp.useApp();

  const { hasPermission } = useAuth();
  const [items, setItems] = useState<MenuItemDto[]>([]);
  const [categorias, setCategorias] = useState<MenuCategoryDto[]>([]);
  const [estaciones, setEstaciones] = useState<WorkStationDto[]>([]);
  const [taxRates, setTaxRates] = useState<TaxRateDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const savingRef = useRef(false);
  const [filters, setFilters] = useState<ItemFilters>({});
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<MenuItemDto | null>(null);
  const [recetaItem, setRecetaItem] = useState<MenuItemDto | null>(null);
  const [modifierItem, setModifierItem] = useState<MenuItemDto | null>(null);
  const [preparationItem, setPreparationItem] = useState<MenuItemDto | null>(null);
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | undefined>();
  const [removeImage, setRemoveImage] = useState(false);
  const [form] = Form.useForm();
  const canManage = hasPermission(PERMISSIONS.menu.itemsManage);

  const formPrice = Form.useWatch('price', form) as number | undefined;
  const formTaxRateId = Form.useWatch('taxRateId', form) as string | undefined;
  const formIsCombo = Form.useWatch('isCombo', form) as boolean | undefined;

  const updateFilters = (changes: Partial<ItemFilters>) => {
    setFilters(current => ({ ...current, ...changes }));
    setPage(1);
  };

  const filteredItems = useMemo(() => {
    const terms = normalizeSearch(filters.search ?? '').trim().split(/\s+/).filter(Boolean);
    return items.filter(item => {
      const searchable = normalizeSearch([item.name, item.generalName, item.internalCode].filter(Boolean).join(' '));
      return terms.every(term => searchable.includes(term))
        && (!filters.categoryId || item.menuCategoryId === filters.categoryId)
        && (!filters.stationId || (filters.stationId === '__none__'
          ? !item.stationId && !item.isCombo
          : item.stationId === filters.stationId || item.comboComponents?.some(c => c.stationId === filters.stationId)))
        && (filters.isActive === undefined || item.isActive === filters.isActive)
        && (filters.availableForSale === undefined || item.availableForSale === filters.availableForSale)
        && (!filters.saleVisibility || item.saleVisibility === filters.saleVisibility);
    });
  }, [items, filters]);

  const priceBreakdown = useMemo(() => {
    if (!formPrice || formPrice <= 0) return null;
    const taxRate = taxRates.find(t => t.id === formTaxRateId);
    if (!taxRate || taxRate.percentage === 0) {
      return { base: formPrice, tax: 0, total: formPrice, label: taxRate?.name ?? 'Sin IVA / Exento', pct: 0 };
    }
    const base = Math.round(formPrice / (1 + taxRate.percentage / 100) * 100) / 100;
    const tax = Math.round((formPrice - base) * 100) / 100;
    return { base, tax, total: formPrice, label: taxRate.name, pct: taxRate.percentage };
  }, [formPrice, formTaxRateId, taxRates]);

  const loadCatalogos = async () => {
    try {
      const [c, e, t] = await Promise.all([menuApi.getCategories(), posApi.getStations(), taxApi.getTaxRates(true)]);
      setCategorias(c.data);
      setEstaciones(e.data);
      setTaxRates(t.data);
    } catch { /* silencioso */ }
  };

  const load = async () => {
    setLoading(true);
    try {
      const [i, c, e, t] = await Promise.all([menuApi.getItems(), menuApi.getCategories(), posApi.getStations(), taxApi.getTaxRates(true)]);
      setItems(i.data);
      setCategorias(c.data);
      setEstaciones(e.data);
      setTaxRates(t.data);
    } catch (e) { message.error(formatError(e)); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  useEffect(() => () => {
    if (imagePreview?.startsWith('blob:')) URL.revokeObjectURL(imagePreview);
  }, [imagePreview]);

  const openModal = async (item?: MenuItemDto) => {
    setEditing(item ?? null);
    setImageFile(null);
    setImagePreview(resolveMediaUrl(item?.imageUrl));
    setRemoveImage(false);
    if (item) {
      form.setFieldsValue({ ...item, isCombo: item.isCombo ?? false, comboComponents: item.comboComponents ?? [] });
    } else {
      form.resetFields();
      form.setFieldsValue({
        isActive: true,
        availableForSale: true,
        price: 0,
        displayOrder: 0,
        saleVisibility: 'MenuAndPromotions',
        isCombo: false,
        comboComponents: [],
      });
    }
    await loadCatalogos();
    setModal(true);
  };

  const save = async () => {
    if (savingRef.current) return;
    savingRef.current = true;
    setSaving(true);
    try {
      const values = await form.validateFields();
      values.comboComponents = values.isCombo ? values.comboComponents : [];
      if (values.isCombo) values.stationId = null;
      if (editing && removeImage && !imageFile && editing.imageUrl) {
        await menuApi.deleteItemImage(editing.id);
      }

      let saved: MenuItemDto;
      if (editing) {
        saved = (await menuApi.updateItem(editing.id, values as UpdateMenuItemDto)).data;
      } else {
        saved = (await menuApi.createItem(values as CreateMenuItemDto)).data;
      }

      if (imageFile) {
        await menuApi.uploadItemImage(saved.id, imageFile);
      }
      message.success('Guardado');
      setModal(false);
      load();
    } catch (e) {
      if (!(e && typeof e === 'object' && 'errorFields' in e)) message.error(formatError(e));
    } finally {
      savingRef.current = false;
      setSaving(false);
    }
  };

  const remove = async (id: string) => {
    try { await menuApi.deleteItem(id); message.success('Eliminado'); load(); }
    catch (e) { message.error(formatError(e)); }
  };

  const openOperationalSheet = async (item: MenuItemDto) => {
    try {
      const response = await menuApi.getOperationalSheetPdf(item.id);
      const url = URL.createObjectURL(response.data);
      window.open(url, '_blank', 'noopener,noreferrer');
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (e) {
      message.error(formatError(e));
    }
  };

  const categoriaOptions = categorias.map(c => ({ label: c.name, value: c.id }));
  const estacionOptions = estaciones.map(e => ({ label: e.name, value: e.id }));
  const taxRateOptions = taxRates.map(t => ({ label: `${t.name} (${t.percentage}%)`, value: t.id }));

  const selectImage = (file: File) => {
    if (file.size > 3 * 1024 * 1024) {
      message.error('La imagen no puede superar 3 MB');
      return Upload.LIST_IGNORE;
    }
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
      message.error('Usa una imagen JPG, PNG o WEBP');
      return Upload.LIST_IGNORE;
    }

    if (imagePreview?.startsWith('blob:')) URL.revokeObjectURL(imagePreview);
    setImageFile(file);
    setImagePreview(URL.createObjectURL(file));
    setRemoveImage(false);
    return false;
  };

  const clearImage = () => {
    if (imagePreview?.startsWith('blob:')) URL.revokeObjectURL(imagePreview);
    setImageFile(null);
    setImagePreview(undefined);
    setRemoveImage(true);
    form.setFieldValue('imageUrl', undefined);
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: 8, marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Ítems del Menú</Title>
        {canManage && <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>Nuevo ítem</Button>}
      </div>

      <Space wrap style={{ marginBottom: 16, width: '100%' }}>
        <Input.Search
          aria-label="Buscar ítems"
          placeholder="Buscar por nombre o código"
          allowClear
          value={filters.search ?? ''}
          onChange={event => updateFilters({ search: event.target.value })}
          style={{ width: 280, maxWidth: '100%' }}
        />
        <Select
          aria-label="Filtrar por categoría"
          placeholder="Categoría"
          allowClear
          showSearch
          optionFilterProp="label"
          options={categoriaOptions}
          value={filters.categoryId}
          onChange={categoryId => updateFilters({ categoryId })}
          style={{ width: 200 }}
        />
        <Select
          aria-label="Filtrar por estación"
          placeholder="Estación"
          allowClear
          showSearch
          optionFilterProp="label"
          options={[{ label: 'Sin estación asignada', value: '__none__' }, ...estacionOptions]}
          value={filters.stationId}
          onChange={stationId => updateFilters({ stationId })}
          style={{ width: 200 }}
        />
        <Select
          aria-label="Filtrar por estado"
          placeholder="Estado"
          allowClear
          options={[{ label: 'Activos', value: true }, { label: 'Inactivos', value: false }]}
          value={filters.isActive}
          onChange={isActive => updateFilters({ isActive })}
          style={{ width: 140 }}
        />
        <Select
          aria-label="Filtrar por disponibilidad"
          placeholder="Disponibilidad"
          allowClear
          options={[{ label: 'Disponibles', value: true }, { label: 'No disponibles', value: false }]}
          value={filters.availableForSale}
          onChange={availableForSale => updateFilters({ availableForSale })}
          style={{ width: 170 }}
        />
        <Select
          aria-label="Filtrar por visibilidad de venta"
          placeholder="Visibilidad de venta"
          allowClear
          options={[
            { label: 'Menú y promociones', value: 'MenuAndPromotions' },
            { label: 'Solo promociones', value: 'PromotionsOnly' },
            { label: 'Solo combos', value: 'ComponentsOnly' },
          ]}
          value={filters.saleVisibility}
          onChange={saleVisibility => updateFilters({ saleVisibility })}
          style={{ width: 190 }}
        />
        <Tooltip title="Limpiar filtros">
          <Button
            aria-label="Limpiar filtros"
            icon={<ClearOutlined />}
            disabled={!Object.values(filters).some(value => value !== undefined && value !== '')}
            onClick={() => { setFilters({}); setPage(1); }}
          />
        </Tooltip>
        <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Actualizar</Button>
      </Space>

      <Table
        dataSource={filteredItems}
        rowKey="id"
        loading={loading}
        size="small"
        scroll={{ x: 1000 }}
        pagination={{
          current: page,
          pageSize,
          showSizeChanger: true,
          pageSizeOptions: ['10', '20', '50'],
          showTotal: total => `${total} ítems`,
          onChange: (current, size) => { setPage(size === pageSize ? current : 1); setPageSize(size); },
        }}
        columns={[
          {
            title: 'Foto', key: 'image', width: 72,
            render: (_: unknown, item: MenuItemDto) => item.imageUrl ? (
              <Image
                src={resolveMediaUrl(item.imageUrl)}
                width={44}
                height={44}
                style={{ objectFit: 'cover', borderRadius: 6 }}
                preview={false}
              />
            ) : <span style={{ color: '#999' }}>-</span>,
          },
          {
            title: 'Nombres', key: 'nombre',
            render: (_: unknown, item: MenuItemDto) => (
              <div>
                <Space wrap>
                  <strong>{item.name}</strong>
                  {item.isCombo && <Tag color="cyan">Combo</Tag>}
                  {!item.isActive && <Tag>Inactivo</Tag>}
                  {!item.availableForSale && <Tag color="orange">No disponible</Tag>}
                  {item.saleVisibility === 'PromotionsOnly' && <Tag color="magenta">Solo promociones</Tag>}
                  {item.saleVisibility === 'ComponentsOnly' && <Tag color="cyan">Solo combos</Tag>}
                </Space>
                <div style={{ color: '#8c8c8c', fontSize: 12 }}>{item.generalName}</div>
              </div>
            ),
          },
          { title: 'Código', dataIndex: 'internalCode', key: 'codigo', width: 100 },
          {
            title: 'Categoría', key: 'categoria',
            render: (_: unknown, item: MenuItemDto) => (
              <Space>
                {item.categoryColor && <span style={{ display: 'inline-block', width: 10, height: 10, borderRadius: 2, background: item.categoryColor }} />}
                {item.categoryName}
              </Space>
            ),
          },
          {
            title: 'Precio', dataIndex: 'price', key: 'precio', width: 100,
            render: (v: number) => `$${v.toFixed(2)}`,
          },
          { title: 'Orden', dataIndex: 'displayOrder', key: 'displayOrder', width: 75, align: 'center' },
          {
            title: 'Estación', key: 'estacion', width: 120,
            render: (_: unknown, item: MenuItemDto) =>
              item.isCombo ? <Space wrap>{[...new Set(item.comboComponents?.map(c => c.stationName).filter(Boolean))].map(name => <Tag key={name}>{name}</Tag>)}</Space>
                : item.stationName ? <Tag>{item.stationName}</Tag> : <span style={{ color: '#999' }}>—</span>,
          },
          {
            title: 'Acciones', key: 'acc', width: 80, fixed: 'right',
            render: (_: unknown, item: MenuItemDto) => (
              <Dropdown
                trigger={['click']}
                menu={{
                  style: { minWidth: 210 },
                  items: [
                    ...(canManage ? [
                      { key: 'edit', label: 'Editar ítem', icon: <EditOutlined /> },
                      { key: 'recipe', icon: <UnorderedListOutlined />, label: (
                        <span style={{ display: 'flex', justifyContent: 'space-between', gap: 16 }}>
                          <span>{item.isCombo ? 'Componentes' : 'Receta'}</span>
                          <Tag style={{ marginInlineEnd: 0 }}>{item.isCombo ? item.comboComponents?.length ?? 0 : item.totalIngredients}</Tag>
                        </span>
                      ) },
                      { key: 'modifiers', icon: <ControlOutlined />, disabled: item.isCombo, label: (
                        <span style={{ display: 'flex', justifyContent: 'space-between', gap: 16 }}>
                          <span>Opciones</span><Tag style={{ marginInlineEnd: 0 }}>{item.modifierGroups?.length ?? 0}</Tag>
                        </span>
                      ) },
                      { key: 'preparation', label: 'Preparación', icon: <FileTextOutlined /> },
                    ] : []),
                    { key: 'sheet', label: 'Ficha operativa', icon: <FileTextOutlined /> },
                    ...(canManage ? [
                      { type: 'divider' as const },
                      { key: 'delete', label: 'Eliminar ítem', icon: <DeleteOutlined />, danger: true },
                    ] : []),
                  ],
                  onClick: ({ key }) => {
                    switch (key) {
                      case 'edit': void openModal(item); break;
                      case 'recipe': if (item.isCombo) void openModal(item); else setRecetaItem(item); break;
                      case 'modifiers': setModifierItem(item); break;
                      case 'preparation': setPreparationItem(item); break;
                      case 'sheet': void openOperationalSheet(item); break;
                      case 'delete':
                        dialogs.confirm({
                          title: 'Eliminar ítem',
                          content: item.name,
                          okText: 'Eliminar', cancelText: 'Cancelar', okButtonProps: { danger: true },
                          onOk: () => remove(item.id),
                        });
                        break;
                    }
                  },
                }}
              >
                <Tooltip title="Acciones">
                  <Button size="small" aria-label={`Acciones de ${item.name}`} icon={<MoreOutlined />} />
                </Tooltip>
              </Dropdown>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Editar ítem del menú' : 'Nuevo ítem del menú'}
        open={modal}
        onOk={save}
        onCancel={() => { if (!savingRef.current) setModal(false); }}
        confirmLoading={saving}
        closable={!saving}
        keyboard={!saving}
        okText="Guardar"
        width={760}
      >
        <Form form={form} layout="vertical">
          <Form.Item name="isCombo" label="Tipo de ítem">
            <Select options={[{ label: 'Plato individual', value: false }, { label: 'Combo', value: true }]} />
          </Form.Item>
          {formIsCombo && <ComboComponentsEditor items={items} editingId={editing?.id} />}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 220px), 1fr))', gap: 12 }}>
            <Form.Item name="name" label="Nombre temático" rules={[{ required: true }, { max: 150 }]}>
              <Input maxLength={150} placeholder="Ej. Pluma mística" />
            </Form.Item>
            <Form.Item name="generalName" label="Nombre general / fiscal" rules={[{ required: true }, { max: 150 }]}>
              <Input maxLength={150} placeholder="Ej. Filete de pollo" />
            </Form.Item>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 220px), 1fr))', gap: 12 }}>
            <Form.Item
              name="internalCode"
              label="Código interno"
              extra="Si lo dejas vacío, se generará automáticamente."
              rules={[{ max: 25, message: 'Máximo 25 caracteres' }]}
            >
              <Input maxLength={25} placeholder="Automático al guardar" />
            </Form.Item>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 260px), 1fr))', gap: 12, alignItems: 'start' }}>
          <Form.Item name="description" label="Descripción"><Input.TextArea rows={4} /></Form.Item>
          <Form.Item name="imageUrl" hidden><Input /></Form.Item>
          <Form.Item label="Imagen del plato">
            <Space align="start">
              {imagePreview ? (
                <Image
                  src={imagePreview}
                  width={96}
                  height={72}
                  style={{ objectFit: 'cover', borderRadius: 6 }}
                  preview
                />
              ) : (
                <div style={{
                  width: 96, height: 72, border: '1px dashed #d9d9d9', borderRadius: 6,
                  display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#999',
                }}>
                  Sin imagen
                </div>
              )}
              <Space direction="vertical" size={8}>
                <Upload
                  accept="image/jpeg,image/png,image/webp"
                  maxCount={1}
                  fileList={imageFile ? [{ uid: 'new-image', name: imageFile.name, status: 'done' } as UploadFile] : []}
                  beforeUpload={(file) => selectImage(file)}
                  onRemove={() => { clearImage(); return true; }}
                  showUploadList={false}
                >
                  <Button icon={<UploadOutlined />}>Seleccionar imagen</Button>
                </Upload>
                {imagePreview && <Button danger size="small" onClick={clearImage}>Quitar imagen</Button>}
                <span style={{ color: '#8c8c8c', fontSize: 12 }}>JPG, PNG o WEBP. Maximo 3 MB.</span>
              </Space>
            </Space>
          </Form.Item>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 180px), 1fr))', gap: 12 }}>
            <Form.Item name="menuCategoryId" label="Categoría" rules={[{ required: true }]} style={{ flex: 1 }}>
              <Select options={categoriaOptions} placeholder="Seleccionar" />
            </Form.Item>
            <Form.Item name="saleVisibility" label="Visibilidad de venta" rules={[{ required: true }]}>
              <Select options={[
                { value: 'MenuAndPromotions', label: 'Menú y promociones' },
                { value: 'PromotionsOnly', label: 'Solo promociones' },
                { value: 'ComponentsOnly', label: 'Solo combos' },
              ]} />
            </Form.Item>
            <Form.Item
              name="price"
              label="Precio ($)"
              tooltip="Ingresa el precio final al público, con IVA incluido. El sistema calculará la base imponible automáticamente."
              rules={[{ required: true }]}
              style={{ width: 140 }}
            >
              <InputNumber style={{ width: '100%' }} min={0} step={0.01} prefix="$" />
            </Form.Item>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 180px), 1fr))', gap: 12 }}>
            <Form.Item
              name="displayOrder"
              label="Orden en menú"
              tooltip="Usa 1, 2, 3... Los ítems con orden 0 quedan después, ordenados alfabéticamente."
              rules={[{ required: true }]}
            >
              <InputNumber style={{ width: '100%' }} min={0} precision={0} />
            </Form.Item>
            <Form.Item name="taxRateId" label="Tarifa de IVA">
            <Select
              options={taxRateOptions}
              placeholder="Sin IVA / Hereda del sistema"
              allowClear
            />
            </Form.Item>
            <Form.Item name="stationId" label="Estación destino">
              <Select
                options={estacionOptions}
                disabled={formIsCombo}
                placeholder="Sin estación asignada"
                allowClear
              />
            </Form.Item>
          </div>
          {priceBreakdown && (
            <div style={{
              background: '#f6ffed', border: '1px solid #b7eb8f', borderRadius: 6,
              padding: '10px 14px', marginBottom: 16, fontSize: 13,
            }}>
              <div style={{ fontWeight: 600, marginBottom: 6, color: '#389e0d' }}>
                Desglose del precio ingresado
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span style={{ color: '#595959' }}>Precio sin IVA (base imponible)</span>
                  <span style={{ fontWeight: 500 }}>${priceBreakdown.base.toFixed(2)}</span>
                </div>
                {priceBreakdown.pct > 0 && (
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <span style={{ color: '#595959' }}>IVA {priceBreakdown.pct}% ({priceBreakdown.label})</span>
                    <span style={{ fontWeight: 500, color: '#1677ff' }}>${priceBreakdown.tax.toFixed(2)}</span>
                  </div>
                )}
                <div style={{ display: 'flex', justifyContent: 'space-between', borderTop: '1px solid #b7eb8f', paddingTop: 4, marginTop: 2 }}>
                  <span style={{ fontWeight: 600 }}>Precio total al cliente</span>
                  <span style={{ fontWeight: 700, fontSize: 14 }}>${priceBreakdown.total.toFixed(2)}</span>
                </div>
              </div>
            </div>
          )}
          {!formTaxRateId && !!formPrice && formPrice > 0 && (
            <Alert
              type="warning"
              showIcon
              message="Sin tarifa de IVA seleccionada, el precio completo se tratará como base exenta."
              style={{ marginBottom: 16, fontSize: 12 }}
            />
          )}
          {editing && (
            <Space>
              <Form.Item name="isActive" label="Activo" valuePropName="checked"><Switch /></Form.Item>
              <Form.Item name="availableForSale" label="Disponible para venta" valuePropName="checked"><Switch /></Form.Item>
            </Space>
          )}
        </Form>
      </Modal>

      {recetaItem && (
        <RecipeEditor
          itemId={recetaItem.id}
          itemName={recetaItem.name}
          open={!!recetaItem}
          onClose={() => { setRecetaItem(null); load(); }}
        />
      )}
      {modifierItem && (
        <ModifierEditor
          itemId={modifierItem.id}
          itemName={modifierItem.name}
          open={!!modifierItem}
          onClose={() => { setModifierItem(null); load(); }}
        />
      )}
      {preparationItem && (
        <PreparationEditor
          itemId={preparationItem.id}
          itemName={preparationItem.name}
          open={!!preparationItem}
          onClose={() => { setPreparationItem(null); load(); }}
        />
      )}
    </div>
  );
}
