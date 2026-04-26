import axios, { type AxiosResponse } from 'axios';
import type {
  UserDto,
  CreateUserDto,
  UpdateUserDto,
  RoleDto,
  CreateRoleDto,
  UpdateRoleDto,
  PermissionDto,
  CreatePermissionDto,
  UpdatePermissionDto,
  EmployeeDto,
  CreateEmployeeDto,
  UpdateEmployeeDto,
  PositionDto,
  CreatePositionDto,
  UpdatePositionDto,
  BranchDto,
  UpdateBranchDto,
  AuthResponse,
  PaginatedResult,
  WorkAreaDto,
  CreateWorkAreaDto,
  UpdateWorkAreaDto,
  WorkRoleDto,
  CreateWorkRoleDto,
  UpdateWorkRoleDto,
  EmployeeWorkRoleDto,
  AssignWorkRolesDto,
  EmployeeAvailabilityDto,
  CreateEmployeeAvailabilityDto,
  ScheduleConfigurationDto,
  CreateScheduleConfigurationDto,
  UpdateScheduleConfigurationDto,
  ShiftAssignmentDto,
  CreateShiftAssignmentDto,
  UpdateShiftAssignmentDto,
  ShiftTemplateDto,
  CreateShiftTemplateDto,
  UpdateShiftTemplateDto,
  ShiftGenerationResultDto,
  RestaurantTableDto,
  CreateRestaurantTableDto,
  UpdateRestaurantTableDto,
  TableServiceRequestDto,
  PublicTableInfoDto,
  PublicCreateTableServiceRequestDto,
  SetTableServiceRequestStatusDto,
  TableServiceRequestStatus,
  PublicRequestStatusDto,
  PublicActiveTableRequestDto,
  PayrollConfigurationDto,
  CreatePayrollConfigurationDto,
  EmployeePayrollSummaryDto,
  PayrollRoleDto,
  PayrollRoleFullDto,
  GeneratePayrollRolesResultDto,
  UpdatePayrollRoleStatusDto,
  PayrollAdvanceDto,
  CreatePayrollAdvanceDto,
  EmployeeConsumptionDto,
  CreateEmployeeConsumptionDto,
  PayrollAdjustmentDto,
  CreatePayrollAdjustmentDto,
  MeasurementUnitDto,
  CreateMeasurementUnitDto,
  UnitConversionDto,
  CreateUnitConversionDto,
  InventoryCategoryDto,
  CreateInventoryCategoryDto,
  InventoryArticleDto,
  CreateInventoryArticleDto,
  UpdateInventoryArticleDto,
  WarehouseDto,
  CreateWarehouseDto,
  WarehouseStockDto,
  StockMovementDto,
  RegisterMovementDto,
  RegisterInitialInventoryDto,
  StockAlertDto,
  ArticleType,
  MovementType,
  MenuCategoryDto,
  CreateMenuCategoryDto,
  MenuItemDto,
  MenuItemDetailDto,
  CreateMenuItemDto,
  UpdateMenuItemDto,
  RecipeIngredientDto,
  UpsertRecipeIngredientDto,
  DeductStockFromSaleDto,
  WorkStationDto,
  CreateWorkStationDto,
  UpdateWorkStationDto,
  OrderDto,
  CreateOrderDto,
  OrderItemDto,
  StationItemDto,
  CreateOrderItemDto,
  SupplierDto,
  CreateSupplierDto,
  UpdateSupplierDto,
  PurchaseOrderDto,
  CreatePurchaseOrderDto,
  UpdatePurchaseOrderDto,
  ReceivePurchaseOrderDto,
} from '../types';
import { getDetailedError } from '../utils/errorHandler';

const defaultApiBaseUrl = '/api';
const API_BASE_URL = import.meta.env.VITE_API_URL || defaultApiBaseUrl;

// Crear instancia de axios
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor para agregar token a todas las requests
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Interceptor para manejar errores (ej: token expirado)
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // No redirigir si es un request de login (credenciales inválidas)
      const isLoginRequest = error.config?.url?.includes('/auth/login');
      
      if (!isLoginRequest) {
        // Token expirado en rutas protegidas, limpia localStorage y redirige a login
        localStorage.removeItem('accessToken');
        localStorage.removeItem('user');
        localStorage.removeItem('branchId');
        window.location.href = '/login';
      }
    }
    
    // Log detallado para debugging (solo en desarrollo)
    if (import.meta.env.DEV) {
      console.error('[API Error]', getDetailedError(error));
    }
    
    return Promise.reject(error);
  }
);

// ======== AUTH ========
export const authApi = {
  login: (email: string, password: string): Promise<AxiosResponse<AuthResponse>> =>
    apiClient.post<AuthResponse>('/auth/login', { email, password }),
};

// ======== BRANCHES ========
export const branchApi = {
  getCurrent: (): Promise<AxiosResponse<BranchDto>> =>
    apiClient.get<BranchDto>('/branches/current'),
  updateCurrent: (data: UpdateBranchDto): Promise<AxiosResponse<BranchDto>> =>
    apiClient.put<BranchDto>('/branches/current', data),
};

// ======== USERS ========
export const userApi = {
  getAll: (): Promise<AxiosResponse<UserDto[]>> => 
    apiClient.get<UserDto[]>('/users'),
  getById: (id: string): Promise<AxiosResponse<UserDto>> => 
    apiClient.get<UserDto>(`/users/${id}`),
  create: (data: CreateUserDto): Promise<AxiosResponse<UserDto>> => 
    apiClient.post<UserDto>('/users', data),
  update: (id: string, data: UpdateUserDto): Promise<AxiosResponse<UserDto>> => 
    apiClient.put<UserDto>(`/users/${id}`, data),
  delete: (id: string): Promise<AxiosResponse<void>> => 
    apiClient.delete<void>(`/users/${id}`),
  assignRoles: (id: string, roleIds: string[]): Promise<AxiosResponse<void>> => 
    apiClient.post<void>(`/users/${id}/roles`, { roleIds }),
  changePassword: (id: string, currentPassword: string, newPassword: string): Promise<AxiosResponse<{ success: boolean; message: string }>> =>
    apiClient.post<{ success: boolean; message: string }>(`/users/${id}/change-password`, { currentPassword, newPassword }),
};

// ======== ROLES ========
export const roleApi = {
  getAll: (): Promise<AxiosResponse<RoleDto[]>> => 
    apiClient.get<RoleDto[]>('/roles'),
  getById: (id: string): Promise<AxiosResponse<RoleDto>> => 
    apiClient.get<RoleDto>(`/roles/${id}`),
  create: (data: CreateRoleDto): Promise<AxiosResponse<RoleDto>> => 
    apiClient.post<RoleDto>('/roles', data),
  update: (id: string, data: UpdateRoleDto): Promise<AxiosResponse<RoleDto>> => 
    apiClient.put<RoleDto>(`/roles/${id}`, data),
  delete: (id: string): Promise<AxiosResponse<void>> => 
    apiClient.delete<void>(`/roles/${id}`),
  assignPermissions: (id: string, permissionIds: string[]): Promise<AxiosResponse<void>> => 
    apiClient.post<void>(`/roles/${id}/permissions`, { permissionIds }),
};

// ======== PERMISSIONS ========
export const permissionApi = {
  getAll: (): Promise<AxiosResponse<PermissionDto[]>> => 
    apiClient.get<PermissionDto[]>('/permissions'),
  getById: (id: string): Promise<AxiosResponse<PermissionDto>> => 
    apiClient.get<PermissionDto>(`/permissions/${id}`),
  create: (data: CreatePermissionDto): Promise<AxiosResponse<PermissionDto>> => 
    apiClient.post<PermissionDto>('/permissions', data),
  update: (id: string, data: UpdatePermissionDto): Promise<AxiosResponse<PermissionDto>> => 
    apiClient.put<PermissionDto>(`/permissions/${id}`, data),
  delete: (id: string): Promise<AxiosResponse<void>> => 
    apiClient.delete<void>(`/permissions/${id}`),
};

// ======== EMPLOYEES ========
export const employeeApi = {
  getAll: (pageNumber = 1, pageSize = 10, onlyActive = true): Promise<AxiosResponse<EmployeeDto[]>> =>
    apiClient.get<EmployeeDto[]>('/employees', { params: { pageNumber, pageSize, onlyActive } }),
  getById: (id: string): Promise<AxiosResponse<EmployeeDto>> =>
    apiClient.get<EmployeeDto>(`/employees/${id}`),
  create: (data: CreateEmployeeDto): Promise<AxiosResponse<EmployeeDto>> =>
    apiClient.post<EmployeeDto>('/employees', data),
  update: (id: string, data: UpdateEmployeeDto): Promise<AxiosResponse<EmployeeDto>> =>
    apiClient.put<EmployeeDto>(`/employees/${id}`, data),
  delete: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/employees/${id}`),
};

// ======== POSITIONS ========
export const positionApi = {
  getAll: (pageNumber = 1, pageSize = 50): Promise<AxiosResponse<PaginatedResult<PositionDto>>> => 
    apiClient.get<PaginatedResult<PositionDto>>('/positions', { params: { pageNumber, pageSize } }),
  getById: (id: string): Promise<AxiosResponse<PositionDto>> => 
    apiClient.get<PositionDto>(`/positions/${id}`),
  create: (data: CreatePositionDto): Promise<AxiosResponse<PositionDto>> => 
    apiClient.post<PositionDto>('/positions', data),
  update: (id: string, data: UpdatePositionDto): Promise<AxiosResponse<PositionDto>> => 
    apiClient.put<PositionDto>(`/positions/${id}`, data),
  delete: (id: string): Promise<AxiosResponse<void>> => 
    apiClient.delete<void>(`/positions/${id}`),
};

// ======================== Scheduling API ========================

export const workAreaApi = {
  getAll: (branchId: string): Promise<AxiosResponse<WorkAreaDto[]>> => 
    apiClient.get<WorkAreaDto[]>('/scheduling/work-areas', { params: { branchId } }),
  getById: (id: string): Promise<AxiosResponse<WorkAreaDto>> => 
    apiClient.get<WorkAreaDto>(`/scheduling/work-areas/${id}`),
  create: (data: CreateWorkAreaDto): Promise<AxiosResponse<WorkAreaDto>> => 
    apiClient.post<WorkAreaDto>('/scheduling/work-areas', data),
  update: (id: string, data: UpdateWorkAreaDto): Promise<AxiosResponse<WorkAreaDto>> => 
    apiClient.put<WorkAreaDto>(`/scheduling/work-areas/${id}`, data),
  delete: (id: string): Promise<AxiosResponse<void>> => 
    apiClient.delete<void>(`/scheduling/work-areas/${id}`),
};

export const workRoleApi = {
  getAll: (workAreaId?: string): Promise<AxiosResponse<WorkRoleDto[]>> => 
    apiClient.get<WorkRoleDto[]>('/scheduling/work-roles', { params: { workAreaId } }),
  getById: (id: string): Promise<AxiosResponse<WorkRoleDto>> => 
    apiClient.get<WorkRoleDto>(`/scheduling/work-roles/${id}`),
  create: (data: CreateWorkRoleDto): Promise<AxiosResponse<WorkRoleDto>> => 
    apiClient.post<WorkRoleDto>('/scheduling/work-roles', data),
  update: (id: string, data: UpdateWorkRoleDto): Promise<AxiosResponse<WorkRoleDto>> => 
    apiClient.put<WorkRoleDto>(`/scheduling/work-roles/${id}`, data),
  delete: (id: string): Promise<AxiosResponse<void>> => 
    apiClient.delete<void>(`/scheduling/work-roles/${id}`),
};

export const employeeWorkRoleApi = {
  getByEmployee: (employeeId: string): Promise<AxiosResponse<EmployeeWorkRoleDto[]>> => 
    apiClient.get<EmployeeWorkRoleDto[]>(`/scheduling/employees/${employeeId}/work-roles`),
  assign: (data: AssignWorkRolesDto): Promise<AxiosResponse<EmployeeWorkRoleDto[]>> => 
    apiClient.post<EmployeeWorkRoleDto[]>(`/scheduling/employees/${data.employeeId}/work-roles`, data),
  remove: (employeeId: string, workRoleId: string): Promise<AxiosResponse<void>> => 
    apiClient.delete<void>(`/scheduling/employees/${employeeId}/work-roles/${workRoleId}`),
};

export const employeeAvailabilityApi = {
  getByEmployee: (employeeId: string, month?: number, year?: number): Promise<AxiosResponse<EmployeeAvailabilityDto[]>> => 
    apiClient.get<EmployeeAvailabilityDto[]>(`/scheduling/employees/${employeeId}/availability`, { params: { month, year } }),
  add: (data: CreateEmployeeAvailabilityDto): Promise<AxiosResponse<EmployeeAvailabilityDto>> => 
    apiClient.post<EmployeeAvailabilityDto>(`/scheduling/employees/${data.employeeId}/availability`, data),
  remove: (employeeId: string, id: string): Promise<AxiosResponse<void>> => 
    apiClient.delete<void>(`/scheduling/employees/${employeeId}/availability/${id}`),
};

// ======================== Payroll API ========================

export const payrollApi = {
  getConfiguration: (): Promise<AxiosResponse<PayrollConfigurationDto | null>> =>
    apiClient.get<PayrollConfigurationDto | null>('/payroll/configuration'),
  updateConfiguration: (data: CreatePayrollConfigurationDto): Promise<AxiosResponse<PayrollConfigurationDto>> =>
    apiClient.put<PayrollConfigurationDto>('/payroll/configuration', data),
  getSummary: (year: number, month: number): Promise<AxiosResponse<EmployeePayrollSummaryDto[]>> =>
    apiClient.get<EmployeePayrollSummaryDto[]>('/payroll/summary', { params: { year, month } }),
  getAdvances: (employeeId?: string, year?: number, month?: number): Promise<AxiosResponse<PayrollAdvanceDto[]>> =>
    apiClient.get<PayrollAdvanceDto[]>('/payroll/advances', { params: { employeeId, year, month } }),
  createAdvance: (data: CreatePayrollAdvanceDto): Promise<AxiosResponse<PayrollAdvanceDto>> =>
    apiClient.post<PayrollAdvanceDto>('/payroll/advances', data),
  deleteAdvance: (advanceId: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/payroll/advances/${advanceId}`),
  getConsumptions: (employeeId?: string, year?: number, month?: number): Promise<AxiosResponse<EmployeeConsumptionDto[]>> =>
    apiClient.get<EmployeeConsumptionDto[]>('/payroll/consumptions', { params: { employeeId, year, month } }),
  createConsumption: (data: CreateEmployeeConsumptionDto): Promise<AxiosResponse<EmployeeConsumptionDto>> =>
    apiClient.post<EmployeeConsumptionDto>('/payroll/consumptions', data),
  deleteConsumption: (consumptionId: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/payroll/consumptions/${consumptionId}`),
  getAdjustments: (employeeId?: string, year?: number, month?: number): Promise<AxiosResponse<PayrollAdjustmentDto[]>> =>
    apiClient.get<PayrollAdjustmentDto[]>('/payroll/adjustments', { params: { employeeId, year, month } }),
  createAdjustment: (data: CreatePayrollAdjustmentDto): Promise<AxiosResponse<PayrollAdjustmentDto>> =>
    apiClient.post<PayrollAdjustmentDto>('/payroll/adjustments', data),
  deleteAdjustment: (adjustmentId: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/payroll/adjustments/${adjustmentId}`),
  generateRoles: (year: number, month: number, employeeId?: string): Promise<AxiosResponse<GeneratePayrollRolesResultDto>> =>
    apiClient.post<GeneratePayrollRolesResultDto>('/payroll/roles/generate', null, { params: { year, month, employeeId } }),
  getRolesByEmployee: (employeeId: string): Promise<AxiosResponse<PayrollRoleDto[]>> =>
    apiClient.get<PayrollRoleDto[]>(`/payroll/roles/employee/${employeeId}`),
  getRoleDetail: (roleId: string): Promise<AxiosResponse<PayrollRoleFullDto>> =>
    apiClient.get<PayrollRoleFullDto>(`/payroll/roles/${roleId}`),
  updateRoleStatus: (roleId: string, data: UpdatePayrollRoleStatusDto): Promise<AxiosResponse<PayrollRoleDto>> =>
    apiClient.patch<PayrollRoleDto>(`/payroll/roles/${roleId}/status`, data),
};

export const scheduleConfigurationApi = {
  get: (branchId: string): Promise<AxiosResponse<ScheduleConfigurationDto>> => 
    apiClient.get<ScheduleConfigurationDto>('/scheduling/configuration', { params: { branchId } }),
  create: (data: CreateScheduleConfigurationDto): Promise<AxiosResponse<ScheduleConfigurationDto>> => 
    apiClient.post<ScheduleConfigurationDto>('/scheduling/configuration', data),
  update: (id: string, data: UpdateScheduleConfigurationDto): Promise<AxiosResponse<ScheduleConfigurationDto>> => 
    apiClient.put<ScheduleConfigurationDto>(`/scheduling/configuration/${id}`, data),
};

export const scheduleShiftApi = {
  getMonthly: (branchId: string, year: number, month: number): Promise<AxiosResponse<ShiftAssignmentDto[]>> =>
    apiClient.get<ShiftAssignmentDto[]>('/scheduling/shifts', { params: { branchId, year, month } }),
  getByDate: (branchId: string, date: string): Promise<AxiosResponse<ShiftAssignmentDto[]>> =>
    apiClient.get<ShiftAssignmentDto[]>('/scheduling/shifts/by-date', { params: { branchId, date } }),
  getByEmployee: (employeeId: string, year: number, month: number): Promise<AxiosResponse<ShiftAssignmentDto[]>> =>
    apiClient.get<ShiftAssignmentDto[]>(`/scheduling/employees/${employeeId}/shifts`, { params: { year, month } }),
  generate: (year: number, month: number, weeklyFreeDaysPattern?: number[]): Promise<AxiosResponse<ShiftGenerationResultDto>> =>
    apiClient.post<ShiftGenerationResultDto>('/scheduling/shifts/generate', { year, month, weeklyFreeDaysPattern }),
  generateWeekly: (
    year: number,
    month: number,
    rangeStartDate: string,
    rangeEndDate: string,
    weeklyFreeDaysPattern?: number[]
  ): Promise<AxiosResponse<ShiftGenerationResultDto>> =>
    apiClient.post<ShiftGenerationResultDto>('/scheduling/shifts/generate', {
      year,
      month,
      rangeStartDate,
      rangeEndDate,
      weeklyFreeDaysPattern
    }),
  getFreeEmployees: (branchId: string, date: string): Promise<AxiosResponse<EmployeeDto[]>> =>
    apiClient.get<EmployeeDto[]>('/scheduling/shifts/free-employees', { params: { branchId, date } }),
  create: (data: CreateShiftAssignmentDto): Promise<AxiosResponse<ShiftAssignmentDto>> =>
    apiClient.post<ShiftAssignmentDto>('/scheduling/shifts', data),
  getEligibleEmployees: (): Promise<AxiosResponse<EmployeeDto[]>> =>
    apiClient.get<EmployeeDto[]>('/scheduling/employees/eligible'),
  update: (id: string, data: UpdateShiftAssignmentDto): Promise<AxiosResponse<ShiftAssignmentDto>> =>
    apiClient.put<ShiftAssignmentDto>(`/scheduling/shifts/${id}`, data),
  delete: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/scheduling/shifts/${id}`),
};

export const shiftTemplateApi = {
  getAll: (branchId: string, dayOfWeek?: number): Promise<AxiosResponse<ShiftTemplateDto[]>> =>
    apiClient.get<ShiftTemplateDto[]>('/scheduling/shift-templates', { params: { branchId, dayOfWeek } }),
  create: (data: CreateShiftTemplateDto): Promise<AxiosResponse<ShiftTemplateDto>> =>
    apiClient.post<ShiftTemplateDto>('/scheduling/shift-templates', data),
  update: (id: string, data: UpdateShiftTemplateDto): Promise<AxiosResponse<ShiftTemplateDto>> =>
    apiClient.put<ShiftTemplateDto>(`/scheduling/shift-templates/${id}`, data),
  delete: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/scheduling/shift-templates/${id}`),
};

export const tableServiceApi = {
  getTables: (branchId: string): Promise<AxiosResponse<RestaurantTableDto[]>> =>
    apiClient.get<RestaurantTableDto[]>('/tableservice/tables', { params: { branchId } }),
  createTable: (data: CreateRestaurantTableDto): Promise<AxiosResponse<RestaurantTableDto>> =>
    apiClient.post<RestaurantTableDto>('/tableservice/tables', data),
  updateTable: (id: string, data: UpdateRestaurantTableDto): Promise<AxiosResponse<RestaurantTableDto>> =>
    apiClient.put<RestaurantTableDto>(`/tableservice/tables/${id}`, data),
  deleteTable: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/tableservice/tables/${id}`),
  regenerateTableToken: (id: string): Promise<AxiosResponse<RestaurantTableDto>> =>
    apiClient.post<RestaurantTableDto>(`/tableservice/tables/${id}/regenerate-token`),
  getRequests: (status?: TableServiceRequestStatus): Promise<AxiosResponse<TableServiceRequestDto[]>> =>
    apiClient.get<TableServiceRequestDto[]>('/tableservice/requests', { params: { status } }),
  takeRequest: (id: string): Promise<AxiosResponse<TableServiceRequestDto>> =>
    apiClient.post<TableServiceRequestDto>(`/tableservice/requests/${id}/take`),
  setRequestStatus: (id: string, data: SetTableServiceRequestStatusDto): Promise<AxiosResponse<TableServiceRequestDto>> =>
    apiClient.post<TableServiceRequestDto>(`/tableservice/requests/${id}/status`, data),
  getPublicTable: (token: string): Promise<AxiosResponse<PublicTableInfoDto>> =>
    apiClient.get<PublicTableInfoDto>(`/tableservice/public/table/${token}`),
  getPublicActiveRequest: (token: string): Promise<AxiosResponse<PublicActiveTableRequestDto | null>> =>
    apiClient.get<PublicActiveTableRequestDto | null>(`/tableservice/public/table/${token}/active-request`),
  createPublicRequest: (data: PublicCreateTableServiceRequestDto): Promise<AxiosResponse<TableServiceRequestDto>> =>
    apiClient.post<TableServiceRequestDto>('/tableservice/public/request', data),
  getPublicRequestStatus: (id: string): Promise<AxiosResponse<PublicRequestStatusDto>> =>
    apiClient.get<PublicRequestStatusDto>(`/tableservice/public/request/${id}`),
};

// Export the external specialDateTemplateApi service
export { specialDateTemplateApi } from './specialDateTemplateApi';

// ======================== Inventario API ========================

export const inventoryApi = {
  // Unidades de medida
  getUnits: (): Promise<AxiosResponse<MeasurementUnitDto[]>> =>
    apiClient.get<MeasurementUnitDto[]>('/inventory/unidades'),
  createUnit: (data: CreateMeasurementUnitDto): Promise<AxiosResponse<MeasurementUnitDto>> =>
    apiClient.post<MeasurementUnitDto>('/inventory/unidades', data),
  updateUnit: (id: string, data: CreateMeasurementUnitDto): Promise<AxiosResponse<MeasurementUnitDto>> =>
    apiClient.put<MeasurementUnitDto>(`/inventory/unidades/${id}`, data),
  deleteUnit: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/inventory/unidades/${id}`),

  // Conversiones
  getConversions: (): Promise<AxiosResponse<UnitConversionDto[]>> =>
    apiClient.get<UnitConversionDto[]>('/inventory/conversiones'),
  createConversion: (data: CreateUnitConversionDto): Promise<AxiosResponse<UnitConversionDto>> =>
    apiClient.post<UnitConversionDto>('/inventory/conversiones', data),
  deleteConversion: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/inventory/conversiones/${id}`),

  // Categorías
  getCategories: (): Promise<AxiosResponse<InventoryCategoryDto[]>> =>
    apiClient.get<InventoryCategoryDto[]>('/inventory/categorias'),
  createCategory: (data: CreateInventoryCategoryDto): Promise<AxiosResponse<InventoryCategoryDto>> =>
    apiClient.post<InventoryCategoryDto>('/inventory/categorias', data),
  updateCategory: (id: string, data: CreateInventoryCategoryDto): Promise<AxiosResponse<InventoryCategoryDto>> =>
    apiClient.put<InventoryCategoryDto>(`/inventory/categorias/${id}`, data),
  deleteCategory: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/inventory/categorias/${id}`),

  // Artículos
  getArticles: (params?: { activeOnly?: boolean; type?: ArticleType; categoryId?: string }): Promise<AxiosResponse<InventoryArticleDto[]>> =>
    apiClient.get<InventoryArticleDto[]>('/inventory/articulos', { params }),
  getArticle: (id: string): Promise<AxiosResponse<InventoryArticleDto>> =>
    apiClient.get<InventoryArticleDto>(`/inventory/articulos/${id}`),
  createArticle: (data: CreateInventoryArticleDto): Promise<AxiosResponse<InventoryArticleDto>> =>
    apiClient.post<InventoryArticleDto>('/inventory/articulos', data),
  updateArticle: (id: string, data: UpdateInventoryArticleDto): Promise<AxiosResponse<InventoryArticleDto>> =>
    apiClient.put<InventoryArticleDto>(`/inventory/articulos/${id}`, data),
  deleteArticle: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/inventory/articulos/${id}`),

  // Bodegas
  getWarehouses: (activeOnly?: boolean): Promise<AxiosResponse<WarehouseDto[]>> =>
    apiClient.get<WarehouseDto[]>('/inventory/bodegas', { params: { activeOnly } }),
  createWarehouse: (data: CreateWarehouseDto): Promise<AxiosResponse<WarehouseDto>> =>
    apiClient.post<WarehouseDto>('/inventory/bodegas', data),
  updateWarehouse: (id: string, data: WarehouseDto): Promise<AxiosResponse<WarehouseDto>> =>
    apiClient.put<WarehouseDto>(`/inventory/bodegas/${id}`, data),
  deleteWarehouse: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/inventory/bodegas/${id}`),

  // Stock
  getStock: (params?: { warehouseId?: string; categoryId?: string; lowStockOnly?: boolean }): Promise<AxiosResponse<WarehouseStockDto[]>> =>
    apiClient.get<WarehouseStockDto[]>('/inventory/stock', { params }),
  getAlerts: (): Promise<AxiosResponse<StockAlertDto[]>> =>
    apiClient.get<StockAlertDto[]>('/inventory/alertas'),

  // Movimientos
  getMovements: (params?: {
    articleId?: string;
    warehouseId?: string;
    type?: MovementType;
    from?: string;
    to?: string;
    pageSize?: number;
  }): Promise<AxiosResponse<StockMovementDto[]>> =>
    apiClient.get<StockMovementDto[]>('/inventory/movimientos', { params }),
  registerMovement: (data: RegisterMovementDto): Promise<AxiosResponse<StockMovementDto>> =>
    apiClient.post<StockMovementDto>('/inventory/movimientos', data),
  registerInitialInventory: (data: RegisterInitialInventoryDto): Promise<AxiosResponse<StockMovementDto[]>> =>
    apiClient.post<StockMovementDto[]>('/inventory/movimientos/inventario-inicial', data),
};

// ======================== Menú API ========================

export const menuApi = {
  // Categorías
  getCategories: (): Promise<AxiosResponse<MenuCategoryDto[]>> =>
    apiClient.get<MenuCategoryDto[]>('/menu/categorias'),
  createCategory: (data: CreateMenuCategoryDto): Promise<AxiosResponse<MenuCategoryDto>> =>
    apiClient.post<MenuCategoryDto>('/menu/categorias', data),
  updateCategory: (id: string, data: MenuCategoryDto): Promise<AxiosResponse<MenuCategoryDto>> =>
    apiClient.put<MenuCategoryDto>(`/menu/categorias/${id}`, data),
  deleteCategory: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/menu/categorias/${id}`),

  // Items
  getItems: (params?: { categoryId?: string; activeOnly?: boolean; availableOnly?: boolean }): Promise<AxiosResponse<MenuItemDto[]>> =>
    apiClient.get<MenuItemDto[]>('/menu/items', { params }),
  getItem: (id: string): Promise<AxiosResponse<MenuItemDetailDto>> =>
    apiClient.get<MenuItemDetailDto>(`/menu/items/${id}`),
  createItem: (data: CreateMenuItemDto): Promise<AxiosResponse<MenuItemDto>> =>
    apiClient.post<MenuItemDto>('/menu/items', data),
  updateItem: (id: string, data: UpdateMenuItemDto): Promise<AxiosResponse<MenuItemDto>> =>
    apiClient.put<MenuItemDto>(`/menu/items/${id}`, data),
  deleteItem: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/menu/items/${id}`),

  // Receta
  upsertRecipe: (itemId: string, ingredientes: UpsertRecipeIngredientDto[]): Promise<AxiosResponse<RecipeIngredientDto[]>> =>
    apiClient.put<RecipeIngredientDto[]>(`/menu/items/${itemId}/receta`, ingredientes),
  deleteIngredient: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/menu/receta/${id}`),

  // Descuento por venta
  deductStock: (data: DeductStockFromSaleDto): Promise<AxiosResponse<void>> =>
    apiClient.post<void>('/menu/venta/descontar-stock', data),
};

// ======================== POS API ========================

export const posApi = {
  // Estaciones
  getStations: (): Promise<AxiosResponse<WorkStationDto[]>> =>
    apiClient.get<WorkStationDto[]>('/pos/estaciones'),
  createStation: (data: CreateWorkStationDto): Promise<AxiosResponse<WorkStationDto>> =>
    apiClient.post<WorkStationDto>('/pos/estaciones', data),
  updateStation: (id: string, data: UpdateWorkStationDto): Promise<AxiosResponse<WorkStationDto>> =>
    apiClient.put<WorkStationDto>(`/pos/estaciones/${id}`, data),
  deleteStation: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/pos/estaciones/${id}`),

  // Posición de mesas
  updateTablePosition: (id: string, posX: number, posY: number): Promise<AxiosResponse<void>> =>
    apiClient.patch<void>(`/pos/tables/${id}/position`, { posX, posY }),

  // Órdenes
  getOrders: (params?: { status?: string; type?: string; tableId?: string; activeOnly?: boolean }): Promise<AxiosResponse<OrderDto[]>> =>
    apiClient.get<OrderDto[]>('/pos/ordenes', { params }),
  getOrden: (id: string): Promise<AxiosResponse<OrderDto>> =>
    apiClient.get<OrderDto>(`/pos/ordenes/${id}`),
  createOrder: (data: CreateOrderDto): Promise<AxiosResponse<OrderDto>> =>
    apiClient.post<OrderDto>('/pos/ordenes', data),
  updateItems: (id: string, items: CreateOrderItemDto[]): Promise<AxiosResponse<OrderDto>> =>
    apiClient.put<OrderDto>(`/pos/ordenes/${id}/items`, { items }),
  confirmOrder: (id: string): Promise<AxiosResponse<OrderDto>> =>
    apiClient.post<OrderDto>(`/pos/ordenes/${id}/confirmar`),
  deliverOrder: (id: string): Promise<AxiosResponse<OrderDto>> =>
    apiClient.post<OrderDto>(`/pos/ordenes/${id}/entregar`),
  cancelOrder: (id: string): Promise<AxiosResponse<OrderDto>> =>
    apiClient.post<OrderDto>(`/pos/ordenes/${id}/cancelar`),
  setItemStatus: (id: string, estado: string): Promise<AxiosResponse<OrderItemDto>> =>
    apiClient.patch<OrderItemDto>(`/pos/orden-items/${id}/estado`, { estado }),

  // Monitor de estación
  getStationItems: (estacionId: string): Promise<AxiosResponse<StationItemDto[]>> =>
    apiClient.get<StationItemDto[]>(`/pos/estaciones/${estacionId}/items`),
};

export const purchasesApi = {
  // Proveedores
  getSuppliers: (activeOnly?: boolean): Promise<AxiosResponse<SupplierDto[]>> =>
    apiClient.get<SupplierDto[]>('/purchases/proveedores', { params: activeOnly !== undefined ? { activeOnly } : undefined }),
  createSupplier: (data: CreateSupplierDto): Promise<AxiosResponse<SupplierDto>> =>
    apiClient.post<SupplierDto>('/purchases/proveedores', data),
  updateSupplier: (id: string, data: UpdateSupplierDto): Promise<AxiosResponse<SupplierDto>> =>
    apiClient.put<SupplierDto>(`/purchases/proveedores/${id}`, data),
  deleteSupplier: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/purchases/proveedores/${id}`),

  // Órdenes de compra
  getOrders: (params?: { status?: string; supplierId?: string }): Promise<AxiosResponse<PurchaseOrderDto[]>> =>
    apiClient.get<PurchaseOrderDto[]>('/purchases/ordenes', { params }),
  getOrden: (id: string): Promise<AxiosResponse<PurchaseOrderDto>> =>
    apiClient.get<PurchaseOrderDto>(`/purchases/ordenes/${id}`),
  createOrden: (data: CreatePurchaseOrderDto): Promise<AxiosResponse<PurchaseOrderDto>> =>
    apiClient.post<PurchaseOrderDto>('/purchases/ordenes', data),
  updateOrden: (id: string, data: UpdatePurchaseOrderDto): Promise<AxiosResponse<PurchaseOrderDto>> =>
    apiClient.put<PurchaseOrderDto>(`/purchases/ordenes/${id}`, data),
  sendOrder: (id: string): Promise<AxiosResponse<PurchaseOrderDto>> =>
    apiClient.post<PurchaseOrderDto>(`/purchases/ordenes/${id}/enviar`),
  receiveOrder: (id: string, data: ReceivePurchaseOrderDto): Promise<AxiosResponse<PurchaseOrderDto>> =>
    apiClient.post<PurchaseOrderDto>(`/purchases/ordenes/${id}/recibir`, data),
  cancelOrder: (id: string): Promise<AxiosResponse<PurchaseOrderDto>> =>
    apiClient.post<PurchaseOrderDto>(`/purchases/ordenes/${id}/cancelar`),
  deleteOrden: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/purchases/ordenes/${id}`),
};

import type { CustomerDto, CreateCustomerDto, UpdateCustomerDto, CashSessionDto, OpenCashSessionDto, CloseCashSessionDto, OrderPaymentDto, PayOrderDto } from '../types';

export const customersApi = {
  getAll: (params?: { activeOnly?: boolean; search?: string }): Promise<AxiosResponse<CustomerDto[]>> =>
    apiClient.get<CustomerDto[]>('/customers', { params }),
  create: (dto: CreateCustomerDto): Promise<AxiosResponse<CustomerDto>> =>
    apiClient.post<CustomerDto>('/customers', dto),
  update: (id: string, dto: UpdateCustomerDto): Promise<AxiosResponse<CustomerDto>> =>
    apiClient.put<CustomerDto>(`/customers/${id}`, dto),
  delete: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/customers/${id}`),
};

export const cashApi = {
  getActiveSession: (): Promise<AxiosResponse<CashSessionDto>> =>
    apiClient.get<CashSessionDto>('/cash/sesion-activa'),
  getSessions: (params?: { from?: string; to?: string; pageSize?: number }): Promise<AxiosResponse<CashSessionDto[]>> =>
    apiClient.get<CashSessionDto[]>('/cash/sesiones', { params }),
  getSession: (id: string): Promise<AxiosResponse<CashSessionDto>> =>
    apiClient.get<CashSessionDto>(`/cash/sesiones/${id}`),
  openSession: (dto: OpenCashSessionDto): Promise<AxiosResponse<CashSessionDto>> =>
    apiClient.post<CashSessionDto>('/cash/abrir', dto),
  closeSession: (id: string, dto: CloseCashSessionDto): Promise<AxiosResponse<CashSessionDto>> =>
    apiClient.post<CashSessionDto>(`/cash/sesiones/${id}/cerrar`, dto),
  payOrder: (orderId: string, dto: PayOrderDto): Promise<AxiosResponse<OrderPaymentDto>> =>
    apiClient.post<OrderPaymentDto>(`/cash/cobrar/${orderId}`, dto),
};

export default apiClient;
