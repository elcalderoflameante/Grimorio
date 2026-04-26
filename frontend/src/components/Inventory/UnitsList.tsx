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
  const [units, setUnits] = useState<MeasurementUnitDto[]>([]);
  const [conversions, setConversions] = useState<UnitConversionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalUnit, setModalUnit] = useState(false);
  const [modalConversion, setModalConversion] = useState(false);
  const [editingUnit, setEditingUnit] = useState<MeasurementUnitDto | null>(null);
  const [formUnit] = Form.useForm();
  const [formConversion] = Form.useForm();

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

  const unitOptions = units.map(u => ({ label: `${u.name} (${u.symbol})`, value: u.id }));

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Unidades de Medida</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => openUnit()}>
          Nueva unidad
        </Button>
      </div>

      <Table
        dataSource={units}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={false}
        columns={[
          { title: 'Nombre', dataIndex: 'name', key: 'name' },
          {
            title: 'Símbolo', dataIndex: 'symbol', key: 'symbol',
            render: (v: string) => <Tag>{v}</Tag>,
          },
          {
            title: 'Acciones', key: 'actions', width: 100,
            render: (_: unknown, u: MeasurementUnitDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openUnit(u)} />
                <Popconfirm title="¿Eliminar?" onConfirm={() => deleteUnit(u.id)}>
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
        dataSource={conversions}
        rowKey="id"
        size="small"
        pagination={false}
        columns={[
          {
            title: 'Origen', key: 'origin',
            render: (_: unknown, c: UnitConversionDto) => `${c.originUnitName} (${c.originUnitSymbol})`,
          },
          {
            title: 'Destino', key: 'destination',
            render: (_: unknown, c: UnitConversionDto) => `${c.destinationUnitName} (${c.destinationUnitSymbol})`,
          },
          {
            title: 'Factor', dataIndex: 'factor', key: 'factor',
            render: (v: number) => `1 origen = ${v} destino`,
          },
          {
            title: '', key: 'actions', width: 60,
            render: (_: unknown, c: UnitConversionDto) => (
              <Popconfirm title="¿Eliminar?" onConfirm={() => deleteConversion(c.id)}>
                <Button size="small" danger icon={<DeleteOutlined />} />
              </Popconfirm>
            ),
          },
        ]}
      />

      <Modal
        title={editingUnit ? 'Editar unidad' : 'Nueva unidad de medida'}
        open={modalUnit}
        onOk={saveUnit}
        onCancel={() => setModalUnit(false)}
        okText="Guardar"
      >
        <Form form={formUnit} layout="vertical">
          <Form.Item name="name" label="Nombre" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="symbol" label="Símbolo" rules={[{ required: true }]}>
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
          <Form.Item name="originUnitId" label="Unidad origen" rules={[{ required: true }]}>
            <Select options={unitOptions} placeholder="Seleccionar" />
          </Form.Item>
          <Form.Item name="destinationUnitId" label="Unidad destino" rules={[{ required: true }]}>
            <Select options={unitOptions} placeholder="Seleccionar" />
          </Form.Item>
          <Form.Item name="factor" label="Factor (1 origen = X destino)" rules={[{ required: true }]}>
            <InputNumber style={{ width: '100%' }} step={0.000001} min={0} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
