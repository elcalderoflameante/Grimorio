import { useCallback, useEffect, useState } from 'react';
import { App, Badge, Button, Card, Col, DatePicker, Form, Input, Modal, Row, Select, Space, Statistic, Table, Tag, Typography } from 'antd';
import { ClockCircleOutlined, EditOutlined, HistoryOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { attendanceApi, type AttendanceAdminRowDto, type AttendanceCorrectionDto } from '../../services/attendanceApi';
import { employeeApi } from '../../services/api';
import type { EmployeeDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import { formatBranchDateTime, formatBranchTime, toBranchDayjs } from '../../utils/branchTimeZone';

interface CorrectionValues {
  clockIn: Dayjs;
  clockOut?: Dayjs;
  breakStart?: Dayjs;
  breakEnd?: Dayjs;
  reason: string;
}

interface ManualAttendanceValues extends CorrectionValues {
  employeeId: string;
  reasonType: string;
  notes?: string;
}

const manualReasons = ['Equipo dañado', 'Sin conexión a Internet', 'Corte eléctrico', 'Olvido de marcación', 'Otro'];

const statusTag = (status: AttendanceAdminRowDto['status']) => {
  if (status === 1) return <Tag color="processing">Trabajando</Tag>;
  if (status === 2) return <Tag color="warning">En descanso</Tag>;
  return <Tag color="success">Finalizada</Tag>;
};

const time = (value?: string) => formatBranchTime(value, '—');
const duration = (minutes: number) => `${Math.floor(minutes / 60)}h ${minutes % 60}m`;

export const AttendanceDashboard = () => {
  const { message } = App.useApp();
  const { hasPermission } = useAuth();
  const [form] = Form.useForm<CorrectionValues>();
  const [manualForm] = Form.useForm<ManualAttendanceValues>();
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs]>([dayjs(), dayjs()]);
  const [employeeId, setEmployeeId] = useState<string>();
  const [employees, setEmployees] = useState<EmployeeDto[]>([]);
  const [rows, setRows] = useState<AttendanceAdminRowDto[]>([]);
  const [editing, setEditing] = useState<AttendanceAdminRowDto>();
  const [history, setHistory] = useState<AttendanceCorrectionDto[]>([]);
  const [historyOpen, setHistoryOpen] = useState(false);
  const [manualOpen, setManualOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const response = await attendanceApi.getClockings(
        dateRange[0].format('YYYY-MM-DD'), dateRange[1].format('YYYY-MM-DD'), employeeId,
      );
      setRows(response.data);
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  }, [dateRange, employeeId, message]);

  useEffect(() => {
    void employeeApi.getAll(1, 500, true).then((response) =>
      setEmployees(Array.isArray(response.data) ? response.data : []));
  }, []);
  useEffect(() => { void load(); }, [load]);

  const openCorrection = (row: AttendanceAdminRowDto) => {
    setEditing(row);
    form.setFieldsValue({
      clockIn: toBranchDayjs(row.clockInTimeUtc) ?? dayjs(row.clockInTimeUtc),
      clockOut: toBranchDayjs(row.clockOutTimeUtc) ?? undefined,
      breakStart: toBranchDayjs(row.breakStartedAtUtc) ?? undefined,
      breakEnd: toBranchDayjs(row.breakEndedAtUtc) ?? undefined,
      reason: '',
    });
  };

  const saveCorrection = async (values: CorrectionValues) => {
    if (!editing) return;
    setSaving(true);
    try {
      await attendanceApi.correctClocking(editing.id, {
        clockInTimeUtc: values.clockIn.toISOString(),
        clockOutTimeUtc: values.clockOut?.toISOString(),
        breakStartedAtUtc: values.breakStart?.toISOString(),
        breakEndedAtUtc: values.breakEnd?.toISOString(),
        reason: values.reason.trim(),
      });
      message.success('Marcación corregida y auditada');
      setEditing(undefined);
      form.resetFields();
      await load();
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setSaving(false);
    }
  };

  const openManual = () => {
    manualForm.resetFields();
    manualForm.setFieldsValue({ clockIn: dayjs().second(0).millisecond(0) });
    setManualOpen(true);
  };

  const saveManual = async (values: ManualAttendanceValues) => {
    setSaving(true);
    try {
      const notes = values.notes?.trim();
      await attendanceApi.createManualClocking({
        employeeId: values.employeeId,
        clockInTimeUtc: values.clockIn.toISOString(),
        clockOutTimeUtc: values.clockOut?.toISOString(),
        breakStartedAtUtc: values.breakStart?.toISOString(),
        breakEndedAtUtc: values.breakEnd?.toISOString(),
        reason: notes ? `${values.reasonType}: ${notes}` : values.reasonType,
      });
      message.success('Marcación manual registrada y auditada');
      setManualOpen(false);
      manualForm.resetFields();
      await load();
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setSaving(false);
    }
  };

  const showHistory = async (row: AttendanceAdminRowDto) => {
    try {
      setHistory((await attendanceApi.getCorrections(row.id)).data);
      setHistoryOpen(true);
    } catch (error) {
      message.error(formatError(error));
    }
  };

  const totalWorked = rows.reduce((sum, row) => sum + row.workedMinutes, 0);
  const totalOvertime = rows.reduce((sum, row) => sum + row.overtimeMinutes, 0);
  const lateCount = rows.filter((row) => row.lateMinutes > 0).length;
  const exceededBreaks = rows.filter((row) => row.breakMinutes > 30).length;

  return <>
    <Card title={<Space><ClockCircleOutlined />Control de asistencia</Space>}>
      <Space wrap style={{ marginBottom: 20 }}>
        <DatePicker.RangePicker value={dateRange} allowClear={false} format="DD/MM/YYYY"
          onChange={(value) => value && setDateRange([value[0]!, value[1]!])} />
        <Select allowClear showSearch optionFilterProp="label" placeholder="Todos los empleados" style={{ width: 280 }}
          value={employeeId} onChange={setEmployeeId}
          options={employees.map((employee) => ({ value: employee.id, label: `${employee.firstName} ${employee.lastName}` }))} />
        <Button icon={<ReloadOutlined />} onClick={() => void load()}>Actualizar</Button>
        {hasPermission(PERMISSIONS.rrhh.attendanceManage) &&
          <Button type="primary" icon={<PlusOutlined />} onClick={openManual}>Nueva marcación manual</Button>}
      </Space>

      <Row gutter={[16, 16]} style={{ marginBottom: 20 }}>
        <Col xs={12} md={6}><Card size="small"><Statistic title="Jornadas" value={rows.length} /></Card></Col>
        <Col xs={12} md={6}><Card size="small"><Statistic title="Horas trabajadas" value={duration(totalWorked)} /></Card></Col>
        <Col xs={12} md={6}><Card size="small"><Statistic title="Horas extra" value={duration(totalOvertime)} /></Card></Col>
        <Col xs={12} md={6}><Card size="small"><Statistic title="Alertas" value={lateCount + exceededBreaks} /></Card></Col>
      </Row>

      <Table rowKey="id" loading={loading} dataSource={rows} scroll={{ x: 1250 }} columns={[
        { title: 'Fecha', dataIndex: 'workDate', render: (value: string) => dayjs(value).format('DD/MM/YYYY') },
        { title: 'Empleado', dataIndex: 'employeeName', fixed: 'left' },
        { title: 'Estado', dataIndex: 'status', render: statusTag },
        { title: 'Origen', dataIndex: 'clockInMethod', render: (value: number) =>
          value === 3 ? <Tag color="purple">Manual</Tag> : <Tag color="blue">Biométrico</Tag> },
        { title: 'Entrada', dataIndex: 'clockInTimeUtc', render: time },
        { title: 'Inicio descanso', dataIndex: 'breakStartedAtUtc', render: time },
        { title: 'Fin descanso', dataIndex: 'breakEndedAtUtc', render: time },
        { title: 'Salida', dataIndex: 'clockOutTimeUtc', render: time },
        { title: 'Descanso', dataIndex: 'breakMinutes', render: (value: number) =>
          <Badge status={value > 30 ? 'error' : 'default'} text={`${value} min`} /> },
        { title: 'Atraso', dataIndex: 'lateMinutes', render: (value: number) =>
          value > 0 ? <Tag color="red">{value} min</Tag> : '—' },
        { title: 'Trabajado', dataIndex: 'workedMinutes', render: duration },
        { title: 'Extra', dataIndex: 'overtimeMinutes', render: duration },
        { title: 'Acciones', fixed: 'right', render: (_: unknown, row: AttendanceAdminRowDto) => <Space>
          {hasPermission(PERMISSIONS.rrhh.attendanceManage) &&
            <Button icon={<EditOutlined />} onClick={() => openCorrection(row)}>Corregir</Button>}
          <Button icon={<HistoryOutlined />} disabled={row.correctionCount === 0} onClick={() => void showHistory(row)}>
            Historial ({row.correctionCount})
          </Button>
        </Space> },
      ]} />
    </Card>

    <Modal title="Nueva marcación manual" open={manualOpen} footer={null}
      onCancel={() => { setManualOpen(false); manualForm.resetFields(); }} destroyOnHidden>
      <Form form={manualForm} layout="vertical" onFinish={saveManual}>
        <Form.Item name="employeeId" label="Empleado" rules={[{ required: true, message: 'Selecciona un empleado' }]}>
          <Select showSearch optionFilterProp="label" placeholder="Selecciona un empleado"
            options={employees.map((employee) => ({ value: employee.id, label: `${employee.firstName} ${employee.lastName}` }))} />
        </Form.Item>
        <Form.Item name="clockIn" label="Entrada" rules={[{ required: true, message: 'Ingresa la fecha y hora de entrada' }]}>
          <DatePicker showTime format="DD/MM/YYYY HH:mm" style={{ width: '100%' }} />
        </Form.Item>
        <Form.Item name="breakStart" label="Inicio del descanso">
          <DatePicker showTime format="DD/MM/YYYY HH:mm" style={{ width: '100%' }} />
        </Form.Item>
        <Form.Item name="breakEnd" label="Fin del descanso">
          <DatePicker showTime format="DD/MM/YYYY HH:mm" style={{ width: '100%' }} />
        </Form.Item>
        <Form.Item name="clockOut" label="Salida">
          <DatePicker showTime format="DD/MM/YYYY HH:mm" style={{ width: '100%' }} />
        </Form.Item>
        <Form.Item name="reasonType" label="Motivo" rules={[{ required: true, message: 'Selecciona un motivo' }]}>
          <Select placeholder="Selecciona el motivo"
            options={manualReasons.map((reason) => ({ value: reason, label: reason }))} />
        </Form.Item>
        <Form.Item noStyle shouldUpdate={(previous, current) => previous.reasonType !== current.reasonType}>
          {({ getFieldValue }) => <Form.Item name="notes" label="Observación"
            rules={[{ required: getFieldValue('reasonType') === 'Otro', message: 'Describe el motivo' }, { max: 400 }]}>
            <Input.TextArea rows={3} placeholder="Detalle adicional de la contingencia" />
          </Form.Item>}
        </Form.Item>
        <Button type="primary" htmlType="submit" loading={saving} block>Registrar marcación manual</Button>
      </Form>
    </Modal>

    <Modal title={`Corregir marcación · ${editing?.employeeName ?? ''}`} open={Boolean(editing)} footer={null}
      onCancel={() => { setEditing(undefined); form.resetFields(); }} destroyOnHidden>
      <Form form={form} layout="vertical" onFinish={saveCorrection}>
        <Form.Item name="clockIn" label="Entrada" rules={[{ required: true }]}><DatePicker showTime format="DD/MM/YYYY HH:mm" style={{ width: '100%' }} /></Form.Item>
        <Form.Item name="breakStart" label="Inicio del descanso"><DatePicker showTime format="DD/MM/YYYY HH:mm" style={{ width: '100%' }} /></Form.Item>
        <Form.Item name="breakEnd" label="Fin del descanso"><DatePicker showTime format="DD/MM/YYYY HH:mm" style={{ width: '100%' }} /></Form.Item>
        <Form.Item name="clockOut" label="Salida"><DatePicker showTime format="DD/MM/YYYY HH:mm" style={{ width: '100%' }} /></Form.Item>
        <Form.Item name="reason" label="Motivo de la corrección" rules={[{ required: true }, { min: 5 }, { max: 500 }]}>
          <Input.TextArea rows={3} placeholder="Explica por qué se corrige esta marcación" />
        </Form.Item>
        <Button type="primary" htmlType="submit" loading={saving} block>Guardar corrección</Button>
      </Form>
    </Modal>

    <Modal title="Historial de correcciones" open={historyOpen} onCancel={() => setHistoryOpen(false)} footer={null} width={780}>
      <Table rowKey="id" dataSource={history} pagination={false} expandable={{
        expandedRowRender: (item) => <Row gutter={12}>
          <Col span={12}><Typography.Text strong>Antes</Typography.Text><pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(JSON.parse(item.beforeJson), null, 2)}</pre></Col>
          <Col span={12}><Typography.Text strong>Después</Typography.Text><pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(JSON.parse(item.afterJson), null, 2)}</pre></Col>
        </Row>,
      }} columns={[
        { title: 'Fecha', dataIndex: 'correctedAtUtc', render: (value: string) => formatBranchDateTime(value) },
        { title: 'Motivo', dataIndex: 'reason' },
        { title: 'Usuario', dataIndex: 'correctedByUserId', ellipsis: true },
      ]} />
    </Modal>
  </>;
};
