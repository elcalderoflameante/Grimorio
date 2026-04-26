import { useEffect, useState } from 'react';
import {
  Table, Button, Modal, Form, Input, InputNumber, Select, Popconfirm, Space, Typography, message, Tag
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, SwapOutlined } from '@ant-design/icons';
import { inventoryApi } from '../../services/api';
import type { MeasurementUnitDto, UnitConversionDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Title } = Typography;

export default function UnitsList() {
  const [unidades, setUnidades] = useState<MeasurementUnitDto[]>([]);
  const [conversiones, setConversiones] = useState<UnitConversionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalUnidad, setModalUnidad] = useState(false);
  const [modalConversion, setModalConversion] = useState(false);
  const [editingUnidad, setEditingUnidad] = useState<MeasurementUnitDto | null>(null);
  const [formUnidad] = Form.useForm();
  const [formConversion] = Form.useForm();

  const load = async () => {
    setLoading(true);
    try {
      const [u, c] = await Promise.all([
        inventoryApi.getUnits(),
        inventoryApi.getConversions(),
      ]);
      setUnidades(u.data);
      setConversiones(c.data);
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const openUnidad = (u?: MeasurementUnitDto) => {
    setEditingUnidad(u ?? null);
    formUnidad.setFieldsValue(u ?? { nombre: '', simbolo: '' });
    setModalUnidad(true);
  };

  const saveUnidad = async () => {
    const values = await formUnidad.validateFields();
    try {
      if (editingUnidad) {
        await inventoryApi.updateUnit(editingUnidad.id, values);
      } else {
        await inventoryApi.createUnit(values);
      }
      message.success('Guardado');
      setModalUnidad(false);
      load();
    } catch (e) {
      message.error(formatError(e));
    }
  };

  const deleteUnidad = async (id: string) => {
    try {
      await inventoryApi.deleteUnit(id);
      message.success('Eliminado');
      load();
    } catch (e) {
      message.error(formatError(e));
    }
  };

  const saveConversion = async () => {
    const values = await formConversion.validateFields();
    try {
      await inventoryApi.createConversion(values);
      message.success('Conversión creada');
      setModalConversion(false);
      formConversion.resetFields();
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

  const unidadOptions = unidades.map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id }));

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Unidades de Medida</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => openUnidad()}>
          Nueva unidad
        </Button>
      </div>

      <Table
        dataSource={unidades}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={false}
        columns={[
          { title: 'Nombre', dataIndex: 'nombre', key: 'nombre' },
          {
            title: 'Símbolo', dataIndex: 'simbolo', key: 'simbolo',
            render: (v: string) => <Tag>{v}</Tag>,
          },
          {
            title: 'Acciones', key: 'acciones', width: 100,
            render: (_: unknown, u: MeasurementUnitDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openUnidad(u)} />
                <Popconfirm title="¿Eliminar?" onConfirm={() => deleteUnidad(u.id)}>
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 32, marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Conversiones de Unidad</Title>
        <Button icon={<SwapOutlined />} onClick={() => setModalConversion(true)}>
          Nueva conversión
        </Button>
      </div>

      <Table
        dataSource={conversiones}
        rowKey="id"
        size="small"
        pagination={false}
        columns={[
          {
            title: 'Origen', key: 'origen',
            render: (_: unknown, c: UnitConversionDto) => `${c.originUnitName} (${c.originUnitSymbol})`,
          },
          {
            title: 'Destino', key: 'destino',
            render: (_: unknown, c: UnitConversionDto) => `${c.destinationUnitName} (${c.destinationUnitSymbol})`,
          },
          {
            title: 'Factor', dataIndex: 'factor', key: 'factor',
            render: (v: number) => `1 origen = ${v} destino`,
          },
          {
            title: '', key: 'acc', width: 60,
            render: (_: unknown, c: UnitConversionDto) => (
              <Popconfirm title="¿Eliminar?" onConfirm={() => deleteConversion(c.id)}>
                <Button size="small" danger icon={<DeleteOutlined />} />
              </Popconfirm>
            ),
          },
        ]}
      />

      <Modal
        title={editingUnidad ? 'Editar unidad' : 'Nueva unidad de medida'}
        open={modalUnidad}
        onOk={saveUnidad}
        onCancel={() => setModalUnidad(false)}
        okText="Guardar"
      >
        <Form form={formUnidad} layout="vertical">
          <Form.Item name="nombre" label="Nombre" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="simbolo" label="Símbolo" rules={[{ required: true }]}>
            <Input placeholder="kg, L, unid..." />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Nueva conversión de unidad"
        open={modalConversion}
        onOk={saveConversion}
        onCancel={() => { setModalConversion(false); formConversion.resetFields(); }}
        okText="Guardar"
      >
        <Form form={formConversion} layout="vertical">
          <Form.Item name="unidadOrigenId" label="Unidad origen" rules={[{ required: true }]}>
            <Select options={unidadOptions} placeholder="Seleccionar" />
          </Form.Item>
          <Form.Item name="unidadDestinoId" label="Unidad destino" rules={[{ required: true }]}>
            <Select options={unidadOptions} placeholder="Seleccionar" />
          </Form.Item>
          <Form.Item name="factor" label="Factor (1 origen = X destino)" rules={[{ required: true }]}>
            <InputNumber style={{ width: '100%' }} step={0.000001} min={0} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
