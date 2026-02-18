import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Button, Calendar, Card, DatePicker, message, Modal, Space, Spin, Table, Tabs, Typography, Select, Tag, Form, Input, InputNumber, TimePicker, Row, Col, List } from 'antd';
import type { TableColumnsType } from 'antd';
import { CalendarOutlined, BarChartOutlined } from '@ant-design/icons';
import html2canvas from 'html2canvas';
import jsPDF from 'jspdf';
import dayjs, { Dayjs } from 'dayjs';
import isoWeek from 'dayjs/plugin/isoWeek';
import 'dayjs/locale/es';
import { scheduleShiftApi, scheduleConfigurationApi, workAreaApi, workRoleApi, employeeWorkRoleApi, branchApi } from '../../services/api';
import { useAuth } from '../../context/AuthContext';
import { formatError } from '../../utils/errorHandler';
import { EmployeeStats } from './EmployeeStats';
import type { ShiftAssignmentDto, EmployeeDto, ScheduleConfigurationDto, WorkAreaDto, WorkRoleDto } from '../../types';
import logo from '../../assets/ECF-Logo.png';

dayjs.extend(isoWeek);
dayjs.locale('es');

const { Text } = Typography;

// Función para generar colores complementarios a partir del color base
const getColorVariants = (baseColor: string) => {
  // Si el color está vacío o es inválido, retornar colores por defecto
  if (!baseColor || baseColor === '#808080' || baseColor === '') {
    return { bg: '#fafafa', text: '#666', border: '#e8e8e8' };
  }
  
  // Usar el color base directamente como text
  // Crear un fondo más claro agregando transparencia o lighten
  const bg = baseColor + '20'; // Agregar transparencia 20 (hex)
  const border = baseColor + '60'; // Agregar transparencia 60 (hex)
  
  return { bg, text: baseColor, border };
};

export const MonthlySchedule = () => {
  const { branchId, user } = useAuth();
  const hasInitializedWeek = useRef(false);
  const weekPrintRef = useRef<HTMLDivElement | null>(null);
  const [selectedMonth, setSelectedMonth] = useState<Dayjs>(dayjs());
  const [selectedWeekStart, setSelectedWeekStart] = useState<Dayjs>(dayjs().startOf('week'));
  const [loading, setLoading] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [shifts, setShifts] = useState<ShiftAssignmentDto[]>([]);
  const [viewMode, setViewMode] = useState<'month' | 'week'>('week');
  const [freeEmployeesByDate, setFreeEmployeesByDate] = useState<Record<string, EmployeeDto[]>>({});
  const [freeDayColor, setFreeDayColor] = useState<string>('#E8E8E8');
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string | null>(null);
  const [eligibleEmployees, setEligibleEmployees] = useState<EmployeeDto[]>([]);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [editShift, setEditShift] = useState<ShiftAssignmentDto | null>(null);
  const [savingEdit, setSavingEdit] = useState(false);
  const [savingDelete, setSavingDelete] = useState(false);
  const [editForm] = Form.useForm();
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [savingCreate, setSavingCreate] = useState(false);
  const [createForm] = Form.useForm();
  const [workAreas, setWorkAreas] = useState<WorkAreaDto[]>([]);
  const [workRoles, setWorkRoles] = useState<WorkRoleDto[]>([]);
  const [employeeRoleMap, setEmployeeRoleMap] = useState<Record<string, Set<string>>>({});
  const [selectedWorkRoleId, setSelectedWorkRoleId] = useState<string | null>(null);
  const [mainTab, setMainTab] = useState<'schedule' | 'stats'>('schedule');
  const [branchName, setBranchName] = useState<string>('');
  const [isExporting, setIsExporting] = useState(false);

  const handleExportPdf = useCallback(async () => {
    try {
      if (mainTab !== 'schedule') {
        setMainTab('schedule');
      }

      if (viewMode !== 'week') {
        setViewMode('week');
      }

      setIsExporting(true);
      await new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)));

      const target = weekPrintRef.current;
      if (!target) {
        message.error('No se pudo preparar la vista semanal para exportar.');
        return;
      }

      const width = target.scrollWidth;
      const height = target.scrollHeight;

      const canvas = await html2canvas(target, {
        backgroundColor: '#ffffff',
        scale: 2.5,
        useCORS: true,
        scrollX: 0,
        scrollY: -window.scrollY,
        width,
        height,
        windowWidth: width,
        windowHeight: height
      });

      const imgData = canvas.toDataURL('image/png');
      const pdf = new jsPDF('landscape', 'mm', 'a4');
      const pageWidth = pdf.internal.pageSize.getWidth();
      const pageHeight = pdf.internal.pageSize.getHeight();

      const margin = 8;
      const imgWidth = pageWidth - margin * 2;
      const imgHeight = (canvas.height * imgWidth) / canvas.width;
      const x = margin;
      let y = margin + 4;

      if (imgHeight <= pageHeight - margin * 2) {
        pdf.addImage(imgData, 'PNG', x, y, imgWidth, imgHeight);
      } else {
        let remainingHeight = imgHeight;
        let position = 0;
        while (remainingHeight > 0) {
          pdf.addImage(imgData, 'PNG', x, y - position, imgWidth, imgHeight);
          remainingHeight -= pageHeight - margin * 2;
          position += pageHeight - margin * 2;
          if (remainingHeight > 0) {
            pdf.addPage();
            y = margin + 4;
          }
        }
      }

      pdf.save(`turnos-${selectedMonth.format('YYYY-MM')}-semana.pdf`);
    } catch (error) {
      message.error('Error al exportar el PDF.');
      console.error(error);
    } finally {
      setIsExporting(false);
    }
  }, [mainTab, selectedMonth, viewMode]);

  const loadScheduleConfig = useCallback(async () => {
    if (!branchId) return;
    try {
      const response = await scheduleConfigurationApi.get(branchId);
      const config = response.data as ScheduleConfigurationDto | null;
      if (config?.freeDayColor) {
        setFreeDayColor(config.freeDayColor);
      }
    } catch (error) {
      console.error(error);
    }
  }, [branchId]);

  const loadBranchName = useCallback(async () => {
    try {
      const response = await branchApi.getCurrent();
      setBranchName(response.data?.name || '');
    } catch (error) {
      console.error(error);
    }
  }, []);


  const loadShifts = useCallback(async (month: Dayjs) => {
    if (!branchId) return;
    setLoading(true);
    try {
      const year = month.year();
      const monthNumber = month.month() + 1;
      const response = await scheduleShiftApi.getMonthly(branchId, year, monthNumber);
      setShifts(Array.isArray(response.data) ? response.data : []);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [branchId]);

  const loadEligibleEmployees = useCallback(async () => {
    try {
      const response = await scheduleShiftApi.getEligibleEmployees();
      setEligibleEmployees(Array.isArray(response.data) ? response.data : []);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    }
  }, []);

  const loadEmployeeRoles = useCallback(async () => {
    if (eligibleEmployees.length === 0) return;
    try {
      const responses = await Promise.all(
        eligibleEmployees.map((employee) => employeeWorkRoleApi.getByEmployee(employee.id))
      );

      const map: Record<string, Set<string>> = {};
      responses.forEach((response, index) => {
        const employeeId = eligibleEmployees[index].id;
        const roles = Array.isArray(response.data) ? response.data : [];
        map[employeeId] = new Set(roles.map((r) => r.workRoleId));
      });

      setEmployeeRoleMap(map);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    }
  }, [eligibleEmployees]);

  const loadWorkAreas = useCallback(async () => {
    if (!branchId) return;
    try {
      const response = await workAreaApi.getAll(branchId);
      setWorkAreas(Array.isArray(response.data) ? response.data : []);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    }
  }, [branchId]);

  const loadWorkRoles = useCallback(async (workAreaId?: string) => {
    try {
      const response = await workRoleApi.getAll(workAreaId);
      setWorkRoles(Array.isArray(response.data) ? response.data : []);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    }
  }, []);

  useEffect(() => {
    loadShifts(selectedMonth);
  }, [loadShifts, selectedMonth]);

  useEffect(() => {
    if (!branchId) return;
    loadBranchName();
  }, [branchId, loadBranchName]);

  useEffect(() => {
    loadScheduleConfig();
  }, [loadScheduleConfig]);

  useEffect(() => {
    loadEligibleEmployees();
  }, [loadEligibleEmployees]);

  useEffect(() => {
    loadEmployeeRoles();
  }, [loadEmployeeRoles]);

  useEffect(() => {
    loadWorkAreas();
  }, [loadWorkAreas]);

  const parseTimeValue = (value?: string) => (value ? dayjs(value, ['HH:mm:ss', 'HH:mm']) : null);
  const parseMinutesValue = (value?: string) => {
    if (!value) return null;
    const parts = value.split(':').map((part) => Number(part));
    if (parts.some((part) => Number.isNaN(part))) return null;
    const [hours = 0, minutes = 0, seconds = 0] = parts;
    return Math.round(hours * 60 + minutes + seconds / 60);
  };

  const toTimeSpanFromMinutes = (minutes?: number | null) => {
    if (minutes == null) return undefined;
    const total = Math.max(0, Math.floor(minutes));
    const hours = Math.floor(total / 60);
    const mins = total % 60;
    return `${String(hours).padStart(2, '0')}:${String(mins).padStart(2, '0')}:00`;
  };

  const loadFreeEmployees = useCallback(async () => {
    if (!branchId) return;
    try {
      const dates = Array.from({ length: 7 }, (_, i) => selectedWeekStart.add(i, 'day'));
      const responses = await Promise.all(
        dates.map((date) => scheduleShiftApi.getFreeEmployees(branchId, date.format('YYYY-MM-DD')))
      );

      const map: Record<string, EmployeeDto[]> = {};
      responses.forEach((response, index) => {
        const key = dates[index].format('YYYY-MM-DD');
        map[key] = Array.isArray(response.data) ? response.data : [];
      });

      setFreeEmployeesByDate(map);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    }
  }, [branchId, selectedWeekStart]);

  const openEditModal = useCallback((shift: ShiftAssignmentDto) => {
    setEditShift(shift);
    editForm.setFieldsValue({
      employeeId: shift.employeeId,
      date: dayjs(shift.date),
      startTime: parseTimeValue(shift.startTime),
      endTime: parseTimeValue(shift.endTime),
      breakDuration: parseMinutesValue(shift.breakDuration),
      lunchDuration: parseMinutesValue(shift.lunchDuration),
      notes: shift.notes ?? ''
    });
    setEditModalOpen(true);
  }, [editForm]);

  const handleUpdateShift = useCallback(async () => {
    if (!editShift) return;
    try {
      const values = await editForm.validateFields();
      setSavingEdit(true);

      const payload = {
        employeeId: values.employeeId as string,
        date: (values.date as Dayjs).format('YYYY-MM-DD'),
        startTime: (values.startTime as Dayjs).format('HH:mm:ss'),
        endTime: (values.endTime as Dayjs).format('HH:mm:ss'),
        breakDuration: toTimeSpanFromMinutes(values.breakDuration as number | null),
        lunchDuration: toTimeSpanFromMinutes(values.lunchDuration as number | null),
        notes: values.notes as string
      };

      const response = await scheduleShiftApi.update(editShift.id, payload);
      const updated = response.data as ShiftAssignmentDto;
      setShifts((prev) => prev.map((s) => (s.id === updated.id ? updated : s)));
      if (viewMode === 'week') {
        await loadFreeEmployees();
      }
      message.success('Turno actualizado correctamente.');
      setEditModalOpen(false);
      setEditShift(null);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setSavingEdit(false);
    }
  }, [editShift, editForm, loadFreeEmployees, viewMode]);

  const handleDeleteShift = useCallback(() => {
    if (!editShift) return;

    Modal.confirm({
      title: 'Eliminar turno',
      content: 'Este turno se eliminara permanentemente. Esta accion no se puede deshacer.',
      okText: 'Eliminar',
      okType: 'danger',
      cancelText: 'Cancelar',
      onOk: async () => {
        try {
          setSavingDelete(true);
          await scheduleShiftApi.delete(editShift.id);
          setShifts((prev) => prev.filter((s) => s.id !== editShift.id));
          if (viewMode === 'week') {
            await loadFreeEmployees();
          }
          message.success('Turno eliminado correctamente.');
          setEditModalOpen(false);
          setEditShift(null);
        } catch (error) {
          message.error(formatError(error));
          console.error(error);
        } finally {
          setSavingDelete(false);
        }
      }
    });
  }, [editShift, loadFreeEmployees, viewMode]);

  const openCreateModal = useCallback(() => {
    createForm.resetFields();
    setWorkRoles([]);
    setSelectedWorkRoleId(null);
    setCreateModalOpen(true);
  }, [createForm]);

  const handleCreateShift = useCallback(async () => {
    try {
      const values = await createForm.validateFields();
      const selectedDate = (values.date as Dayjs).format('YYYY-MM-DD');
      const selectedEmployeeId = values.employeeId as string;

      const alreadyAssigned = shifts.some((shift) =>
        shift.employeeId === selectedEmployeeId && dayjs(shift.date).format('YYYY-MM-DD') === selectedDate
      );

      if (alreadyAssigned) {
        message.error('Este empleado ya tiene un turno asignado en esa fecha.');
        return;
      }

      setSavingCreate(true);

      const payload = {
        employeeId: values.employeeId as string,
        date: selectedDate,
        startTime: (values.startTime as Dayjs).format('HH:mm:ss'),
        endTime: (values.endTime as Dayjs).format('HH:mm:ss'),
        breakDuration: toTimeSpanFromMinutes(values.breakDuration as number | null),
        lunchDuration: toTimeSpanFromMinutes(values.lunchDuration as number | null),
        workAreaId: values.workAreaId as string,
        workRoleId: values.workRoleId as string,
        notes: values.notes as string
      };

      await scheduleShiftApi.create(payload);
      await loadShifts(selectedMonth);
      if (viewMode === 'week') {
        await loadFreeEmployees();
      }
      message.success('Turno creado correctamente.');
      setCreateModalOpen(false);
    } catch (error) {
      message.error(formatError(error));
      console.error(error);
    } finally {
      setSavingCreate(false);
    }
  }, [createForm, shifts, loadFreeEmployees, loadShifts, selectedMonth, viewMode]);

  const handleGenerate = useCallback(async () => {
    if (!branchId) return;

    // Validación 1: No permitir generar meses pasados
    const today = dayjs();
    const selectedYear = selectedMonth.year();
    const selectedMonthNumber = selectedMonth.month() + 1;
    const currentYear = today.year();
    const currentMonthNumber = today.month() + 1;

    if (selectedYear < currentYear || (selectedYear === currentYear && selectedMonthNumber < currentMonthNumber)) {
      message.error('No se puede generar turnos para meses pasados. Los empleados ya deberían estar trabajando según el horario anterior.');
      return;
    }

    // Validación 2: Si es el mes actual, verificar que no intente regenerar días pasados
    const isCurrentMonth = selectedYear === currentYear && selectedMonthNumber === currentMonthNumber;
    const generationStart = isCurrentMonth ? today.add(1, 'day') : null;
    
    // Validación 3: Si ya existen turnos, mostrar advertencia
    const hasExistingShifts = shifts.some(shift => 
      dayjs(shift.date).isSame(selectedMonth, 'month')
    );

    if (hasExistingShifts) {
      Modal.confirm({
        title: '⚠️ Advertencia: Regenerar Turnos',
        content: (
          <div>
            <p>Ya existen turnos generados para <strong>{selectedMonth.format('MMMM YYYY')}</strong>.</p>
            <p style={{ marginTop: 12 }}>
              <strong>¿Está seguro que desea regenerarlos?</strong>
            </p>
            <ul style={{ marginTop: 12, color: '#ff4d4f' }}>
              <li>Se eliminarán todos los turnos actuales del mes</li>
              <li>Los empleados podrían estar viendo horarios que cambiarán</li>
              <li>Esto puede causar confusión si se hace frecuentemente</li>
            </ul>
            {isCurrentMonth && (
              <p style={{ marginTop: 12, color: '#faad14', fontWeight: 'bold' }}>
                Nota: Es el mes actual. Solo se regenerarán turnos desde el día {generationStart?.format('DD/MM/YYYY')} en adelante.
              </p>
            )}
          </div>
        ),
        okText: 'Sí, regenerar',
        okType: 'danger',
        cancelText: 'Cancelar',
        onOk: async () => {
          await executeGeneration();
        }
      });
    } else {
      await executeGeneration();
    }

    async function executeGeneration() {
      setGenerating(true);
      try {
        const year = selectedMonth.year();
        const monthNumber = selectedMonth.month() + 1;
        const result = await scheduleShiftApi.generate(year, monthNumber);
        const { warnings, totalShiftsGenerated, totalShiftsNotCovered } = result.data;
      
      if (warnings && warnings.length > 0) {
        const precheckWarnings = warnings.filter(w => w.reason?.startsWith('PreCheck:'));
        const generationWarnings = warnings.filter(w => !w.reason?.startsWith('PreCheck:'));

        const columns = [
          {
            title: 'Fecha',
            dataIndex: 'date',
            key: 'date',
            width: 100,
            render: (date: string) => dayjs(date).format('DD/MM/YYYY')
          },
          {
            title: 'Día',
            dataIndex: 'dayOfWeek',
            key: 'dayOfWeek',
            width: 100
          },
          {
            title: 'Área',
            dataIndex: 'workAreaName',
            key: 'workAreaName',
            width: 120
          },
          {
            title: 'Rol',
            dataIndex: 'workRoleName',
            key: 'workRoleName',
            width: 120
          },
          {
            title: 'Requerido',
            dataIndex: 'requiredCount',
            key: 'requiredCount',
            width: 80,
            align: 'center' as const
          },
          {
            title: 'Asignado',
            dataIndex: 'assignedCount',
            key: 'assignedCount',
            width: 80,
            align: 'center' as const
          },
          {
            title: 'Motivo',
            dataIndex: 'reason',
            key: 'reason',
            ellipsis: true
          }
        ];

        const generationStart = isCurrentMonth ? dayjs().add(1, 'day') : null;

        Modal.warning({
          title: `Generación completada con observaciones`,
          width: 900,
          content: (
            <div>
              <p style={{ marginBottom: 16 }}>
                <strong>Turnos generados:</strong> {totalShiftsGenerated}<br />
                <strong>Turnos sin cubrir:</strong> {totalShiftsNotCovered}
              </p>
              {generationStart && (
                <p style={{ marginBottom: 16 }}>
                  <strong>Regeneración parcial:</strong> desde {generationStart.format('DD/MM/YYYY')}
                </p>
              )}

              {precheckWarnings.length > 0 && (
                <div style={{ marginBottom: 16 }}>
                  <div style={{ marginBottom: 8 }}>
                    <Tag color="blue">PreCheck</Tag>
                    <strong>Validación previa de capacidad</strong>
                  </div>
                    <Table
                    dataSource={precheckWarnings}
                    pagination={false}
                    size="small"
                      rowKey={(record) => `${record.date}-${record.workAreaName}-${record.workRoleName}-${record.reason}-${record.requiredCount}-${record.assignedCount}`}
                    columns={columns}
                  />
                </div>
              )}

              {generationWarnings.length > 0 && (
                <div>
                  <div style={{ marginBottom: 8 }}>
                    <Tag color="gold">Generación</Tag>
                    <strong>Observaciones durante la generación</strong>
                  </div>
                    <Table
                    dataSource={generationWarnings}
                    pagination={false}
                    size="small"
                      rowKey={(record) => `${record.date}-${record.workAreaName}-${record.workRoleName}-${record.reason}-${record.requiredCount}-${record.assignedCount}`}
                    columns={columns}
                  />
                </div>
              )}

              <p style={{ marginTop: 16, color: '#ff4d4f' }}>
                <strong>Importante:</strong> Revise las observaciones anteriores para asegurar que tiene el personal necesario.
                Puede que necesite contratar empleados con estos roles o ajustar la disponibilidad del personal existente.
              </p>
            </div>
          )
        });
      } else {
        const generationStart = isCurrentMonth ? dayjs().add(1, 'day') : null;
        const partialMessage = generationStart
          ? ` (desde ${generationStart.format('DD/MM/YYYY')})`
          : '';
        message.success(`${totalShiftsGenerated} turnos generados correctamente${partialMessage}`);
      }
      
      await loadShifts(selectedMonth);
      if (viewMode === 'week') {
        await loadFreeEmployees();
      }
    } catch (error: unknown) {
      const axiosError = error as { response?: { data?: { message?: string } } };
      const errorMessage = axiosError.response?.data?.message || 'Error al generar turnos';
      message.error(errorMessage);
      console.error(error);
    } finally {
      setGenerating(false);
    }
    }
  }, [branchId, loadShifts, loadFreeEmployees, selectedMonth, viewMode, shifts]);

  useEffect(() => {
    if (viewMode !== 'week') {
      hasInitializedWeek.current = false;
      return;
    }

    if (!hasInitializedWeek.current) {
      setSelectedWeekStart(dayjs().startOf('week'));
      hasInitializedWeek.current = true;
    }
  }, [viewMode]);

  useEffect(() => {
    if (viewMode === 'week') {
      loadFreeEmployees();
    }
  }, [loadFreeEmployees, viewMode]);

  const shiftsByDate = useMemo(() => {
    const map = new Map<string, ShiftAssignmentDto[]>();
    shifts.forEach((shift) => {
      const key = dayjs(shift.date).format('YYYY-MM-DD');
      const list = map.get(key) ?? [];
      list.push(shift);
      map.set(key, list);
    });
    return map;
  }, [shifts]);

  // Para la vista de semana
  const weekShifts = useMemo(() => {
    const result: { date: Dayjs; dayName: string; dayShifts: ShiftAssignmentDto[] }[] = [];
    for (let i = 0; i < 7; i++) {
      const date = selectedWeekStart.add(i, 'day');
      const key = date.format('YYYY-MM-DD');
      const dayShifts = shiftsByDate.get(key) ?? [];
      result.push({
        date,
        dayName: date.format('dddd').charAt(0).toUpperCase() + date.format('dddd').slice(1),
        dayShifts,
      });
    }
    return result;
  }, [selectedWeekStart, shiftsByDate]);

  type WeekTableRow = { key: string } & Record<string, ReactNode>;

  const weekTableData = useMemo(() => {
    const data: WeekTableRow[] = [];
    const freeDayColors = getColorVariants(freeDayColor);

    // Obtener el número máximo de turnos en un día
    const maxShiftsPerDay = Math.max(...weekShifts.map(w => w.dayShifts.length), 1);

    for (let i = 0; i < maxShiftsPerDay; i++) {
      const row: WeekTableRow = { key: `shift-${i}` };
      weekShifts.forEach(({ date, dayShifts }) => {
        // Ordenar turnos alfabéticamente por área de trabajo
        const sortedDayShifts = [...dayShifts].sort((a, b) => 
          (a.workAreaName || '').localeCompare(b.workAreaName || '')
        );
        
        const shift = sortedDayShifts[i];
        const startTime = shift?.startTime?.substring(0, 5) || '--:--';
        const endTime = shift?.endTime?.substring(0, 5) || '--:--';
        const colors = getColorVariants(shift?.workAreaColor || '');
        
        row[date.format('YYYY-MM-DD')] = shift ? (
          <div 
            style={{ 
              fontSize: '9px',
              padding: '2px 4px',
              backgroundColor: colors.bg,
              border: `0.5px solid ${colors.border}`,
              borderRadius: '4px',
              minHeight: '34px',
              display: 'flex',
              flexDirection: 'column',
              justifyContent: 'space-between',
              cursor: 'pointer'
            }}
            onClick={() => openEditModal(shift)}
          >
            <div style={{ fontWeight: 600, color: colors.text }}>
              {startTime}-{endTime}
            </div>
            <div style={{ color: '#333', fontWeight: 500 }}>{shift.employeeName}</div>
            <div style={{ color: colors.text, fontSize: '8px', fontWeight: 500, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
              {shift.workAreaName} / {shift.workRoleName}
            </div>
          </div>
        ) : null;
      });
      data.push(row);
    }

    // Fila de empleados libres
    const freeRow: WeekTableRow = { key: 'free-employees' };
    weekShifts.forEach(({ date }) => {
      const key = date.format('YYYY-MM-DD');
      const freeEmployees = freeEmployeesByDate[key] ?? [];

      freeRow[key] = (
        <div
          style={{
            fontSize: '9px',
            padding: '2px 4px',
            backgroundColor: freeDayColors.bg,
            border: `0.5px dashed ${freeDayColors.border}`,
            borderRadius: '4px',
            minHeight: '34px',
            maxHeight: '80px',
            overflowY: 'auto',
          }}
        >
          <div style={{ fontWeight: 600, color: freeDayColors.text, marginBottom: 2 }}>Libres</div>
          {freeEmployees.length === 0 && (
            <div style={{ color: '#999' }}>Sin libres</div>
          )}
          {freeEmployees.map((employee) => (
            <div key={employee.id} style={{ color: '#333' }}>
              {employee.firstName} {employee.lastName}
            </div>
          ))}
        </div>
      );
    });
    data.push(freeRow);

    return data;
  }, [freeDayColor, freeEmployeesByDate, weekShifts, openEditModal]);

  const weekColumns = useMemo(() => {
    const columns: TableColumnsType<WeekTableRow> = [];
    weekShifts.forEach(({ date, dayName }) => {
      columns.push({
        title: (
          <div style={{ textAlign: 'center', fontWeight: 600, fontSize: '12px' }}>
            {dayName} {date.format('DD/MM')}
          </div>
        ),
        dataIndex: date.format('YYYY-MM-DD'),
        key: date.format('YYYY-MM-DD'),
        align: 'center' as const,
        width: '14.28%',
        onCell: () => ({ style: { verticalAlign: 'top' } }),
      });
    });
    return columns;
  }, [weekShifts]);

  // Obtener empleados únicos con turnos en el mes
  const employeesWithShifts = useMemo(() => {
    const monthShifts = shifts.filter(shift => 
      dayjs(shift.date).isSame(selectedMonth, 'month')
    );
    const uniqueEmployees = Array.from(
      new Map(monthShifts.map(s => [s.employeeId, { id: s.employeeId, name: s.employeeName }])).values()
    ).sort((a, b) => a.name.localeCompare(b.name));
    return uniqueEmployees;
  }, [shifts, selectedMonth]);

  const employeeStatsSummary = useMemo(() => {
    const summary: Record<string, { shiftCount: number; totalHours: number }> = {};
    shifts.forEach((shift) => {
      if (!dayjs(shift.date).isSame(selectedMonth, 'month')) return;
      if (!summary[shift.employeeId]) {
        summary[shift.employeeId] = { shiftCount: 0, totalHours: 0 };
      }
      summary[shift.employeeId].shiftCount += 1;
      summary[shift.employeeId].totalHours += shift.workedHours;
    });
    return summary;
  }, [shifts, selectedMonth]);

  const createModalEmployees = useMemo(() => {
    if (!selectedWorkRoleId) return eligibleEmployees;
    return eligibleEmployees.filter((employee) =>
      employeeRoleMap[employee.id]?.has(selectedWorkRoleId)
    );
  }, [eligibleEmployees, employeeRoleMap, selectedWorkRoleId]);

  const editModalEmployees = useMemo(() => {
    if (!editShift?.workRoleId) return eligibleEmployees;
    return eligibleEmployees.filter((employee) =>
      employeeRoleMap[employee.id]?.has(editShift.workRoleId)
    );
  }, [editShift?.workRoleId, eligibleEmployees, employeeRoleMap]);

  const exportedBy = user
    ? `${user.firstName} ${user.lastName}`.trim() || user.email
    : 'Usuario';
  const exportedEmail = user?.email || '';
  const branchLabel = branchName || 'Sucursal';

  return (
    <Card size="small">
      <Space orientation="vertical" size="large" style={{ width: '100%' }}>
        <Space orientation="horizontal" size="middle" style={{ width: '100%', justifyContent: 'space-between' }}>
          <Space>
            <DatePicker
              picker="month"
              value={selectedMonth}
              onChange={(value) => value && setSelectedMonth(value)}
              allowClear={false}
            />
            {mainTab === 'schedule' && (
              <>
                <Button
                  type="primary"
                  loading={generating}
                  onClick={handleGenerate}
                >
                  Generar turnos
                </Button>
                <Button onClick={openCreateModal}>
                  Agregar turno
                </Button>
                <Button onClick={handleExportPdf}>
                  Exportar PDF
                </Button>
              </>
            )}
          </Space>
        </Space>

        <Tabs
          activeKey={mainTab}
          onChange={(key) => setMainTab(key as 'schedule' | 'stats')}
          type="card"
          items={[
            {
              key: 'schedule',
              label: (
                <span>
                  <CalendarOutlined /> Horarios
                </span>
              ),
              children: (
                <Spin spinning={loading}>
                  <Tabs
                    activeKey={viewMode}
                    onChange={(key) => setViewMode(key as 'month' | 'week')}
                    items={[
              {
                      key: 'week',
                      label: 'Vista de Semana',
                      children: (
                        <div style={{ width: '100%', overflowX: 'auto' }}>
                        <div ref={weekPrintRef} style={{ background: '#fff', padding: isExporting ? 8 : 12, minWidth: isExporting ? 1120 : 0, width: isExporting ? 1120 : '100%', maxWidth: '100%', fontSize: isExporting ? 9 : undefined }}>
                          {isExporting && (
                            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, marginBottom: 6 }}>
                              <div>
                                <div style={{ fontSize: 14, fontWeight: 600 }}>Calendario de turnos</div>
                                <div style={{ color: '#555', fontSize: 11 }}>{branchLabel}</div>
                                <div style={{ color: '#555', fontSize: 11 }}>
                                  Semana del {selectedWeekStart.format('DD/MM/YYYY')} al {selectedWeekStart.add(6, 'day').format('DD/MM/YYYY')}
                                </div>
                                <div style={{ color: '#555', fontSize: 11 }}>
                                  Exportado por: {exportedBy}{exportedEmail ? ` (${exportedEmail})` : ''}
                                </div>
                              </div>
                              <img src={logo} alt="Logo" style={{ height: 90 }} />
                            </div>
                          )}
                          <Space orientation="vertical" size={isExporting ? 'small' : 'large'} style={{ width: '100%' }}>
                            <Space wrap style={{ width: '100%', justifyContent: 'space-between' }}>
                              {!isExporting && (
                                <Text>
                                  Semana del {selectedWeekStart.format('DD/MM/YYYY')} al {selectedWeekStart.add(6, 'day').format('DD/MM/YYYY')}
                                </Text>
                              )}
                              {!isExporting && (
                                <Space wrap>
                                  <Button 
                                    onClick={() => setSelectedWeekStart(selectedWeekStart.subtract(1, 'week'))}
                                  >
                                    ← Semana Anterior
                                  </Button>
                                  <Button 
                                    onClick={() => setSelectedWeekStart(dayjs().startOf('week'))}
                                    type="dashed"
                                  >
                                    Hoy
                                  </Button>
                                  <Button 
                                    onClick={() => setSelectedWeekStart(selectedWeekStart.add(1, 'week'))}
                                  >
                                    Semana Siguiente →
                                  </Button>
                                </Space>
                              )}
                            </Space>
                            
                            <Table
                              columns={weekColumns}
                              dataSource={weekTableData}
                              pagination={false}
                              bordered
                              size="small"
                              scroll={{ x: 'max-content' }}
                              style={{ fontSize: '12px', width: '100%' }}
                            />
                          </Space>
                        </div>
                        </div>
                      ),
                    },
              {
                key: 'month',
                label: 'Vista de Mes',
                children: (
                  <Calendar
                    value={selectedMonth}
                    onPanelChange={(value) => setSelectedMonth(value)}
                    cellRender={(current, info) => {
                      if (info.type !== 'date') return info.originNode;

                      const key = current.format('YYYY-MM-DD');
                      const dayShifts = shiftsByDate.get(key) ?? [];
                      if (dayShifts.length === 0) return info.originNode;

                      return (
                        <div>
                          {info.originNode}
                          <div style={{ marginTop: 4, fontSize: '11px' }}>
                            {dayShifts.slice(0, 3).map((shift) => {
                              const startTime = shift.startTime?.substring(0, 5) || '??:??';
                              const endTime = shift.endTime?.substring(0, 5) || '??:??';
                              const shiftLabel = `${startTime}-${endTime}`;
                              
                              return (
                                <div 
                                  key={shift.id} 
                                  style={{ 
                                    marginBottom: 4,
                                    padding: '2px 4px',
                                    backgroundColor: '#f0f5ff',
                                    borderRadius: '2px',
                                    border: '1px solid #d6e4ff',
                                    cursor: 'pointer',
                                  }}
                                  onClick={() => openEditModal(shift)}
                                >
                                  <div style={{ fontWeight: 500, color: '#1890ff' }}>
                                    {shiftLabel}
                                  </div>
                                  <div style={{ color: '#666', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                    {shift.employeeName}
                                  </div>
                                  <div style={{ color: '#999', fontSize: '10px' }}>
                                    {shift.workRoleName}
                                  </div>
                                </div>
                              );
                            })}
                            {dayShifts.length > 3 && (
                              <Text type="secondary" style={{ fontSize: '10px' }}>+{dayShifts.length - 3} más</Text>
                            )}
                          </div>
                        </div>
                      );
                    }}
                  />
                ),
              },
                    ]}
                  />
                </Spin>
              ),
            },
            {
              key: 'stats',
              label: (
                <span>
                  <BarChartOutlined /> Estadísticas
                </span>
              ),
              children: (
                <Space orientation="vertical" size="large" style={{ width: '100%' }}>
                  {/* Estadísticas por Empleado */}
                  {employeesWithShifts.length > 0 ? (
                    <Row gutter={[16, 16]} style={{ width: '100%' }}>
                      <Col xs={24} md={8}>
                        <Card title="Empleados" size="small" style={{ height: '100%' }}>
                          <List
                            dataSource={employeesWithShifts}
                            locale={{ emptyText: 'Sin empleados con turnos' }}
                            renderItem={(employee) => {
                              const summary = employeeStatsSummary[employee.id] ?? { shiftCount: 0, totalHours: 0 };
                              return (
                                <List.Item
                                  style={{
                                    cursor: 'pointer',
                                    padding: '8px 12px',
                                    background: employee.id === selectedEmployeeId ? '#e6f4ff' : 'transparent',
                                    borderRadius: 6
                                  }}
                                  actions={[
                                    <Tag color="blue" key="shifts">{summary.shiftCount} turnos</Tag>,
                                    <Tag color="green" key="hours">{summary.totalHours.toFixed(1)} h</Tag>
                                  ]}
                                  onClick={() => setSelectedEmployeeId(employee.id)}
                                >
                                  {employee.name}
                                </List.Item>
                              );
                            }}
                          />
                        </Card>
                      </Col>
                      <Col xs={24} md={16}>
                        {selectedEmployeeId ? (
                          <EmployeeStats
                            employeeId={selectedEmployeeId}
                            employeeName={employeesWithShifts.find(e => e.id === selectedEmployeeId)?.name || ''}
                            shifts={shifts}
                            currentMonth={selectedMonth}
                          />
                        ) : (
                          <Card size="small">
                            <div style={{ textAlign: 'center', padding: '20px 0' }}>
                              <Text type="secondary">Selecciona un empleado para ver sus estadísticas.</Text>
                            </div>
                          </Card>
                        )}
                      </Col>
                    </Row>
                  ) : (
                    <Card size="small">
                      <div style={{ textAlign: 'center', padding: '20px 0' }}>
                        <Text type="secondary">No hay turnos generados para este mes. Genera turnos primero para ver estadísticas individuales.</Text>
                      </div>
                    </Card>
                  )}
                </Space>
              ),
            },
          ]}
        />
      </Space>
      <Modal
        open={editModalOpen}
        title="Editar turno"
        onCancel={() => setEditModalOpen(false)}
        footer={[
          <Button key="delete" danger onClick={handleDeleteShift} loading={savingDelete}>
            Eliminar
          </Button>,
          <Button key="cancel" onClick={() => setEditModalOpen(false)}>
            Cancelar
          </Button>,
          <Button key="save" type="primary" onClick={handleUpdateShift} loading={savingEdit}>
            Guardar
          </Button>
        ]}
      >
        {editShift && (
          <div style={{ marginBottom: 12 }}>
            <Text strong>Area:</Text> {editShift.workAreaName} &nbsp; | &nbsp;
            <Text strong>Rol:</Text> {editShift.workRoleName}
          </div>
        )}
        <Form form={editForm} layout="vertical">
          <Row gutter={[16, 16]}>
            <Col xs={24} sm={12}>
              <Form.Item
                name="employeeId"
                label="Empleado"
                rules={[{ required: true, message: 'Selecciona un empleado' }]}
              >
                <Select
                  placeholder="Selecciona un empleado"
                  notFoundContent="No hay empleados elegibles para este rol"
                  options={editModalEmployees.map((e) => ({
                    label: `${e.firstName} ${e.lastName}`,
                    value: e.id
                  }))}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="date"
                label="Fecha"
                rules={[{ required: true, message: 'Selecciona una fecha' }]}
              >
                <DatePicker style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="startTime"
                label="Hora inicio"
                rules={[{ required: true, message: 'Ingresa hora de inicio' }]}
              >
                <TimePicker format="HH:mm" style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="endTime"
                label="Hora fin"
                rules={[{ required: true, message: 'Ingresa hora de fin' }]}
              >
                <TimePicker format="HH:mm" style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="breakDuration" label="Descanso (minutos)">
                <InputNumber<number>
                  min={0}
                  max={300}
                  step={1}
                  precision={0}
                  style={{ width: '100%' }}
                  placeholder="30"
                  parser={(value) => (value ? Number(value.replace(/[^0-9]/g, '')) : 0)}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="lunchDuration" label="Almuerzo (minutos)">
                <InputNumber<number>
                  min={0}
                  max={300}
                  step={1}
                  precision={0}
                  style={{ width: '100%' }}
                  placeholder="60"
                  parser={(value) => (value ? Number(value.replace(/[^0-9]/g, '')) : 0)}
                />
              </Form.Item>
            </Col>
            <Col xs={24}>
              <Form.Item name="notes" label="Notas">
                <Input.TextArea rows={3} />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Modal>
      <Modal
        open={createModalOpen}
        title="Agregar turno"
        onCancel={() => setCreateModalOpen(false)}
        onOk={handleCreateShift}
        okText="Crear"
        cancelText="Cancelar"
        confirmLoading={savingCreate}
      >
        <Form form={createForm} layout="vertical">
          <Row gutter={[16, 16]}>
            <Col xs={24} sm={12}>
              <Form.Item
                name="employeeId"
                label="Empleado"
                rules={[{ required: true, message: 'Selecciona un empleado' }]}
              >
                <Select
                  placeholder="Selecciona un empleado"
                  notFoundContent="No hay empleados elegibles para este rol"
                  options={createModalEmployees.map((e) => ({
                    label: `${e.firstName} ${e.lastName}`,
                    value: e.id
                  }))}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="date"
                label="Fecha"
                rules={[{ required: true, message: 'Selecciona una fecha' }]}
              >
                <DatePicker style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="workAreaId"
                label="Area"
                rules={[{ required: true, message: 'Selecciona un area' }]}
              >
                <Select
                  placeholder="Selecciona un area"
                  options={workAreas.map((area) => ({ label: area.name, value: area.id }))}
                  onChange={(value) => {
                    createForm.setFieldsValue({ workRoleId: undefined });
                    loadWorkRoles(value);
                  }}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="workRoleId"
                label="Rol"
                rules={[{ required: true, message: 'Selecciona un rol' }]}
              >
                <Select
                  placeholder="Selecciona un rol"
                  options={workRoles.map((role) => ({ label: role.name, value: role.id }))}
                  onChange={(value) => setSelectedWorkRoleId(value)}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="startTime"
                label="Hora inicio"
                rules={[{ required: true, message: 'Ingresa hora de inicio' }]}
              >
                <TimePicker format="HH:mm" style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="endTime"
                label="Hora fin"
                rules={[{ required: true, message: 'Ingresa hora de fin' }]}
              >
                <TimePicker format="HH:mm" style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="breakDuration" label="Descanso (minutos)">
                <InputNumber<number>
                  min={0}
                  max={300}
                  step={1}
                  precision={0}
                  style={{ width: '100%' }}
                  placeholder="30"
                  parser={(value) => (value ? Number(value.replace(/[^0-9]/g, '')) : 0)}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="lunchDuration" label="Almuerzo (minutos)">
                <InputNumber<number>
                  min={0}
                  max={300}
                  step={1}
                  precision={0}
                  style={{ width: '100%' }}
                  placeholder="60"
                  parser={(value) => (value ? Number(value.replace(/[^0-9]/g, '')) : 0)}
                />
              </Form.Item>
            </Col>
            <Col xs={24}>
              <Form.Item name="notes" label="Notas">
                <Input.TextArea rows={3} />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Modal>
    </Card>
  );
};
