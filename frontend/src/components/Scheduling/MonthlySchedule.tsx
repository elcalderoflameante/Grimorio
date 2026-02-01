import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Calendar, Card, DatePicker, message, Modal, Space, Spin, Table, Tabs, Typography } from 'antd';
import dayjs, { Dayjs } from 'dayjs';
import isoWeek from 'dayjs/plugin/isoWeek';
import 'dayjs/locale/es';
import { scheduleShiftApi, scheduleConfigurationApi } from '../../services/api';
import { useAuth } from '../../context/AuthContext';
import { formatError } from '../../utils/errorHandler';
import type { ShiftAssignmentDto, EmployeeDto, ScheduleConfigurationDto, ShiftGenerationWarningDto } from '../../types';

dayjs.extend(isoWeek);
dayjs.locale('es');

const { Title, Text } = Typography;

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
  const { branchId } = useAuth();
  const [selectedMonth, setSelectedMonth] = useState<Dayjs>(dayjs());
  const [selectedWeekStart, setSelectedWeekStart] = useState<Dayjs>(dayjs().startOf('week'));
  const [loading, setLoading] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [shifts, setShifts] = useState<ShiftAssignmentDto[]>([]);
  const [viewMode, setViewMode] = useState<'month' | 'week'>('month');
  const [freeEmployeesByDate, setFreeEmployeesByDate] = useState<Record<string, EmployeeDto[]>>({});
  const [freeDayColor, setFreeDayColor] = useState<string>('#E8E8E8');

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

  useEffect(() => {
    loadShifts(selectedMonth);
  }, [loadShifts, selectedMonth]);

  useEffect(() => {
    loadScheduleConfig();
  }, [loadScheduleConfig]);

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

  const handleGenerate = useCallback(async () => {
    if (!branchId) return;
    setGenerating(true);
    try {
      const year = selectedMonth.year();
      const monthNumber = selectedMonth.month() + 1;
      const result = await scheduleShiftApi.generate(year, monthNumber);
      const { assignments, warnings, totalShiftsGenerated, totalShiftsNotCovered } = result.data;
      
      if (warnings && warnings.length > 0) {
        Modal.warning({
          title: `Generación completada con observaciones`,
          width: 900,
          content: (
            <div>
              <p style={{ marginBottom: 16 }}>
                <strong>Turnos generados:</strong> {totalShiftsGenerated}<br />
                <strong>Turnos sin cubrir:</strong> {totalShiftsNotCovered}
              </p>
              <Table
                dataSource={warnings}
                pagination={false}
                size="small"
                rowKey={(_, index) => `warning-${index}`}
                columns={[
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
                ]}
              />
              <p style={{ marginTop: 16, color: '#ff4d4f' }}>
                <strong>Importante:</strong> Revise las observaciones anteriores para asegurar que tiene el personal necesario.
                Puede que necesite contratar empleados con estos roles o ajustar la disponibilidad del personal existente.
              </p>
            </div>
          )
        });
      } else {
        message.success(`${totalShiftsGenerated} turnos generados correctamente`);
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
  }, [branchId, loadShifts, loadFreeEmployees, selectedMonth, viewMode]);

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

  const weekTableData = useMemo(() => {
    const data: any[] = [];
    const freeDayColors = getColorVariants(freeDayColor);

    // Obtener el número máximo de turnos en un día
    const maxShiftsPerDay = Math.max(...weekShifts.map(w => w.dayShifts.length), 1);

    for (let i = 0; i < maxShiftsPerDay; i++) {
      const row: any = { key: `shift-${i}` };
      weekShifts.forEach(({ date, dayName, dayShifts }) => {
        const shift = dayShifts[i];
        const startTime = shift?.startTime?.substring(0, 5) || '--:--';
        const endTime = shift?.endTime?.substring(0, 5) || '--:--';
        const colors = getColorVariants(shift?.workAreaColor || '');
        
        row[date.format('YYYY-MM-DD')] = shift ? (
          <div 
            style={{ 
              fontSize: '12px',
              padding: '8px',
              backgroundColor: colors.bg,
              border: `1px solid ${colors.border}`,
              borderRadius: '4px',
              minHeight: '60px',
              display: 'flex',
              flexDirection: 'column',
              justifyContent: 'space-between'
            }}
          >
            <div style={{ fontWeight: 600, color: colors.text }}>
              {startTime}-{endTime}
            </div>
            <div style={{ color: '#333', fontWeight: 500 }}>{shift.employeeName}</div>
            <div style={{ color: colors.text, fontSize: '11px', fontWeight: 500 }}>{shift.workAreaName}</div>
            <div style={{ color: '#999', fontSize: '10px' }}>{shift.workRoleName}</div>
          </div>
        ) : null;
      });
      data.push(row);
    }

    // Fila de empleados libres
    const freeRow: any = { key: 'free-employees' };
    weekShifts.forEach(({ date }) => {
      const key = date.format('YYYY-MM-DD');
      const freeEmployees = freeEmployeesByDate[key] ?? [];

      freeRow[key] = (
        <div
          style={{
            fontSize: '11px',
            padding: '8px',
            backgroundColor: freeDayColors.bg,
            border: `1px dashed ${freeDayColors.border}`,
            borderRadius: '4px',
            minHeight: '60px',
            maxHeight: '140px',
            overflowY: 'auto',
          }}
        >
          <div style={{ fontWeight: 600, color: freeDayColors.text, marginBottom: 4 }}>Libres</div>
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
  }, [freeDayColor, freeEmployeesByDate, weekShifts]);

  const weekColumns = useMemo(() => {
    const columns: any[] = [];
    weekShifts.forEach(({ date, dayName }) => {
      columns.push({
        title: (
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontWeight: 600, fontSize: '14px' }}>{dayName}</div>
            <div style={{ fontSize: '12px', color: '#999' }}>{date.format('DD/MM')}</div>
          </div>
        ),
        dataIndex: date.format('YYYY-MM-DD'),
        key: date.format('YYYY-MM-DD'),
        align: 'center' as const,
        width: '14.28%',
      });
    });
    return columns;
  }, [weekShifts]);

  return (
    <Card>
      <Space orientation="vertical" size="large" style={{ width: '100%' }}>
        <Space orientation="horizontal" size="middle" style={{ width: '100%', justifyContent: 'space-between' }}>
          <div>
            <Title level={4} style={{ margin: 0 }}>Horarios</Title>
            <Text type="secondary">Genera y visualiza los turnos de trabajo.</Text>
          </div>
          <Space>
            <DatePicker
              picker="month"
              value={selectedMonth}
              onChange={(value) => value && setSelectedMonth(value)}
              allowClear={false}
            />
            <Button
              type="primary"
              loading={generating}
              onClick={handleGenerate}
            >
              Generar turnos
            </Button>
          </Space>
        </Space>

        <Spin spinning={loading}>
          <Tabs
            activeKey={viewMode}
            onChange={(key) => setViewMode(key as 'month' | 'week')}
            items={[
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
                                  }}
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
              {
                key: 'week',
                label: 'Vista de Semana',
                children: (
                  <Space orientation="vertical" size="large" style={{ width: '100%' }}>
                    <Space style={{ width: '100%', justifyContent: 'space-between' }}>
                      <Text>
                        Semana del {selectedWeekStart.format('DD/MM/YYYY')} al {selectedWeekStart.add(6, 'day').format('DD/MM/YYYY')}
                      </Text>
                      <Space>
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
                    </Space>
                    
                    <Table
                      columns={weekColumns}
                      dataSource={weekTableData}
                      pagination={false}
                      bordered
                      size="small"
                      scroll={{ x: 'max-content' }}
                      style={{ fontSize: '12px' }}
                    />
                  </Space>
                ),
              },
            ]}
          />
        </Spin>
      </Space>
    </Card>
  );
};
