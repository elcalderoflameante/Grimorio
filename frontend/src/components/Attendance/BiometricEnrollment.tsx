import { useCallback, useEffect, useRef, useState } from 'react';
import { App, Button, Card, Modal, Popconfirm, Select, Space, Table, Tag, Typography } from 'antd';
import { CameraOutlined, DeleteOutlined, ReloadOutlined, SafetyCertificateOutlined } from '@ant-design/icons';
import { employeeApi } from '../../services/api';
import { attendanceApi, type FacialEnrollmentDto } from '../../services/attendanceApi';
import type { EmployeeDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { formatBranchDateTime } from '../../utils/branchTimeZone';

const requiredSamples = 3;
const sampleInstructions = [
  'Mira de frente con expresión neutral',
  'Mantente de frente y haz una sonrisa leve',
  'Mantente de frente con expresión natural',
];

export const BiometricEnrollment = () => {
  const { message } = App.useApp();
  const videoRef = useRef<HTMLVideoElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const [employees, setEmployees] = useState<EmployeeDto[]>([]);
  const [enrollments, setEnrollments] = useState<FacialEnrollmentDto[]>([]);
  const [employeeId, setEmployeeId] = useState<string>();
  const [samples, setSamples] = useState<Blob[]>([]);
  const [sampleUrls, setSampleUrls] = useState<string[]>([]);
  const [modalOpen, setModalOpen] = useState(false);
  const [cameraReady, setCameraReady] = useState(false);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  const stopCamera = useCallback(() => {
    streamRef.current?.getTracks().forEach((track) => track.stop());
    streamRef.current = null;
    setCameraReady(false);
  }, []);

  const clearSamples = useCallback(() => {
    sampleUrls.forEach((url) => URL.revokeObjectURL(url));
    setSamples([]);
    setSampleUrls([]);
  }, [sampleUrls]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [employeeResponse, enrollmentResponse] = await Promise.all([
        employeeApi.getAll(1, 500, true),
        attendanceApi.getFacialEnrollments(),
      ]);
      setEmployees(Array.isArray(employeeResponse.data) ? employeeResponse.data : []);
      setEnrollments(enrollmentResponse.data);
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  }, [message]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => () => {
    streamRef.current?.getTracks().forEach((track) => track.stop());
  }, []);

  const openEnrollment = () => {
    setEmployeeId(undefined);
    clearSamples();
    setModalOpen(true);
  };

  const startCamera = async () => {
    try {
      stopCamera();
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'user', width: { ideal: 1280 }, height: { ideal: 720 } },
        audio: false,
      });
      streamRef.current = stream;
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
        await videoRef.current.play();
      }
      setCameraReady(true);
    } catch (error) {
      const cameraError = error instanceof DOMException ? ` (${error.name})` : '';
      message.error(`No se pudo acceder a la cámara${cameraError}. Verifica el permiso del navegador y usa HTTPS o localhost.`);
    }
  };

  const capture = async () => {
    const video = videoRef.current;
    if (!video || !cameraReady || samples.length >= requiredSamples) return;
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext('2d')?.drawImage(video, 0, 0);
    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', 0.9));
    if (!blob) return message.error('No se pudo capturar la imagen.');
    setSamples((current) => [...current, blob]);
    setSampleUrls((current) => [...current, URL.createObjectURL(blob)]);
  };

  const removeSample = (index: number) => {
    URL.revokeObjectURL(sampleUrls[index]);
    setSamples((current) => current.filter((_, itemIndex) => itemIndex !== index));
    setSampleUrls((current) => current.filter((_, itemIndex) => itemIndex !== index));
  };

  const enroll = async () => {
    if (!employeeId || samples.length !== requiredSamples) return;
    setSaving(true);
    try {
      await attendanceApi.enrollEmployeeFace(employeeId, samples);
      message.success('Biometría facial enrolada correctamente');
      stopCamera();
      clearSamples();
      setModalOpen(false);
      await load();
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setSaving(false);
    }
  };

  const revoke = async (id: string) => {
    try {
      await attendanceApi.revokeEmployeeFace(id);
      message.success('Biometría revocada');
      await load();
    } catch (error) {
      message.error(formatError(error));
    }
  };

  return <>
    <Card
      title={<Space><SafetyCertificateOutlined />Enrolamiento facial</Space>}
      extra={<Space>
        <Button icon={<ReloadOutlined />} onClick={() => void load()}>Actualizar</Button>
        <Button type="primary" icon={<CameraOutlined />} onClick={openEnrollment}>Enrolar empleado</Button>
      </Space>}
    >
      <Typography.Paragraph type="secondary">
        Se guardan vectores biométricos cifrados. Las fotografías utilizadas durante el enrolamiento no se almacenan.
      </Typography.Paragraph>
      <Table
        rowKey="employeeId"
        loading={loading}
        dataSource={enrollments}
        pagination={false}
        columns={[
          { title: 'Empleado', dataIndex: 'employeeName' },
          { title: 'Modelo', dataIndex: 'modelVersion', render: (value: string) => <Tag color="blue">{value}</Tag> },
          { title: 'Muestras', dataIndex: 'sampleCount' },
          { title: 'Enrolado', dataIndex: 'enrolledAtUtc', render: (value: string) => formatBranchDateTime(value) },
          { title: 'Acciones', render: (_: unknown, item: FacialEnrollmentDto) =>
            <Popconfirm title="¿Revocar la biometría?" description="El empleado no podrá marcar con su rostro." onConfirm={() => void revoke(item.employeeId)}>
              <Button danger icon={<DeleteOutlined />}>Revocar</Button>
            </Popconfirm> },
        ]}
      />
    </Card>

    <Modal
      title="Enrolar rostro del empleado"
      open={modalOpen}
      width={760}
      okText="Guardar enrolamiento"
      okButtonProps={{ disabled: !employeeId || samples.length !== requiredSamples, loading: saving }}
      onOk={() => void enroll()}
      onCancel={() => { stopCamera(); clearSamples(); setModalOpen(false); }}
      destroyOnHidden
    >
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <Select
          showSearch
          optionFilterProp="label"
          value={employeeId}
          onChange={setEmployeeId}
          placeholder="Selecciona un empleado"
          style={{ width: '100%' }}
          options={employees.map((employee) => ({
            value: employee.id,
            label: `${employee.firstName} ${employee.lastName} · ${employee.identificationNumber}`,
          }))}
        />
        <Typography.Text strong style={{ textAlign: 'center' }}>
          {samples.length < requiredSamples ? sampleInstructions[samples.length] : 'Las tres muestras están listas'}
        </Typography.Text>
        <div style={{ position: 'relative', background: '#111', borderRadius: 8, overflow: 'hidden', minHeight: 320, display: 'grid', placeItems: 'center' }}>
          <video ref={videoRef} muted playsInline style={{ width: '100%', maxHeight: 420, objectFit: 'contain', transform: 'scaleX(-1)' }} />
          <div
            aria-hidden="true"
            style={{
              position: 'absolute',
              left: '50%',
              top: '50%',
              width: 'clamp(190px, 36%, 270px)',
              aspectRatio: '3 / 4',
              transform: 'translate(-50%, -50%)',
              border: '4px solid rgba(255, 255, 255, 0.92)',
              borderRadius: '50%',
              boxShadow: '0 0 0 9999px rgba(0, 0, 0, 0.28), 0 0 18px rgba(255, 255, 255, 0.35)',
              pointerEvents: 'none',
            }}
          />
          <div style={{ position: 'absolute', bottom: 12, left: 12, right: 12, color: '#fff', textAlign: 'center', textShadow: '0 1px 3px #000', pointerEvents: 'none' }}>
            Acércate y mantén todo el rostro dentro del óvalo
          </div>
        </div>
        <Space>
          <Button disabled={cameraReady} onClick={() => void startCamera()}>{cameraReady ? 'Cámara activa' : 'Iniciar cámara'}</Button>
          <Button type="primary" icon={<CameraOutlined />} disabled={!cameraReady || samples.length >= requiredSamples} onClick={() => void capture()}>
            Capturar ({samples.length}/{requiredSamples})
          </Button>
        </Space>
        <Space wrap>
          {sampleUrls.map((url, index) => (
            <div key={url} style={{ position: 'relative' }}>
              <img src={url} alt={`Muestra ${index + 1}`} style={{ width: 150, height: 110, objectFit: 'cover', borderRadius: 6 }} />
              <Button danger size="small" shape="circle" icon={<DeleteOutlined />} onClick={() => removeSample(index)}
                style={{ position: 'absolute', right: 4, top: 4 }} />
            </div>
          ))}
        </Space>
        <Typography.Text type="secondary">
          Toma una foto de frente y dos con variaciones leves de expresión o inclinación. Debe aparecer una sola persona.
        </Typography.Text>
      </Space>
    </Modal>
  </>;
};
