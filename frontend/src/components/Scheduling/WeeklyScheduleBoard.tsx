/**
 * WeeklyScheduleBoard
 * Tablero semanal de planificación con drag & drop.
 *
 * Izquierda  : lista de empleados elegibles (arrastrables)
 * Derecha     : columnas por día con los slots de plantilla de cada día
 * Acciones    : Auto-rellenar la semana o colocar empleados manualmente
 *               Confirmar la semana (guarda en servidor)
 */
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Badge,
  Button,
  Card,
  Col,
  Divider,
  message,
  Popover,
  Popconfirm,
  Row,
  Space,
  Spin,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import {
  CheckOutlined,
  CloseOutlined,
  DeleteOutlined,
  InfoCircleOutlined,
  PrinterOutlined,
  RobotOutlined,
  LeftOutlined,
  RightOutlined,
  UserOutlined,
} from '@ant-design/icons';
import dayjs, { Dayjs } from 'dayjs';
import 'dayjs/locale/es';
import {
  scheduleShiftApi,
  shiftTemplateApi,
} from '../../services/api';
import { specialDateApi } from '../../services/specialDateApi';
import { specialDateTemplateApi } from '../../services/specialDateTemplateApi';
import { useAuth } from '../../context/useAuth';
import { formatError, getDetailedError } from '../../utils/errorHandler';
import type { EmployeeDto, ShiftAssignmentDto, ShiftTemplateDto, SpecialDateTemplateDto } from '../../types';

dayjs.locale('es');

const { Text } = Typography;

type BoardTemplate = ShiftTemplateDto & { effectiveDate?: string };

const DAY_LABELS = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];

const compareText = (a?: string, b?: string) =>
  (a ?? '').localeCompare(b ?? '', 'es', { sensitivity: 'base' });

const personNameSortKey = (name?: string) => {
  const parts = (name ?? '').trim().split(/\s+/).filter(Boolean);
  if (parts.length < 2) return parts[0] ?? '';
  const [first, ...rest] = parts;
  return `${rest.join(' ')} ${first}`;
};

const compareTemplatesByAreaAndName = (a: BoardTemplate, b: BoardTemplate) =>
  compareText(a.workAreaName, b.workAreaName)
    || compareText(a.workRoleName, b.workRoleName)
    || compareText(a.startTime, b.startTime)
    || compareText(a.endTime, b.endTime);

const shiftTemplateMatchKey = (
  date: string,
  workAreaId: string,
  workRoleId: string,
  startTime: string,
) => `${date}|${workAreaId}|${workRoleId}|${startTime.substring(0, 5)}`;

const parseDurationToMinutes = (duration?: string) => {
  if (!duration) return 0;
  const [hh = '0', mm = '0', ss = '0'] = duration.split(':');
  const hours = Number(hh) || 0;
  const minutes = Number(mm) || 0;
  const seconds = Number(ss) || 0;
  return hours * 60 + minutes + Math.floor(seconds / 60);
};

// ---------------------------------------------------------------------------
// Tipos locales
// ---------------------------------------------------------------------------

/** Un slot es un cupo dentro de una plantilla (1 empleado por slot) */
interface SlotKey {
  templateId: string;
  date: string; // YYYY-MM-DD
  slotIndex: number; // 0..RequiredCount-1
}

/** Identificador serializado de un slot */
const slotId = (s: SlotKey) => `${s.templateId}|${s.date}|${s.slotIndex}`;

interface BoardSlot extends SlotKey {
  employee: EmployeeDto | null;
  existingShiftId?: string;
}

// ---------------------------------------------------------------------------

interface WeeklyScheduleBoardProps {
  /** Empleados elegibles ya cargados por el padre */
  eligibleEmployees: EmployeeDto[];
  /** Mes actualmente seleccionado en pantalla */
  selectedMonth: Dayjs;
  /** Resumen de estadísticas mensual (incluye preview del tablero) */
  employeeStatsSummary: Record<string, { shiftCount: number; totalHours: number }>;
  /** Resumen de días libres mensual (incluye preview del tablero) */
  employeeFreeDaysSummary: Record<string, {
    assignedDays: number;
    freeDays: number;
    validDaysInMonth: number;
    freeDayLabels: string[];
    freeDayWeekLines: string[];
  }>;
  /** Semana activa (primer día) */
  weekStart: Dayjs;
  onWeekChange: (next: Dayjs) => void;
  /** Callback tras confirmar exitosamente */
  onConfirmed?: () => void;
  /** Notifica el borrador semanal para preview de estadísticas */
  onPreviewAssignmentsChange?: (assignments: ShiftAssignmentDto[]) => void;
}

export const WeeklyScheduleBoard = ({
  eligibleEmployees,
  selectedMonth,
  employeeStatsSummary,
  employeeFreeDaysSummary,
  weekStart,
  onWeekChange,
  onConfirmed,
  onPreviewAssignmentsChange,
}: WeeklyScheduleBoardProps) => {
  const { branchId } = useAuth();

  // -------------------------------------------------------------------------
  // Impresión
  // -------------------------------------------------------------------------
  useEffect(() => {
    const id = 'wsb-print-styles';
    if (document.getElementById(id)) return;
    const el = document.createElement('style');
    el.id = id;
    el.textContent = `
      @media print {
        @page { size: A4 landscape; margin: 7mm; }
        body * { visibility: hidden !important; }
        #wsb-print-view, #wsb-print-view * { visibility: visible !important; }
        #wsb-print-view {
          position: fixed !important;
          top: 0 !important; left: 0 !important;
          width: 100% !important;
          background: white !important;
          padding: 0 !important;
          box-sizing: border-box !important;
          font-family: Arial, Helvetica, sans-serif !important;
        }
      }
    `;
    document.head.appendChild(el);
    return () => { document.getElementById(id)?.remove(); };
  }, []);

  const handlePrint = useCallback(() => window.print(), []);

  const abbrevName = (first: string, last: string) =>
    `${first.charAt(0).toUpperCase()}. ${last}`;

  // -------------------------------------------------------------------------
  // Estado
  // -------------------------------------------------------------------------
  const [templates, setTemplates] = useState<BoardTemplate[]>([]);
  const [slots, setSlots] = useState<Record<string, BoardSlot>>({});
  const [loadingTemplates, setLoadingTemplates] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [autofilling, setAutofilling] = useState(false);
  const [deletingWeek, setDeletingWeek] = useState(false);
  const [dragEmployee, setDragEmployee] = useState<EmployeeDto | null>(null);
  const [dragOverSlot, setDragOverSlot] = useState<string | null>(null);
  const dragEmployeeRef = useRef<EmployeeDto | null>(null);

  // Ref para saber cuándo no desmontar
  const isMounted = useRef(true);
  useEffect(() => {
    isMounted.current = true;
    return () => { isMounted.current = false; };
  }, []);

  // -------------------------------------------------------------------------
  // Auxiliares de fechas
  // -------------------------------------------------------------------------
  const weekDays = useMemo(
    () => Array.from({ length: 7 }, (_, i) => weekStart.add(i, 'day')),
    [weekStart],
  );
  const monthStart = useMemo(() => selectedMonth.startOf('month'), [selectedMonth]);
  const monthEnd = useMemo(() => selectedMonth.endOf('month'), [selectedMonth]);
  const isDateInSelectedMonth = useCallback(
    (date: string | Dayjs) => dayjs(date).isSame(selectedMonth, 'month'),
    [selectedMonth],
  );

  const weekIntersectsSelectedMonth = useCallback(
    (start: Dayjs) => {
      const end = start.add(6, 'day');
      return !(end.isBefore(monthStart, 'day') || start.isAfter(monthEnd, 'day'));
    },
    [monthStart, monthEnd],
  );

  // -------------------------------------------------------------------------
  // Carga de plantillas + asignaciones existentes
  // -------------------------------------------------------------------------
  const buildSlots = useCallback(
    (tmplList: BoardTemplate[], existing: ShiftAssignmentDto[]) => {
      const nextSlots: Record<string, BoardSlot> = {};
      const existingByTemplate: Record<string, ShiftAssignmentDto[]> = {};
      const consumedExistingByTemplate: Record<string, number> = {};

      for (const shift of existing) {
        const key = shiftTemplateMatchKey(
          dayjs(shift.date).format('YYYY-MM-DD'),
          shift.workAreaId,
          shift.workRoleId,
          shift.startTime,
        );
        if (!existingByTemplate[key]) existingByTemplate[key] = [];
        existingByTemplate[key].push(shift);
      }

      Object.values(existingByTemplate).forEach(assignments => {
        assignments.sort((a, b) =>
          compareText(personNameSortKey(a.employeeName), personNameSortKey(b.employeeName))
            || compareText(a.startTime, b.startTime)
            || compareText(a.id, b.id),
        );
      });

      for (const day of weekDays) {
        const dateStr = day.format('YYYY-MM-DD');
        const dow = day.day(); // 0=Dom..6=Sáb

        const specialTemplates = tmplList.filter(t => t.effectiveDate === dateStr);
        const dayTemplates = (specialTemplates.length > 0
          ? specialTemplates
          : tmplList.filter(t => !t.effectiveDate && t.dayOfWeek === dow)
        ).sort(compareTemplatesByAreaAndName);

        for (const tmpl of dayTemplates) {
          for (let idx = 0; idx < tmpl.requiredCount; idx++) {
            const key = slotId({ templateId: tmpl.id, date: dateStr, slotIndex: idx });

            const matchKey = shiftTemplateMatchKey(dateStr, tmpl.workAreaId, tmpl.workRoleId, tmpl.startTime);
            const matchingShifts = existingByTemplate[matchKey] ?? [];
            const consumedIndex = consumedExistingByTemplate[matchKey] ?? 0;
            const existingShift = matchingShifts[consumedIndex];
            consumedExistingByTemplate[matchKey] = consumedIndex + 1;
            const alreadyUsedEmployee = existingShift?.employeeId ?? null;

            const emp = alreadyUsedEmployee
              ? eligibleEmployees.find(e => e.id === alreadyUsedEmployee) ?? null
              : null;

            nextSlots[key] = {
              templateId: tmpl.id,
              date: dateStr,
              slotIndex: idx,
              employee: emp,
              existingShiftId: existingShift?.id,
            };
          }
        }
      }

      return nextSlots;
    },
    [weekDays, eligibleEmployees],
  );

  const loadWeek = useCallback(async () => {
    if (!branchId) return;
    setLoadingTemplates(true);
    try {
      const uniqueYearMonth = Array.from(
        new Set(weekDays.map(d => `${d.year()}-${d.month() + 1}`)),
      ).map(v => {
        const [year, month] = v.split('-').map(Number);
        return { year, month };
      });

      const monthlyShiftResponses = await Promise.all(
        uniqueYearMonth.map(({ year, month }) => scheduleShiftApi.getMonthly(branchId, year, month)),
      );

      const [tmplRes, specialDatesRes, shiftsRes] = await Promise.all([
        shiftTemplateApi.getAll(branchId),
        specialDateApi.getAll(branchId),
        Promise.resolve({
          data: monthlyShiftResponses
            .flatMap(res => (Array.isArray(res.data) ? res.data : [])),
        }),
      ]);

      const weekDayStrings = new Set(weekDays.map(d => d.format('YYYY-MM-DD')));
      const relevantSpecialDates = (Array.isArray(specialDatesRes.data) ? specialDatesRes.data : [])
        .filter(specialDate => weekDayStrings.has(dayjs(specialDate.date).format('YYYY-MM-DD')));
      const specialTemplateResponses = await Promise.all(
        relevantSpecialDates.map(specialDate =>
          specialDateTemplateApi.getBySpecialDateId(specialDate.id),
        ),
      );

      const specialTemplates: BoardTemplate[] = relevantSpecialDates.flatMap((specialDate, index) => {
        const effectiveDate = dayjs(specialDate.date).format('YYYY-MM-DD');
        const dateTemplates: SpecialDateTemplateDto[] = Array.isArray(specialTemplateResponses[index].data)
          ? specialTemplateResponses[index].data
          : [];
        return dateTemplates.map(template => ({
          ...template,
          branchId,
          dayOfWeek: dayjs(specialDate.date).day(),
          effectiveDate,
        }));
      });
      const normalTemplates: BoardTemplate[] = Array.isArray(tmplRes.data) ? tmplRes.data : [];
      const tmplList = [...normalTemplates, ...specialTemplates].sort(compareTemplatesByAreaAndName);
      const existingShifts = Array.isArray(shiftsRes.data) ? shiftsRes.data : [];

      // Filtrar turnos de la semana visible
      const weekShifts = existingShifts.filter(s =>
        weekDayStrings.has(dayjs(s.date).format('YYYY-MM-DD')),
      );

      if (!isMounted.current) return;
      setTemplates(tmplList);
      setSlots(buildSlots(tmplList, weekShifts));
    } catch (err) {
      message.error(formatError(err));
    } finally {
      if (isMounted.current) setLoadingTemplates(false);
    }
  }, [branchId, weekDays, buildSlots]);

  useEffect(() => {
    loadWeek();
  }, [loadWeek]);

  // -------------------------------------------------------------------------
  // Drag & Drop (HTML5 nativo)
  // -------------------------------------------------------------------------
  const handleDragStart = (emp: EmployeeDto) => {
    dragEmployeeRef.current = emp;
    setDragEmployee(emp);
  };

  const handleDragEnd = () => {
    dragEmployeeRef.current = null;
    setDragEmployee(null);
    setDragOverSlot(null);
  };

  const handleDragOverSlot = (key: string, e: React.DragEvent) => {
    e.preventDefault();
    setDragOverSlot(key);
  };

  const handleDropOnSlot = (slot: BoardSlot) => {
    const employee = dragEmployeeRef.current ?? dragEmployee;
    if (!employee) return;
    if (!isDateInSelectedMonth(slot.date)) {
      message.warning('No se puede modificar días fuera del mes seleccionado.');
      return;
    }
    const key = slotId(slot);
    setSlots(prev => {
      const next = { ...prev };
      for (const slotKey of Object.keys(next)) {
        const current = next[slotKey];
        if (slotKey !== key && current.date === slot.date && current.employee?.id === employee.id) {
          next[slotKey] = { ...current, employee: null };
        }
      }
      next[key] = { ...next[key], employee };
      return next;
    });
    dragEmployeeRef.current = null;
    setDragEmployee(null);
    setDragOverSlot(null);
  };

  const clearSlot = (key: string) => {
    if (!isDateInSelectedMonth(slots[key]?.date)) {
      message.warning('No se puede modificar días fuera del mes seleccionado.');
      return;
    }
    setSlots(prev => ({
      ...prev,
      [key]: { ...prev[key], employee: null },
    }));
  };

  // -------------------------------------------------------------------------
  // Auto-rellenar
  // -------------------------------------------------------------------------
  const handleAutoFill = useCallback(async () => {
    if (!branchId) return;
    setAutofilling(true);
    try {
      const weekEnd = weekStart.add(6, 'day');
      const rangeStart = weekStart.isBefore(monthStart, 'day') ? monthStart : weekStart;
      const rangeEnd = weekEnd.isAfter(monthEnd, 'day') ? monthEnd : weekEnd;

      if (rangeStart.isAfter(rangeEnd, 'day')) {
        message.info('La semana visible está fuera del mes seleccionado.');
        return;
      }

      const result = await scheduleShiftApi.generateWeekly(
        selectedMonth.year(),
        selectedMonth.month() + 1,
        rangeStart.format('YYYY-MM-DD'),
        rangeEnd.format('YYYY-MM-DD'),
      );

      const generated: ShiftAssignmentDto[] = result.data.assignments ?? [];

      // Actualizar slots con las asignaciones generadas
      setSlots(prev => {
        const next = { ...prev };

        // Agrupar generados por la plantilla visible del casillero.
        const grouped: Record<string, ShiftAssignmentDto[]> = {};
        const consumedGeneratedByTemplate: Record<string, number> = {};
        for (const a of generated) {
          const gKey = shiftTemplateMatchKey(
            dayjs(a.date).format('YYYY-MM-DD'),
            a.workAreaId,
            a.workRoleId,
            a.startTime,
          );
          if (!grouped[gKey]) grouped[gKey] = [];
          grouped[gKey].push(a);
        }

        for (const key of Object.keys(next)) {
          const s = next[key];
          const tmpl = templates.find(t => t.id === s.templateId);
          if (!tmpl) continue;

          const gKey = shiftTemplateMatchKey(s.date, tmpl.workAreaId, tmpl.workRoleId, tmpl.startTime);
          const assignments = grouped[gKey] ?? [];
          const consumedIndex = consumedGeneratedByTemplate[gKey] ?? 0;
          const assigned = assignments[consumedIndex];
          consumedGeneratedByTemplate[gKey] = consumedIndex + 1;

          if (assigned) {
            const emp = eligibleEmployees.find(e => e.id === assigned.employeeId) ?? null;
            next[key] = { ...s, employee: emp, existingShiftId: assigned.id };
          }
        }
        return next;
      });

      const total = result.data.totalShiftsGenerated ?? 0;
      message.success(`${total} turno(s) autogenerado(s) para la semana.`);
    } catch (err) {
      message.error(formatError(err));
    } finally {
      if (isMounted.current) setAutofilling(false);
    }
  }, [branchId, weekStart, templates, eligibleEmployees, monthStart, monthEnd, selectedMonth]);

  // -------------------------------------------------------------------------
  // Confirmar semana
  // -------------------------------------------------------------------------
  const handleConfirm = useCallback(async () => {
    if (!branchId) return;
    setConfirming(true);

    try {
      // Recolectar todos los slots que tienen empleado asignado
      const toSave = Object.values(slots)
        .filter(s => s.employee !== null && isDateInSelectedMonth(s.date))
        .sort((a, b) =>
          compareText(a.date, b.date)
            || compareText(a.templateId, b.templateId)
            || a.slotIndex - b.slotIndex,
        );

      const employeeDaySlots = new Map<string, BoardSlot[]>();
      toSave.forEach(slot => {
        if (!slot.employee) return;
        const key = `${slot.date}|${slot.employee.id}`;
        const current = employeeDaySlots.get(key) ?? [];
        current.push(slot);
        employeeDaySlots.set(key, current);
      });

      const duplicateEmployeeDay = Array.from(employeeDaySlots.values()).find(items => items.length > 1);
      if (duplicateEmployeeDay?.[0]?.employee) {
        const employee = duplicateEmployeeDay[0].employee;
        message.error(`${employee.firstName} ${employee.lastName} ya está asignado en ${dayjs(duplicateEmployeeDay[0].date).format('DD/MM')}. Cada empleado solo puede ocupar un turno por día.`);
        return;
      }

      const weekEnd = weekStart.add(6, 'day');
      const daysToReplace = weekDays.filter(day => day.isSame(selectedMonth, 'month'));
      if (daysToReplace.length === 0) return;

      const assignments = toSave.flatMap(slot => {
        const tmpl = templates.find(template => template.id === slot.templateId);
        if (!tmpl || !slot.employee) return [];
        return [{
          employeeId: slot.employee.id,
          date: slot.date,
          startTime: tmpl.startTime,
          endTime: tmpl.endTime,
          breakDuration: tmpl.breakDuration,
          lunchDuration: tmpl.lunchDuration,
          workAreaId: tmpl.workAreaId,
          workRoleId: tmpl.workRoleId,
          notes: tmpl.notes,
        }];
      });

      const response = await scheduleShiftApi.replaceWeek(
        branchId,
        daysToReplace[0].format('YYYY-MM-DD'),
        daysToReplace[daysToReplace.length - 1].format('YYYY-MM-DD'),
        assignments,
      );
      const savedAssignments = Array.isArray(response.data) ? response.data : [];
      const savedIdsBySlotKey: Record<string, string> = {};
      toSave.forEach((slot, index) => {
        const saved = savedAssignments[index];
        if (saved) savedIdsBySlotKey[slotId(slot)] = saved.id;
      });

      setSlots(prev => {
        const next = { ...prev };
        for (const [key, id] of Object.entries(savedIdsBySlotKey)) {
          if (next[key]) next[key] = { ...next[key], existingShiftId: id };
        }
        return next;
      });
      message.success(`Semana del ${weekStart.format('DD/MM')} al ${weekEnd.format('DD/MM')} confirmada correctamente.`);

      onConfirmed?.();
    } catch (err) {
      const details = getDetailedError(err);
      message.error(details.detail
        ? `${details.message}: ${details.detail}`
        : formatError(err));
    } finally {
      if (isMounted.current) setConfirming(false);
    }
  }, [branchId, weekStart, weekDays, slots, templates, loadWeek, onConfirmed, isDateInSelectedMonth, selectedMonth]);

  // -------------------------------------------------------------------------
  // Borrar turnos de la semana (solo días del mes seleccionado)
  // -------------------------------------------------------------------------
  const handleDeleteWeekShifts = useCallback(async () => {
    if (!branchId) return;
    setDeletingWeek(true);
    try {
      const daysInMonth = weekDays.filter(d => d.isSame(selectedMonth, 'month'));
      if (daysInMonth.length === 0) {
        message.info('Ningún día de esta semana pertenece al mes seleccionado.');
        return;
      }
      const dayStrings = new Set(daysInMonth.map(d => d.format('YYYY-MM-DD')));

      const res = await scheduleShiftApi.getMonthly(
        branchId,
        selectedMonth.year(),
        selectedMonth.month() + 1,
      );
      const allShifts: ShiftAssignmentDto[] = Array.isArray(res.data) ? res.data : [];
      const toDelete = allShifts.filter(s =>
        dayStrings.has(dayjs(s.date).format('YYYY-MM-DD')),
      );

      if (toDelete.length === 0) {
        message.info('No hay turnos guardados para estos días.');
        return;
      }

      await Promise.all(toDelete.map(s => scheduleShiftApi.delete(s.id)));
      message.success(`${toDelete.length} turno(s) eliminado(s) correctamente.`);
      await loadWeek();
      onConfirmed?.();
    } catch (err) {
      message.error(formatError(err));
    } finally {
      if (isMounted.current) setDeletingWeek(false);
    }
  }, [branchId, weekDays, selectedMonth, loadWeek, onConfirmed]);

  const previewAssignments = useMemo<ShiftAssignmentDto[]>(() => {
    const result: ShiftAssignmentDto[] = [];

    for (const slot of Object.values(slots)) {
      if (!slot.employee || !isDateInSelectedMonth(slot.date)) continue;

      const tmpl = templates.find(t => t.id === slot.templateId);
      if (!tmpl) continue;

      const breakMinutes = parseDurationToMinutes(tmpl.breakDuration);
      const lunchMinutes = parseDurationToMinutes(tmpl.lunchDuration);
      const startDateTime = dayjs(`${slot.date}T${tmpl.startTime}`);
      const endDateTime = dayjs(`${slot.date}T${tmpl.endTime}`);
      const rawMinutes = Math.max(0, endDateTime.diff(startDateTime, 'minute'));
      const workedHours = Math.max(0, rawMinutes - breakMinutes - lunchMinutes) / 60;

      result.push({
        id: slot.existingShiftId ?? `draft-${slotId(slot)}`,
        employeeId: slot.employee.id,
        employeeName: `${slot.employee.firstName} ${slot.employee.lastName}`,
        date: slot.date,
        startTime: tmpl.startTime,
        endTime: tmpl.endTime,
        breakDuration: tmpl.breakDuration,
        lunchDuration: tmpl.lunchDuration,
        workAreaId: tmpl.workAreaId,
        workAreaName: tmpl.workAreaName,
        workAreaColor: tmpl.workAreaColor || '#808080',
        workRoleId: tmpl.workRoleId,
        workRoleName: tmpl.workRoleName,
        workedHours,
        notes: tmpl.notes,
        isApproved: false,
      });
    }

    return result;
  }, [slots, templates, isDateInSelectedMonth]);

  useEffect(() => {
    onPreviewAssignmentsChange?.(previewAssignments);
  }, [previewAssignments, onPreviewAssignmentsChange]);

  const employeeWeeklyHours = useMemo(() => {
    const hoursByEmployee: Record<string, number> = {};

    for (const slot of Object.values(slots)) {
      if (!slot.employee) continue;

      const tmpl = templates.find(t => t.id === slot.templateId);
      if (!tmpl) continue;

      const breakMinutes = parseDurationToMinutes(tmpl.breakDuration);
      const lunchMinutes = parseDurationToMinutes(tmpl.lunchDuration);
      const startDateTime = dayjs(`${slot.date}T${tmpl.startTime}`);
      const endDateTime = dayjs(`${slot.date}T${tmpl.endTime}`);
      const rawMinutes = Math.max(0, endDateTime.diff(startDateTime, 'minute'));
      const workedHours = Math.max(0, rawMinutes - breakMinutes - lunchMinutes) / 60;

      hoursByEmployee[slot.employee.id] =
        (hoursByEmployee[slot.employee.id] ?? 0) + workedHours;
    }

    return hoursByEmployee;
  }, [slots, templates]);

  const deleteWeekLabel = useMemo(() => {
    const days = weekDays.filter(d => d.isSame(selectedMonth, 'month'));
    if (days.length === 0) return '';
    if (days.length === 7)
      return `del ${days[0].format('DD/MM')} al ${days[6].format('DD/MM')}`;
    return `del ${days[0].format('DD/MM')} al ${days[days.length - 1].format('DD/MM')} (${days.length} día${days.length > 1 ? 's' : ''} de ${selectedMonth.format('MMMM')})`;
  }, [weekDays, selectedMonth]);

  const canGoPrev = useMemo(
    () => weekIntersectsSelectedMonth(weekStart.subtract(1, 'week')),
    [weekIntersectsSelectedMonth, weekStart],
  );
  const canGoNext = useMemo(
    () => weekIntersectsSelectedMonth(weekStart.add(1, 'week')),
    [weekIntersectsSelectedMonth, weekStart],
  );

  // -------------------------------------------------------------------------
  // Cómputos de interfaz
  // -------------------------------------------------------------------------
  const totalSlots = Object.values(slots).length;
  const filledSlots = Object.values(slots).filter(s => s.employee !== null).length;
  const coveragePercent = totalSlots > 0 ? Math.round((filledSlots / totalSlots) * 100) : 0;

  // Empleados ya colocados en la semana (para indicar cuáles están ocupados)
  const employeesOnBoard = useMemo(() => {
    const ids = new Set<string>();
    for (const s of Object.values(slots)) {
      if (s.employee) ids.add(s.employee.id);
    }
    return ids;
  }, [slots]);

  // Asignar color a cada área de trabajo
  const areaColors: Record<string, string> = useMemo(() => {
    const map: Record<string, string> = {};
    for (const tmpl of templates) {
      if (!(tmpl.workAreaId in map)) {
        map[tmpl.workAreaId] = tmpl.workAreaColor || '#4096ff';
      }
    }
    return map;
  }, [templates]);

  // -------------------------------------------------------------------------
  // Render
  // -------------------------------------------------------------------------
  return (
    <Spin spinning={loadingTemplates} tip="Cargando plantillas...">
      <Space direction="vertical" style={{ width: '100%' }} size="middle">
        {/* ── Cabecera ─────────────────────────────────────────────────── */}
        <Row justify="space-between" align="middle" wrap>
          <Col>
            <Space>
              <Button
                icon={<LeftOutlined />}
                onClick={() => onWeekChange(weekStart.subtract(1, 'week'))}
                disabled={!canGoPrev}
              />
              <Text strong style={{ fontSize: 15 }}>
                Semana del {weekStart.format('DD/MM/YYYY')} al {weekStart.add(6, 'day').format('DD/MM/YYYY')}
              </Text>
              <Button
                icon={<RightOutlined />}
                onClick={() => onWeekChange(weekStart.add(1, 'week'))}
                disabled={!canGoNext}
              />
              <Button
                type="dashed"
                onClick={() => onWeekChange(dayjs().startOf('week'))}
              >
                Hoy
              </Button>
              <Badge
                count={`${filledSlots}/${totalSlots}`}
                color={coveragePercent === 100 ? 'green' : coveragePercent >= 50 ? 'blue' : 'red'}
                overflowCount={999}
              >
                <Button disabled>Cubierto {coveragePercent}%</Button>
              </Badge>
            </Space>
          </Col>
          <Col>
            <Space>
              <Button
                icon={<PrinterOutlined />}
                onClick={handlePrint}
                disabled={loadingTemplates}
              >
                Imprimir / PDF
              </Button>
              <Button
                icon={<RobotOutlined />}
                onClick={handleAutoFill}
                loading={autofilling}
                disabled={confirming}
                type="default"
              >
                Auto-rellenar
              </Button>
              <Popconfirm
                title="Confirmar semana"
                description="Se guardarán los turnos asignados en el tablero. ¿Continuar?"
                onConfirm={handleConfirm}
                okText="Confirmar"
                cancelText="Cancelar"
                disabled={filledSlots === 0}
              >
                <Button
                  type="primary"
                  icon={<CheckOutlined />}
                  loading={confirming}
                  disabled={filledSlots === 0 || autofilling}
                >
                  Confirmar semana
                </Button>
              </Popconfirm>
            </Space>
          </Col>
        </Row>

        {coveragePercent > 0 && coveragePercent < 100 && (
          <Alert
            type="warning"
            showIcon
            title={`Faltan ${totalSlots - filledSlots} cupo(s) por asignar para completar la semana.`}
          />
        )}

        {/* ── Cuerpo: izquierda empleados | derecha calendario ─────────── */}
        <Row gutter={[12, 0]} style={{ alignItems: 'flex-start' }}>
          {/* ── Panel izquierdo: empleados ─────────────────────────────── */}
          <Col xs={24} lg={5}>
            <Card
              title={
                <Space>
                  <UserOutlined />
                  <span>Empleados</span>
                  <Tag color="blue">{eligibleEmployees.length}</Tag>
                </Space>
              }
              size="small"
              bodyStyle={{ padding: 8, maxHeight: 580, overflowY: 'auto' }}
            >
              {eligibleEmployees.length === 0 && (
                <Text type="secondary">Sin empleados elegibles</Text>
              )}
              {eligibleEmployees.map(emp => {
                const busy = employeesOnBoard.has(emp.id);
                const summary = employeeStatsSummary[emp.id] ?? { shiftCount: 0, totalHours: 0 };
                const weeklyHours = employeeWeeklyHours[emp.id] ?? 0;
                const freeSummary = employeeFreeDaysSummary[emp.id] ?? {
                  assignedDays: 0,
                  freeDays: 0,
                  validDaysInMonth: selectedMonth.daysInMonth(),
                  freeDayLabels: [],
                  freeDayWeekLines: [],
                };
                return (
                  <div
                    key={emp.id}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 6,
                      marginBottom: 4,
                    }}
                  >
                    <Popover
                      trigger={['hover', 'click']}
                      placement="right"
                      title={`${emp.firstName} ${emp.lastName}`}
                      content={
                        <Space direction="vertical" size={2}>
                          <Text style={{ fontSize: 12 }}>
                            Horas de la semana: <Text strong>{weeklyHours.toFixed(1)} h</Text>
                          </Text>
                          <Text style={{ fontSize: 12 }}>
                            Turnos del mes: <Text strong>{summary.shiftCount}</Text>
                          </Text>
                          <Text style={{ fontSize: 12 }}>
                            Horas del mes: <Text strong>{summary.totalHours.toFixed(1)} h</Text>
                          </Text>
                          <Text style={{ fontSize: 12 }}>
                            Días trabajados: <Text strong>{freeSummary.assignedDays}</Text> / {freeSummary.validDaysInMonth}
                          </Text>
                          <Text style={{ fontSize: 12 }}>
                            Días libres: <Text strong>{freeSummary.freeDays}</Text>
                          </Text>
                          <div style={{ maxWidth: 260 }}>
                            <Text style={{ fontSize: 12 }} strong>Días libres del mes:</Text>
                            <div style={{ marginTop: 4, maxHeight: 120, overflowY: 'auto' }}>
                              {freeSummary.freeDayWeekLines.length > 0 ? (
                                freeSummary.freeDayWeekLines.map((line, idx) => (
                                  <div key={`${emp.id}-free-week-${idx}`} style={{ fontSize: 12, lineHeight: 1.35 }}>
                                    {line}
                                  </div>
                                ))
                              ) : (
                                'Sin días libres'
                              )}
                            </div>
                          </div>
                        </Space>
                      }
                    >
                      <InfoCircleOutlined
                        onMouseDown={e => e.preventDefault()}
                        style={{ color: '#1677ff', fontSize: 14, cursor: 'pointer', flexShrink: 0 }}
                      />
                    </Popover>

                    <div
                      draggable
                      onDragStart={() => handleDragStart(emp)}
                      onDragEnd={handleDragEnd}
                      title={busy ? 'Ya asignado en la semana (puedes asignar a otro turno)' : 'Arrastrar al turno'}
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        padding: '6px 8px',
                        borderRadius: 6,
                        border: `1px solid ${busy ? '#91caff' : '#d9d9d9'}`,
                        background: busy ? '#e6f4ff' : '#fafafa',
                        cursor: 'grab',
                        userSelect: 'none',
                        fontSize: 13,
                        flex: 1,
                        minWidth: 0,
                      }}
                    >
                      <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {emp.firstName} {emp.lastName}
                      </span>
                      <Space size={4}>
                        <Tag color="geekblue" style={{ marginInlineEnd: 0, fontSize: 11 }}>
                          {weeklyHours.toFixed(1)}h
                        </Tag>
                        {busy && <Badge status="processing" />}
                      </Space>
                    </div>
                  </div>
                );
              })}
            </Card>
          </Col>

          {/* ── Panel derecho: tablero semanal ─────────────────────────── */}
          <Col xs={24} lg={19}>
            <div style={{ overflowX: 'auto', WebkitOverflowScrolling: 'touch', paddingBottom: 4 }}>
              <Row gutter={[4, 0]} style={{ flexWrap: 'nowrap', minWidth: 640 }}>
                {weekDays.map(day => {
                  const dateStr = day.format('YYYY-MM-DD');
                  const dow = day.day();
                  const isToday = day.isSame(dayjs(), 'day');
                  const isLockedDay = !day.isSame(selectedMonth, 'month');
                  const specialTemplates = templates.filter(t => t.effectiveDate === dateStr);
                  const dayTemplates = (specialTemplates.length > 0
                    ? specialTemplates
                    : templates.filter(t => !t.effectiveDate && t.dayOfWeek === dow)
                  ).sort(compareTemplatesByAreaAndName);

                  const daySlots = Object.values(slots).filter(
                    s => s.date === dateStr,
                  );
                  const assignedEmployeeIds = new Set(
                    daySlots
                      .filter(s => s.employee !== null)
                      .map(s => s.employee!.id),
                  );
                  const unassignedEmployeesForDay = eligibleEmployees.filter(
                    employee => !assignedEmployeeIds.has(employee.id),
                  );

                  return (
                    <Col key={dateStr} style={{ flex: '1 1 0', minWidth: 90 }}>
                      {/* Cabecera del día */}
                      <div
                        style={{
                          textAlign: 'center',
                          padding: '3px 2px',
                          borderRadius: '6px 6px 0 0',
                          background: isToday ? '#1677ff' : '#f0f2f5',
                          color: isToday ? '#fff' : isLockedDay ? '#999' : '#333',
                          opacity: isLockedDay ? 0.7 : 1,
                          fontWeight: 600,
                          fontSize: 11,
                          marginBottom: 3,
                        }}
                      >
                        <div>{DAY_LABELS[dow]}</div>
                        <div style={{ fontWeight: 400, fontSize: 10 }}>
                          {day.format('DD/MM')}
                        </div>
                      </div>

                      {/* Plantillas del día */}
                      {dayTemplates.length === 0 ? (
                        <div
                          style={{
                            textAlign: 'center',
                            padding: 12,
                            color: '#bfbfbf',
                            fontSize: 12,
                            border: '1px dashed #d9d9d9',
                            borderRadius: 6,
                          }}
                        >
                          Sin plantilla
                        </div>
                      ) : (
                        dayTemplates.map(tmpl => {
                          const color = areaColors[tmpl.workAreaId] ?? '#4096ff';
                          const templateSlots = daySlots
                            .filter(s => s.templateId === tmpl.id)
                            .sort((a, b) => a.slotIndex - b.slotIndex);

                          return (
                            <div
                              key={`${tmpl.id}-${dateStr}`}
                              style={{
                                marginBottom: 6,
                                border: `1px solid ${color}40`,
                                borderRadius: 6,
                                overflow: 'hidden',
                              }}
                            >
                              {/* Cabecera de la plantilla */}
                              <div
                                style={{
                                  background: `${color}20`,
                                  borderBottom: `1px solid ${color}40`,
                                  padding: '2px 4px',
                                }}
                              >
                                <div
                                  style={{
                                    fontWeight: 600,
                                    fontSize: 10,
                                    color,
                                    whiteSpace: 'nowrap',
                                    overflow: 'hidden',
                                    textOverflow: 'ellipsis',
                                  }}
                                >
                                  {tmpl.workRoleName}
                                </div>
                                <div style={{ fontSize: 9, color: '#888' }}>
                                  {tmpl.startTime.substring(0, 5)}-{tmpl.endTime.substring(0, 5)}
                                </div>
                              </div>

                              {/* Slots de empleado */}
                              <div style={{ padding: '4px 4px 2px' }}>
                                {templateSlots.map(slot => {
                                  const key = slotId(slot);
                                  const isDragOver = dragOverSlot === key;
                                  const hasEmployee = slot.employee !== null;

                                  return (
                                    <div
                                      key={key}
                                      onDragOver={e => {
                                        if (isLockedDay) return;
                                        handleDragOverSlot(key, e);
                                      }}
                                      onDrop={() => {
                                        if (isLockedDay) return;
                                        handleDropOnSlot(slot);
                                      }}
                                      onDragLeave={() => setDragOverSlot(null)}
                                      style={{
                                        minHeight: 24,
                                        marginBottom: 2,
                                        borderRadius: 3,
                                        border: isDragOver
                                          ? `2px dashed ${color}`
                                          : hasEmployee
                                          ? `1px solid ${color}80`
                                          : '1px dashed #d9d9d9',
                                        background: isDragOver
                                          ? `${color}15`
                                          : hasEmployee
                                          ? `${color}10`
                                          : '#fafafa',
                                        padding: '2px 4px',
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'space-between',
                                        transition: 'border 0.15s, background 0.15s',
                                        cursor: isLockedDay ? 'not-allowed' : 'default',
                                        opacity: isLockedDay ? 0.6 : 1,
                                      }}
                                    >
                                      {hasEmployee ? (
                                        <>
                                          <Tooltip
                                            title={`${slot.employee!.firstName} ${slot.employee!.lastName}`}
                                          >
                                            <span
                                              style={{
                                                fontSize: 10,
                                                overflow: 'hidden',
                                                textOverflow: 'ellipsis',
                                                whiteSpace: 'nowrap',
                                                maxWidth: '80%',
                                                color: '#222',
                                              }}
                                            >
                                              {abbrevName(slot.employee!.firstName, slot.employee!.lastName)}
                                            </span>
                                          </Tooltip>
                                          <CloseOutlined
                                            style={{ fontSize: 9, color: '#bbb', cursor: 'pointer', flexShrink: 0 }}
                                            onClick={() => clearSlot(key)}
                                          />
                                        </>
                                      ) : (
                                        <span
                                          style={{
                                            fontSize: 10,
                                            color: isDragOver ? color : '#bfbfbf',
                                            fontStyle: 'italic',
                                          }}
                                        >
                                          {isDragOver ? 'Soltar' : '—'}
                                        </span>
                                      )}
                                    </div>
                                  );
                                })}
                              </div>
                            </div>
                          );
                        })
                      )}

                      <div
                        style={{
                          marginTop: 4,
                          padding: '3px 4px',
                          border: '1px dashed #d9d9d9',
                          borderRadius: 4,
                          background: isLockedDay ? '#fafafa' : '#fcfcfc',
                          opacity: isLockedDay ? 0.7 : 1,
                        }}
                      >
                        <div style={{ fontSize: 9, fontWeight: 600, color: '#888', marginBottom: 2 }}>
                          Libres
                        </div>
                        {unassignedEmployeesForDay.length === 0 ? (
                          <div style={{ fontSize: 9, color: '#ccc', fontStyle: 'italic' }}>—</div>
                        ) : (
                          <div style={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                            {unassignedEmployeesForDay.map(employee => (
                              <div
                                key={`${dateStr}-${employee.id}`}
                                style={{
                                  fontSize: 9,
                                  color: '#666',
                                  padding: '1px 3px',
                                  borderRadius: 3,
                                  background: '#f5f5f5',
                                  whiteSpace: 'nowrap',
                                  overflow: 'hidden',
                                  textOverflow: 'ellipsis',
                                }}
                                title={`${employee.firstName} ${employee.lastName}`}
                              >
                                {abbrevName(employee.firstName, employee.lastName)}
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    </Col>
                  );
                })}
              </Row>
            </div>
          </Col>
        </Row>

        <Divider style={{ margin: '8px 0' }} />
        <Row justify="end">
          <Space>
            <Button
              onClick={() => loadWeek()}
              disabled={loadingTemplates || autofilling || confirming}
            >
              Recargar
            </Button>
            <Button
              danger
              icon={<DeleteOutlined />}
              onClick={() => {
                setSlots(prev => {
                  const next = { ...prev };
                  for (const k of Object.keys(next)) {
                    if (isDateInSelectedMonth(next[k].date)) {
                      next[k] = { ...next[k], employee: null };
                    }
                  }
                  return next;
                });
              }}
              disabled={filledSlots === 0 || autofilling || confirming}
            >
              Limpiar tablero
            </Button>
            <Popconfirm
              title="Borrar turnos de la semana"
              description={`Se eliminarán permanentemente los turnos guardados ${deleteWeekLabel}. Esta acción no se puede deshacer.`}
              onConfirm={handleDeleteWeekShifts}
              okText="Borrar"
              okButtonProps={{ danger: true }}
              cancelText="Cancelar"
              disabled={deletingWeek || autofilling || confirming}
            >
              <Button
                danger
                type="primary"
                icon={<DeleteOutlined />}
                loading={deletingWeek}
                disabled={autofilling || confirming || loadingTemplates}
              >
                Borrar turnos de la semana
              </Button>
            </Popconfirm>
          </Space>
        </Row>
      </Space>

      {/* ── Vista solo impresión — A4 horizontal ─────────────────────────── */}
      <div
        id="wsb-print-view"
        style={{
          position: 'absolute',
          left: '-9999px',
          top: 0,
          width: '277mm',
          overflow: 'hidden',
          pointerEvents: 'none',
        }}
      >
        {/* Encabezado */}
        <div style={{
          display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end',
          borderBottom: '0.5mm solid #333', paddingBottom: '2mm', marginBottom: '3mm',
        }}>
          <div>
            <div style={{ fontSize: '11pt', fontWeight: 700, letterSpacing: '0.05em' }}>
              HORARIO SEMANAL
            </div>
          </div>
          <div style={{ textAlign: 'right', fontSize: '8pt', color: '#444' }}>
            Semana del {weekStart.format('DD/MM/YYYY')} al {weekStart.add(6, 'day').format('DD/MM/YYYY')}
          </div>
        </div>

        {/* 7 columnas de días */}
        <div style={{ display: 'flex', gap: '2mm' }}>
          {weekDays.map(day => {
            const dateStr = day.format('YYYY-MM-DD');
            const dow = day.day();
            const isToday = day.isSame(dayjs(), 'day');
            const isLocked = !day.isSame(selectedMonth, 'month');
            const specialTemplates = templates.filter(t => t.effectiveDate === dateStr);
            const dayTemplates = (specialTemplates.length > 0
              ? specialTemplates
              : templates.filter(t => !t.effectiveDate && t.dayOfWeek === dow)
            ).sort(compareTemplatesByAreaAndName);
            const daySlots = Object.values(slots).filter(s => s.date === dateStr);
            const assignedIds = new Set(daySlots.filter(s => s.employee).map(s => s.employee!.id));
            const freeEmployees = eligibleEmployees.filter(e => !assignedIds.has(e.id));

            return (
              <div key={`print-${dateStr}`} style={{ flex: '1 1 0', minWidth: 0 }}>
                {/* Cabecera del día */}
                <div style={{
                  background: isToday ? '#1677ff' : isLocked ? '#bdbdbd' : '#434343',
                  color: 'white',
                  textAlign: 'center',
                  padding: '1.5mm 1mm',
                  borderRadius: '1mm 1mm 0 0',
                  marginBottom: '1mm',
                }}>
                  <div style={{ fontSize: '8pt', fontWeight: 700 }}>{DAY_LABELS[dow]}</div>
                  <div style={{ fontSize: '7pt', fontWeight: 400 }}>{day.format('DD/MM')}</div>
                </div>

                {/* Plantillas */}
                {dayTemplates.length === 0 ? (
                  <div style={{
                    fontSize: '7pt', color: '#aaa', textAlign: 'center',
                    padding: '2mm', border: '0.3mm dashed #ccc', borderRadius: '1mm',
                  }}>
                    Libre
                  </div>
                ) : dayTemplates.map(tmpl => {
                  const color = areaColors[tmpl.workAreaId] ?? '#4096ff';
                  const templateSlots = daySlots
                    .filter(s => s.templateId === tmpl.id)
                    .sort((a, b) => a.slotIndex - b.slotIndex);
                  return (
                    <div key={`print-${tmpl.id}-${dateStr}`} style={{
                      marginBottom: '1.5mm',
                      border: `0.3mm solid ${color}70`,
                      borderRadius: '0.8mm',
                      overflow: 'hidden',
                    }}>
                      <div style={{
                        background: `${color}20`,
                        borderBottom: `0.3mm solid ${color}50`,
                        padding: '1mm 1.5mm',
                      }}>
                        <div style={{
                          fontSize: '7pt', fontWeight: 700, color,
                          whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis',
                        }}>
                          {tmpl.workRoleName}
                        </div>
                        <div style={{ fontSize: '6pt', color: '#555' }}>
                          {tmpl.startTime.substring(0, 5)} – {tmpl.endTime.substring(0, 5)}
                        </div>
                      </div>
                      <div style={{ padding: '0.5mm' }}>
                        {templateSlots.map((slot, idx) => (
                          <div key={idx} style={{
                            fontSize: '7pt',
                            padding: '0.6mm 1mm',
                            borderTop: idx > 0 ? '0.2mm solid #eee' : 'none',
                            whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis',
                            color: slot.employee ? '#111' : '#aaa',
                            fontStyle: slot.employee ? 'normal' : 'italic',
                          }}>
                            {slot.employee
                              ? `${slot.employee.firstName} ${slot.employee.lastName}`
                              : 'Sin asignar'}
                          </div>
                        ))}
                      </div>
                    </div>
                  );
                })}

                {/* Empleados libres ese día */}
                {freeEmployees.length > 0 && (
                  <div style={{
                    border: '0.3mm dashed #ccc', borderRadius: '0.8mm',
                    padding: '1mm', marginTop: '1mm',
                  }}>
                    <div style={{ fontSize: '6pt', fontWeight: 700, color: '#888', marginBottom: '0.5mm' }}>
                      Libres
                    </div>
                    {freeEmployees.map(emp => (
                      <div key={emp.id} style={{
                        fontSize: '6.5pt', color: '#555',
                        whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis',
                      }}>
                        {emp.firstName} {emp.lastName}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            );
          })}
        </div>

        {/* Pie de página */}
        <div style={{
          marginTop: '3mm', borderTop: '0.3mm solid #ddd',
          paddingTop: '1.5mm', fontSize: '6pt', color: '#aaa',
          display: 'flex', justifyContent: 'space-between',
        }}>
          <span>Generado el {dayjs().format('DD/MM/YYYY HH:mm')}</span>
          <span>
            {Object.values(slots).filter(s => s.employee).length} turno(s) asignado(s) /{' '}
            {Object.values(slots).length} cupo(s) total
          </span>
        </div>
      </div>
    </Spin>
  );
};

export default WeeklyScheduleBoard;
