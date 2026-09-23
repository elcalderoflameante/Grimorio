export interface InventoryReconciliationFindingDto {
  code: string;
  severity: 'Confirmed' | 'Review' | 'Incomplete';
  message: string;
  articleId: string;
  articleName: string;
  warehouseId: string;
  warehouseName: string;
  unitSymbol?: string;
  expectedQuantity?: number;
  actualQuantity?: number;
  reference?: string;
  sourceId?: string;
  movementId?: string;
}

export interface InventoryReconciliationDto {
  checkedAt: string;
  checkedBalances: number;
  confirmedCount: number;
  reviewCount: number;
  incompleteCount: number;
  totalFindings: number;
  findings: InventoryReconciliationFindingDto[];
}

export interface StockMovementTraceDto {
  movementId: string;
  origin: 'Sale' | 'Purchase' | 'Production' | 'Manual' | 'Incomplete';
  orderId?: string;
  orderNumber?: number;
  orderItemId?: string;
  orderPaymentId?: string;
  orderPaymentItemId?: string;
  paidItemQuantity?: number;
  paidAt?: string;
  stockReservationId?: string;
  purchaseId?: string;
  purchaseItemId?: string;
  documentNumber?: string;
  productionOrderId?: string;
  productionNumber?: string;
  reference?: string;
  notes?: string;
  createdAt: string;
}
