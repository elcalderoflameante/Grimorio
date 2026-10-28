export interface CountOption { id: string; name: string }
export interface CountRule { articleId: string; warehouseId: string; frequency: 'Daily' | 'Weekly'; weekDay: number }
export interface CountArea { id: string; name: string; isActive: boolean; userIds: string[]; rules: CountRule[] }
export interface CountCatalog {
  areas: CountArea[]; users: CountOption[]; articles: (CountOption & { unitSymbol: string })[]; warehouses: CountOption[];
}
export interface StaffCountLine {
  id: string; articleId: string; articleName: string; warehouseName: string; unitSymbol: string;
  quantity: number | null; expectedQuantity: number | null; difference: number | null; countedAt: string | null; notes: string | null;
}
export interface StaffCount {
  id: string; areaId: string; areaName: string; businessDate: string; counterName: string; status: string;
  submittedAt: string | null; reviewedAt: string | null; reviewerName: string | null; reviewNotes: string | null;
  recountOfId: string | null; lines: StaffCountLine[];
}
