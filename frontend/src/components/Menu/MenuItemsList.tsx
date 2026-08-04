import { useEffect, useMemo, useState } from 'react';
import { App as AntApp, Table, Button, Modal, Form, Input, InputNumber, Select, Switch,
  Popconfirm, Space, Typography, Tag, Badge, Tooltip, Alert, Upload, Image } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, UnorderedListOutlined, ControlOutlined, UploadOutlined, FileTextOutlined } from '@ant-design/icons';
import { menuApi, posApi, resolveMediaUrl, taxApi } from '../../services/api';
import type { MenuItemDto, MenuCategoryDto, CreateMenuItemDto, UpdateMenuItemDto, WorkStationDto, TaxRateDto } from '../../types';
import type { UploadFile } from 'antd/es/upload/interface';
import { formatError } from '../../utils/errorHandler';
import RecipeEditor from './RecipeEditor';
import ModifierEditor from './ModifierEditor';
import PreparationEditor from './PreparationEditor';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Title } = Typography;

export default function MenuItemsList() {
  const { message } = AntApp.useApp();

  const { hasPermission } = useAuth();
  const [items, setItems] = useState<MenuItemDto[]>([]);
  const [categorias, setCategorias] = useState<MenuCategoryDto[]>([]);
  const [estaciones, setEstaciones] = useState<WorkStationDto[]>([]);
  const [taxRates, setTaxRates] = useState<TaxRateDto[]>([]);
  const [loading, setLoading] = useState(false);
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
      form.setFieldsValue(item);
    } else {
      form.resetFields();
      form.setFieldsValue({ isActive: true, availableForSale: true, price: 0 });
    }
    await loadCatalogos();
    setModal(true);
  };

  const save = async () => {
    const values = await form.validateFields();
    try {
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
    } catch (e) { message.error(formatError(e)); }
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
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Ítems del Menú</Title>
        {canManage && <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>Nuevo ítem</Button>}
      </div>

      <Table
        dataSource={items}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={{ defaultPageSize: 20, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
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
            title: 'Nombre', key: 'nombre',
            render: (_: unknown, item: MenuItemDto) => (
              <Space>
                {item.name}
                {!item.isActive && <Tag>Inactivo</Tag>}
                {!item.availableForSale && <Tag color="orange">No disponible</Tag>}
              </Space>
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
          {
            title: 'Estación', key: 'estacion', width: 120,
            render: (_: unknown, item: MenuItemDto) =>
              item.stationName ? <Tag>{item.stationName}</Tag> : <span style={{ color: '#999' }}>—</span>,
          },
          {
            title: 'IVA', key: 'iva', width: 90,
            render: (_: unknown, item: MenuItemDto) =>
              item.taxRateName
                ? <Tag color="blue">{item.taxRateName}</Tag>
                : <span style={{ color: '#999' }}>—</span>,
          },
          ...(canManage ? [{
            title: 'Receta', key: 'receta', width: 90,
            render: (_: unknown, item: MenuItemDto) => (
              <Tooltip title="Ver/editar receta">
                <Badge count={item.totalIngredients} size="small" showZero>
                  <Button
                    size="small"
                    icon={<UnorderedListOutlined />}
                    onClick={() => setRecetaItem(item)}
                  />
                </Badge>
              </Tooltip>
            ),
          }] : []),
          ...(canManage ? [{
            title: 'Opciones', key: 'modifiers', width: 95,
            render: (_: unknown, item: MenuItemDto) => (
              <Tooltip title="Ver/editar modificadores">
                <Badge count={item.modifierGroups?.length ?? 0} size="small" showZero>
                  <Button
                    size="small"
                    icon={<ControlOutlined />}
                    onClick={() => setModifierItem(item)}
                  />
                </Badge>
              </Tooltip>
            ),
          }] : []),
          ...(canManage ? [{
            title: 'Preparación', key: 'preparation', width: 105,
            render: (_: unknown, item: MenuItemDto) => (
              <Tooltip title="Ver/editar preparación">
                <Button
                  size="small"
                  icon={<FileTextOutlined />}
                  onClick={() => setPreparationItem(item)}
                />
              </Tooltip>
            ),
          }] : []),
          {
            title: 'Ficha', key: 'operationalSheet', width: 80,
            render: (_: unknown, item: MenuItemDto) => (
              <Tooltip title="Abrir ficha operativa">
                <Button
                  size="small"
                  icon={<FileTextOutlined />}
                  onClick={() => openOperationalSheet(item)}
                />
              </Tooltip>
            ),
          },
          ...(canManage ? [{
            title: 'Acciones', key: 'acc', width: 100,
            render: (_: unknown, item: MenuItemDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openModal(item)} />
                <Popconfirm title="¿Eliminar?" onConfirm={() => remove(item.id)}>
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          }] : []),
        ]}
      />

      <Modal
        title={editing ? 'Editar ítem del menú' : 'Nuevo ítem del menú'}
        open={modal}
        onOk={save}
        onCancel={() => setModal(false)}
        okText="Guardar"
        width={760}
      >
        <Form form={form} layout="vertical">
          <div style={{ display: 'grid', gridTemplateColumns: 'minmax(220px, 1fr) minmax(160px, 0.6fr)', gap: 12 }}>
            <Form.Item name="name" label="Nombre" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="internalCode" label="Código interno"><Input /></Form.Item>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'minmax(260px, 1fr) 280px', gap: 12, alignItems: 'start' }}>
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
          <div style={{ display: 'grid', gridTemplateColumns: 'minmax(180px, 1fr) 130px minmax(170px, 1fr) minmax(170px, 1fr)', gap: 12 }}>
            <Form.Item name="menuCategoryId" label="Categoría" rules={[{ required: true }]} style={{ flex: 1 }}>
              <Select options={categoriaOptions} placeholder="Seleccionar" />
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
