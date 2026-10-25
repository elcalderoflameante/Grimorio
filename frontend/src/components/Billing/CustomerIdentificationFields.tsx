import { useEffect } from 'react';
import { Col, Form, Input, Select } from 'antd';
import { customersApi } from '../../services/api';
import { customerIdentificationError, detectCustomerId, normalizeCustomerId } from '../../utils/customerIdentification';

export default function CustomerIdentificationFields({ customerId }: { customerId?: string }) {
  const form = Form.useFormInstance();
  const number = Form.useWatch('taxId', form) as string | undefined;
  const type = Form.useWatch('taxIdType', form) as string | undefined;
  useEffect(() => {
    const detected = detectCustomerId(number);
    if (type !== 'Passport' && detected && type !== detected) form.setFieldValue('taxIdType', detected);
  }, [form, number, type]);

  return <>
    <Col xs={24} md={12}>
      <Form.Item name="taxIdType" label="Tipo de identificacion" rules={[{ required: true }]}>
        <Select options={[{ value: 'Cedula', label: 'Cedula' }, { value: 'Ruc', label: 'RUC' },
          { value: 'Passport', label: 'Pasaporte' }, { value: 'FinalConsumer', label: 'Consumidor final' }]} />
      </Form.Item>
    </Col>
    <Col xs={24} md={12}>
      <Form.Item name="taxId" label="Numero de identificacion" validateTrigger="onBlur" dependencies={['taxIdType']}
        normalize={normalizeCustomerId} rules={[{ validator: async (_, value: string | undefined) => {
          const currentType = form.getFieldValue('taxIdType') ?? 'FinalConsumer';
          const error = customerIdentificationError(value, currentType);
          if (error) throw new Error(error);
          const id = normalizeCustomerId(value);
          if (!id || id === '9999999999999') return;
          // The server remains authoritative if listing customers is unavailable or not permitted.
          const response = await customersApi.getAll().catch(() => null);
          if (!response) return;
          const duplicate = response.data.find(c => c.id !== customerId && normalizeCustomerId(c.taxId) === id);
          if (duplicate) throw new Error(`Ya existe: ${duplicate.name}${duplicate.isActive ? '' : ' (inactivo)'}. Utiliza o actualiza ese registro.`);
        } }]}>
        <Input maxLength={24} autoComplete="off" />
      </Form.Item>
    </Col>
  </>;
}
