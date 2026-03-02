import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Button, DatePicker, Form, Input, InputNumber, Modal, Select, Space, Table, message, Card, Row, Col, List, Divider, Empty, Popconfirm } from 'antd';
import html2canvas from 'html2canvas';
import jsPDF from 'jspdf';
import dayjs, { type Dayjs } from 'dayjs';
import { branchApi, employeeService, payrollApi } from '../../services/api';
import { formatError } from '../../utils/errorHandler';
import type {
  EmployeePayrollSummaryDto,
  CreatePayrollAdvanceDto,
  CreateEmployeeConsumptionDto,
  CreatePayrollAdjustmentDto,
  PayrollConfigurationDto,
  PayrollAdvanceDto,
  EmployeeConsumptionDto,
  PayrollAdjustmentDto,
  PayrollAdjustmentTypeValue,
  PayrollAdjustmentCategoryValue,
  PayrollRoleStatusValue,
  BranchDto,
  EmployeeDto,
} from '../../types';
import { PayrollAdjustmentType, PayrollAdjustmentCategory, PayrollRoleStatus } from '../../types';
import logo from '../../assets/ECF-Logo.png';

interface AdvanceFormValues {
  date: Dayjs;
  amount: number;
  method: string;
  notes?: string;
}

interface ConsumptionFormValues {
  date: Dayjs;
  amount: number;
  notes?: string;
}

interface AdjustmentFormValues {
  date: Dayjs;
  type: PayrollAdjustmentTypeValue;
  category: PayrollAdjustmentCategoryValue;
  hours?: number;
  amount?: number;
  notes?: string;
}

const currency = (value: number) => `$${value.toFixed(2)}`;

const formatPeriod = (value: Dayjs) => {
  const label = value.format('MMMM YYYY');
  return label.charAt(0).toUpperCase() + label.slice(1);
};

const getAdjustmentCategoryLabel = (category: PayrollAdjustmentCategoryValue): string => {
  switch (category) {
    case PayrollAdjustmentCategory.Overtime50:
      return 'Horas extra 50%';
    case PayrollAdjustmentCategory.Overtime100:
      return 'Horas extra 100%';
    case PayrollAdjustmentCategory.Bonus:
      return 'Bono';
    case PayrollAdjustmentCategory.OtherIncome:
      return 'Otro ingreso';
    case PayrollAdjustmentCategory.OtherDeduction:
      return 'Otro descuento';
    default:
      return 'Ajuste';
  }
};

interface SalaryBreakdownItem {
  key: string;
  detail: string;
  note: string;
  amount: number;
  deleteType?: 'adjustment' | 'advance' | 'consumption';
  deleteId?: string;
}

interface SalaryRow {
  key: string;
  label: string;
  value: string | ReactNode;
  breakdown?: SalaryBreakdownItem[];
}

interface PdfLineItem {
  label: string;
  amount: number;
}

export const PayrollSummary = () => {
  const payrollPdfRef = useRef<HTMLDivElement | null>(null);
  const [month, setMonth] = useState<Dayjs>(dayjs());
  const [loading, setLoading] = useState(false);
  const [summary, setSummary] = useState<EmployeePayrollSummaryDto[]>([]);
  const [selectedEmployee, setSelectedEmployee] = useState<EmployeePayrollSummaryDto | null>(null);
  const [selectedEmployeeDetail, setSelectedEmployeeDetail] = useState<EmployeeDto | null>(null);
  const [advanceOpen, setAdvanceOpen] = useState(false);
  const [consumptionOpen, setConsumptionOpen] = useState(false);
  const [adjustmentOpen, setAdjustmentOpen] = useState(false);
  const [advanceForm] = Form.useForm<AdvanceFormValues>();
  const [consumptionForm] = Form.useForm<ConsumptionFormValues>();
  const [adjustmentForm] = Form.useForm<AdjustmentFormValues>();
  const [generatingEmployeeId, setGeneratingEmployeeId] = useState<string | null>(null);
  const [payrollConfig, setPayrollConfig] = useState<PayrollConfigurationDto | null>(null);
  const [branchInfo, setBranchInfo] = useState<BranchDto | null>(null);
  const [advancesDetails, setAdvancesDetails] = useState<PayrollAdvanceDto[]>([]);
  const [consumptionsDetails, setConsumptionsDetails] = useState<EmployeeConsumptionDto[]>([]);
  const [adjustmentsDetails, setAdjustmentsDetails] = useState<PayrollAdjustmentDto[]>([]);
  const [detailsLoading, setDetailsLoading] = useState(false);
  const [currentRoleStatus, setCurrentRoleStatus] = useState<PayrollRoleStatusValue | null>(null);
  const [isExportingPdf, setIsExportingPdf] = useState(false);
  const adjustmentCategory = Form.useWatch('category', adjustmentForm);
  const isOvertimeCategory = adjustmentCategory === PayrollAdjustmentCategory.Overtime50
    || adjustmentCategory === PayrollAdjustmentCategory.Overtime100;
  const isRoleLocked = currentRoleStatus != null && currentRoleStatus !== PayrollRoleStatus.Generated;

  const loadSummary = async () => {
    setLoading(true);
    try {
      const response = await payrollApi.getSummary(month.year(), month.month() + 1);
      const summaryRows = Array.isArray(response.data) ? response.data : [];
      setSummary(summaryRows);

      if (selectedEmployee) {
        const refreshedSelected = summaryRows.find((item) => item.employeeId === selectedEmployee.employeeId);
        setSelectedEmployee(refreshedSelected ?? null);
        if (refreshedSelected) {
          await loadMovementDetails(refreshedSelected.employeeId);
        }
      }
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadSummary();
  }, [month]);

  useEffect(() => {
    const loadConfiguration = async () => {
      try {
        const response = await payrollApi.getConfiguration();
        setPayrollConfig(response.data ?? null);
      } catch {
        setPayrollConfig(null);
      }
    };

    loadConfiguration();
  }, []);

  useEffect(() => {
    const loadBranchInfo = async () => {
      try {
        const response = await branchApi.getCurrent();
        setBranchInfo(response.data ?? null);
      } catch {
        setBranchInfo(null);
      }
    };

    loadBranchInfo();
  }, []);

  useEffect(() => {
    if (!selectedEmployee) {
      setAdvancesDetails([]);
      setConsumptionsDetails([]);
      setAdjustmentsDetails([]);
      setCurrentRoleStatus(null);
      setSelectedEmployeeDetail(null);
      return;
    }

    loadMovementDetails(selectedEmployee.employeeId);
  }, [month, selectedEmployee?.employeeId]);

  useEffect(() => {
    const loadEmployeeDetail = async () => {
      if (!selectedEmployee) {
        setSelectedEmployeeDetail(null);
        return;
      }

      try {
        const response = await employeeService.getById(selectedEmployee.employeeId);
        setSelectedEmployeeDetail(response.data ?? null);
      } catch {
        setSelectedEmployeeDetail(null);
      }
    };

    loadEmployeeDetail();
  }, [selectedEmployee?.employeeId]);

  const openAdvanceModal = (employee: EmployeePayrollSummaryDto) => {
    setSelectedEmployee(employee);
    advanceForm.resetFields();
    advanceForm.setFieldsValue({ date: dayjs(), amount: 0, method: 'Transferencia' });
    setAdvanceOpen(true);
  };

  const openConsumptionModal = (employee: EmployeePayrollSummaryDto) => {
    setSelectedEmployee(employee);
    consumptionForm.resetFields();
    consumptionForm.setFieldsValue({ date: dayjs(), amount: 0 });
    setConsumptionOpen(true);
  };

  const openAdjustmentModal = (employee: EmployeePayrollSummaryDto) => {
    setSelectedEmployee(employee);
    adjustmentForm.resetFields();
    adjustmentForm.setFieldsValue({
      date: dayjs(),
      type: PayrollAdjustmentType.Income,
      category: PayrollAdjustmentCategory.OtherIncome,
    });
    setAdjustmentOpen(true);
  };

  const handleAdvanceSubmit = async (values: AdvanceFormValues) => {
    if (!selectedEmployee) return;
    const payload: CreatePayrollAdvanceDto = {
      employeeId: selectedEmployee.employeeId,
      date: values.date.format('YYYY-MM-DD'),
      amount: values.amount,
      method: values.method,
      notes: values.notes,
    };

    try {
      await payrollApi.createAdvance(payload);
      message.success('Adelanto registrado');
      setAdvanceOpen(false);
      await loadSummary();
      await loadMovementDetails(selectedEmployee.employeeId);
    } catch (error) {
      message.error(formatError(error));
    }
  };

  const handleConsumptionSubmit = async (values: ConsumptionFormValues) => {
    if (!selectedEmployee) return;
    const payload: CreateEmployeeConsumptionDto = {
      employeeId: selectedEmployee.employeeId,
      date: values.date.format('YYYY-MM-DD'),
      amount: values.amount,
      notes: values.notes,
    };

    try {
      await payrollApi.createConsumption(payload);
      message.success('Consumo registrado');
      setConsumptionOpen(false);
      await loadSummary();
      await loadMovementDetails(selectedEmployee.employeeId);
    } catch (error) {
      message.error(formatError(error));
    }
  };

  const handleAdjustmentSubmit = async (values: AdjustmentFormValues) => {
    if (!selectedEmployee) return;
    const isOvertime = values.category === PayrollAdjustmentCategory.Overtime50
      || values.category === PayrollAdjustmentCategory.Overtime100;
    const payload: CreatePayrollAdjustmentDto = {
      employeeId: selectedEmployee.employeeId,
      date: values.date.format('YYYY-MM-DD'),
      type: values.type,
      category: values.category,
      hours: values.hours ?? null,
      amount: isOvertime ? null : (values.amount ?? null),
      notes: values.notes,
    };

    try {
      await payrollApi.createAdjustment(payload);
      message.success('Ajuste registrado');
      setAdjustmentOpen(false);
      await loadSummary();
      await loadMovementDetails(selectedEmployee.employeeId);
    } catch (error) {
      message.error(formatError(error));
    }
  };

  const handleGeneratePayrollRole = async (employee: EmployeePayrollSummaryDto) => {
    setGeneratingEmployeeId(employee.employeeId);
    try {
      const response = await payrollApi.generateRoles(month.year(), month.month() + 1, employee.employeeId);
      message.success(
        `Rol ${response.data.updatedCount > 0 ? 'recalculado' : 'generado'} para ${employee.employeeName}`
      );
      await loadSummary();
      await loadMovementDetails(employee.employeeId);
    } catch (error) {
      message.error(`Error generando rol para ${employee.employeeName}: ${formatError(error)}`);
    } finally {
      setGeneratingEmployeeId(null);
    }
  };

  const loadMovementDetails = async (employeeId: string) => {
    setDetailsLoading(true);
    try {
      const year = month.year();
      const monthNumber = month.month() + 1;
      const [advancesResponse, consumptionsResponse, adjustmentsResponse, rolesResponse] = await Promise.all([
        payrollApi.getAdvances(employeeId, year, monthNumber),
        payrollApi.getConsumptions(employeeId, year, monthNumber),
        payrollApi.getAdjustments(employeeId, year, monthNumber),
        payrollApi.getRolesByEmployee(employeeId),
      ]);

      setAdvancesDetails(Array.isArray(advancesResponse.data) ? advancesResponse.data : []);
      setConsumptionsDetails(Array.isArray(consumptionsResponse.data) ? consumptionsResponse.data : []);
      setAdjustmentsDetails(Array.isArray(adjustmentsResponse.data) ? adjustmentsResponse.data : []);

      const currentRole = rolesResponse.data.find((role) => role.year === year && role.month === monthNumber);
      setCurrentRoleStatus(currentRole?.status ?? null);
    } catch (error) {
      setAdvancesDetails([]);
      setConsumptionsDetails([]);
      setAdjustmentsDetails([]);
      setCurrentRoleStatus(null);
    } finally {
      setDetailsLoading(false);
    }
  };

  const handleDeleteAdjustment = async (adjustmentId: string) => {
    if (!selectedEmployee) return;

    try {
      await payrollApi.deleteAdjustment(adjustmentId);
      message.success('Ajuste eliminado');
      await Promise.all([
        loadSummary(),
        loadMovementDetails(selectedEmployee.employeeId),
      ]);
    } catch (error) {
      message.error(`No se pudo eliminar el ajuste: ${formatError(error)}`);
    }
  };

  const handleDeleteAdvance = async (advanceId: string) => {
    if (!selectedEmployee) return;

    try {
      await payrollApi.deleteAdvance(advanceId);
      message.success('Adelanto eliminado');
      await Promise.all([
        loadSummary(),
        loadMovementDetails(selectedEmployee.employeeId),
      ]);
    } catch (error) {
      message.error(`No se pudo eliminar el adelanto: ${formatError(error)}`);
    }
  };

  const handleDeleteConsumption = async (consumptionId: string) => {
    if (!selectedEmployee) return;

    try {
      await payrollApi.deleteConsumption(consumptionId);
      message.success('Consumo eliminado');
      await Promise.all([
        loadSummary(),
        loadMovementDetails(selectedEmployee.employeeId),
      ]);
    } catch (error) {
      message.error(`No se pudo eliminar el consumo: ${formatError(error)}`);
    }
  };

  const handleExportPayrollPdf = async () => {
    if (!selectedEmployee) {
      message.info('Selecciona un empleado para generar el PDF.');
      return;
    }

    if (!currentRoleStatus) {
      message.info('No hay rol generado para este período.');
      return;
    }

    const target = payrollPdfRef.current;
    if (!target) {
      message.error('No se pudo preparar el PDF del rol.');
      return;
    }

    try {
      setIsExportingPdf(true);
      await new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)));

      const canvas = await html2canvas(target, {
        backgroundColor: '#ffffff',
        scale: 2.2,
        useCORS: true,
        scrollX: 0,
        scrollY: -window.scrollY,
        width: target.scrollWidth,
        height: target.scrollHeight,
        windowWidth: target.scrollWidth,
        windowHeight: target.scrollHeight,
      });

      const imgData = canvas.toDataURL('image/png');
      const pdf = new jsPDF('p', 'mm', 'a4');
      const pageWidth = pdf.internal.pageSize.getWidth();
      const pageHeight = pdf.internal.pageSize.getHeight();
      const margin = 8;
      const imgWidth = pageWidth - margin * 2;
      const imgHeight = (canvas.height * imgWidth) / canvas.width;
      let position = 0;

      if (imgHeight <= pageHeight - margin * 2) {
        pdf.addImage(imgData, 'PNG', margin, margin, imgWidth, imgHeight);
      } else {
        let remainingHeight = imgHeight;
        while (remainingHeight > 0) {
          pdf.addImage(imgData, 'PNG', margin, margin - position, imgWidth, imgHeight);
          remainingHeight -= pageHeight - margin * 2;
          position += pageHeight - margin * 2;
          if (remainingHeight > 0) {
            pdf.addPage();
          }
        }
      }

      pdf.save(`rol-pagos-${selectedEmployee.employeeName}-${month.format('YYYY-MM')}.pdf`);
    } catch (error) {
      message.error('No se pudo generar el PDF del rol.');
    } finally {
      setIsExportingPdf(false);
    }
  };

  const breakdownData = useMemo(() => {
    if (!selectedEmployee) {
      return null;
    }

    const monthlyHours = payrollConfig?.monthlyHours && payrollConfig.monthlyHours > 0
      ? payrollConfig.monthlyHours
      : 240;
    const overtimeRate50 = payrollConfig?.overtimeRate50 ?? 50;
    const overtimeRate100 = payrollConfig?.overtimeRate100 ?? 100;

    const resolveAdjustmentAmount = (item: PayrollAdjustmentDto) => {
      const isOvertime50 = item.category === PayrollAdjustmentCategory.Overtime50;
      const isOvertime100 = item.category === PayrollAdjustmentCategory.Overtime100;

      if (isOvertime50 || isOvertime100) {
        if (item.hours != null) {
          const hourlyRate = selectedEmployee.baseSalary / monthlyHours;
          const surcharge = isOvertime100 ? overtimeRate100 : overtimeRate50;
          return Math.round(item.hours * hourlyRate * (1 + surcharge / 100) * 100) / 100;
        }
        return item.amount ?? 0;
      }

      return item.amount ?? 0;
    };

    const decimosBreakdown: SalaryBreakdownItem[] = [
      {
        key: 'decimo-third',
        detail: 'Décimo tercero',
        note: '-',
        amount: selectedEmployee.decimoThird,
      },
      {
        key: 'decimo-fourth',
        detail: 'Décimo cuarto',
        note: '-',
        amount: selectedEmployee.decimoFourth,
      },
    ];

    const advancesBreakdown: SalaryBreakdownItem[] = advancesDetails.map((item) => ({
      key: item.id,
      detail: `${dayjs(item.date).format('DD/MM/YYYY')} - ${item.method}`,
      note: item.notes?.trim() || '-',
      amount: item.amount,
      deleteType: 'advance',
      deleteId: item.id,
    }));

    const consumptionsBreakdown: SalaryBreakdownItem[] = consumptionsDetails.map((item) => ({
      key: item.id,
      detail: dayjs(item.date).format('DD/MM/YYYY'),
      note: item.notes?.trim() || '-',
      amount: item.amount,
      deleteType: 'consumption',
      deleteId: item.id,
    }));

    const adjustmentIncomeBreakdown: SalaryBreakdownItem[] = adjustmentsDetails
      .filter((item) => item.type === PayrollAdjustmentType.Income
        && item.category !== PayrollAdjustmentCategory.Overtime50
        && item.category !== PayrollAdjustmentCategory.Overtime100)
      .map((item) => ({
        key: item.id,
        detail: `${dayjs(item.date).format('DD/MM/YYYY')} - ${getAdjustmentCategoryLabel(item.category)}`,
        note: item.notes?.trim() || '-',
        amount: resolveAdjustmentAmount(item),
        deleteType: 'adjustment',
        deleteId: item.id,
      }));

    const overtime50Breakdown: SalaryBreakdownItem[] = adjustmentsDetails
      .filter((item) => item.type === PayrollAdjustmentType.Income && item.category === PayrollAdjustmentCategory.Overtime50)
      .map((item) => ({
        key: item.id,
        detail: `${dayjs(item.date).format('DD/MM/YYYY')} - ${item.hours ?? 0}h`,
        note: item.notes?.trim() || '-',
        amount: resolveAdjustmentAmount(item),
        deleteType: 'adjustment',
        deleteId: item.id,
      }));

    const overtime100Breakdown: SalaryBreakdownItem[] = adjustmentsDetails
      .filter((item) => item.type === PayrollAdjustmentType.Income && item.category === PayrollAdjustmentCategory.Overtime100)
      .map((item) => ({
        key: item.id,
        detail: `${dayjs(item.date).format('DD/MM/YYYY')} - ${item.hours ?? 0}h`,
        note: item.notes?.trim() || '-',
        amount: resolveAdjustmentAmount(item),
        deleteType: 'adjustment',
        deleteId: item.id,
      }));

    const adjustmentDeductionBreakdown: SalaryBreakdownItem[] = adjustmentsDetails
      .filter((item) => item.type === PayrollAdjustmentType.Deduction)
      .map((item) => ({
        key: item.id,
        detail: `${dayjs(item.date).format('DD/MM/YYYY')} - ${getAdjustmentCategoryLabel(item.category)}`,
        note: item.notes?.trim() || '-',
        amount: resolveAdjustmentAmount(item),
        deleteType: 'adjustment',
        deleteId: item.id,
      }));

    return {
      decimosBreakdown,
      advancesBreakdown,
      consumptionsBreakdown,
      adjustmentIncomeBreakdown,
      overtime50Breakdown,
      overtime100Breakdown,
      adjustmentDeductionBreakdown,
    };
  }, [selectedEmployee, advancesDetails, consumptionsDetails, adjustmentsDetails, payrollConfig]);

  const salaryRows = useMemo<SalaryRow[]>(() => {
    if (!selectedEmployee) {
      return [];
    }

    if (!breakdownData) {
      return [];
    }

    const {
      decimosBreakdown,
      advancesBreakdown,
      consumptionsBreakdown,
      adjustmentIncomeBreakdown,
      overtime50Breakdown,
      overtime100Breakdown,
      adjustmentDeductionBreakdown,
    } = breakdownData;

    return [
      { key: 'position', label: 'Posición', value: selectedEmployee.positionName },
      { key: 'base-salary', label: 'Sueldo base', value: currency(selectedEmployee.baseSalary) },
      { key: 'iess', label: 'IESS', value: currency(selectedEmployee.iessEmployee) },
      {
        key: 'overtime-50',
        label: 'Horas extra 50%',
        value: currency(selectedEmployee.overtime50),
        breakdown: overtime50Breakdown,
      },
      {
        key: 'overtime-100',
        label: 'Horas extra 100%',
        value: currency(selectedEmployee.overtime100),
        breakdown: overtime100Breakdown,
      },
      {
        key: 'decimos',
        label: 'Décimos',
        value: currency(selectedEmployee.decimoThird + selectedEmployee.decimoFourth),
        breakdown: decimosBreakdown,
      },
      { key: 'reserve-fund', label: 'Fondo de reserva', value: currency(selectedEmployee.reserveFund) },
      {
        key: 'advances',
        label: 'Adelantos',
        value: currency(selectedEmployee.advances),
        breakdown: advancesBreakdown,
      },
      {
        key: 'consumptions',
        label: 'Consumos',
        value: currency(selectedEmployee.consumptions),
        breakdown: consumptionsBreakdown,
      },
      {
        key: 'adjustments-income',
        label: 'Ajustes (ingresos)',
        value: currency(selectedEmployee.otherIncome),
        breakdown: adjustmentIncomeBreakdown,
      },
      {
        key: 'adjustments-deduction',
        label: 'Ajustes (descuentos)',
        value: currency(selectedEmployee.otherDeductions),
        breakdown: adjustmentDeductionBreakdown,
      },
      {
        key: 'net',
        label: 'Neto',
        value: <strong style={{ color: selectedEmployee.netPay >= 0 ? 'green' : 'red' }}>{currency(selectedEmployee.netPay)}</strong>,
      },
    ];
  }, [selectedEmployee, breakdownData]);

  const pdfIncomeRows = useMemo<PdfLineItem[]>(() => {
    if (!selectedEmployee || !breakdownData) {
      return [];
    }

    const rows: PdfLineItem[] = [
      { label: 'Sueldo base', amount: selectedEmployee.baseSalary },
      { label: 'Décimo tercero', amount: selectedEmployee.decimoThird },
      { label: 'Décimo cuarto', amount: selectedEmployee.decimoFourth },
    ];

    if (selectedEmployee.reserveFund > 0) {
      rows.push({ label: 'Fondo de reserva', amount: selectedEmployee.reserveFund });
    }

    breakdownData.overtime50Breakdown.forEach((item) => {
      rows.push({ label: `Horas extra 50% - ${item.detail}`, amount: item.amount });
    });

    breakdownData.overtime100Breakdown.forEach((item) => {
      rows.push({ label: `Horas extra 100% - ${item.detail}`, amount: item.amount });
    });

    breakdownData.adjustmentIncomeBreakdown.forEach((item) => {
      rows.push({ label: `Ajuste ingreso - ${item.detail}`, amount: item.amount });
    });

    return rows;
  }, [selectedEmployee, breakdownData]);

  const pdfDeductionRows = useMemo<PdfLineItem[]>(() => {
    if (!selectedEmployee || !breakdownData) {
      return [];
    }

    const rows: PdfLineItem[] = [
      { label: 'IESS empleado', amount: selectedEmployee.iessEmployee },
    ];

    if (selectedEmployee.incomeTax > 0) {
      rows.push({ label: 'Impuesto renta', amount: selectedEmployee.incomeTax });
    }

    breakdownData.advancesBreakdown.forEach((item) => {
      rows.push({ label: `Adelanto - ${item.detail}`, amount: item.amount });
    });

    breakdownData.consumptionsBreakdown.forEach((item) => {
      rows.push({ label: `Consumo - ${item.detail}`, amount: item.amount });
    });

    breakdownData.adjustmentDeductionBreakdown.forEach((item) => {
      rows.push({ label: `Ajuste descuento - ${item.detail}`, amount: item.amount });
    });

    return rows;
  }, [selectedEmployee, breakdownData]);



  return (
    <Card size="small">
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        {/* Header */}
        <Space wrap>
          <DatePicker
            picker="month"
            value={month}
            onChange={(value) => value && setMonth(value)}
            allowClear={false}
          />
          <Button onClick={loadSummary} loading={loading}>Actualizar</Button>
        </Space>

        {/* Main Layout: 2 Columns */}
        <Row gutter={16} style={{ minHeight: 500 }}>
          {/* Left Column: Employee List */}
          <Col xs={24} sm={24} md={8}>
            <Card size="small" title="Empleados">
              <List
                itemLayout="vertical"
                dataSource={summary}
                loading={loading}
                locale={{ emptyText: 'No hay empleados para este período' }}
                split={false}
                renderItem={(employee) => (
                  <List.Item
                    style={{
                      padding: '8px 12px',
                      cursor: 'pointer',
                      borderRadius: 4,
                      marginBottom: 4,
                      backgroundColor: selectedEmployee?.employeeId === employee.employeeId ? '#e6f7ff' : 'transparent',
                      border: selectedEmployee?.employeeId === employee.employeeId ? '1px solid #1890ff' : '1px solid #f0f0f0',
                    }}
                    onClick={() => {
                      setSelectedEmployee(employee);
                    }}
                  >
                    <div style={{ fontWeight: 600, fontSize: 14 }}>{employee.employeeName}</div>
                    <div style={{ color: '#666', fontSize: 12 }}>{employee.positionName}</div>
                    <div style={{ color: '#999', fontSize: 12, marginTop: 4 }}>
                      Sueldo: {currency(employee.baseSalary)}
                    </div>
                  </List.Item>
                )}
              />
            </Card>
          </Col>

          {/* Right Column: Employee Details */}
          <Col xs={24} sm={24} md={16}>
            {selectedEmployee ? (
              <Card size="small" title={`Detalles - ${selectedEmployee.employeeName}`}>
                <Space direction="vertical" size="large" style={{ width: '100%' }}>
                  {/* Employee Info */}
                  <div>
                    <h4>Información Salarial - {formatPeriod(month)}</h4>
                    <Table
                      size="small"
                      pagination={false}
                      loading={detailsLoading}
                      dataSource={salaryRows}
                      rowKey="key"
                      columns={[
                        { dataIndex: 'label', key: 'label', width: '50%' },
                        { dataIndex: 'value', key: 'value', width: '50%', align: 'right' },
                      ]}
                      expandable={{
                        rowExpandable: (record) => Boolean(record.breakdown && record.breakdown.length > 0),
                        expandedRowRender: (record) => (
                          <Table
                            size="small"
                            pagination={false}
                            rowKey="key"
                            dataSource={record.breakdown || []}
                            columns={[
                              {
                                title: 'Detalle',
                                dataIndex: 'detail',
                                key: 'detail',
                                width: '45%',
                              },
                              {
                                title: 'Observación',
                                dataIndex: 'note',
                                key: 'note',
                                width: '35%',
                              },
                              {
                                title: 'Valor',
                                dataIndex: 'amount',
                                key: 'amount',
                                width: '15%',
                                align: 'right',
                                render: (value: number) => currency(value),
                              },
                              {
                                title: 'Acción',
                                key: 'action',
                                width: '15%',
                                align: 'center',
                                render: (_, item) => {
                                  if (!item.deleteType || !item.deleteId || isRoleLocked) return null;

                                  const confirmText = item.deleteType === 'advance'
                                    ? '¿Eliminar este adelanto?'
                                    : item.deleteType === 'consumption'
                                      ? '¿Eliminar este consumo?'
                                      : '¿Eliminar este ajuste?';

                                  const onConfirm = item.deleteType === 'advance'
                                    ? () => handleDeleteAdvance(item.deleteId as string)
                                    : item.deleteType === 'consumption'
                                      ? () => handleDeleteConsumption(item.deleteId as string)
                                      : () => handleDeleteAdjustment(item.deleteId as string);

                                  return (
                                    <Popconfirm
                                      title={confirmText}
                                      description="Esta acción no se puede deshacer"
                                      okText="Sí, eliminar"
                                      cancelText="Cancelar"
                                      onConfirm={onConfirm}
                                    >
                                      <Button type="link" danger size="small">Eliminar</Button>
                                    </Popconfirm>
                                  );
                                },
                              },
                            ]}
                          />
                        ),
                      }}
                    />
                  </div>

                  <Divider />

                  {/* Actions */}
                  <div>
                    <h4>Acciones</h4>
                    {isRoleLocked && (
                      <div style={{ fontSize: 12, color: '#999', marginBottom: 8 }}>
                        Rol autorizado/pagado: no se permiten cambios.
                      </div>
                    )}
                    <Space wrap>
                      <Button 
                        type="primary"
                        onClick={() => handleGeneratePayrollRole(selectedEmployee)}
                        loading={generatingEmployeeId === selectedEmployee.employeeId}
                        disabled={isRoleLocked}
                      >
                        Generar rol del período
                      </Button>
                      <Button 
                        onClick={() => openAdvanceModal(selectedEmployee)}
                        disabled={isRoleLocked}
                      >
                        Registrar adelanto
                      </Button>
                      <Button 
                        onClick={() => openConsumptionModal(selectedEmployee)}
                        disabled={isRoleLocked}
                      >
                        Registrar consumo
                      </Button>
                      <Button 
                        onClick={() => openAdjustmentModal(selectedEmployee)}
                        disabled={isRoleLocked}
                      >
                        Registrar ajuste
                      </Button>
                      <Button
                        onClick={handleExportPayrollPdf}
                        disabled={!currentRoleStatus || isExportingPdf}
                      >
                        {isExportingPdf ? 'Generando PDF...' : 'Generar PDF'}
                      </Button>
                    </Space>
                  </div>
                </Space>
              </Card>
            ) : (
              <Card size="small">
                <Empty description="Selecciona un empleado para ver sus detalles" />
              </Card>
            )}
          </Col>
        </Row>
      </Space>

      {/* Modals */}
      <Modal
        title={`Registrar adelanto${selectedEmployee ? ` - ${selectedEmployee.employeeName}` : ''}`}
        open={advanceOpen}
        onCancel={() => setAdvanceOpen(false)}
        onOk={() => advanceForm.submit()}
      >
        <Form form={advanceForm} layout="vertical" onFinish={handleAdvanceSubmit}>
          <Form.Item name="date" label="Fecha" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="amount" label="Monto" rules={[{ required: true }]}>
            <InputNumber min={0} step={0.01} precision={2} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="method" label="Método" rules={[{ required: true }]}>
            <Select
              options={[
                { value: 'Transferencia', label: 'Transferencia' },
                { value: 'Efectivo', label: 'Efectivo' },
                { value: 'Otro', label: 'Otro' },
              ]}
            />
          </Form.Item>
          <Form.Item name="notes" label="Notas">
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={`Registrar consumo${selectedEmployee ? ` - ${selectedEmployee.employeeName}` : ''}`}
        open={consumptionOpen}
        onCancel={() => setConsumptionOpen(false)}
        onOk={() => consumptionForm.submit()}
      >
        <Form form={consumptionForm} layout="vertical" onFinish={handleConsumptionSubmit}>
          <Form.Item name="date" label="Fecha" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="amount" label="Monto" rules={[{ required: true }]}>
            <InputNumber min={0} step={0.01} precision={2} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="notes" label="Notas">
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={`Registrar ajuste${selectedEmployee ? ` - ${selectedEmployee.employeeName}` : ''}`}
        open={adjustmentOpen}
        onCancel={() => setAdjustmentOpen(false)}
        onOk={() => adjustmentForm.submit()}
      >
        <Form form={adjustmentForm} layout="vertical" onFinish={handleAdjustmentSubmit}>
          <Form.Item name="date" label="Fecha" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="type" label="Tipo" rules={[{ required: true }]}>
            <Select
              options={[
                { value: PayrollAdjustmentType.Income, label: 'Ingreso' },
                { value: PayrollAdjustmentType.Deduction, label: 'Descuento' },
              ]}
            />
          </Form.Item>
          <Form.Item name="category" label="Categoría" rules={[{ required: true }]}>
            <Select
              options={[
                { value: PayrollAdjustmentCategory.Overtime50, label: 'Horas extra 50%' },
                { value: PayrollAdjustmentCategory.Overtime100, label: 'Horas extra 100%' },
                { value: PayrollAdjustmentCategory.Bonus, label: 'Bono' },
                { value: PayrollAdjustmentCategory.OtherIncome, label: 'Otro ingreso' },
                { value: PayrollAdjustmentCategory.OtherDeduction, label: 'Otro descuento' },
              ]}
            />
          </Form.Item>
          <Form.Item
            name="hours"
            label="Horas"
            rules={isOvertimeCategory ? [{ required: true, message: 'Para horas extra debes ingresar horas' }] : []}
          >
            <InputNumber min={0} step={0.25} precision={2} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="amount" label={isOvertimeCategory ? 'Monto (calculado automáticamente)' : 'Monto (opcional)'}>
            <InputNumber min={0} step={0.01} precision={2} style={{ width: '100%' }} disabled={isOvertimeCategory} />
          </Form.Item>
          <Form.Item name="notes" label="Notas">
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>
      {selectedEmployee && (
        <div
          ref={payrollPdfRef}
          style={{
            position: 'fixed',
            left: -9999,
            top: 0,
            width: 794,
            padding: 24,
            backgroundColor: '#ffffff',
            color: '#000000',
            fontFamily: 'Arial, sans-serif',
            fontSize: 12,
          }}
        >
          {currentRoleStatus === PayrollRoleStatus.Generated && (
            <div
              style={{
                position: 'absolute',
                inset: 0,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                textAlign: 'center',
                fontSize: 28,
                fontWeight: 700,
                color: 'rgba(200, 0, 0, 0.15)',
                transform: 'rotate(-20deg)',
                pointerEvents: 'none',
                zIndex: 0,
              }}
            >
              NO VALIDO COMO ROL FINAL, ES UN BORRADOR
            </div>
          )}
          <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 16, marginBottom: 12 }}>
            <div style={{ flex: 1 }}>
              <div style={{ fontSize: 16, fontWeight: 700 }}>{branchInfo?.name || 'Empresa'}</div>
              <div>RUC: {branchInfo?.identificationNumber || 'N/A'}</div>
              <div>Dirección: {branchInfo?.address || 'N/A'}</div>
              <div>Teléfono: {branchInfo?.phone || 'N/A'}</div>
            </div>
            <img src={logo} alt="Logo" style={{ width: 120, height: 'auto' }} />
          </div>

          <div style={{ textAlign: 'center', fontSize: 16, fontWeight: 700, marginBottom: 12 }}>
            Rol de pagos
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 12 }}>
            <div>
              <div><strong>Empleado:</strong> {selectedEmployeeDetail ? `${selectedEmployeeDetail.firstName} ${selectedEmployeeDetail.lastName}` : selectedEmployee.employeeName}</div>
              <div><strong>Cédula:</strong> {selectedEmployeeDetail?.identificationNumber || 'N/A'}</div>
              <div><strong>Cargo:</strong> {selectedEmployee.positionName}</div>
            </div>
            <div>
              <div><strong>Período:</strong> {formatPeriod(month)}</div>
              <div><strong>Cuenta bancaria:</strong> {selectedEmployee.bankAccount}</div>
              <div><strong>Estado:</strong> {currentRoleStatus ? (currentRoleStatus === PayrollRoleStatus.Generated ? 'Generado' : currentRoleStatus === PayrollRoleStatus.Authorized ? 'Autorizado' : 'Pagado') : 'N/A'}</div>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <div>
              <div style={{ fontWeight: 700, marginBottom: 6 }}>Ingresos</div>
              <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead>
                  <tr>
                    <th style={{ textAlign: 'left', borderBottom: '1px solid #ddd', paddingBottom: 4 }}>Detalle</th>
                    <th style={{ textAlign: 'right', borderBottom: '1px solid #ddd', paddingBottom: 4 }}>Valor</th>
                  </tr>
                </thead>
                <tbody>
                  {pdfIncomeRows.map((row) => (
                    <tr key={row.label}>
                      <td style={{ padding: '4px 0' }}>{row.label}</td>
                      <td style={{ padding: '4px 0', textAlign: 'right' }}>{currency(row.amount)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div>
              <div style={{ fontWeight: 700, marginBottom: 6 }}>Egresos</div>
              <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead>
                  <tr>
                    <th style={{ textAlign: 'left', borderBottom: '1px solid #ddd', paddingBottom: 4 }}>Detalle</th>
                    <th style={{ textAlign: 'right', borderBottom: '1px solid #ddd', paddingBottom: 4 }}>Valor</th>
                  </tr>
                </thead>
                <tbody>
                  {pdfDeductionRows.map((row) => (
                    <tr key={row.label}>
                      <td style={{ padding: '4px 0' }}>{row.label}</td>
                      <td style={{ padding: '4px 0', textAlign: 'right' }}>{currency(row.amount)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div style={{ marginTop: 12, borderTop: '1px solid #ddd', paddingTop: 8 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <div><strong>Total ingresos:</strong> {currency(selectedEmployee.totalIncome)}</div>
              <div><strong>Total egresos:</strong> {currency(selectedEmployee.totalDeductions)}</div>
              <div><strong>Neto a recibir:</strong> {currency(selectedEmployee.netPay)}</div>
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 80 }}>
            <div style={{ width: '40%', textAlign: 'center' }}>
              <div style={{ height: 40 }} />
              <div style={{ borderTop: '1px solid #000', marginBottom: 4 }} />
              <div>Firma empleado</div>
            </div>
            <div style={{ width: '40%', textAlign: 'center' }}>
              <div style={{ height: 40 }} />
              <div style={{ borderTop: '1px solid #000', marginBottom: 4 }} />
              <div>Firma empleador</div>
            </div>
          </div>
        </div>
      )}
    </Card>
  );
};
