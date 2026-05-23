import { useEffect, useMemo, useState } from 'react';
import {
  Table, Button, Modal, Form, Input, InputNumber, Select, Switch,
  Popconfirm, Space, Typography, message, Tag, Badge, Tooltip, Alert
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, UnorderedListOutlined } from '@ant-design/icons';
import { menuApi, posApi, taxApi } from '../../services/api';
import type { MenuItemDto, MenuCategoryDto, CreateMenuItemDto, UpdateMenuItemDto, WorkStationDto, TaxRateDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import RecipeEditor from './RecipeEditor';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Title } = Typography;

export default function MenuItemsList() {
  const { hasPermission } = useAuth();
  const [items, setItems] = useState<MenuItemDto[]>([]);
  const [categorias, setCategorias] = useState<MenuCategoryDto[]>([]);
  const [estaciones, setEstaciones] = useState<WorkStationDto[]>([]);
  const [taxRates, setTaxRates] = useState<TaxRateDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<MenuItemDto | null>(null);
  const [recetaItem, setRecetaItem] = useState<MenuItemDto | null>(null);
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

  const openModal = async (item?: MenuItemDto) => {
    setEditing(item ?? null);
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
      if (editing) {
        await menuApi.updateItem(editing.id, values as UpdateMenuItemDto);
      } else {
        await menuApi.createItem(values as CreateMenuItemDto);
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

  const categoriaOptions = categorias.map(c => ({ label: c.name, value: c.id }));
  const estacionOptions = estaciones.map(e => ({ label: e.name, value: e.id }));
  const taxRateOptions = taxRates.map(t => ({ label: `${t.name} (${t.percentage}%)`, value: t.id }));

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
        width={560}
      >
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="Nombre" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="internalCode" label="Código interno"><Input /></Form.Item>
          <Form.Item name="description" label="Descripción"><Input.TextArea rows={2} /></Form.Item>
          <Space style={{ width: '100%' }} size="middle">
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
          </Space>
          <Form.Item name="taxRateId" label="Tarifa de IVA">
            <Select
              options={taxRateOptions}
              placeholder="Sin IVA / Hereda del sistema"
              allowClear
            />
          </Form.Item>
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
          <Form.Item name="stationId" label="Estación destino">
            <Select
              options={estacionOptions}
              placeholder="Sin estación asignada"
              allowClear
            />
          </Form.Item>
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
    </div>
  );
}
