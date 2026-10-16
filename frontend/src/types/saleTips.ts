export type TipMethod = 'Cash' | 'Card' | 'Transfer';
export const TIP_METHODS: { value: TipMethod; label: string }[] = [
  { value: 'Cash', label: 'Efectivo' },
  { value: 'Card', label: 'Tarjeta' },
  { value: 'Transfer', label: 'Transferencia' },
];
export interface SaleTipDto {
  id: string;
  orderPaymentId: string;
  orderNumber: number;
  saleDate: string;
  registeredAt: string;
  amount: number;
  method: TipMethod;
  registeredByName: string;
  updatedByName?: string;
}
export interface SaleTipReportDto {
  totalAmount: number;
  count: number;
  byMethod: { method: TipMethod; amount: number; count: number }[];
  items: SaleTipDto[];
  page: number;
  pageSize: number;
}
