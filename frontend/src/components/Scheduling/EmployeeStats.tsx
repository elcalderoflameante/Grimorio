import { Card, Row, Col, Statistic, Empty, Progress, Typography, Popover } from 'antd';
import { CalendarOutlined, ClockCircleOutlined, DashboardOutlined, CheckCircleOutlined, WarningOutlined } from '@ant-design/icons';
import type { ShiftAssignmentDto } from '../../types';
import dayjs from 'dayjs';
import 'dayjs/locale/es';

const { Text } = Typography;

interface EmployeeStatsProps {
  employeeId: string;
  employeeName: string;
  shifts: ShiftAssignmentDto[];
  currentMonth: dayjs.Dayjs;
}

export const EmployeeStats = ({ employeeId, employeeName, shifts, currentMonth }: EmployeeStatsProps) => {
  dayjs.locale('es');
  // Filtrar turnos del mes actual del empleado
  const monthShifts = shifts.filter(shift => {
    const shiftDate = dayjs(shift.date);
    return shiftDate.isSame(currentMonth, 'month') && shift.employeeId === employeeId;
  });

  if (monthShifts.length === 0) {
    return (
      <Card style={{ marginTop: 16 }}>
        <Empty description={`Sin turnos asignados en ${currentMonth.format('MMMM YYYY')}`} />
      </Card>
    );
  }

  // Calcular estadísticas
  const daysAssigned = new Set(monthShifts.map(s => dayjs(s.date).format('YYYY-MM-DD'))).size;
  const totalHours = monthShifts.reduce((sum, shift) => sum + shift.workedHours, 0);
  
  // Nota: Las validaciones de horas ahora son por semana (WeeklyMinHours/WeeklyMaxHours) a nivel de Employee
  // No hay validación mensual global en ScheduleConfiguration
  
  // Días libres en el mes
  const daysInMonth = currentMonth.daysInMonth();
  const freeDays = daysInMonth - daysAssigned;
  const recommendedFreeDays = 6; // Estándar recomendado
  const freeDaysStatus = freeDays >= recommendedFreeDays ? 'success' : freeDays >= 4 ? 'normal' : 'exception';

  const assignedDates = new Set(monthShifts.map(s => dayjs(s.date).format('YYYY-MM-DD')));
  const freeDayLabels = Array.from({ length: daysInMonth }, (_, index) => {
    const date = currentMonth.date(index + 1);
    const key = date.format('YYYY-MM-DD');
    return assignedDates.has(key) ? null : date.format('dddd D');
  }).filter((label): label is string => Boolean(label));

  // Horas promedio por día
  const avgHoursPerDay = daysAssigned > 0 ? (totalHours / daysAssigned).toFixed(2) : '0.00';

  return (
    <Card 
      title={`Estadísticas - ${employeeName} (${currentMonth.format('MMMM YYYY')})`}
      style={{ marginTop: 16 }}
      size="small"
    >
      {/* Sección de Resumen */}
      <Card 
        type="inner" 
        title="📊 Resumen de Asignaciones" 
        size="small"
        style={{ marginBottom: 16 }}
      >
        <Row gutter={[16, 16]}>
          <Col xs={24} sm={12} md={6}>
            <Statistic
              title="Días Asignados"
              value={daysAssigned}
              prefix={<CalendarOutlined />}
              styles={{ content: { fontSize: '20px' } }}
            />
          </Col>
          <Col xs={24} sm={12} md={6}>
            <Statistic
              title="Horas Totales"
              value={totalHours}
              suffix="h"
              precision={2}
              prefix={<ClockCircleOutlined />}
              styles={{ content: { fontSize: '20px' } }}
            />
          </Col>
          <Col xs={24} sm={12} md={6}>
            <Statistic
              title="Promedio/Día"
              value={avgHoursPerDay}
              suffix="h"
              prefix={<DashboardOutlined />}
              styles={{ content: { fontSize: '20px' } }}
            />
          </Col>
          <Col xs={24} sm={12} md={6}>
            <Statistic
              title="Turnos"
              value={monthShifts.length}
              prefix={<CheckCircleOutlined />}
              styles={{ content: { fontSize: '20px' } }}
            />
          </Col>
        </Row>
      </Card>

      {/* Sección de Días Libres */}
      <Card 
        type="inner" 
        title="🏖️ Días Libres" 
        size="small"
        style={{ marginBottom: 16 }}
      >
        <Row gutter={[16, 16]} align="middle">
          <Col xs={24} md={12}>
            <div style={{ marginBottom: 8 }}>
              <Text strong>Días libres: </Text>
              <Popover
                title="Fechas libres"
                content={
                  <div style={{ maxWidth: 360 }}>
                    {freeDayLabels.length > 0 ? freeDayLabels.join(', ') : 'Sin dias libres'}
                  </div>
                }
              >
                <Text
                  type={freeDaysStatus === 'exception' ? 'danger' : freeDaysStatus === 'success' ? 'success' : 'warning'}
                  style={{ cursor: 'pointer' }}
                >
                  {freeDays} / {recommendedFreeDays} recomendados
                </Text>
              </Popover>
            </div>
            <Progress 
              percent={(freeDays / recommendedFreeDays) * 100} 
              status={freeDaysStatus}
              strokeColor={freeDaysStatus === 'success' ? '#52c41a' : freeDaysStatus === 'exception' ? '#ff4d4f' : '#faad14'}
            />
            {freeDays < recommendedFreeDays && (
              <Text type="warning" style={{ fontSize: '12px', display: 'block', marginTop: 8 }}>
                <WarningOutlined /> Faltan {recommendedFreeDays - freeDays} días libres para alcanzar lo recomendado
              </Text>
            )}
          </Col>
          <Col xs={24} md={12}>
            <Row gutter={[8, 8]}>
              <Col span={12}>
                <Statistic
                  title="Días Trabajados"
                  value={daysAssigned}
                  suffix={`/ ${daysInMonth}`}
                  prefix={<CalendarOutlined />}
                />
              </Col>
              <Col span={12}>
                <Statistic
                  title="Días Libres"
                  value={freeDays}
                  prefix={<CheckCircleOutlined />}
                  styles={{ content: { 
                    color: freeDaysStatus === 'success' ? '#52c41a' : freeDaysStatus === 'exception' ? '#ff4d4f' : '#faad14'
                  } }}
                />
              </Col>
            </Row>
          </Col>
        </Row>
      </Card>
    </Card>
  );
};
