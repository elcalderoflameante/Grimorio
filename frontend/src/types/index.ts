// DTOs del backend
export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  roles: string[];
  roleDetails: UserRoleDto[];
}

export interface UserRoleDto {
  roleId: string;
  roleName: string;
}

export interface CreateUserDto {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
}

export interface UpdateUserDto {
  firstName: string;
  lastName: string;
  isActive: boolean;
}

export interface RoleDto {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  permissions: string[];
  permissionDetails: RolePermissionDto[];
}

export interface RolePermissionDto {
  permissionId: string;
  permissionCode: string;
  permissionDescription: string;
}

export interface CreateRoleDto {
  name: string;
  description: string;
}

export interface UpdateRoleDto {
  name: string;
  description: string;
  isActive: boolean;
}

export interface PermissionDto {
  id: string;
  code: string;
  description: string;
  isActive: boolean;
}

export interface CreatePermissionDto {
  code: string;
  description: string;
}

export interface UpdatePermissionDto {
  description: string;
  isActive: boolean;
}

export interface EmployeeDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  identificationNumber: string;
  positionId: string;
  positionName: string;
  hireDate: string;
  terminationDate?: string;
  isActive: boolean;
}

export interface CreateEmployeeDto {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  identificationNumber: string;
  positionId: string;
  hireDate: string;
}

export interface UpdateEmployeeDto {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  positionId: string;
  terminationDate?: string;
  isActive: boolean;
}

export interface PositionDto {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
}

export interface CreatePositionDto {
  name: string;
  description: string;
}

export interface UpdatePositionDto {
  name: string;
  description: string;
  isActive: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  permissions: string[];
}

export interface PaginatedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// ======================== Scheduling DTOs ========================

export interface ScheduleConfigurationDto {
  id: string;
  branchId: string;
  minHoursPerMonth: number;
  maxHoursPerMonth: number;
  hoursMondayThursday: number;
  hoursFridaySaturday: number;
  hoursSunday: number;
  freeDaysParrillero: number;
  freeDaysOtherRoles: number;
  minStaffCocina: number;
  minStaffCaja: number;
  minStaffMesas: number;
  minStaffBar: number;
}

export interface CreateScheduleConfigurationDto {
  minHoursPerMonth: number;
  maxHoursPerMonth: number;
  hoursMondayThursday: number;
  hoursFridaySaturday: number;
  hoursSunday: number;
  freeDaysParrillero: number;
  freeDaysOtherRoles: number;
  minStaffCocina: number;
  minStaffCaja: number;
  minStaffMesas: number;
  minStaffBar: number;
}

export interface UpdateScheduleConfigurationDto {
  minHoursPerMonth: number;
  maxHoursPerMonth: number;
  hoursMondayThursday: number;
  hoursFridaySaturday: number;
  hoursSunday: number;
  freeDaysParrillero: number;
  freeDaysOtherRoles: number;
  minStaffCocina: number;
  minStaffCaja: number;
  minStaffMesas: number;
  minStaffBar: number;
}
