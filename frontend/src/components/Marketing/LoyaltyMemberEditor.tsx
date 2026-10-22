import { useState } from 'react';
import { App, Checkbox, Col, Form, Input, InputNumber, Modal, Row, Select } from 'antd';
import { loyaltyApi } from '../../services/loyaltyApi';
import type { LoyaltyDetail, LoyaltyMember, LoyaltyMemberProfile } from '../../services/loyaltyApi';
import type { CustomerDto } from '../../types';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import { formatError } from '../../utils/errorHandler';
import CustomerSelector from '../Billing/CustomerSelector';

const months = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];
type ProfileForm = LoyaltyMemberProfile & { acceptsProgram?: boolean };

export default function LoyaltyMemberEditor({ member, onClose, onSaved }: {
  member?: LoyaltyMember; onClose: () => void; onSaved: (detail: LoyaltyDetail) => void;
}) {
  const { message } = App.useApp();
  const { hasPermission, branchId } = useAuth();
  const [form] = Form.useForm<ProfileForm>();
  const [saving, setSaving] = useState(false);
  const [customer, setCustomer] = useState<CustomerDto | null>(member?.customerId ? {
    id: member.customerId, name: member.billingCustomerName ?? member.customerName, isActive: true, taxIdType: 'Cedula',
  } : null);
  const save = async () => {
    const values = await form.validateFields().catch(() => null);
    if (!values || saving) return;
    setSaving(true);
    const profile: LoyaltyMemberProfile = {
      ...values, name: values.name.trim(), phone: values.phone.trim(), email: values.email?.trim() || null,
      birthDay: values.birthDay ?? null, birthMonth: values.birthMonth ?? null, customerId: customer?.id ?? null,
      marketingConsent: values.marketingConsent ?? false,
    };
    try {
      const result = member ? await loyaltyApi.updateMember(member.id, profile)
        : await loyaltyApi.enroll({ ...profile, acceptsProgram: !!values.acceptsProgram });
      message.success(member ? 'Ficha actualizada' : 'Miembro inscrito'); onSaved(result.data);
    } catch (error) { message.error(formatError(error)); }
    finally { setSaving(false); }
  };
  return <Modal open title={member ? 'Actualizar ficha de fidelizacion' : 'Inscribir miembro'} width={680}
    onCancel={() => !saving && onClose()} onOk={() => void save()} confirmLoading={saving} closable={!saving} maskClosable={!saving}
    okText={member ? 'Guardar' : 'Inscribir'} cancelText="Cancelar" cancelButtonProps={{ disabled: saving }}>
    <Form name="loyalty-member-profile" form={form} layout="vertical" disabled={saving} initialValues={{ name: member?.customerName ?? '', phone: member?.phone ?? '',
      email: member?.email ?? '', birthDay: member?.birthDay, birthMonth: member?.birthMonth, marketingConsent: member?.marketingConsent ?? false }}>
      <Row gutter={16}>
        <Col xs={24} sm={12}><Form.Item name="phone" label="Telefono" rules={[{ required: true, whitespace: true, message: 'Ingresa el telefono' }, { max: 40 }]}>
          <Input type="tel" autoComplete="tel" placeholder="0991234567 o +593991234567" maxLength={40} />
        </Form.Item></Col>
        <Col xs={24} sm={12}><Form.Item name="name" label="Nombre" rules={[{ required: true, whitespace: true, message: 'Ingresa el nombre' }, { max: 200 }]}>
          <Input autoComplete="name" maxLength={200} />
        </Form.Item></Col>
        <Col xs={24} sm={12}><Form.Item name="email" label="Correo (opcional)" rules={[{ type: 'email', message: 'Revisa el correo' }, { max: 254 }]}>
          <Input type="email" autoComplete="email" maxLength={254} />
        </Form.Item></Col>
        <Col xs={24} sm={12}>
          <Form.Item label="Cumpleanos (opcional)">
          <Row gutter={12}>
            <Col span={10}><Form.Item name="birthDay" noStyle dependencies={['birthMonth']} rules={[({ getFieldValue }) => ({
              validator(_, day: number | null | undefined) {
                const month = getFieldValue('birthMonth') as number | null | undefined;
                if (!day && !month) return Promise.resolve();
                if (day && month && day >= 1 && Number.isInteger(day) && day <= new Date(2000, month, 0).getDate()) return Promise.resolve();
                return Promise.reject(new Error('Completa un dia y mes validos'));
              },
            })]}><InputNumber aria-label="Dia de cumpleanos" placeholder="Dia" min={1} max={31} precision={0} style={{ width: '100%' }} /></Form.Item></Col>
            <Col span={14}><Form.Item name="birthMonth" noStyle><Select aria-label="Mes de cumpleanos" placeholder="Mes" allowClear options={months.map((label, index) => ({ value: index + 1, label }))} /></Form.Item></Col>
          </Row>
          </Form.Item>
        </Col>
      </Row>
      {hasPermission(PERMISSIONS.billing.customersView) && <Form.Item label="Ficha de facturacion vinculada (opcional)">
        <CustomerSelector branchId={branchId ?? ''} value={customer} allowCreate={false} excludeFinalConsumer ariaLabel="Ficha de facturacion vinculada"
          onChange={selected => {
            setCustomer(selected);
            if (!selected) return;
            form.setFieldsValue({
              ...(!form.getFieldValue('name')?.trim() ? { name: selected.name } : {}),
              ...(!form.getFieldValue('phone')?.trim() && selected.phone ? { phone: selected.phone } : {}),
              ...(!form.getFieldValue('email')?.trim() && selected.email ? { email: selected.email } : {}),
            });
          }} />
      </Form.Item>}
      {!member && <Form.Item name="acceptsProgram" valuePropName="checked" rules={[{ validator: (_, value) => value ? Promise.resolve() : Promise.reject(new Error('Confirma la aceptacion del programa')) }]}>
        <Checkbox>El cliente acepta participar en el programa</Checkbox>
      </Form.Item>}
      <Form.Item name="marketingConsent" valuePropName="checked" style={{ marginBottom: 0 }}>
        <Checkbox>Autoriza recibir comunicaciones comerciales (opcional)</Checkbox>
      </Form.Item>
    </Form>
  </Modal>;
}
