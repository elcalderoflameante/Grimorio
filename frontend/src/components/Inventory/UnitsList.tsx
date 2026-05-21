import { useEffect, useState } from 'react';
import {
  Table, Button, Modal, Form, Input, InputNumber, Select,
  Popconfirm, Space, Typography, message, Tag, Alert,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, SwapOutlined, ArrowRightOutlined } from '@ant-design/icons';
import { inventoryApi } from '../../services/api';
import type { MeasurementUnitDto, UnitConversionDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';

const { Title, Text } = Typography;

export default function UnitsList() {
  const { hasPermission } = useAuth();
  const [units, setUnits] = useState<MeasurementUnitDto[]>([]);
  const [conversions, setConversions] = useState<UnitConversionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalUnit, setModalUnit] = useState(false);
  const [modalConversion, setModalConversion] = useState(false);
  const [editingUnit, setEditingUnit] = useState<MeasurementUnitDto | null>(null);
  const [formUnit] = Form.useForm();
  const [formConversion] = Form.useForm();

  // Estado para el preview reactivo del modal de conversión
  const [previewOrigin, setPreviewOrigin] = useState<MeasurementUnitDto | null>(null);
  const [previewDest, setPreviewDest] = useState<MeasurementUnitDto | null>(null);
  const [previewFactor, setPreviewFactor] = useState<number | null>(null);
  const canManage = hasPermission(PERMISSIONS.inventory.configManage);

  const load = async () => {
    setLoading(true);
    try {
      const [u, c] = await Promise.all([
        inventoryApi.getUnits(),
        inventoryApi.getConversions(),
      ]);
      setUnits(u.data);
      setConversions(c.data);
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const openUnit = (u?: MeasurementUnitDto) => {
    setEditingUnit(u ?? null);
    formUnit.setFieldsValue(u ?? { name: '', symbol: '' });
    setModalUnit(true);
  };

  const saveUnit = async () => {
    const values = await formUnit.validateFields();
    try {
      if (editingUnit) {
        await inventoryApi.updateUnit(editingUnit.id, values);
      } else {
        await inventoryApi.createUnit(values);
      }
      message.success('Guardado');
      setModalUnit(false);
      load();
    } catch (e) {
      message.error(formatError(e));
    }
  };

  const deleteUnit = async (id: string) => {
    try {
      await inventoryApi.deleteUnit(id);
      message.success('Eliminado');
      load();
    } catch (e) {
      message.error(formatError(e));
    }
  };

  const openConversion = () => {
    setPreviewOrigin(null);
    setPreviewDest(null);
    setPreviewFactor(null);
    formConversion.resetFields();
    setModalConversion(true);
  };

  const saveConversion = async () => {
    const values = await formConversion.validateFields();
    try {
      await inventoryApi.createConversion(values);
      message.success('Conversión creada');
      setModalConversion(false);
      load();
    } catch (e) {
      message.error(formatError(e));
    }
  };

  const deleteConversion = async (id: string) => {
    try {
      await inventoryApi.deleteConversion(id);
      message.success('Eliminado');
      load();
    } catch (e) {
      message.error(formatError(e));
    }
  };

  const unitOptions = units.map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id }));

  const handleConversionFieldChange = () => {
    const vals = formConversion.getFieldsValue();
    setPreviewOrigin(units.find(u => u.id === vals.originUnitId) ?? null);
    setPreviewDest(units.find(u => u.id === vals.destinationUnitId) ?? null);
    setPreviewFactor(vals.factor ?? null);
  };

  const fmtFactor = (v: number) =>
    v % 1 === 0 ? v.toString() : v.toLocaleString('es-EC', { maximumFractionDigits: 6 });

  return (
    <div>
      {/* ── Unidades ──────────────────────────────────────────────────────── */}
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Unidades de Medida</Title>
        {canManage && <Button type="primary" icon={<PlusOutlined />} onClick={() => openUnit()}>
          Nueva unidad
        </Button>}
      </div>

      <Table
        dataSource={units}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={{ defaultPageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
        columns={[
          { title: 'Nombre', dataIndex: 'name' },
          {
            title: 'Símbolo', dataIndex: 'symbol',
            render: (v: string) => <Tag>{v}</Tag>,
          },
          ...(canManage ? [{
            title: '', key: 'actions', width: 80,
            render: (_: unknown, u: MeasurementUnitDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openUnit(u)} />
                <Popconfirm title="¿Eliminar?" onConfirm={() => deleteUnit(u.id)}>
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          }] : []),
        ]}
      />

      {/* ── Conversiones ──────────────────────────────────────────────────── */}
      <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 32, marginBottom: 12 }}>
        <Title level={5} style={{ margin: 0 }}>Conversiones de Unidad</Title>
        {canManage && <Button icon={<SwapOutlined />} onClick={openConversion}>
          Nueva conversión
        </Button>}
      </div>

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        title="¿Cómo funciona?"
        description={
          <span>
            Define cuántas unidades destino equivalen a <strong>1 unidad origen</strong>.
            Ejemplo: <code>1 kg → 1000 g</code> significa que el factor es <strong>1000</strong>.
            El sistema también calculará el sentido inverso automáticamente
            (si la receta usa gramos y el stock está en kg, no necesitas crear otra conversión).
          </span>
        }
      />

      <Table
        dataSource={conversions}
        rowKey="id"
        size="small"
        pagination={{ defaultPageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
        columns={[
          {
            title: 'Equivalencia',
            key: 'formula',
            render: (_: unknown, c: UnitConversionDto) => (
              <Space size={6}>
                <Tag style={{ fontSize: 13, padding: '2px 8px' }}>
                  1 {c.originUnitName}
                  {c.originUnitSymbol !== c.originUnitName ? ` (${c.originUnitSymbol})` : ''}
                </Tag>
                <ArrowRightOutlined style={{ color: '#8c8c8c' }} />
                <Tag color="blue" style={{ fontSize: 13, padding: '2px 8px' }}>
                  {fmtFactor(c.factor)} {c.destinationUnitName}
                  {c.destinationUnitSymbol !== c.destinationUnitName ? ` (${c.destinationUnitSymbol})` : ''}
                </Tag>
              </Space>
            ),
          },
          {
            title: 'Inversa implícita',
            key: 'reverse',
            render: (_: unknown, c: UnitConversionDto) => (
              <Text type="secondary" style={{ fontSize: 12 }}>
                1 {c.destinationUnitSymbol} = {(1 / c.factor).toLocaleString('es-EC', { maximumFractionDigits: 8 })} {c.originUnitSymbol}
              </Text>
            ),
          },
          ...(canManage ? [{
            title: '', key: 'actions', width: 60,
            render: (_: unknown, c: UnitConversionDto) => (
              <Popconfirm title="¿Eliminar?" onConfirm={() => deleteConversion(c.id)}>
                <Button size="small" danger icon={<DeleteOutlined />} />
              </Popconfirm>
            ),
          }] : []),
        ]}
      />

      {/* ── Modal unidad ──────────────────────────────────────────────────── */}
      <Modal
        title={editingUnit ? 'Editar unidad' : 'Nueva unidad de medida'}
        open={modalUnit}
        onOk={saveUnit}
        onCancel={() => setModalUnit(false)}
        okText="Guardar"
        cancelText="Cancelar"
      >
        <Form form={formUnit} layout="vertical">
          <Form.Item name="name" label="Nombre" rules={[{ required: true }]}>
            <Input placeholder="Kilogramo, Litro, Mililitro..." />
          </Form.Item>
          <Form.Item name="symbol" label="Símbolo" rules={[{ required: true }]}>
            <Input placeholder="kg, L, ml, unid..." />
          </Form.Item>
        </Form>
      </Modal>

      {/* ── Modal conversión ──────────────────────────────────────────────── */}
      <Modal
        title="Nueva conversión de unidad"
        open={modalConversion}
        onOk={saveConversion}
        onCancel={() => setModalConversion(false)}
        okText="Guardar"
        cancelText="Cancelar"
      >
        <Alert
          type="info"
          showIcon={false}
          style={{ marginBottom: 16, fontSize: 13 }}
          title={
            <span>
              Ingresa cuántas unidades destino hay en <strong>1 unidad origen</strong>.
              <br />
              Ej: para convertir kg ↔ g, elige origen = <em>kg</em>, destino = <em>g</em>, factor = <strong>1000</strong>.
            </span>
          }
        />

        <Form
          form={formConversion}
          layout="vertical"
          onValuesChange={handleConversionFieldChange}
        >
          <Form.Item name="originUnitId" label="Unidad origen (la que tienes en stock)" rules={[{ required: true }]}>
            <Select options={unitOptions} placeholder="Ej: Kilogramo (kg)" />
          </Form.Item>
          <Form.Item name="destinationUnitId" label="Unidad destino (la que usas en recetas)" rules={[{ required: true }]}>
            <Select options={unitOptions} placeholder="Ej: Gramo (g)" />
          </Form.Item>
          <Form.Item
            name="factor"
            label="Factor — ¿cuántas unidades destino caben en 1 unidad origen?"
            rules={[{ required: true }, { type: 'number', min: 0.000001, message: 'El factor debe ser mayor a 0' }]}
          >
            <InputNumber
              style={{ width: '100%' }}
              step={1}
              min={0.000001}
              precision={6}
              placeholder="Ej: 1000"
            />
          </Form.Item>
        </Form>

        {/* Preview reactivo */}
        {previewOrigin && previewDest && previewFactor && previewFactor > 0 ? (
          <div
            style={{
              marginTop: 8,
              padding: '12px 16px',
              background: '#f6ffed',
              border: '1px solid #b7eb8f',
              borderRadius: 8,
            }}
          >
            <Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 6 }}>
              Vista previa de la conversión
            </Text>
            <Space size={10} align="center">
              <Tag style={{ fontSize: 15, padding: '3px 10px', margin: 0 }}>
                1 {previewOrigin.name} ({previewOrigin.symbol})
              </Tag>
              <ArrowRightOutlined style={{ fontSize: 16, color: '#52c41a' }} />
              <Tag color="green" style={{ fontSize: 15, padding: '3px 10px', margin: 0 }}>
                {fmtFactor(previewFactor)} {previewDest.name} ({previewDest.symbol})
              </Tag>
            </Space>
            <Text type="secondary" style={{ fontSize: 12, display: 'block', marginTop: 8 }}>
              Inversa automática: 1 {previewDest.symbol} ={' '}
              {(1 / previewFactor).toLocaleString('es-EC', { maximumFractionDigits: 8 })} {previewOrigin.symbol}
            </Text>
          </div>
        ) : (
          <div
            style={{
              marginTop: 8,
              padding: '10px 14px',
              background: '#fafafa',
              border: '1px dashed #d9d9d9',
              borderRadius: 8,
              textAlign: 'center',
            }}
          >
            <Text type="secondary" style={{ fontSize: 13 }}>
              Completa los campos para ver la vista previa
            </Text>
          </div>
        )}
      </Modal>
    </div>
  );
}
