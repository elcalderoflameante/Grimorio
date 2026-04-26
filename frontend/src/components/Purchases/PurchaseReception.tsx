import { useState, useEffect } from 'react';
import { Modal, Table, InputNumber, Select, message, Alert } from 'antd';
import type { PurchaseOrderDto, WarehouseDto, ReceptionItemDto } from '../../types';
import { purchasesApi, inventoryApi } from '../../services/api';

interface Props {
  open: boolean;
  orden: PurchaseOrderDto;
  onClose: () => void;
  onSaved: () => void;
}

interface RecepcionRow {
  purchaseOrderItemId: string;
  articleName: string;
  internalCode?: string;
  unitSymbol: string;
  quantityOrdered: number;
  quantityReceived: number;
}

export default function PurchaseReception({ open, orden, onClose, onSaved }: Props) {
  const [bodegas, setBodegas] = useState<WarehouseDto[]>([]);
  const [bodegaId, setBodegaId] = useState<string | undefined>(orden.destinationWarehouseId ?? undefined);
  const [rows, setRows] = useState<RecepcionRow[]>([]);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    inventoryApi.getWarehouses().then(res => {
      const activas = res.data.filter((b: WarehouseDto) => b.isActive);
      setBodegas(activas);
    });
  }, []);

  useEffect(() => {
    if (!open) return;
    setBodegaId(orden.destinationWarehouseId ?? undefined);
    setRows(orden.items.map(i => ({
      purchaseOrderItemId: i.id,
      articleName: i.articleName,
      internalCode: i.internalCode,
      unitSymbol: i.unitSymbol,
      quantityOrdered: i.quantityOrdered,
      quantityReceived: i.quantityOrdered, // default to full quantity
    })));
  }, [open, orden]);

  const updateCantidad = (id: string, val: number) => {
    setRows(prev => prev.map(r => r.purchaseOrderItemId === id ? { ...r, quantityReceived: val } : r));
  };

  const handleConfirmar = async () => {
    if (!bodegaId) { message.warning('Selecciona la bodega de destino'); return; }

    const items: ReceptionItemDto[] = rows
      .filter(r => r.quantityReceived > 0)
      .map(r => ({ purchaseOrderItemId: r.purchaseOrderItemId, quantityReceived: r.quantityReceived }));

    if (items.length === 0) { message.warning('Ingresa al menos una cantidad recibida'); return; }

    setSaving(true);
    try {
      await purchasesApi.receiveOrder(orden.id, { warehouseId: bodegaId, items });
      message.success('Recepción registrada — stock actualizado');
      onSaved();
    } catch {
      message.error('Error al registrar la recepción');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      open={open}
      onCancel={onClose}
      onOk={handleConfirmar}
      okText="Confirmar recepción"
      cancelText="Cancelar"
      confirmLoading={saving}
      title={`Recibir orden ${orden.orderNumber}`}
      width={680}
      okButtonProps={{ disabled: !bodegaId }}
    >
      <Alert
        type="info"
        message="Al confirmar, se registrará una entrada de stock en la bodega seleccionada por cada ítem recibido."
        style={{ marginBottom: 16 }}
        showIcon
      />

      <div style={{ marginBottom: 16 }}>
        <strong>Bodega de destino: </strong>
        <Select
          style={{ width: 240 }}
          value={bodegaId}
          onChange={setBodegaId}
          options={bodegas.map(b => ({ value: b.id, label: b.name }))}
          placeholder="Seleccionar bodega"
        />
      </div>

      <Table
        size="small"
        rowKey="purchaseOrderItemId"
        dataSource={rows}
        pagination={false}
        columns={[
          { title: 'Artículo', dataIndex: 'articleName' },
          { title: 'Código', dataIndex: 'internalCode', render: (v?: string) => v ?? '—', width: 100 },
          { title: 'Unidad', dataIndex: 'unitSymbol', width: 80 },
          { title: 'Pedido', dataIndex: 'quantityOrdered', align: 'right', width: 90 },
          {
            title: 'Recibido',
            key: 'quantityReceived',
            width: 120,
            render: (_: unknown, row: RecepcionRow) => (
              <InputNumber
                min={0}
                max={row.quantityOrdered * 2}
                step={1}
                value={row.quantityReceived}
                onChange={v => updateCantidad(row.purchaseOrderItemId, v ?? 0)}
                style={{ width: '100%' }}
              />
            ),
          },
        ]}
      />
    </Modal>
  );
}
