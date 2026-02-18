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
  ShiftGenerationResultDto
} from '../types';
import { getDetailedError } from '../utils/errorHandler';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5186/api';

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
export const authService = {
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
export const userService = {
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
export const roleService = {
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
export const permissionService = {
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
export const employeeService = {
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
export const positionService = {
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
  generate: (year: number, month: number): Promise<AxiosResponse<ShiftGenerationResultDto>> =>
    apiClient.post<ShiftGenerationResultDto>('/scheduling/shifts/generate', { year, month }),
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

// Export the external specialDateTemplateApi service
export { specialDateTemplateApi } from './specialDateTemplateApi';

export default apiClient;
