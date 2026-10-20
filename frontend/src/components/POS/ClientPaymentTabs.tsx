import type { ReactNode } from 'react';
import { Button, Empty, Popconfirm, Space, Tabs, Tag, Tooltip, Typography } from 'antd';
import { CheckCircleOutlined, DeleteOutlined, PlusOutlined, PrinterOutlined } from '@ant-design/icons';
import type { ClientPaymentAccount } from '../../utils/clientPaymentAccounts';

interface Props {
  accounts: ClientPaymentAccount[];
  activeId: number;
  disabled: boolean;
  canAdd: boolean;
  onChange: (id: number) => void;
  onAdd: () => void;
  onRemove: (id: number) => void;
  onPrint: (id: string) => void;
  children: ReactNode;
}

export default function ClientPaymentTabs({ accounts, activeId, disabled, canAdd, onChange, onAdd, onRemove, onPrint, children }: Props) {
  const active = accounts.find(a => a.id === activeId);
  const payment = active?.payment;
  return <>
    <Tabs size="small" activeKey={String(activeId)} onChange={key => !disabled && onChange(Number(key))}
      tabBarExtraContent={<Tooltip title="Agregar cliente"><Button size="small" icon={<PlusOutlined />} aria-label="Agregar cliente"
        disabled={disabled || !canAdd} onClick={onAdd} /></Tooltip>}
      items={accounts.map(account => ({ key: String(account.id), disabled,
        label: <Space size={4}><span>Cliente {account.id}</span>{account.payment && <CheckCircleOutlined style={{ color: '#389e0d' }} />}</Space>,
      }))} />
    {payment ? (
      <div aria-label={`Cobro de Cliente ${activeId}`}>
        <Space wrap style={{ marginBottom: 12 }}><Tag color="success">Pagado</Tag>
          <Typography.Text strong>${payment.orderAmount.toFixed(2)}</Typography.Text>
          <Tag>{payment.documentType === 'Factura' ? 'Factura' : 'Nota de venta'}</Tag>
        </Space>
        {payment.customerName && <div style={{ marginBottom: 8, overflowWrap: 'anywhere' }}>{payment.customerName}</div>}
        {payment.items.map(item => <div key={item.id} style={{ display: 'flex', gap: 8, padding: '6px 0', borderBottom: '1px solid #f0f0f0' }}>
          <div style={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}>{item.quantity} x {item.itemName}
            {item.modifierSelections?.length ? <div style={{ fontSize: 12, color: '#595959' }}>
              {item.modifierSelections.map(s => `${s.optionName}${s.quantity > 1 ? ` x${s.quantity}` : ''}`).join(', ')}
            </div> : null}
          </div><strong>${item.total.toFixed(2)}</strong>
        </div>)}
        <Space wrap style={{ margin: '12px 0' }}>{payment.lines.map(line => <Tag key={line.id}>
          {line.methodName}: ${line.netAmount.toFixed(2)}
        </Tag>)}</Space>
        <Button block icon={<PrinterOutlined />} disabled={disabled} onClick={() => onPrint(payment.id)}>Imprimir comprobante</Button>
      </div>
    ) : active ? <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
        <Typography.Text strong>Cliente {activeId}</Typography.Text>
        <Popconfirm title={`Eliminar Cliente ${activeId}?`} description="Sus platos quedaran disponibles para los otros clientes."
          onConfirm={() => onRemove(activeId)} okText="Eliminar" cancelText="Volver" disabled={disabled}>
          <Tooltip title="Eliminar cliente"><Button size="small" danger icon={<DeleteOutlined />} aria-label="Eliminar cliente" disabled={disabled} /></Tooltip>
        </Popconfirm>
      </div>
      {children}
    </> : <Empty description="Sin clientes pendientes" image={Empty.PRESENTED_IMAGE_SIMPLE} />}
  </>;
}
