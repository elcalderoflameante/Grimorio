import type { AddOrderPaymentDto } from '../types';

export const pendingPaymentStorageKey = (branchId: string, orderId: string) =>
  `grimorio:pending-payment:${branchId}:${orderId}`;

export function readPendingPayment(key: string): AddOrderPaymentDto | null {
  const stored = localStorage.getItem(key);
  if (!stored) return null;
  const value = JSON.parse(stored) as AddOrderPaymentDto;
  if (!value || typeof value.idempotencyKey !== 'string' || !value.idempotencyKey
    || typeof value.orderAmount !== 'number' || !Number.isFinite(value.orderAmount)
    || value.orderAmount <= 0 || typeof value.documentType !== 'string'
    || !Array.isArray(value.items) || !Array.isArray(value.lines) || value.lines.length === 0) {
    throw new Error('No se puede leer el intento de cobro pendiente. Revisa los pagos antes de continuar.');
  }
  return value;
}

export function clearPendingPayment(key: string, idempotencyKey: string) {
  if (readPendingPayment(key)?.idempotencyKey === idempotencyKey)
    localStorage.removeItem(key);
}
