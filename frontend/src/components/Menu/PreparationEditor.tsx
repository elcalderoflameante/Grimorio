import { useEffect, useState } from 'react';
import { App as AntApp, Button, Input, InputNumber, Modal, Popconfirm, Space, Switch, Tag, Typography } from 'antd';
import { DeleteOutlined, PlusOutlined, SaveOutlined } from '@ant-design/icons';
import { menuApi } from '../../services/api';
import type { UpsertMenuItemPreparationDto, UpsertMenuItemPreparationStepDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Text } = Typography;

interface Props {
  itemId: string;
  itemName: string;
  open: boolean;
  onClose: () => void;
}

const emptyStep = (stepNumber: number): UpsertMenuItemPreparationStepDto => ({
  stepNumber,
  instructions: '',
  estimatedMinutes: undefined,
  temperature: '',
  isCritical: false,
});

const emptyPreparation = (): UpsertMenuItemPreparationDto => ({
  estimatedMinutes: undefined,
  yield: '',
  temperature: '',
  presentation: '',
  notes: '',
  steps: [emptyStep(1)],
});

export default function PreparationEditor({ itemId, itemName, open, onClose }: Props) {
  const { message } = AntApp.useApp();
  const [preparation, setPreparation] = useState<UpsertMenuItemPreparationDto>(emptyPreparation());
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!open) return;
    const load = async () => {
      try {
        const detail = await menuApi.getItem(itemId);
        const current = detail.data.preparation;
        setPreparation(current ? {
          estimatedMinutes: current.estimatedMinutes,
          yield: current.yield,
          temperature: current.temperature,
          presentation: current.presentation,
          notes: current.notes,
          steps: current.steps.length > 0
            ? current.steps.map(s => ({
              id: s.id,
              stepNumber: s.stepNumber,
              instructions: s.instructions,
              estimatedMinutes: s.estimatedMinutes,
              temperature: s.temperature,
              isCritical: s.isCritical,
            }))
            : [emptyStep(1)],
        } : emptyPreparation());
      } catch (e) { message.error(formatError(e)); }
    };
    load();
  }, [itemId, open]);

  const updatePreparation = (patch: Partial<UpsertMenuItemPreparationDto>) =>
    setPreparation(prev => ({ ...prev, ...patch }));

  const updateStep = (idx: number, patch: Partial<UpsertMenuItemPreparationStepDto>) =>
    setPreparation(prev => ({
      ...prev,
      steps: prev.steps.map((step, i) => i === idx ? { ...step, ...patch } : step),
    }));

  const removeStep = (idx: number) =>
    setPreparation(prev => ({
      ...prev,
      steps: prev.steps
        .filter((_, i) => i !== idx)
        .map((step, i) => ({ ...step, stepNumber: i + 1 })),
    }));

  const addStep = () =>
    setPreparation(prev => ({ ...prev, steps: [...prev.steps, emptyStep(prev.steps.length + 1)] }));

  const save = async () => {
    const steps = preparation.steps
      .map((step, idx) => ({ ...step, stepNumber: idx + 1, instructions: step.instructions.trim() }))
      .filter(step => step.instructions.length > 0);

    if (steps.length === 0) {
      message.warning('Agrega al menos un paso con instrucciones.');
      return;
    }

    setSaving(true);
    try {
      await menuApi.upsertPreparation(itemId, { ...preparation, steps });
      message.success('Preparación guardada');
      onClose();
    } catch (e) { message.error(formatError(e)); }
    finally { setSaving(false); }
  };

  return (
    <Modal
      title={`Preparación - ${itemName}`}
      open={open}
      onCancel={onClose}
      width={900}
      footer={[
        <Button key="cancel" onClick={onClose}>Cancelar</Button>,
        <Button key="save" type="primary" icon={<SaveOutlined />} loading={saving} onClick={save}>
          Guardar preparación
        </Button>,
      ]}
    >
      <Text type="secondary" style={{ display: 'block', marginBottom: 14 }}>
        Documenta el procedimiento operativo para que cualquier persona pueda preparar el plato con el mismo estándar.
      </Text>

      <div style={{ display: 'grid', gridTemplateColumns: '150px minmax(150px, 1fr) minmax(150px, 1fr)', gap: 10, marginBottom: 10 }}>
        <div>
          <Text type="secondary">Tiempo total</Text>
          <InputNumber
            min={0}
            value={preparation.estimatedMinutes}
            onChange={v => updatePreparation({ estimatedMinutes: v ?? undefined })}
            addonAfter="min"
            style={{ width: '100%', marginTop: 4 }}
          />
        </div>
        <div>
          <Text type="secondary">Rendimiento / porción</Text>
          <Input value={preparation.yield} onChange={e => updatePreparation({ yield: e.target.value })} placeholder="Ej: 1 plato" style={{ marginTop: 4 }} />
        </div>
        <div>
          <Text type="secondary">Temperatura general</Text>
          <Input value={preparation.temperature} onChange={e => updatePreparation({ temperature: e.target.value })} placeholder="Ej: 180 C" style={{ marginTop: 4 }} />
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10, marginBottom: 14 }}>
        <div>
          <Text type="secondary">Montaje / presentación</Text>
          <Input.TextArea value={preparation.presentation} onChange={e => updatePreparation({ presentation: e.target.value })} rows={3} style={{ marginTop: 4 }} />
        </div>
        <div>
          <Text type="secondary">Notas internas</Text>
          <Input.TextArea value={preparation.notes} onChange={e => updatePreparation({ notes: e.target.value })} rows={3} style={{ marginTop: 4 }} />
        </div>
      </div>

      <Space direction="vertical" style={{ width: '100%' }} size={10}>
        {preparation.steps.map((step, idx) => (
          <div key={idx} style={{ border: '1px solid #f0f0f0', borderRadius: 8, padding: 12 }}>
            <div style={{ display: 'grid', gridTemplateColumns: '72px 120px 120px 96px 34px', gap: 8, alignItems: 'center', marginBottom: 8 }}>
              <Tag color="blue" style={{ textAlign: 'center', margin: 0 }}>Paso {idx + 1}</Tag>
              <InputNumber min={0} value={step.estimatedMinutes} onChange={v => updateStep(idx, { estimatedMinutes: v ?? undefined })} addonAfter="min" style={{ width: '100%' }} />
              <Input value={step.temperature} onChange={e => updateStep(idx, { temperature: e.target.value })} placeholder="Temp." />
              <Space size={4} style={{ whiteSpace: 'nowrap' }}>
                <Text type="secondary">Crítico</Text>
                <Switch checked={step.isCritical} onChange={v => updateStep(idx, { isCritical: v })} />
              </Space>
              <Popconfirm title="Quitar paso" onConfirm={() => removeStep(idx)}>
                <Button danger size="small" icon={<DeleteOutlined />} disabled={preparation.steps.length === 1} />
              </Popconfirm>
            </div>
            <Input.TextArea
              value={step.instructions}
              onChange={e => updateStep(idx, { instructions: e.target.value })}
              placeholder="Describe qué debe hacerse en este paso"
              rows={3}
            />
          </div>
        ))}
      </Space>

      <Button type="dashed" block icon={<PlusOutlined />} onClick={addStep} style={{ marginTop: 12 }}>
        Agregar paso
      </Button>
    </Modal>
  );
}
