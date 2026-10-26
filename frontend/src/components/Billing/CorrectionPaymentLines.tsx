import { Button, Col, Form, Input, InputNumber, Row, Select, Tooltip } from 'antd';
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import type { CardBankDto, PaymentMethodConfigDto } from '../../types';

function CorrectionLine({ name, remove, removable, methods, banks, disabled }: {
  name: number; remove: () => void; removable: boolean; disabled: boolean;
  methods: PaymentMethodConfigDto[]; banks: CardBankDto[];
}) {
  const form = Form.useFormInstance();
  const methodId = Form.useWatch(['lines', name, 'methodId'], form);
  const isCard = methods.find(m => m.id === methodId)?.isCard;
  const index = name + 1;
  return <div style={{ borderBottom: '1px solid #e5e7eb', paddingTop: 12, marginBottom: 12 }}>
    <Row gutter={12} align="top">
      <Col xs={24} sm={13}>
        <Form.Item name={[name, 'methodId']} label={`Medio de pago ${index}`} rules={[{ required: true, message: 'Selecciona el medio' }]}>
          <Select options={methods.map(m => ({ value: m.id, label: m.name }))} onChange={value => {
            const amount = form.getFieldValue(['lines', name, 'amount']);
            form.setFieldValue(['lines', name], { methodId: value, amount });
          }} />
        </Form.Item>
      </Col>
      <Col xs={19} sm={8}>
        <Form.Item name={[name, 'amount']} label={`Importe ${index}`} rules={[{ required: true, message: 'Ingresa el importe' },
          { type: 'number', min: 0.01, message: 'El importe debe ser positivo' }]}>
          <InputNumber prefix="$" min={0.01} precision={2} step={0.01} style={{ width: '100%' }} />
        </Form.Item>
      </Col>
      <Col xs={5} sm={3} style={{ paddingTop: 30, textAlign: 'right' }}>
        <Tooltip title="Quitar medio de pago"><Button danger icon={<DeleteOutlined />} disabled={disabled || !removable}
          aria-label={`Quitar medio de pago ${index}`} onClick={remove} /></Tooltip>
      </Col>
    </Row>
    {isCard && <Row gutter={12}>
      <Col xs={24} sm={12}><Form.Item name={[name, 'cardPaymentType']} label={`Tipo de tarjeta ${index}`}
        rules={[{ required: true, message: 'Selecciona credito o debito' }]}>
        <Select options={[{ value: 'Credit', label: 'Credito' }, { value: 'Debit', label: 'Debito' }]} />
      </Form.Item></Col>
      <Col xs={24} sm={12}><Form.Item name={[name, 'cardBankId']} label={`Banco ${index}`}
        rules={[{ required: true, message: 'Selecciona el banco' }]}>
        <Select showSearch optionFilterProp="label" options={banks.filter(b => b.isActive).map(b => ({ value: b.id, label: b.name }))} />
      </Form.Item></Col>
      <Col xs={24} sm={12}><Form.Item name={[name, 'cardBrand']} label={`Marca ${index}`}
        rules={[{ required: true, message: 'Selecciona la marca' }]}>
        <Select options={['Visa', 'Mastercard', 'Diners', 'American Express', 'Discover', 'Otro'].map(value => ({ value, label: value }))} />
      </Form.Item></Col>
      <Col xs={24} sm={12}><Form.Item name={[name, 'authorizationNumber']} label={`Autorizacion ${index}`}
        rules={[{ required: true, whitespace: true, message: 'Ingresa la autorizacion' }]}>
        <Input maxLength={50} />
      </Form.Item></Col>
    </Row>}
  </div>;
}

export default function CorrectionPaymentLines({ methods, banks, disabled }: {
  methods: PaymentMethodConfigDto[]; banks: CardBankDto[]; disabled: boolean;
}) {
  return <Form.List name="lines" rules={[{ validator: async (_, lines) => {
    if (!lines?.length || lines.length > 10) throw new Error('Selecciona de 1 a 10 medios de pago.');
  } }]}>
    {(fields, { add, remove }, { errors }) => <>
      {fields.map(field => <CorrectionLine key={field.key} name={field.name} methods={methods} banks={banks}
        disabled={disabled} removable remove={() => remove(field.name)} />)}
      <Button icon={<PlusOutlined />} aria-label="Agregar medio" disabled={disabled || fields.length >= 10}
        onClick={() => add({})} style={{ marginBottom: 12 }}>Agregar medio</Button>
      <Form.ErrorList errors={errors} />
    </>}
  </Form.List>;
}
