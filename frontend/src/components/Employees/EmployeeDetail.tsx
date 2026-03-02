import { useCallback, useEffect, useState, useRef } from 'react';
import { Button, Form, Input, Select, DatePicker, Row, Col, InputNumber, Switch, Tabs, Space, Typography, message, Spin, Upload, Card, Divider, Table, Tag } from 'antd';
import { ArrowLeftOutlined, CameraOutlined, DeleteOutlined, InboxOutlined, PictureOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { employeeService, payrollApi, positionService } from '../../services/api';
import { ContractType, PayrollRoleDetailType, PayrollRoleStatus, type EmployeeDto, type PositionDto, type CreateEmployeeDto, type UpdateEmployeeDto, type ContractTypeValue, type PayrollRoleDto, type PayrollRoleDetailDto } from '../../types';
import { isValidEcuadorCedula, isValidEcuadorCell } from '../../utils/ecuadorValidators';

interface EmployeeFormValues {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  identificationNumber: string;
  positionId: string;
  hireDate: Dayjs;
  contractType: ContractTypeValue;
  weeklyMinHours: number;
  weeklyMaxHours: number;
  baseSalary: number;
  bankAccount: string;
  decimoThirdMonthly: boolean;
  decimoFourthMonthly: boolean;
  reserveFundMonthly: boolean;
  freeDaysPerMonth?: number;
  isActive?: boolean;
  photo?: string;
  dateOfBirth?: Dayjs;
  civilStatus: string;
  sex: string;
  nationality: string;
  emergencyContactPerson: string;
  emergencyContactRelationship: string;
  emergencyContactPhone: string;
}

interface EmployeeDetailProps {
  employeeId: string | null;
  onSaved: () => void;
  onCancel: () => void;
}

const contractTypeOptions = [
  { value: ContractType.FullTime, label: 'Tiempo completo' },
  { value: ContractType.PartTime, label: 'Tiempo parcial' },
  { value: ContractType.Temporary, label: 'Temporal' },
  { value: ContractType.Seasonal, label: 'Temporada' },
];

const { Dragger } = Upload;

interface EmployeePayrollRoleRow {
  key: string;
  id: string;
  period: string;
  year: number;
  month: number;
  totalIncome: number;
  totalDeductions: number;
  netPay: number;
  status: number;
}

const MONTH_LABELS = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];

const extractPositionItems = (value: unknown): PositionDto[] => {
  if (Array.isArray(value)) {
    return value as PositionDto[];
  }

  if (value && typeof value === 'object') {
    const items = (value as { items?: unknown }).items;
    if (Array.isArray(items)) {
      return items as PositionDto[];
    }
  }

  return [];
};

export default function EmployeeDetail({ employeeId, onSaved, onCancel }: EmployeeDetailProps) {
  const [form] = Form.useForm<EmployeeFormValues>();
  const [positions, setPositions] = useState<PositionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [photoPreview, setPhotoPreview] = useState<string | null>(null);
  const [showCamera, setShowCamera] = useState(false);
  const [cameraReady, setCameraReady] = useState(false);
  const [payrollRolesLoading, setPayrollRolesLoading] = useState(false);
  const [payrollRoles, setPayrollRoles] = useState<EmployeePayrollRoleRow[]>([]);
  const [payrollRoleDetails, setPayrollRoleDetails] = useState<Record<string, PayrollRoleDetailDto[]>>({});
  const [payrollRoleDetailLoadingId, setPayrollRoleDetailLoadingId] = useState<string | null>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const isEdit = Boolean(employeeId);

  const loadPositions = useCallback(async () => {
    try {
      const response = await positionService.getAll();
      setPositions(extractPositionItems(response.data));
    } catch {
      message.error('Error al cargar posiciones');
    }
  }, []);

  const loadEmployee = useCallback(async (id: string) => {
    setLoading(true);
    try {
      const response = await employeeService.getById(id);
      const employee: EmployeeDto = response.data;
      form.setFieldsValue({
        firstName: employee.firstName,
        lastName: employee.lastName,
        email: employee.email,
        phone: employee.phone,
        identificationNumber: employee.identificationNumber,
        positionId: employee.positionId,
        hireDate: dayjs(employee.hireDate),
        contractType: employee.contractType,
        weeklyMinHours: employee.weeklyMinHours,
        weeklyMaxHours: employee.weeklyMaxHours,
        baseSalary: employee.baseSalary,
        bankAccount: employee.bankAccount,
        decimoThirdMonthly: employee.decimoThirdMonthly,
        decimoFourthMonthly: employee.decimoFourthMonthly,
        reserveFundMonthly: employee.reserveFundMonthly,
        freeDaysPerMonth: employee.freeDaysPerMonth,
        isActive: employee.isActive,
        photo: employee.photo,
        dateOfBirth: employee.dateOfBirth ? dayjs(employee.dateOfBirth) : undefined,
        civilStatus: employee.civilStatus || '',
        sex: employee.sex || '',
        nationality: employee.nationality || '',
        emergencyContactPerson: employee.emergencyContactPerson || '',
        emergencyContactRelationship: employee.emergencyContactRelationship || '',
        emergencyContactPhone: employee.emergencyContactPhone || '',
      });
      // Mostrar preview de la foto si existe
      if (employee.photo) {
        setPhotoPreview(employee.photo);
      }
    } catch {
      message.error('Error al cargar empleado');
    } finally {
      setLoading(false);
    }
  }, [form]);

  const startCamera = useCallback(async () => {
    try {
      setCameraReady(false);
      setShowCamera(true);
      
      // Pequeño delay para asegurar que el elemento video esté montado en el DOM
      await new Promise(resolve => setTimeout(resolve, 100));
      
      if (!videoRef.current) {
        message.error('Error: el elemento video no está disponible');
        setShowCamera(false);
        return;
      }

      const constraints = {
        video: {
          width: { min: 320, ideal: 400, max: 800 },
          height: { min: 240, ideal: 300, max: 600 },
        }
      };

      const stream = await navigator.mediaDevices.getUserMedia(constraints);
      streamRef.current = stream;
      
      // Asignar stream de forma más robusta
      videoRef.current.srcObject = stream;
      
      // Agregar listener para saber cuando está listo
      const onLoadedMetadata = () => {
        console.log('Video: loadedmetadata - video listo');
        setCameraReady(true);
        videoRef.current?.removeEventListener('loadedmetadata', onLoadedMetadata);
      };
      
      videoRef.current.addEventListener('loadedmetadata', onLoadedMetadata);
      videoRef.current.addEventListener('playing', () => console.log('Video: playing'));
      
      message.info('Cámara iniciada. Esperando carga del video...');
    } catch (error: any) {
      console.error('Error al acceder a la cámara:', error);
      setShowCamera(false);
      setCameraReady(false);
      
      if (error.name === 'NotAllowedError') {
        message.error('Permiso de cámara denegado. Verifica la configuración del navegador.');
      } else if (error.name === 'NotFoundError') {
        message.error('No se encontró ninguna cámara en el dispositivo.');
      } else if (error.name === 'NotReadableError') {
        message.error('La cámara está siendo usada por otra aplicación.');
      } else {
        message.error(`Error al acceder a la cámara: ${error.message}`);
      }
    }
  }, []);

  const stopCamera = useCallback((showNotification = false) => {
    if (videoRef.current) {
      videoRef.current.srcObject = null;
    }
    if (streamRef.current) {
      streamRef.current.getTracks().forEach(track => {
        track.stop();
        console.log('Track detenido:', track.kind);
      });
      streamRef.current = null;
    }
    setShowCamera(false);
    setCameraReady(false);
    if (showNotification) {
      message.info('Cámara detenida');
    }
  }, []);

  const capturePhoto = useCallback(() => {
    if (!videoRef.current || !canvasRef.current) {
      message.error('Error: elementos de cámara no disponibles');
      return;
    }

    try {
      const video = videoRef.current;
      const canvas = canvasRef.current;

      console.log('Capturando foto...');
      console.log('Video readyState:', video.readyState);
      console.log('Video networkState:', video.networkState);
      console.log('Video videoWidth:', video.videoWidth);
      console.log('Video videoHeight:', video.videoHeight);

      if (video.videoWidth === 0 || video.videoHeight === 0) {
        message.warning('El video aún no está cargado. Intenta nuevamente en un momento.');
        return;
      }

      // Establecer dimensiones del canvas
      canvas.width = video.videoWidth;
      canvas.height = video.videoHeight;

      const ctx = canvas.getContext('2d');
      if (!ctx) {
        message.error('No se pudo obtener el contexto del canvas');
        return;
      }

      // Dibujar imagen espejada (compensar el scaleX(-1) del video)
      ctx.scale(-1, 1);
      ctx.drawImage(video, -video.videoWidth, 0);

      // Comprimir foto más agresivamente para reducir tamaño
      let photoData = canvas.toDataURL('image/jpeg', 0.75);
      let fileSizeKB = Math.round((photoData.length * 3) / 4 / 1024);
      
      // Si la foto es muy grande, comprimirla más
      if (fileSizeKB > 500) {
        console.warn(`Foto muy grande (${fileSizeKB}KB), recomprimiendo...`);
        photoData = canvas.toDataURL('image/jpeg', 0.5);
        fileSizeKB = Math.round((photoData.length * 3) / 4 / 1024);
        console.log(`Foto recomprimida a ${fileSizeKB}KB`);
      }
      
      // Si aún es muy grande, reducir más
      if (fileSizeKB > 500) {
        console.warn(`Foto aún muy grande (${fileSizeKB}KB), reduciendo más...`);
        photoData = canvas.toDataURL('image/jpeg', 0.3);
        fileSizeKB = Math.round((photoData.length * 3) / 4 / 1024);
        console.log(`Foto reducida a ${fileSizeKB}KB`);
      }
      
      if (photoData && photoData.length > 100) {
        form.setFieldValue('photo', photoData);
        setPhotoPreview(photoData);
        stopCamera();
        console.log('Foto guardada en formulario, tamaño:', fileSizeKB, 'KB');
        message.success(`Foto capturada (${fileSizeKB} KB)`);
      } else {
        message.error('La foto capturada no es válida');
      }
    } catch (error: any) {
      console.error('Error al capturar foto:', error);
      message.error(`Error al capturar: ${error.message}`);
    }
  }, [form, stopCamera]);

  const handlePhotoUpload = useCallback((e: any) => {
    const file = e?.file?.originFileObj ?? e?.file;
    if (!(file instanceof File)) {
      message.error('No se pudo leer el archivo de imagen');
      return;
    }

    if (!file.type.startsWith('image/')) {
      message.error('Selecciona un archivo de imagen válido');
      return;
    }

    const reader = new FileReader();
    reader.onload = (event) => {
      const result = event.target?.result as string;
      const fileSizeKB = Math.round((result.length * 3) / 4 / 1024);
      console.log('Foto cargada, tamaño:', fileSizeKB, 'KB');
      
      // Si es muy grande, mostrar advertencia
      if (fileSizeKB > 500) {
        message.warning(`Foto muy grande (${fileSizeKB} KB). Se enviará comprimida.`);
      } else {
        message.success(`Foto cargada (${fileSizeKB} KB)`);
      }
      
      form.setFieldValue('photo', result);
      setPhotoPreview(result);
    };
    reader.readAsDataURL(file);
  }, [form]);

  const deletePhoto = useCallback(() => {
    form.setFieldValue('photo', undefined);
    setPhotoPreview(null);
    message.success('Foto eliminada');
  }, [form]);

  const loadPayrollRoles = useCallback(async (id: string) => {
    setPayrollRolesLoading(true);
    setPayrollRoleDetails({});
    try {
      const response = await payrollApi.getRolesByEmployee(id);
      const rows = response.data.map((role: PayrollRoleDto) => ({
        key: role.id,
        id: role.id,
        period: `${MONTH_LABELS[role.month - 1]} ${role.year}`,
        year: role.year,
        month: role.month,
        totalIncome: role.totalIncome,
        totalDeductions: role.totalDeductions,
        netPay: role.netPay,
        status: role.status,
      }));

      setPayrollRoles(rows);
    } catch (error) {
      console.error('Error al cargar roles de pago:', error);
      message.error('No se pudieron cargar los roles de pago');
      setPayrollRoles([]);
    } finally {
      setPayrollRolesLoading(false);
    }
  }, []);

  useEffect(() => {
    return () => {
      stopCamera();
    };
  }, [stopCamera]);

  useEffect(() => {
    loadPositions();
  }, [loadPositions]);

  useEffect(() => {
    if (employeeId) {
      loadEmployee(employeeId);
      loadPayrollRoles(employeeId);
      return;
    }

    form.resetFields();
    form.setFieldsValue({
      contractType: ContractType.FullTime,
      weeklyMinHours: 40,
      weeklyMaxHours: 40,
      baseSalary: 0,
      bankAccount: '',
      decimoThirdMonthly: true,
      decimoFourthMonthly: true,
      reserveFundMonthly: false,
      freeDaysPerMonth: 6,
      isActive: true,
    });
    setPayrollRoles([]);
    setPayrollRoleDetails({});
  }, [employeeId, form, loadEmployee, loadPayrollRoles]);

  const getPayrollStatusLabel = (status: number) => {
    if (status === PayrollRoleStatus.Paid) return 'Pagado';
    if (status === PayrollRoleStatus.Authorized) return 'Autorizado';
    return 'Generado';
  };

  const getPayrollStatusColor = (status: number) => {
    if (status === PayrollRoleStatus.Paid) return 'green';
    if (status === PayrollRoleStatus.Authorized) return 'blue';
    return 'gold';
  };

  const handleExpandPayrollRole = async (expanded: boolean, record: EmployeePayrollRoleRow) => {
    if (!expanded || payrollRoleDetails[record.id]) {
      return;
    }

    setPayrollRoleDetailLoadingId(record.id);
    try {
      const response = await payrollApi.getRoleDetail(record.id);
      setPayrollRoleDetails(prev => ({
        ...prev,
        [record.id]: response.data.details,
      }));
    } catch (error) {
      console.error('Error al cargar detalle del rol:', error);
      message.error('No se pudo cargar el detalle del rol de pago');
    } finally {
      setPayrollRoleDetailLoadingId(null);
    }
  };

  const handleSave = async (values: EmployeeFormValues) => {
    setSaving(true);
    try {
      console.log('=== INICIO GUARDADO ===');
      console.log('Valores del formulario completos:', values);
      console.log('Foto:', values.photo ? `${Math.round((values.photo.length * 3) / 4 / 1024)}KB` : 'sin foto');
      console.log('Fecha nacimiento:', values.dateOfBirth?.toISOString() || 'sin fecha');
      console.log('Estado civil:', values.civilStatus);
      console.log('Sexo:', values.sex);
      console.log('Nacionalidad:', values.nationality);
      console.log('Contacto emergencia:', values.emergencyContactPerson);
      console.log('Relación:', values.emergencyContactRelationship);
      console.log('Teléfono emergencia:', values.emergencyContactPhone);
      
      if (employeeId) {
        const updateData: UpdateEmployeeDto = {
          firstName: values.firstName,
          lastName: values.lastName,
          email: values.email,
          phone: values.phone || '',
          identificationNumber: values.identificationNumber,
          positionId: values.positionId,
          isActive: values.isActive ?? true,
          terminationDate: undefined,
          contractType: values.contractType,
          weeklyMinHours: values.weeklyMinHours,
          weeklyMaxHours: values.weeklyMaxHours,
          baseSalary: values.baseSalary,
          bankAccount: values.bankAccount,
          decimoThirdMonthly: values.decimoThirdMonthly,
          decimoFourthMonthly: values.decimoFourthMonthly,
          reserveFundMonthly: values.reserveFundMonthly,
          freeDaysPerMonth: values.freeDaysPerMonth || 6,
          photo: values.photo || undefined,
          dateOfBirth: values.dateOfBirth?.toISOString(),
          civilStatus: values.civilStatus || '',
          sex: values.sex || '',
          nationality: values.nationality || '',
          emergencyContactPerson: values.emergencyContactPerson || '',
          emergencyContactRelationship: values.emergencyContactRelationship || '',
          emergencyContactPhone: values.emergencyContactPhone || '',
        };
        console.log('=== UPDATE DATA ===');
        console.log('Datos de actualización:', JSON.stringify(updateData, null, 2));
        await employeeService.update(employeeId, updateData);
        message.success('Empleado actualizado');
      } else {
        const createData: CreateEmployeeDto = {
          firstName: values.firstName,
          lastName: values.lastName,
          email: values.email,
          phone: values.phone || '',
          identificationNumber: values.identificationNumber,
          positionId: values.positionId,
          hireDate: values.hireDate.toISOString(),
          contractType: values.contractType,
          weeklyMinHours: values.weeklyMinHours,
          weeklyMaxHours: values.weeklyMaxHours,
          baseSalary: values.baseSalary,
          bankAccount: values.bankAccount,
          decimoThirdMonthly: values.decimoThirdMonthly,
          decimoFourthMonthly: values.decimoFourthMonthly,
          reserveFundMonthly: values.reserveFundMonthly,
          freeDaysPerMonth: values.freeDaysPerMonth || 6,
          photo: values.photo || undefined,
          dateOfBirth: values.dateOfBirth?.toISOString(),
          civilStatus: values.civilStatus || '',
          sex: values.sex || '',
          nationality: values.nationality || '',
          emergencyContactPerson: values.emergencyContactPerson || '',
          emergencyContactRelationship: values.emergencyContactRelationship || '',
          emergencyContactPhone: values.emergencyContactPhone || '',
        };
        console.log('=== CREATE DATA ===');
        console.log('Datos de creación:', JSON.stringify(createData, null, 2));
        await employeeService.create(createData);
        message.success('Empleado creado');
      }

      onSaved();
    } catch (error: unknown) {
      console.error('Error al guardar:', error);
      const err = error as { response?: { data?: { message?: string; errors?: any } } };
      const errorMessage = err.response?.data?.message || 'Error al guardar';
      const errors = err.response?.data?.errors;
      
      if (errors) {
        console.error('Detalles del error:', errors);
        message.error(`${errorMessage} - ${JSON.stringify(errors)}`);
      } else {
        message.error(errorMessage);
      }
    } finally {
      setSaving(false);
    }
  };

  return (
    <div>
      <Form
        form={form}
        layout="vertical"
        onFinish={handleSave}
      >
        <Row justify="space-between" align="middle" style={{ marginBottom: 16 }}>
          <Col>
            <Space>
              <Button icon={<ArrowLeftOutlined />} onClick={onCancel}>
                Volver
              </Button>
              <Typography.Title level={4} style={{ margin: 0 }}>
                {isEdit ? 'Editar empleado' : 'Nuevo empleado'}
              </Typography.Title>
            </Space>
          </Col>
          <Col>
            <Space>
              <Button type="primary" htmlType="submit" loading={saving}>
                Guardar
              </Button>
              <Button onClick={onCancel}>
                Cancelar
              </Button>
            </Space>
          </Col>
        </Row>

        <Tabs
          defaultActiveKey="personales"
          items={[
            {
              key: 'personales',
              label: 'Información personal',
              children: (
                <>
                  {/* Ficha de empleado */}
                  <Card style={{ marginBottom: 24, padding: '24px' }}>
                    <Row gutter={[24, 24]} align="top">
                      <Col xs={24} md={8}>
                        <div
                          style={{
                            width: '200px',
                            height: '240px',
                            margin: '0 auto',
                            backgroundColor: '#f0f0f0',
                            borderRadius: '8px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            overflow: 'hidden',
                            border: '2px solid #d9d9d9',
                          }}
                        >
                          {photoPreview ? (
                            <img
                              src={photoPreview}
                              alt="Foto"
                              style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                            />
                          ) : (
                            <PictureOutlined style={{ fontSize: '48px', color: '#999' }} />
                          )}
                        </div>

                        <Space orientation="vertical" style={{ width: '100%', marginTop: 16 }}>
                          <Dragger
                            beforeUpload={() => false}
                            onChange={handlePhotoUpload}
                            maxCount={1}
                            accept="image/*"
                            showUploadList={false}
                            style={{ padding: '8px' }}
                          >
                            <p className="ant-upload-drag-icon">
                              <InboxOutlined />
                            </p>
                            <p className="ant-upload-text">
                              Arrastra y suelta una foto aquí
                            </p>
                            <p className="ant-upload-hint">
                              o haz clic para seleccionar desde tu equipo
                            </p>
                          </Dragger>

                          <Button
                            icon={<CameraOutlined />}
                            block
                            onClick={startCamera}
                            disabled={showCamera}
                          >
                            Desde cámara
                          </Button>

                          {photoPreview && (
                            <Button
                              danger
                              icon={<DeleteOutlined />}
                              block
                              onClick={deletePhoto}
                            >
                              Eliminar foto
                            </Button>
                          )}
                        </Space>

                        {/* Cámara en vivo */}
                        {showCamera && (
                          <div style={{ marginTop: '20px' }}>
                          <video
                            ref={videoRef}
                            autoPlay
                            playsInline
                            muted
                            style={{
                              width: '100%',
                              maxWidth: '400px',
                              height: 'auto',
                              borderRadius: '8px',
                              backgroundColor: '#000',
                              display: 'block',
                              transform: 'scaleX(-1)',
                            }}
                          />
                          <canvas
                            ref={canvasRef}
                            style={{ display: 'none' }}
                          />

                          {!cameraReady && (
                            <div style={{ textAlign: 'center', color: '#999', marginTop: '12px' }}>
                              Cargando cámara...
                            </div>
                          )}

                          <Space style={{ marginTop: '12px', width: '100%', justifyContent: 'center' }}>
                            <Button 
                              type="primary" 
                              onClick={capturePhoto}
                              disabled={!cameraReady}
                              loading={!cameraReady}
                            >
                              {cameraReady ? 'Capturar foto' : 'Cargando...'}
                            </Button>
                            <Button onClick={() => stopCamera(true)}>
                              Cancelar
                            </Button>
                          </Space>
                          </div>
                        )}
                      </Col>

                      <Col xs={24} md={16}>

                        <Row gutter={16}>
                          <Col xs={24} sm={12}>
                            <Form.Item
                              label="Nombre"
                              name="firstName"
                              rules={[{ required: true }]}
                            >
                              <Input />
                            </Form.Item>
                          </Col>

                          <Col xs={24} sm={12}>
                            <Form.Item
                              label="Apellido"
                              name="lastName"
                              rules={[{ required: true }]}
                            >
                              <Input />
                            </Form.Item>
                          </Col>
                        </Row>

                        <Row gutter={16}>
                          <Col xs={24} sm={12}>
                            <Form.Item
                              label="Cedula"
                              name="identificationNumber"
                              rules={[
                                { required: true, message: 'La cedula es requerida' },
                                {
                                  validator: (_, value) => {
                                    if (!value) return Promise.reject('La cedula es requerida');
                                    return isValidEcuadorCedula(value)
                                      ? Promise.resolve()
                                      : Promise.reject('Cedula invalida (Ecuador)');
                                  },
                                },
                              ]}
                            >
                              <Input />
                            </Form.Item>
                          </Col>

                          <Col xs={24} sm={12}>
                            <Form.Item
                              label="Telefono"
                              name="phone"
                              rules={[
                                {
                                  validator: (_, value) => {
                                    if (!value) return Promise.resolve();
                                    return isValidEcuadorCell(value)
                                      ? Promise.resolve()
                                      : Promise.reject('Telefono celular invalido (Ecuador, debe iniciar con 09)');
                                  },
                                },
                              ]}
                            >
                              <Input placeholder="09xxxxxxxx" />
                            </Form.Item>
                          </Col>
                        </Row>

                        <Row gutter={16}>
                          <Col xs={24} sm={12}>
                            <Form.Item
                              label="Email"
                              name="email"
                              rules={[{ required: true, type: 'email' }]}
                            >
                              <Input />
                            </Form.Item>
                          </Col>

                          <Col xs={24} sm={12}>
                            <Form.Item
                              label="Nacionalidad"
                              name="nationality"
                            >
                              <Input placeholder="Ejemplo: Ecuatoriana" />
                            </Form.Item>
                          </Col>
                        </Row>

                        <Row gutter={16}>
                          <Col xs={24} sm={12}>
                            <Form.Item
                              label="Estado civil"
                              name="civilStatus"
                            >
                              <Select
                                placeholder="Selecciona estado civil"
                                options={[
                                  { value: 'Soltero', label: 'Soltero' },
                                  { value: 'Casado', label: 'Casado' },
                                  { value: 'Divorciado', label: 'Divorciado' },
                                  { value: 'Viudo', label: 'Viudo' },
                                ]}
                              />
                            </Form.Item>
                          </Col>

                          <Col xs={24} sm={12}>
                            <Form.Item
                              label="Sexo"
                              name="sex"
                            >
                              <Select
                                placeholder="Selecciona sexo"
                                options={[
                                  { value: 'Masculino', label: 'Masculino' },
                                  { value: 'Femenino', label: 'Femenino' },
                                  { value: 'Otro', label: 'Otro' },
                                ]}
                              />
                            </Form.Item>
                          </Col>
                        </Row>

                        <Row gutter={16}>
                          <Col xs={24} sm={isEdit ? 12 : 24}>
                            <Form.Item
                              label="Fecha de nacimiento"
                              name="dateOfBirth"
                            >
                              <DatePicker style={{ width: '100%' }} />
                            </Form.Item>
                          </Col>

                          {isEdit && (
                            <Col xs={24} sm={12}>
                              <Form.Item label="Estado" name="isActive" valuePropName="checked">
                                <Switch checkedChildren="Activo" unCheckedChildren="Inactivo" />
                              </Form.Item>
                            </Col>
                          )}
                        </Row>
                      </Col>
                    </Row>

                    <Form.Item name="photo" style={{ display: 'none' }}>
                      <Input type="hidden" />
                    </Form.Item>
                  </Card>
                </>
              ),
            },
            {
              key: 'laboral',
              label: 'Información laboral',
              children: (
                <>
                  <Row gutter={16}>
                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item
                        label="Posicion"
                        name="positionId"
                        rules={[{ required: true }]}
                      >
                        <Select
                          placeholder="Selecciona una posicion"
                          showSearch
                          optionFilterProp="label"
                          options={positions.map(p => ({
                            label: p.name || p.description || p.id,
                            value: p.id,
                          }))}
                        />
                      </Form.Item>
                    </Col>

                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item
                        label="Fecha de contratacion"
                        name="hireDate"
                        rules={[{ required: true }]}
                      >
                        <DatePicker style={{ width: '100%' }} />
                      </Form.Item>
                    </Col>

                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item
                        label="Dias libres al mes"
                        name="freeDaysPerMonth"
                        rules={[{ required: true, message: 'Campo obligatorio' }]}
                      >
                        <Input type="number" min={1} max={30} placeholder="6" />
                      </Form.Item>
                    </Col>
                  </Row>

                  <Row gutter={16}>
                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item
                        label="Tipo de contrato"
                        name="contractType"
                        rules={[{ required: true, message: 'Selecciona un tipo de contrato' }]}
                      >
                        <Select
                          placeholder="Selecciona un tipo"
                          options={contractTypeOptions}
                        />
                      </Form.Item>
                    </Col>

                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item
                        label="Horas minimas por semana"
                        name="weeklyMinHours"
                        rules={[{ required: true, message: 'Campo obligatorio' }]}
                      >
                        <InputNumber<number>
                          min={0}
                          max={80}
                          step={1}
                          precision={0}
                          style={{ width: '100%' }}
                          placeholder="40"
                        />
                      </Form.Item>
                    </Col>

                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item
                        label="Horas maximas por semana"
                        name="weeklyMaxHours"
                        rules={[
                          { required: true, message: 'Campo obligatorio' },
                          ({ getFieldValue }) => ({
                            validator(_, value) {
                              const minValue = getFieldValue('weeklyMinHours');
                              if (value == null || minValue == null || value >= minValue) {
                                return Promise.resolve();
                              }
                              return Promise.reject('Debe ser mayor o igual a las horas minimas');
                            },
                          }),
                        ]}
                      >
                        <InputNumber<number>
                          min={0}
                          max={80}
                          step={1}
                          precision={0}
                          style={{ width: '100%' }}
                          placeholder="40"
                        />
                      </Form.Item>
                    </Col>
                  </Row>

                  <Divider />

                  <Row gutter={16}>
                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item
                        label="Sueldo base"
                        name="baseSalary"
                        rules={[{ required: true, message: 'Campo obligatorio' }]}
                      >
                        <InputNumber<number>
                          min={0}
                          max={100000}
                          step={0.01}
                          precision={2}
                          style={{ width: '100%' }}
                          placeholder="0.00"
                        />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item
                        label="Cuenta deposito"
                        name="bankAccount"
                        rules={[{ required: true, message: 'Campo obligatorio' }]}
                      >
                        <Input placeholder="Numero de cuenta" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item label="Decimo tercero mensual" name="decimoThirdMonthly" valuePropName="checked">
                        <Switch />
                      </Form.Item>
                    </Col>
                  </Row>

                  <Row gutter={16}>
                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item label="Decimo cuarto mensual" name="decimoFourthMonthly" valuePropName="checked">
                        <Switch />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item label="Fondos de reserva mensual" name="reserveFundMonthly" valuePropName="checked">
                        <Switch />
                      </Form.Item>
                    </Col>
                  </Row>
                </>
              ),
            },
            {
              key: 'rol-pagos',
              label: 'Rol de pagos',
              children: (
                <>
                  <div style={{ marginBottom: 16, color: '#666', fontSize: 12 }}>
                    Para generar o modificar roles de pago, ve a Nómina → Rol de pagos
                  </div>

                  <Table<EmployeePayrollRoleRow>
                    rowKey="key"
                    loading={payrollRolesLoading}
                    dataSource={payrollRoles}
                    pagination={{ pageSize: 8 }}
                    locale={{ emptyText: 'No hay roles de pago generados para este empleado.' }}
                    columns={[
                      {
                        title: 'Periodo',
                        dataIndex: 'period',
                        key: 'period',
                      },
                      {
                        title: 'Ingresos',
                        dataIndex: 'totalIncome',
                        key: 'totalIncome',
                        align: 'right',
                        render: (value: number) => `$ ${value.toFixed(2)}`,
                      },
                      {
                        title: 'Descuentos',
                        dataIndex: 'totalDeductions',
                        key: 'totalDeductions',
                        align: 'right',
                        render: (value: number) => `$ ${value.toFixed(2)}`,
                      },
                      {
                        title: 'Neto a pagar',
                        dataIndex: 'netPay',
                        key: 'netPay',
                        align: 'right',
                        render: (value: number) => `$ ${value.toFixed(2)}`,
                      },
                      {
                        title: 'Estado',
                        dataIndex: 'status',
                        key: 'status',
                        render: (status: number) => {
                          const color = getPayrollStatusColor(status);
                          return <Tag color={color}>{getPayrollStatusLabel(status)}</Tag>;
                        },
                      },
                      {
                        title: 'Acciones',
                        key: 'actions',
                        render: () => (
                          <Button type="link" size="small">PDF</Button>
                        ),
                      },
                    ]}
                    expandable={{
                      onExpand: handleExpandPayrollRole,
                      expandedRowRender: (record) => (
                        <Table<PayrollRoleDetailDto>
                          rowKey="id"
                          size="small"
                          loading={payrollRoleDetailLoadingId === record.id}
                          pagination={false}
                          dataSource={payrollRoleDetails[record.id] || []}
                          columns={[
                            {
                              title: 'Tipo',
                              dataIndex: 'type',
                              key: 'type',
                              width: 140,
                              render: (value: number) => value === PayrollRoleDetailType.Deduction ? 'Descuento' : 'Ingreso',
                            },
                            {
                              title: 'Concepto',
                              dataIndex: 'concept',
                              key: 'concept',
                            },
                            {
                              title: 'Monto',
                              dataIndex: 'amount',
                              key: 'amount',
                              align: 'right',
                              render: (value: number) => `$ ${value.toFixed(2)}`,
                            },
                          ]}
                        />
                      ),
                    }}
                  />
                </>
              ),
            },
            {
              key: 'other-info',
              label: 'Otra Información',
              children: (
                <>
                  <Typography.Title level={5} style={{ marginBottom: 16 }}>
                    Datos Básicos
                  </Typography.Title>

                  <Divider />

                  <Typography.Title level={5} style={{ marginBottom: 16 }}>
                    Contacto de Emergencia
                  </Typography.Title>

                  <Row gutter={16}>
                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item
                        label="Persona para contactar"
                        name="emergencyContactPerson"
                      >
                        <Input placeholder="Nombre completo" />
                      </Form.Item>
                    </Col>

                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item
                        label="Parentesco/Relacion"
                        name="emergencyContactRelationship"
                      >
                        <Select
                          placeholder="Selecciona relacion"
                          options={[
                            { value: 'Padre/Madre', label: 'Padre/Madre' },
                            { value: 'Hermano/Hermana', label: 'Hermano/Hermana' },
                            { value: 'Esposo/Esposa', label: 'Esposo/Esposa' },
                            { value: 'Hijo/Hija', label: 'Hijo/Hija' },
                            { value: 'Otro', label: 'Otro' },
                          ]}
                        />
                      </Form.Item>
                    </Col>

                    <Col xs={24} sm={12} lg={8}>
                      <Form.Item
                        label="Telefono emergencia"
                        name="emergencyContactPhone"
                        rules={[
                          {
                            validator: (_, value) => {
                              if (!value) return Promise.resolve();
                              return isValidEcuadorCell(value)
                                ? Promise.resolve()
                                : Promise.reject('Telefono invalido (Ecuador, debe iniciar con 09)');
                            },
                          },
                        ]}
                      >
                        <Input placeholder="09xxxxxxxx" />
                      </Form.Item>
                    </Col>
                  </Row>
                </>
              ),
            },
          ]}
        />

      </Form>

      {loading && (
        <div style={{ marginTop: 16 }}>
          <Spin />
        </div>
      )}
    </div>
  );
}
