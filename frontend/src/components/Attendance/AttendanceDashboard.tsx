import { useCallback, useEffect, useState } from 'react';
import { App, Badge, Button, Card, Col, DatePicker, Descriptions, Form, Grid, Input, Modal, Row, Select, Space, Statistic, Table, Tag, Tooltip, Typography } from 'antd';
import { ClockCircleOutlined, EditOutlined, HistoryOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { attendanceApi, type AttendanceAdminRowDto, type AttendanceCorrectionDto } from '../../services/attendanceApi';
import { employeeApi } from '../../services/api';
import type { EmployeeDto } from '../../types';
import { formatError } from '../../utils/errorHandler';
import { useAuth } from '../../context/useAuth';
import { PERMISSIONS } from '../../constants/permissions';
import { branchDateTimeUtcIso, formatBranchDateTime, formatBranchTime, toBranchDayjs } from '../../utils/branchTimeZone';
import './AttendanceDashboard.css';

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

const originTag = (method: AttendanceAdminRowDto['clockInMethod']) =>
  method === 3 ? <Tag color="orange">Manual</Tag> : <Tag color="cyan">Biométrico</Tag>;

const time = (value?: string) => formatBranchTime(value, '—');
const duration = (minutes: number) => `${Math.floor(minutes / 60)}h ${minutes % 60}m`;

export const AttendanceDashboard = () => {
  const screens = Grid.useBreakpoint();
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
        clockInTimeUtc: branchDateTimeUtcIso(values.clockIn)!,
        clockOutTimeUtc: branchDateTimeUtcIso(values.clockOut),
        breakStartedAtUtc: branchDateTimeUtcIso(values.breakStart),
        breakEndedAtUtc: branchDateTimeUtcIso(values.breakEnd),
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
    manualForm.setFieldsValue({ clockIn: toBranchDayjs(new Date())!.second(0).millisecond(0) });
    setManualOpen(true);
  };

  const saveManual = async (values: ManualAttendanceValues) => {
    setSaving(true);
    try {
      const notes = values.notes?.trim();
      await attendanceApi.createManualClocking({
        employeeId: values.employeeId,
        clockInTimeUtc: branchDateTimeUtcIso(values.clockIn)!,
        clockOutTimeUtc: branchDateTimeUtcIso(values.clockOut),
        breakStartedAtUtc: branchDateTimeUtcIso(values.breakStart),
        breakEndedAtUtc: branchDateTimeUtcIso(values.breakEnd),
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
    <Card className="attendance-dashboard" title={<Space><ClockCircleOutlined />Control de asistencia</Space>}>
      <Space wrap className="attendance-filters" style={{ marginBottom: 20 }}>
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

      <Table<AttendanceAdminRowDto> className="attendance-table" size="small" tableLayout="fixed" rowKey="id" loading={loading} dataSource={rows}
        expandable={screens.xl ? undefined : {
          expandedRowRender: (row) => <Descriptions size="small" column={{ xs: 1, sm: 2, md: 3 }} items={[
            { key: 'date', label: 'Fecha', children: dayjs(row.workDate).format('DD/MM/YYYY') },
            { key: 'origin', label: 'Origen', children: originTag(row.clockInMethod) },
            { key: 'in', label: 'Entrada', children: time(row.clockInTimeUtc) },
            { key: 'break-start', label: 'Inicio descanso', children: time(row.breakStartedAtUtc) },
            { key: 'break-end', label: 'Fin descanso', children: time(row.breakEndedAtUtc) },
            { key: 'break', label: 'Descanso', children: `${row.breakMinutes} min` },
            { key: 'out', label: 'Salida', children: time(row.clockOutTimeUtc) },
            { key: 'late', label: 'Atraso', children: `${row.lateMinutes} min` },
            { key: 'worked', label: 'Trabajado', children: duration(row.workedMinutes) },
            { key: 'extra', label: 'Horas extra', children: duration(row.overtimeMinutes) },
          ]} />,
          columnWidth: 28,
        }} columns={[
        { title: 'Fecha', dataIndex: 'workDate', width: 96, responsive: ['md'], className: 'attendance-nowrap', render: (value: string) => dayjs(value).format('DD/MM/YYYY') },
        { title: 'Empleado', dataIndex: 'employeeName', className: 'attendance-employee' },
        { title: 'Estado', key: 'status', width: 108, align: 'center', render: (_, row) => <div className="attendance-time-stack attendance-status-stack">
          <span>{statusTag(row.status)}</span>
          {screens.xl && <span>{originTag(row.clockInMethod)}</span>}
        </div> },
        { title: 'Entrada', dataIndex: 'clockInTimeUtc', width: 64, responsive: ['lg'], className: 'attendance-nowrap', render: time },
        { title: 'Descanso', key: 'break', width: 112, responsive: ['xl'], render: (_, row) => <div className="attendance-time-stack">
          <span>{time(row.breakStartedAtUtc)} / {time(row.breakEndedAtUtc)}</span>
          <Badge status={row.breakMinutes > 30 ? 'error' : 'default'} text={`${row.breakMinutes} min`} />
        </div> },
        { title: 'Salida', dataIndex: 'clockOutTimeUtc', width: 64, responsive: ['lg'], className: 'attendance-nowrap', render: time },
        { title: 'Atraso', dataIndex: 'lateMinutes', width: 64, responsive: ['xl'], render: (value: number) =>
          value > 0 ? <Tag color="red">{value} min</Tag> : '—' },
        { title: 'Jornada', key: 'duration', width: 96, responsive: ['xl'], render: (_, row) => <div className="attendance-time-stack">
          <span>{duration(row.workedMinutes)}</span>
          <Typography.Text type="secondary">Extra: {duration(row.overtimeMinutes)}</Typography.Text>
        </div> },
        { title: 'Acciones', width: 80, render: (_: unknown, row: AttendanceAdminRowDto) => <Space size={4}>
          {hasPermission(PERMISSIONS.rrhh.attendanceManage) &&
            <Tooltip title="Corregir marcación"><Button size="small" aria-label="Corregir marcación" icon={<EditOutlined />} onClick={() => openCorrection(row)} /></Tooltip>}
          <Tooltip title={`Historial (${row.correctionCount})`}><Button size="small" aria-label={`Historial (${row.correctionCount})`} icon={<HistoryOutlined />} disabled={row.correctionCount === 0} onClick={() => void showHistory(row)} /></Tooltip>
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
