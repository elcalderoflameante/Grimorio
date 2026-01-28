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
  AuthResponse,
  PaginatedResult,
  ScheduleConfigurationDto,
  CreateScheduleConfigurationDto,
  UpdateScheduleConfigurationDto
} from '../types';

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
    return Promise.reject(error);
  }
);

// ======== AUTH ========
export const authService = {
  login: (email: string, password: string): Promise<AxiosResponse<AuthResponse>> =>
    apiClient.post<AuthResponse>('/auth/login', { email, password }),
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
  getAll: (pageNumber = 1, pageSize = 10): Promise<AxiosResponse<EmployeeDto[]>> =>
    apiClient.get<EmployeeDto[]>('/employees', { params: { pageNumber, pageSize } }),
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

export const scheduleConfigurationApi = {
  get: (branchId: string): Promise<AxiosResponse<ScheduleConfigurationDto>> => 
    apiClient.get<ScheduleConfigurationDto>('/scheduling/configuration', { params: { branchId } }),
  create: (data: CreateScheduleConfigurationDto): Promise<AxiosResponse<ScheduleConfigurationDto>> => 
    apiClient.post<ScheduleConfigurationDto>('/scheduling/configuration', data),
  update: (id: string, data: UpdateScheduleConfigurationDto): Promise<AxiosResponse<ScheduleConfigurationDto>> => 
    apiClient.put<ScheduleConfigurationDto>(`/scheduling/configuration/${id}`, data),
};

export default apiClient;
