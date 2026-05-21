import { useEffect, useState } from 'react';
import { Table, Button, Modal, Form, Input, Select, Switch, Popconfirm, Space, Typography, message, Tag } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { posApi } from '../../services/api';
import type { WorkStationDto, StationType } from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Title } = Typography;

const TIPOS_ESTACION: { label: string; value: StationType; color: string }[] = [
  { label: 'Kitchen', value: 'Kitchen', color: 'orange' },
  { label: 'Bar', value: 'Bar', color: 'purple' },
  { label: 'Beverages', value: 'Beverages', color: 'blue' },
  { label: 'Cocina Caliente / Parrilla', value: 'HotKitchen', color: 'red' },
  { label: 'Fries', value: 'Fries', color: 'gold' },
];

const tipoLabel = (tipo: StationType) => TIPOS_ESTACION.find(t => t.value === tipo)?.label ?? tipo;
const tipoColor = (tipo: StationType) => TIPOS_ESTACION.find(t => t.value === tipo)?.color ?? 'default';

export default function StationsConfig() {
  const [estaciones, setEstaciones] = useState<WorkStationDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<WorkStationDto | null>(null);
  const [form] = Form.useForm();

  const load = async () => {
    setLoading(true);
    try { setEstaciones((await posApi.getStations()).data); }
    catch (e) { message.error(formatError(e)); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const openModal = (e?: WorkStationDto) => {
    setEditing(e ?? null);
    form.setFieldsValue(e ?? { name: '', type: 'Kitchen', isActive: true });
    setModal(true);
  };

  const save = async () => {
    const values = await form.validateFields();
    try {
      if (editing) {
        await posApi.updateStation(editing.id, values);
      } else {
        await posApi.createStation(values);
      }
      message.success('Guardado');
      setModal(false);
      load();
    } catch (e) { message.error(formatError(e)); }
  };

  const remove = async (id: string) => {
    try { await posApi.deleteStation(id); message.success('Eliminado'); load(); }
    catch (e) { message.error(formatError(e)); }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Estaciones de Trabajo</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>Nueva estación</Button>
      </div>

      <Table
        dataSource={estaciones}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={{ defaultPageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
        columns={[
          { title: 'Nombre', dataIndex: 'name', key: 'name' },
          {
            title: 'Tipo', dataIndex: 'type', key: 'type',
            render: (tipo: StationType) => <Tag color={tipoColor(tipo)}>{tipoLabel(tipo)}</Tag>,
          },
          {
            title: 'Estado', dataIndex: 'isActive', key: 'isActive', width: 90,
            render: (v: boolean) => <Tag color={v ? 'green' : 'default'}>{v ? 'Activa' : 'Inactiva'}</Tag>,
          },
          {
            title: 'Acciones', key: 'acc', width: 100,
            render: (_: unknown, e: WorkStationDto) => (
              <Space>
                <Button size="small" icon={<EditOutlined />} onClick={() => openModal(e)} />
                <Popconfirm title="¿Eliminar?" onConfirm={() => remove(e.id)}>
                  <Button size="small" danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Editar estación' : 'Nueva estación'}
        open={modal}
        onOk={save}
        onCancel={() => setModal(false)}
        okText="Guardar"
      >
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="Nombre" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="type" label="Tipo" rules={[{ required: true }]}>
            <Select options={TIPOS_ESTACION.map(t => ({ label: t.label, value: t.value }))} />
          </Form.Item>
          {editing && (
            <Form.Item name="isActive" label="Activa" valuePropName="checked"><Switch /></Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  );
}
