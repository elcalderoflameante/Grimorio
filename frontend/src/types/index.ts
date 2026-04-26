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

export const PayrollAdjustmentType = {
  Income: 1,
  Deduction: 2,
} as const;

export type PayrollAdjustmentTypeValue = (typeof PayrollAdjustmentType)[keyof typeof PayrollAdjustmentType];

export const PayrollAdjustmentCategory = {
  Overtime50: 1,
  Overtime100: 2,
  Bonus: 3,
  OtherIncome: 4,
  OtherDeduction: 5,
} as const;

export type PayrollAdjustmentCategoryValue = (typeof PayrollAdjustmentCategory)[keyof typeof PayrollAdjustmentCategory];

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
  baseSalary: number;
  bankAccount: string;
  decimoThirdMonthly: boolean;
  decimoFourthMonthly: boolean;
  reserveFundMonthly: boolean;
  freeDaysPerMonth: number;
  photo?: string;
  dateOfBirth?: string;
  civilStatus: string;
  sex: string;
  nationality: string;
  emergencyContactPerson: string;
  emergencyContactRelationship: string;
  emergencyContactPhone: string;
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
  baseSalary: number;
  bankAccount: string;
  decimoThirdMonthly: boolean;
  decimoFourthMonthly: boolean;
  reserveFundMonthly: boolean;
  freeDaysPerMonth?: number;
  photo?: string;
  dateOfBirth?: string;
  civilStatus: string;
  sex: string;
  nationality: string;
  emergencyContactPerson: string;
  emergencyContactRelationship: string;
  emergencyContactPhone: string;
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
  baseSalary: number;
  bankAccount: string;
  decimoThirdMonthly: boolean;
  decimoFourthMonthly: boolean;
  reserveFundMonthly: boolean;
  freeDaysPerMonth: number;
  photo?: string;
  dateOfBirth?: string;
  civilStatus: string;
  sex: string;
  nationality: string;
  emergencyContactPerson: string;
  emergencyContactRelationship: string;
  emergencyContactPhone: string;
}

export interface PayrollConfigurationDto {
  id: string;
  branchId: string;
  iessEmployeeRate: number;
  iessEmployerRate: number;
  incomeTaxRate: number;
  overtimeRate50: number;
  overtimeRate100: number;
  decimoThirdRate: number;
  decimoFourthRate: number;
  reserveFundRate: number;
  monthlyHours: number;
}

export interface CreatePayrollConfigurationDto {
  iessEmployeeRate: number;
  iessEmployerRate: number;
  incomeTaxRate: number;
  overtimeRate50: number;
  overtimeRate100: number;
  decimoThirdRate: number;
  decimoFourthRate: number;
  reserveFundRate: number;
  monthlyHours: number;
}

export interface PayrollAdvanceDto {
  id: string;
  employeeId: string;
  date: string;
  amount: number;
  method: string;
  notes?: string;
}

export interface CreatePayrollAdvanceDto {
  employeeId: string;
  date: string;
  amount: number;
  method: string;
  notes?: string;
}

export interface EmployeeConsumptionDto {
  id: string;
  employeeId: string;
  date: string;
  amount: number;
  notes?: string;
}

export interface CreateEmployeeConsumptionDto {
  employeeId: string;
  date: string;
  amount: number;
  notes?: string;
}

export interface PayrollAdjustmentDto {
  id: string;
  employeeId: string;
  date: string;
  type: PayrollAdjustmentTypeValue;
  category: PayrollAdjustmentCategoryValue;
  hours?: number | null;
  amount?: number | null;
  notes?: string;
}

export interface CreatePayrollAdjustmentDto {
  employeeId: string;
  date: string;
  type: PayrollAdjustmentTypeValue;
  category: PayrollAdjustmentCategoryValue;
  hours?: number | null;
  amount?: number | null;
  notes?: string;
}

export interface EmployeePayrollSummaryDto {
  employeeId: string;
  employeeName: string;
  positionName: string;
  bankAccount: string;
  baseSalary: number;
  iessEmployee: number;
  iessEmployer: number;
  incomeTax: number;
  decimoThird: number;
  decimoFourth: number;
  reserveFund: number;
  overtime50: number;
  overtime100: number;
  otherIncome: number;
  otherDeductions: number;
  advances: number;
  consumptions: number;
  totalIncome: number;
  totalDeductions: number;
  netPay: number;
}

export const PayrollRoleStatus = {
  Generated: 1,
  Authorized: 2,
  Paid: 3,
} as const;

export type PayrollRoleStatusValue = (typeof PayrollRoleStatus)[keyof typeof PayrollRoleStatus];

export interface PayrollRoleDto {
  id: string;
  employeeId: string;
  employeeName: string;
  year: number;
  month: number;
  status: PayrollRoleStatusValue;
  generatedAt: string;
  authorizedAt?: string;
  paidAt?: string;
  totalIncome: number;
  totalDeductions: number;
  netPay: number;
}

export const PayrollRoleDetailType = {
  Income: 1,
  Deduction: 2,
} as const;

export type PayrollRoleDetailTypeValue = (typeof PayrollRoleDetailType)[keyof typeof PayrollRoleDetailType];

export interface PayrollRoleDetailDto {
  id: string;
  payrollRoleHeaderId: string;
  type: PayrollRoleDetailTypeValue;
  concept: string;
  amount: number;
  sortOrder: number;
  notes?: string;
}

export interface PayrollRoleFullDto {
  header: PayrollRoleDto;
  details: PayrollRoleDetailDto[];
}

export interface GeneratePayrollRolesResultDto {
  year: number;
  month: number;
  generatedCount: number;
  updatedCount: number;
}

export interface UpdatePayrollRoleStatusDto {
  status: PayrollRoleStatusValue;
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
  identificationNumber: string;
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
  identificationNumber: string;
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
  id: string;
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
  branchId: string;
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
  id: string;
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

// ======================== POS - Table Service ========================

export type TableServiceRequestType = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 99;
export type TableServiceRequestStatus = 1 | 2 | 3 | 4 | 5;

export interface RestaurantTableDto {
  id: string;
  branchId: string;
  code: string;
  name: string;
  area?: string;
  capacity: number;
  publicToken: string;
  publicUrl: string;
  isActive: boolean;
  posX: number;
  posY: number;
  currentStatus: 'Free' | 'Occupied';
  currentOrderId?: string;
}

export interface CreateRestaurantTableDto {
  code: string;
  name: string;
  area?: string;
  capacity: number;
}

export interface UpdateRestaurantTableDto {
  id?: string;
  code: string;
  name: string;
  area?: string;
  capacity: number;
  isActive: boolean;
}

export interface PublicTableInfoDto {
  tableId: string;
  code: string;
  name: string;
  area?: string;
  isActive: boolean;
}

export interface TableServiceRequestDto {
  id: string;
  branchId: string;
  restaurantTableId: string;
  tableCode: string;
  tableName: string;
  tableArea?: string;
  type: TableServiceRequestType;
  customMessage?: string;
  status: TableServiceRequestStatus;
  requestedAt: string;
  takenAt?: string;
  completedAt?: string;
  takenByUserId?: string;
  takenByName?: string;
}

export interface PublicCreateTableServiceRequestDto {
  tableToken: string;
  type: TableServiceRequestType;
  customMessage?: string;
  clientFingerprint?: string;
}

export interface SetTableServiceRequestStatusDto {
  status: TableServiceRequestStatus;
}

export interface PublicRequestStatusDto {
  id: string;
  status: TableServiceRequestStatus;
}

export interface PublicActiveTableRequestDto {
  id: string;
  status: TableServiceRequestStatus;
}

// ======================== Inventory ========================

export type ArticleType = 'Ingredient' | 'FinishedProduct' | 'Supply';
export type MovementType =
  | 'InitialInventory'
  | 'PurchaseEntry'
  | 'ManualEntry'
  | 'ManualExit'
  | 'Waste'
  | 'Spoilage'
  | 'SaleDeduction'
  | 'TransferIn'
  | 'TransferOut'
  | 'PositiveAdjustment'
  | 'NegativeAdjustment';

export interface MeasurementUnitDto {
  id: string;
  name: string;
  symbol: string;
}

export interface CreateMeasurementUnitDto {
  name: string;
  symbol: string;
}

export interface UnitConversionDto {
  id: string;
  originUnitId: string;
  originUnitName: string;
  originUnitSymbol: string;
  destinationUnitId: string;
  destinationUnitName: string;
  destinationUnitSymbol: string;
  factor: number;
}

export interface CreateUnitConversionDto {
  originUnitId: string;
  destinationUnitId: string;
  factor: number;
}

export interface InventoryCategoryDto {
  id: string;
  name: string;
  description?: string;
  color?: string;
  totalArticles: number;
}

export interface CreateInventoryCategoryDto {
  name: string;
  description?: string;
  color?: string;
}

export interface InventoryArticleDto {
  id: string;
  name: string;
  description?: string;
  internalCode?: string;
  type: ArticleType;
  categoryId: string;
  categoryName: string;
  categoryColor?: string;
  baseUnitId: string;
  baseUnitName: string;
  baseUnitSymbol: string;
  minStock: number;
  totalStock: number;
  stockAlertActive: boolean;
  lowStock: boolean;
  isActive: boolean;
}

export interface CreateInventoryArticleDto {
  name: string;
  description?: string;
  internalCode?: string;
  type: ArticleType;
  categoryId: string;
  baseUnitId: string;
  minStock: number;
  stockAlertActive: boolean;
}

export interface UpdateInventoryArticleDto extends CreateInventoryArticleDto {
  isActive: boolean;
}

export interface WarehouseDto {
  id: string;
  name: string;
  description?: string;
  location?: string;
  isActive: boolean;
}

export interface CreateWarehouseDto {
  name: string;
  description?: string;
  location?: string;
}

export interface WarehouseStockDto {
  articleId: string;
  articleName: string;
  internalCode?: string;
  categoryName: string;
  categoryColor?: string;
  type: ArticleType;
  warehouseId: string;
  warehouseName: string;
  quantity: number;
  unitSymbol: string;
  minStock: number;
  lowStock: boolean;
  lastUpdatedAt: string;
}

export interface StockMovementDto {
  id: string;
  articleId: string;
  articleName: string;
  warehouseId: string;
  warehouseName: string;
  type: MovementType;
  quantity: number;
  unitSymbol: string;
  baseQuantity: number;
  baseUnitSymbol: string;
  reference?: string;
  notes?: string;
  movedAt: string;
}

export interface RegisterMovementDto {
  articleId: string;
  warehouseId: string;
  type: MovementType;
  quantity: number;
  unitId: string;
  reference?: string;
  notes?: string;
}

export interface InitialInventoryItemDto {
  articleId: string;
  warehouseId: string;
  quantity: number;
  unitId: string;
  notes?: string;
}

export interface RegisterInitialInventoryDto {
  items: InitialInventoryItemDto[];
}

export interface StockAlertDto {
  articleId: string;
  articleName: string;
  internalCode?: string;
  unitSymbol: string;
  currentStock: number;
  minStock: number;
}

// ======================== Menu ========================

export interface MenuCategoryDto {
  id: string;
  name: string;
  description?: string;
  color?: string;
  order: number;
  isActive: boolean;
  totalItems: number;
}

export interface CreateMenuCategoryDto {
  name: string;
  description?: string;
  color?: string;
  order: number;
}

export interface MenuItemDto {
  id: string;
  menuCategoryId: string;
  categoryName: string;
  categoryColor?: string;
  name: string;
  description?: string;
  internalCode?: string;
  price: number;
  isActive: boolean;
  availableForSale: boolean;
  totalIngredients: number;
  stationId?: string;
  stationName?: string;
}

export interface MenuItemDetailDto extends MenuItemDto {
  recipe: RecipeIngredientDto[];
}

export interface CreateMenuItemDto {
  menuCategoryId: string;
  name: string;
  description?: string;
  internalCode?: string;
  price: number;
  stationId?: string;
}

export interface UpdateMenuItemDto extends CreateMenuItemDto {
  isActive: boolean;
  availableForSale: boolean;
}

export interface RecipeIngredientDto {
  id: string;
  articleId: string;
  articleName: string;
  internalCode?: string;
  unitId: string;
  unitName: string;
  unitSymbol: string;
  quantity: number;
  notes?: string;
}

export interface UpsertRecipeIngredientDto {
  articleId: string;
  unitId: string;
  quantity: number;
  notes?: string;
}

export interface DeductStockFromSaleDto {
  warehouseId: string;
  items: SaleItemDto[];
}

export interface SaleItemDto {
  menuItemId: string;
  quantity: number;
}

// ── POS ───────────────────────────────────────────────────────────────────

export type StationType = 'Kitchen' | 'Bar' | 'Beverages' | 'HotKitchen' | 'Fries';
export type OrderType = 'DineIn' | 'Takeout' | 'Delivery';
export type OrderStatus = 'Draft' | 'Confirmed' | 'InPreparation' | 'Ready' | 'Delivered' | 'Cancelled';
export type OrderItemStatus = 'Pending' | 'InPreparation' | 'Ready' | 'Cancelled';

export interface WorkStationDto {
  id: string;
  name: string;
  type: StationType;
  isActive: boolean;
}

export interface CreateWorkStationDto {
  name: string;
  type: StationType;
}

export interface UpdateWorkStationDto {
  name: string;
  type: StationType;
  isActive: boolean;
}

export interface OrderItemDto {
  id: string;
  menuItemId: string;
  itemName: string;
  itemCode?: string;
  stationId?: string;
  stationName?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  notes?: string;
  status: OrderItemStatus;
}

export interface OrderDto {
  id: string;
  number: number;
  type: OrderType;
  status: OrderStatus;
  tableId?: string;
  tableCode?: string;
  tableName?: string;
  customerName?: string;
  deliveryAddress?: string;
  notes?: string;
  subtotal: number;
  total: number;
  createdAt: string;
  confirmedAt?: string;
  deliveredAt?: string;
  totalItems: number;
  items: OrderItemDto[];
}

export interface CreateOrderItemDto {
  menuItemId: string;
  quantity: number;
  notes?: string;
}

export interface CreateOrderDto {
  type: OrderType;
  tableId?: string;
  customerName?: string;
  deliveryAddress?: string;
  notes?: string;
  items: CreateOrderItemDto[];
}

export interface StationItemDto {
  orderItemId: string;
  orderId: string;
  orderNumber: number;
  orderType: OrderType;
  tableCode?: string;
  customerName?: string;
  itemName: string;
  quantity: number;
  notes?: string;
  status: OrderItemStatus;
  confirmedAt: string;
}

// ── Purchases ───────────────────────────────────────────────────────────────

export interface SupplierDto {
  id: string;
  name: string;
  taxId?: string;
  phone?: string;
  email?: string;
  address?: string;
  contactName?: string;
  isActive: boolean;
  totalOrders: number;
}

export interface CreateSupplierDto {
  name: string;
  taxId?: string;
  phone?: string;
  email?: string;
  address?: string;
  contactName?: string;
}

export interface UpdateSupplierDto extends CreateSupplierDto {
  isActive: boolean;
}

export type PurchaseOrderStatus = 'Draft' | 'Sent' | 'Received' | 'Cancelled';

export interface PurchaseOrderItemDto {
  id: string;
  articleId: string;
  articleName: string;
  internalCode?: string;
  unitId: string;
  unitSymbol: string;
  quantityOrdered: number;
  quantityReceived: number;
  unitPrice: number;
  totalPrice: number;
  notes?: string;
}

export interface PurchaseOrderDto {
  id: string;
  orderNumber: string;
  status: PurchaseOrderStatus;
  supplierId: string;
  supplierName: string;
  issuedAt: string;
  expectedAt?: string;
  receivedAt?: string;
  notes?: string;
  subtotal: number;
  total: number;
  destinationWarehouseId?: string;
  warehouseName?: string;
  totalItems: number;
  items: PurchaseOrderItemDto[];
}

export interface PurchaseOrderItemInputDto {
  articleId: string;
  unitId: string;
  quantityOrdered: number;
  unitPrice: number;
  notes?: string;
}

export interface CreatePurchaseOrderDto {
  supplierId: string;
  expectedAt?: string;
  notes?: string;
  destinationWarehouseId?: string;
  items: PurchaseOrderItemInputDto[];
}

export interface UpdatePurchaseOrderDto extends CreatePurchaseOrderDto {}

export interface ReceptionItemDto {
  purchaseOrderItemId: string;
  quantityReceived: number;
}

export interface ReceivePurchaseOrderDto {
  warehouseId: string;
  items: ReceptionItemDto[];
}


// ── Billing: Clientes ─────────────────────────────────────────────────────────

export interface CustomerDto {
  id: string;
  name: string;
  taxId?: string;
  taxIdType: 'Cedula' | 'Ruc' | 'Passport' | 'FinalConsumer';
  address?: string;
  phone?: string;
  email?: string;
  isActive: boolean;
}

export interface CreateCustomerDto {
  name: string;
  taxId?: string;
  taxIdType: string;
  address?: string;
  phone?: string;
  email?: string;
}

export interface UpdateCustomerDto extends CreateCustomerDto {
  isActive: boolean;
}

// ── Billing: Caja ─────────────────────────────────────────────────────────────

export interface CashSessionDto {
  id: string;
  openedByName: string;
  openingBalance: number;
  openedAt: string;
  closedAt?: string;
  closedByName?: string;
  actualCash?: number;
  closeNotes?: string;
  status: 'Open' | 'Closed';
  totalCash: number;
  totalCard: number;
  totalTransfer: number;
  totalQr: number;
  totalSales: number;
  totalOrders: number;
  expectedCash: number;
  cashDifference?: number;
}

export interface OpenCashSessionDto {
  openingBalance: number;
}

export interface CloseCashSessionDto {
  actualCash: number;
  notes?: string;
}

// ── Billing: Cobro ────────────────────────────────────────────────────────────

export interface PaymentLineDto {
  id: string;
  method: 'Cash' | 'Card' | 'Transfer' | 'QR';
  amountTendered: number;
  change: number;
  netAmount: number;
}

export interface OrderPaymentDto {
  id: string;
  orderId: string;
  orderNumber: number;
  customerId?: string;
  customerName?: string;
  customerTaxId?: string;
  documentType: 'NotaDeVenta' | 'Factura';
  orderAmount: number;
  paidAt: string;
  lines: PaymentLineDto[];
}

export interface AddPaymentLineDto {
  method: string;
  amountTendered: number;
}

export interface AddOrderPaymentDto {
  orderAmount: number;
  documentType: string;
  customerId?: string;
  cashSessionId?: string;
  lines: AddPaymentLineDto[];
}
