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

export const ContractType = {
  FullTime: 1,
  PartTime: 2,
  Temporary: 3,
  Seasonal: 4,
} as const;

export type ContractTypeValue = (typeof ContractType)[keyof typeof ContractType];

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
  contractType: ContractTypeValue;
  weeklyMinHours: number;
  weeklyMaxHours: number;
  freeDaysPerMonth: number;
}

export interface CreateEmployeeDto {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  identificationNumber: string;
  positionId: string;
  hireDate: string;
  contractType: ContractTypeValue;
  weeklyMinHours: number;
  weeklyMaxHours: number;
  freeDaysPerMonth?: number;
}

export interface UpdateEmployeeDto {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  identificationNumber: string;
  positionId: string;
  terminationDate?: string;
  isActive: boolean;
  contractType: ContractTypeValue;
  weeklyMinHours: number;
  weeklyMaxHours: number;
  freeDaysPerMonth: number;
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

export interface BranchDto {
  id: string;
  name: string;
  code: string;
  address: string;
  phone: string;
  email: string;
  isActive: boolean;
  latitude?: number;
  longitude?: number;
}

export interface UpdateBranchDto {
  name: string;
  code: string;
  address: string;
  phone: string;
  email: string;
  isActive: boolean;
  latitude?: number;
  longitude?: number;
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

export interface WorkAreaDto {
  id: string;
  name: string;
  description?: string;
  color: string;
  displayOrder: number;
  branchId: string;
  workRoles?: WorkRoleDto[];
}

export interface CreateWorkAreaDto {
  name: string;
  description?: string;
  color: string;
  displayOrder: number;
}

export interface UpdateWorkAreaDto {
  name: string;
  description?: string;
  color: string;
  displayOrder: number;
}

export interface WorkRoleDto {
  id: string;
  name: string;
  description?: string;
  workAreaId: string;
  workAreaName?: string;
  freeDaysPerMonth: number;
  dailyHoursTarget: number;
}

export interface CreateWorkRoleDto {
  name: string;
  description?: string;
  workAreaId: string;
  freeDaysPerMonth: number;
  dailyHoursTarget: number;
}

export interface UpdateWorkRoleDto {
  name: string;
  description?: string;
  workAreaId: string;
  freeDaysPerMonth: number;
  dailyHoursTarget: number;
}

export interface EmployeeWorkRoleDto {
  id: string;
  employeeId: string;
  employeeName?: string;
  workRoleId: string;
  workRoleName?: string;
  isPrimary: boolean;
  priority: number;
}

export interface AssignWorkRolesDto {
  employeeId: string;
  workRoleIds: string[];
}

export interface EmployeeAvailabilityDto {
  id: string;
  employeeId: string;
  unavailableDate: string; // ISO date string
  reason?: string;
}

export interface CreateEmployeeAvailabilityDto {
  employeeId: string;
  unavailableDate: string; // ISO date string
  reason?: string;
}

export interface ScheduleConfigurationDto {
  id: string;
  branchId: string;
  hoursPerDay: number;
  freeDayColor: string;
}

export interface ShiftAssignmentDto {
  id: string;
  employeeId: string;
  employeeName: string;
  date: string;
  startTime: string;
  endTime: string;
  breakDuration?: string;
  lunchDuration?: string;
  workAreaId: string;
  workAreaName: string;
  workAreaColor: string;
  workRoleId: string;
  workRoleName: string;
  workedHours: number;
  notes?: string;
  isApproved: boolean;
  approvedBy?: string;
  approvedAt?: string;
}

export interface CreateShiftAssignmentDto {
  employeeId: string;
  date: string;
  startTime: string;
  endTime: string;
  breakDuration?: string;
  lunchDuration?: string;
  workAreaId: string;
  workRoleId: string;
  notes?: string;
}

export interface UpdateShiftAssignmentDto {
  employeeId: string;
  date: string;
  startTime: string;
  endTime: string;
  breakDuration?: string;
  lunchDuration?: string;
  notes?: string;
}

export interface ShiftTemplateDto {
  id: string;
  branchId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  breakDuration?: string;
  lunchDuration?: string;
  workAreaId: string;
  workAreaName: string;
  workRoleId: string;
  workRoleName: string;
  requiredCount: number;
  notes?: string;
}

export interface CreateShiftTemplateDto {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  breakDuration?: string;
  lunchDuration?: string;
  workAreaId: string;
  workRoleId: string;
  requiredCount: number;
  notes?: string;
}
export interface ShiftGenerationResultDto {
  assignments: ShiftAssignmentDto[];
  warnings: ShiftGenerationWarningDto[];
  totalShiftsGenerated: number;
  totalShiftsNotCovered: number;
}

export interface ShiftGenerationWarningDto {
  date: string;
  dayOfWeek: number;
  workAreaName: string;
  workRoleName: string;
  requiredCount: number;
  assignedCount: number;
  reason: string;
}
export interface UpdateShiftTemplateDto {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  breakDuration?: string;
  lunchDuration?: string;
  requiredCount: number;
  notes?: string;
}

// SpecialDateTemplate DTOs
export interface SpecialDateTemplateDto {
  id: string;
  specialDateId: string;
  startTime: string;
  endTime: string;
  breakDuration?: string;
  lunchDuration?: string;
  workAreaId: string;
  workAreaName: string;
  workRoleId: string;
  workRoleName: string;
  requiredCount: number;
  notes?: string;
}

export interface CreateSpecialDateTemplateDto {
  specialDateId: string;
  startTime: string;
  endTime: string;
  breakDuration?: string;
  lunchDuration?: string;
  workAreaId: string;
  workRoleId: string;
  requiredCount: number;
  notes?: string;
}

export interface UpdateSpecialDateTemplateDto {
  startTime: string;
  endTime: string;
  breakDuration?: string;
  lunchDuration?: string;
  requiredCount: number;
  notes?: string;
}

export interface CreateScheduleConfigurationDto {
  branchId: string;
  hoursPerDay?: number;
  freeDayColor: string;
}

export interface UpdateScheduleConfigurationDto {
  hoursPerDay: number;
  freeDayColor: string;
}

export interface SpecialDateDto {
  id: string;
  branchId: string;
  date: Date | string;
  name: string;
  description?: string;
}

export interface CreateSpecialDateDto {
  date: Date | string;
  name: string;
  description?: string;
}

export interface UpdateSpecialDateDto {
  date: Date | string;
  name: string;
  description?: string;
}
