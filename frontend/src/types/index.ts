// DTOs del backend
export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  hasKdsPin: boolean;
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
  payrollYear: number;
  payrollMonth: number;
  amount: number;
  method: string;
  notes?: string;
}

export interface CreatePayrollAdvanceDto {
  employeeId: string;
  date: string;
  payrollYear?: number;
  payrollMonth?: number;
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
  paymentReceiptFileName?: string;
  paymentReceiptContentType?: string;
  hasPaymentReceipt: boolean;
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
  branchId: string;
}

export interface UpdateWorkAreaDto {
  id: string;
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
}

export interface CreateWorkRoleDto {
  name: string;
  description?: string;
  workAreaId: string;
}

export interface UpdateWorkRoleDto {
  id: string;
  name: string;
  description?: string;
  workAreaId: string;
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
  workAreaColor: string;
  workRoleId: string;
  workRoleName: string;
  requiredCount: number;
  notes?: string;
}

export interface ShiftTemplateImpactDto {
  shiftTemplateId: string;
  futureAssignmentsCount: number;
  firstAffectedDate?: string;
  lastAffectedDate?: string;
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
  workAreaColor: string;
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
  workAreaId: string;
  workRoleId: string;
  requiredCount: number;
  notes?: string;
}

export interface CreateScheduleConfigurationDto {
  branchId: string;
  hoursPerDay?: number;
  freeDayColor: string;
}

export interface UpdateScheduleConfigurationDto {
  id: string;
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
  area?: string;
  capacity: number;
  publicToken: string;
  publicUrl: string;
  isActive: boolean;
  posX: number;
  posY: number;
  currentStatus: 'Free' | 'Draft' | 'Occupied';
  currentOrderId?: string;
  currentOrderStartedAt?: string;
  currentOrderTotal?: number;
  pendingPaymentTotal?: number;
}

export interface CreateRestaurantTableDto {
  code: string;
  area?: string;
  capacity: number;
}

export interface UpdateRestaurantTableDto {
  id?: string;
  code: string;
  area?: string;
  capacity: number;
  isActive: boolean;
}

export interface PublicTableInfoDto {
  tableId: string;
  code: string;
  area?: string;
  isActive: boolean;
}

export interface TableServiceRequestDto {
  id: string;
  branchId: string;
  restaurantTableId: string;
  tableCode: string;
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
  reservedQuantity: number;
  availableQuantity: number;
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
  taxRateId?: string;
  taxRateName?: string;
  taxRatePercentage?: number;
  taxRateSriCode?: string;
  hasModifiers: boolean;
  modifierGroups: MenuItemModifierGroupDto[];
}

export interface MenuItemDetailDto extends MenuItemDto {
  recipe: RecipeIngredientDto[];
}

export interface MenuItemAvailabilityComponentDto {
  recipeIngredientId?: string;
  articleId: string;
  articleName: string;
  requiredQuantity: number;
  requiredUnitSymbol: string;
  stockQuantity: number;
  stockUnitSymbol: string;
  availableServings: number;
}

export interface MenuItemAvailabilityDto {
  menuItemId: string;
  isTracked: boolean;
  isAvailable: boolean;
  availableQuantity?: number | null;
  unitLabel: string;
  limitingArticleName?: string;
  components: MenuItemAvailabilityComponentDto[];
}

export interface MenuItemProfitabilityIngredientDto {
  recipeIngredientId: string;
  articleId: string;
  articleName: string;
  internalCode?: string;
  quantity: number;
  unitId: string;
  unitSymbol: string;
  baseQuantity: number;
  baseUnitSymbol: string;
  averageUnitCost: number;
  lastUnitCost?: number;
  totalCost: number;
  costSharePercentage: number;
  hasCost: boolean;
  warning?: string;
}

export interface MenuItemProfitabilityDto {
  menuItemId: string;
  menuItemName: string;
  internalCode?: string;
  categoryName: string;
  categoryColor?: string;
  grossSalePrice: number;
  taxPercentage: number;
  netSalePrice: number;
  taxAmount: number;
  recipeCost: number;
  grossProfit: number;
  foodCostPercentage: number;
  grossMarginPercentage: number;
  status: string;
  statusLabel: string;
  costMethod: string;
  hasRecipe: boolean;
  hasMissingCosts: boolean;
  hasConversionWarnings: boolean;
  ingredients: MenuItemProfitabilityIngredientDto[];
}

export interface CreateMenuItemDto {
  menuCategoryId: string;
  name: string;
  description?: string;
  internalCode?: string;
  price: number;
  stationId?: string;
  taxRateId?: string;
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

export interface MenuItemModifierOptionDto {
  id: string;
  modifierGroupId: string;
  name: string;
  articleId?: string;
  articleName?: string;
  unitId?: string;
  unitName?: string;
  unitSymbol?: string;
  quantity: number;
  priceDelta: number;
  displayOrder: number;
  isActive: boolean;
  isTracked: boolean;
  isAvailable: boolean;
  availableQuantity?: number | null;
}

export interface MenuItemModifierGroupDto {
  id: string;
  menuItemId: string;
  name: string;
  minSelections: number;
  maxSelections: number;
  isRequired: boolean;
  allowDuplicates: boolean;
  displayOrder: number;
  isActive: boolean;
  options: MenuItemModifierOptionDto[];
}

export interface UpsertMenuItemModifierOptionDto {
  id?: string;
  name: string;
  articleId?: string;
  unitId?: string;
  quantity: number;
  priceDelta: number;
  displayOrder: number;
  isActive: boolean;
}

export interface UpsertMenuItemModifierGroupDto {
  id?: string;
  name: string;
  minSelections: number;
  maxSelections: number;
  isRequired: boolean;
  allowDuplicates: boolean;
  displayOrder: number;
  isActive: boolean;
  options: UpsertMenuItemModifierOptionDto[];
}

export interface ModifierSelectionDto {
  modifierGroupId: string;
  modifierOptionId: string;
  groupName: string;
  optionName: string;
  quantity: number;
  unitPriceDelta: number;
  totalPriceDelta: number;
}

export interface CreateModifierSelectionDto {
  modifierOptionId: string;
  quantity: number;
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
  discountPct: number;
  discountAmount: number;
  taxRateId?: string;
  taxRateName?: string;
  taxRatePercentage?: number;
  taxAmount: number;
  totalPrice: number;
  notes?: string;
  status: OrderItemStatus;
  modifierSelections: ModifierSelectionDto[];
}

export interface OrderDto {
  id: string;
  number: number;
  type: OrderType;
  status: OrderStatus;
  tableId?: string;
  tableCode?: string;
  customerName?: string;
  deliveryAddress?: string;
  notes?: string;
  subtotal: number;
  discountTotal: number;
  taxableBase15: number;
  taxableBase0: number;
  taxableBaseExempt: number;
  iva15: number;
  ice: number;
  taxAmount: number;
  total: number;
  createdAt: string;
  confirmedAt?: string;
  deliveredAt?: string;
  totalItems: number;
  items: OrderItemDto[];
}

export interface ActiveOrderSummaryDto {
  id: string;
  number: number;
  type: OrderType;
  status: OrderStatus;
  tableCode?: string;
  customerName?: string;
  total: number;
  createdAt: string;
  confirmedAt?: string;
  totalItems: number;
}

export interface CreateOrderItemDto {
  menuItemId: string;
  quantity: number;
  notes?: string;
  modifierSelections?: CreateModifierSelectionDto[];
}

export interface CreateOrderDto {
  type: OrderType;
  tableId?: string;
  customerName?: string;
  deliveryAddress?: string;
  notes?: string;
  items: CreateOrderItemDto[];
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
  totalPurchases: number;
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

export type PurchaseStatus = 'Registrada' | 'Anulada';

export interface PurchaseItemDto {
  id: string;
  articleId: string;
  articleName: string;
  internalCode?: string;
  unitId: string;
  unitSymbol: string;
  quantity: number;
  unitPrice: number;
  discountPct: number;
  discountAmount: number;
  taxRateId?: string;
  taxRateName?: string;
  taxRatePercentage?: number;
  taxAmount: number;
  totalPrice: number;
  notes?: string;
}

export interface PurchaseDto {
  id: string;
  documentType: string;
  documentNumber?: string;
  documentDate: string;
  status: PurchaseStatus;
  supplierId?: string;
  supplierName?: string;
  notes?: string;
  destinationWarehouseId?: string;
  warehouseName?: string;
  subtotal: number;
  discountTotal: number;
  taxableBase15: number;
  taxableBase0: number;
  taxableBaseExempt: number;
  iva15: number;
  ice: number;
  total: number;
  totalItems: number;
  items: PurchaseItemDto[];
}

export interface PurchaseItemInputDto {
  articleId: string;
  unitId: string;
  quantity: number;
  unitPrice: number;
  discountPct: number;
  taxRateId?: string;
  notes?: string;
}

export interface CreatePurchaseDto {
  documentType: number;
  documentNumber?: string;
  documentDate: string;
  supplierId?: string;
  notes?: string;
  destinationWarehouseId?: string;
  items: PurchaseItemInputDto[];
}

export type UpdatePurchaseDto = CreatePurchaseDto;


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

// ── Billing: Métodos de pago ──────────────────────────────────────────────────

export interface PaymentMethodConfigDto {
  id: string;
  name: string;
  color: string;
  isCash: boolean;
  isCard: boolean;
  isActive: boolean;
  sortOrder: number;
}

export interface CreatePaymentMethodConfigDto {
  name: string;
  color: string;
  isCash: boolean;
  isCard: boolean;
  sortOrder: number;
}

export interface UpdatePaymentMethodConfigDto {
  name: string;
  color: string;
  isCash: boolean;
  isCard: boolean;
  isActive: boolean;
  sortOrder: number;
}

export interface CardBankDto {
  id: string;
  name: string;
  isActive: boolean;
  sortOrder: number;
}

export interface CreateCardBankDto {
  name: string;
  sortOrder: number;
}

export interface UpdateCardBankDto {
  name: string;
  isActive: boolean;
  sortOrder: number;
}

// ── Billing: Caja ─────────────────────────────────────────────────────────────

export interface PaymentMethodTotalDto {
  methodId: string;
  methodName: string;
  methodColor: string;
  isCash: boolean;
  total: number;
}

export interface CashRegisterDto {
  id: string;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
  hasOpenSession: boolean;
}

export interface CreateCashRegisterDto {
  name: string;
  code: string;
  description?: string;
}

export interface UpdateCashRegisterDto {
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
}

export interface CashSessionDto {
  id: string;
  cashRegisterId: string;
  cashRegisterName: string;
  cashRegisterCode: string;
  openedByName: string;
  openingBalance: number;
  openedAt: string;
  closedAt?: string;
  closedByName?: string;
  actualCash?: number;
  closeNotes?: string;
  status: 'Open' | 'Closed';
  totals: PaymentMethodTotalDto[];
  totalCash: number;   // computed by backend: sum of isCash totals
  totalSales: number;
  totalOrders: number;
  expectedCash: number;
  cashDifference?: number;
}

export interface OpenCashSessionDto {
  cashRegisterId: string;
  openingBalance: number;
}

export interface CloseCashSessionDto {
  actualCash: number;
  notes?: string;
}

// ── Billing: Cobro ────────────────────────────────────────────────────────────

export interface PaymentLineDto {
  id: string;
  methodId: string;
  methodName: string;
  methodColor: string;
  isCash: boolean;
  isCard: boolean;
  amountTendered: number;
  change: number;
  netAmount: number;
  cardPaymentType?: 'Credit' | 'Debit';
  cardBankId?: string;
  cardBankName?: string;
  cardBrand?: string;
  authorizationNumber?: string;
}

export interface OrderPaymentDto {
  id: string;
  orderId: string;
  cashSessionId?: string;
  cashRegisterId?: string;
  cashRegisterName?: string;
  cashRegisterCode?: string;
  cashierName?: string;
  orderNumber: number;
  orderType?: string;
  customerId?: string;
  customerName?: string;
  customerTaxId?: string;
  tableCode?: string;
  documentType: 'NotaDeVenta' | 'Factura';
  orderAmount: number;
  paidAt: string;
  lines: PaymentLineDto[];
  items: OrderPaymentItemDto[];
  electronicDocumentId?: string;
  electronicDocumentStatus?: string;
}

export interface OrderPaymentItemDto {
  id: string;
  orderItemId: string;
  itemName: string;
  quantity: number;
  unitPrice: number;
  total: number;
}

export interface SalesProfitabilityItemDto {
  menuItemId: string;
  menuItemName: string;
  internalCode?: string;
  categoryName: string;
  quantity: number;
  grossSales: number;
  netSales: number;
  taxAmount: number;
  unitRecipeCost: number;
  totalFoodCost: number;
  grossProfit: number;
  foodCostPercentage: number;
  grossMarginPercentage: number;
  hasMissingCosts: boolean;
  hasConversionWarnings: boolean;
}

export interface SalesProfitabilityCashRegisterDto {
  cashRegisterId?: string;
  cashRegisterName: string;
  grossSales: number;
  netSales: number;
  foodCost: number;
  grossProfit: number;
  foodCostPercentage: number;
  totalOrders: number;
}

export interface SalesProfitabilityReportDto {
  fromUtc?: string;
  toUtc?: string;
  cashRegisterId?: string;
  cashRegisterName?: string;
  grossSales: number;
  netSales: number;
  taxAmount: number;
  foodCost: number;
  grossProfit: number;
  foodCostPercentage: number;
  grossMarginPercentage: number;
  totalOrders: number;
  totalItems: number;
  missingCostLines: number;
  conversionWarningLines: number;
  items: SalesProfitabilityItemDto[];
  cashRegisters: SalesProfitabilityCashRegisterDto[];
}

export interface AddPaymentLineDto {
  methodId: string;
  amountTendered: number;
  cardPaymentType?: 'Credit' | 'Debit';
  cardBankId?: string;
  cardBrand?: string;
  authorizationNumber?: string;
}

export interface AddOrderPaymentDto {
  orderAmount: number;
  documentType: string;
  customerId?: string;
  cashSessionId?: string;
  lines: AddPaymentLineDto[];
  items: AddOrderPaymentItemDto[];
}

export interface AddOrderPaymentItemDto {
  orderItemId: string;
  quantity: number;
}

// ── Billing: IVA y configuración fiscal ──────────────────────────────────────

export interface TaxRateDto {
  id: string;
  name: string;
  percentage: number;
  sriCode: string;
  isDefault: boolean;
  isActive: boolean;
}

export interface UpsertTaxRateDto {
  name: string;
  percentage: number;
  sriCode: string;
  isDefault: boolean;
  isActive: boolean;
}

export interface BranchTaxConfigDto {
  id?: string;
  ruc: string;
  razonSocial: string;
  nombreComercial?: string;
  direccion: string;
  codigoEstablecimiento: string;
  puntoEmision: string;
  ambiente: string;
  contribuyenteEspecial?: string;
  obligadoContabilidad?: boolean;
  secuencial?: number;
}

export interface SmtpConfigDto {
  host: string;
  port: number;
  username: string;
  fromEmail: string;
  fromName: string;
  enableSsl: boolean;
  isActive: boolean;
  hasPassword: boolean;
}

export interface UpsertSmtpConfigDto {
  host: string;
  port: number;
  username: string;
  password?: string;
  fromEmail: string;
  fromName: string;
  enableSsl: boolean;
  isActive: boolean;
}

export interface SriCertificateStatusDto {
  hasCertificate: boolean;
  fileName?: string;
  expiresAt?: string;
  isExpired?: boolean;
  uploadedAt?: string;
}

export interface ElectronicDocumentDto {
  id: string;
  orderPaymentId: string;
  claveAcceso: string;
  numeroFactura: string;
  secuencial: number;
  environment: string;
  status: string;
  totalSinImpuestos: number;
  totalDescuento: number;
  totalIva: number;
  importeTotal: number;
  numeroAutorizacion?: string;
  fechaAutorizacion?: string;
  errorMessage?: string;
  sentAt?: string;
  retryCount: number;
  createdAt: string;
  hasRide: boolean;
  hasXml: boolean;
  hasXmlResponse: boolean;
}

// ── Invoice Template ──────────────────────────────────────────────────────────

export type PdfBlockType = 'header' | 'customer' | 'items' | 'payments' | 'totals' | 'footer';
export type EmailBlockType = 'header' | 'greeting' | 'message' | 'invoice_summary' | 'legal_note' | 'footer';

export interface PdfBlock {
  id: string;
  type: PdfBlockType;
  visible: boolean;
  label: string;
  // header
  primaryColor: string;
  showLogo: boolean;
  // customer
  showEmail: boolean;
  showPhone: boolean;
  showAddress: boolean;
  // items
  showAuxCode: boolean;
  showDiscount: boolean;
  // totals
  showZeroLines: boolean;
  // footer
  customText?: string;
}

export interface EmailBlock {
  id: string;
  type: EmailBlockType;
  visible: boolean;
  label: string;
  // header
  bgColor: string;
  title?: string;
  subtitle?: string;
  // text
  text?: string;
}

export interface InvoiceTemplateDto {
  logoBase64?: string;
  primaryColor: string;
  accentColor: string;
  pdfBlocks: PdfBlock[];
  emailSubject: string;
  emailBlocks: EmailBlock[];
}
