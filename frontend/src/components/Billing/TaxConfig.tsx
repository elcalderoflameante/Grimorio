import { useCallback, useEffect, useState } from 'react';
import {
  Table, Button, Modal, Form, Input, Switch, InputNumber,
  Space, Tag, Popconfirm, Typography, message, Card, Divider, Select, Row, Col,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, PercentageOutlined, BankOutlined } from '@ant-design/icons';
import type { TaxRateDto, UpsertTaxRateDto, BranchTaxConfigDto } from '../../types';
import { taxApi } from '../../services/api';

const { Text } = Typography;

const SRI_CODES = [
  { value: '10', label: 'IVA 15% (código 10)' },
  { value: '8',  label: 'IVA 5% (código 8)' },
  { value: '0',  label: 'IVA 0% / No objeto (código 0)' },
  { value: '6',  label: 'No objeto de IVA (código 6)' },
  { value: '7',  label: 'Exento de IVA (código 7)' },
];

export default function TaxConfig() {
  const [rates, setRates]       = useState<TaxRateDto[]>([]);
  const [loadingRates, setLoadingRates] = useState(true);
  const [rateModalOpen, setRateModalOpen] = useState(false);
  const [editingRate, setEditingRate] = useState<TaxRateDto | null>(null);
  const [savingRate, setSavingRate] = useState(false);
  const [rateForm] = Form.useForm();

  const [loadingConfig, setLoadingConfig] = useState(true);
  const [savingConfig, setSavingConfig] = useState(false);
  const [configForm] = Form.useForm();

  const loadRates = useCallback(async () => {
    setLoadingRates(true);
    try {
      const r = await taxApi.getTaxRates();
      setRates(r.data);
    } catch {
      message.error('Error al cargar tarifas de IVA');
    } finally {
      setLoadingRates(false);
    }
  }, []);

  const loadConfig = useCallback(async () => {
    setLoadingConfig(true);
    try {
      const r = await taxApi.getConfig();
      configForm.setFieldsValue(r.data);
    } catch {
      // 404 = no config aún, es normal
    } finally {
      setLoadingConfig(false);
    }
  }, [configForm]);

  useEffect(() => {
    loadRates();
    loadConfig();
  }, [loadConfig, loadRates]);

  // ── Tarifas ──

  const openCreateRate = () => {
    setEditingRate(null);
    rateForm.resetFields();
    rateForm.setFieldsValue({ isDefault: false, isActive: true, sriCode: '10', percentage: 15 });
    setRateModalOpen(true);
  };

  const openEditRate = (r: TaxRateDto) => {
    setEditingRate(r);
    rateForm.setFieldsValue(r);
    setRateModalOpen(true);
  };

  const handleSaveRate = async () => {
    const values = await rateForm.validateFields() as UpsertTaxRateDto;
    setSavingRate(true);
    try {
      if (editingRate) {
        await taxApi.updateTaxRate(editingRate.id, values);
        message.success('Tarifa actualizada');
      } else {
        await taxApi.createTaxRate(values);
        message.success('Tarifa creada');
      }
      setRateModalOpen(false);
      loadRates();
    } catch {
      message.error('Error al guardar tarifa');
    } finally {
      setSavingRate(false);
    }
  };

  const handleDeleteRate = async (id: string) => {
    try {
      await taxApi.deleteTaxRate(id);
      message.success('Tarifa eliminada');
      loadRates();
    } catch {
      message.error('Error al eliminar tarifa');
    }
  };

  // ── Config fiscal ──

  const handleSaveConfig = async () => {
    const values = await configForm.validateFields() as BranchTaxConfigDto;
    setSavingConfig(true);
    try {
      await taxApi.upsertConfig(values);
      message.success('Configuración fiscal guardada');
      loadConfig();
    } catch {
      message.error('Error al guardar configuración fiscal');
    } finally {
      setSavingConfig(false);
    }
  };

  const rateColumns = [
    {
      title: 'Nombre',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: 'Porcentaje',
      dataIndex: 'percentage',
      key: 'percentage',
      render: (v: number) => `${v}%`,
    },
    {
      title: 'Código SRI',
      dataIndex: 'sriCode',
      key: 'sriCode',
      render: (v: string) => <Tag>{v}</Tag>,
    },
    {
      title: 'Por defecto',
      dataIndex: 'isDefault',
      key: 'isDefault',
      render: (v: boolean) => v ? <Tag color="blue">Sí</Tag> : null,
    },
    {
      title: 'Estado',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (v: boolean) => <Tag color={v ? 'green' : 'red'}>{v ? 'Activo' : 'Inactivo'}</Tag>,
    },
    {
      title: 'Acciones',
      key: 'actions',
      render: (_: unknown, record: TaxRateDto) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEditRate(record)} />
          <Popconfirm title="¿Eliminar tarifa?" onConfirm={() => handleDeleteRate(record.id)} okText="Sí" cancelText="No">
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: 24, maxWidth: 900 }}>
      {/* ── Tarifas de IVA ── */}
      <Card
        title={<Space><PercentageOutlined /><Text strong>Tarifas de IVA</Text></Space>}
        extra={<Button type="primary" icon={<PlusOutlined />} onClick={openCreateRate}>Nueva tarifa</Button>}
        style={{ marginBottom: 24 }}
      >
        <Table
          dataSource={rates}
          columns={rateColumns}
          rowKey="id"
          loading={loadingRates}
          pagination={false}
          size="small"
        />
      </Card>

      {/* ── Configuración fiscal SRI ── */}
      <Card
        title={<Space><BankOutlined /><Text strong>Datos fiscales (SRI)</Text></Space>}
        loading={loadingConfig}
      >
        {!loadingConfig && (
          <Form form={configForm} layout="vertical" onFinish={handleSaveConfig}>
            <Row gutter={16}>
              <Col xs={24} md={12}>
                <Form.Item name="ruc" label="RUC" rules={[{ required: true, message: 'Ingrese el RUC' }]}>
                  <Input maxLength={13} placeholder="Ej: 1234567890001" />
                </Form.Item>
              </Col>
              <Col xs={24} md={12}>
                <Form.Item name="razonSocial" label="Razón social" rules={[{ required: true }]}>
                  <Input />
                </Form.Item>
              </Col>
              <Col xs={24} md={12}>
                <Form.Item name="nombreComercial" label="Nombre comercial">
                  <Input />
                </Form.Item>
              </Col>
              <Col xs={24} md={12}>
                <Form.Item name="direccion" label="Dirección" rules={[{ required: true }]}>
                  <Input />
                </Form.Item>
              </Col>
              <Col xs={24} md={8}>
                <Form.Item name="codigoEstablecimiento" label="Código establecimiento" rules={[{ required: true }]}>
                  <Input maxLength={3} placeholder="001" />
                </Form.Item>
              </Col>
              <Col xs={24} md={8}>
                <Form.Item name="puntoEmision" label="Punto de emisión" rules={[{ required: true }]}>
                  <Input maxLength={3} placeholder="001" />
                </Form.Item>
              </Col>
              <Col xs={24} md={8}>
                <Form.Item name="ambiente" label="Ambiente SRI" rules={[{ required: true }]}>
                  <Select options={[{ value: '1', label: 'Pruebas' }, { value: '2', label: 'Producción' }]} />
                </Form.Item>
              </Col>
            </Row>
            <Divider />
            <Button type="primary" htmlType="submit" loading={savingConfig}>
              Guardar configuración fiscal
            </Button>
          </Form>
        )}
      </Card>

      {/* ── Modal tarifa ── */}
      <Modal
        title={editingRate ? 'Editar tarifa de IVA' : 'Nueva tarifa de IVA'}
        open={rateModalOpen}
        onOk={handleSaveRate}
        onCancel={() => setRateModalOpen(false)}
        confirmLoading={savingRate}
        okText="Guardar"
        cancelText="Cancelar"
      >
        <Form form={rateForm} layout="vertical">
          <Form.Item name="name" label="Nombre" rules={[{ required: true }]}>
            <Input placeholder="Ej: IVA 15%" />
          </Form.Item>
          <Form.Item name="sriCode" label="Código SRI" rules={[{ required: true }]}>
            <Select
              options={SRI_CODES}
              onChange={(val) => {
                const pct = val === '10' ? 15 : val === '8' ? 5 : 0;
                rateForm.setFieldValue('percentage', pct);
              }}
            />
          </Form.Item>
          <Form.Item name="percentage" label="Porcentaje (%)" rules={[{ required: true }]}>
            <InputNumber min={0} max={100} step={0.01} style={{ width: '100%' }} addonAfter="%" />
          </Form.Item>
          <Space>
            <Form.Item name="isDefault" valuePropName="checked" label="Por defecto">
              <Switch />
            </Form.Item>
            <Form.Item name="isActive" valuePropName="checked" label="Activo">
              <Switch />
            </Form.Item>
          </Space>
        </Form>
      </Modal>
    </div>
  );
}
