import type { AddPaymentLineDto, CustomerDto, OrderDto, OrderPaymentDto } from '../types';
import type { LoyaltyMemberReference } from '../services/loyaltyApi';

export interface SplitRow { checked: boolean; qty: number }

export interface ClientPaymentAccount {
  id: number;
  docType: string;
  customer: CustomerDto | null;
  loyaltyMember: LoyaltyMemberReference | null;
  payLines: AddPaymentLineDto[];
  splitRows: Record<string, SplitRow>;
  payment?: OrderPaymentDto;
}

export const createClientPaymentAccount = (id: number): ClientPaymentAccount => ({
  id, docType: 'NotaDeVenta', customer: null, loyaltyMember: null, payLines: [], splitRows: {},
});

export function paidItemQuantities(payments: OrderPaymentDto[]) {
  const result: Record<string, number> = {};
  for (const payment of payments) for (const item of payment.items ?? [])
    result[item.orderItemId] = (result[item.orderItemId] ?? 0) + item.quantity;
  return result;
}

export function clientAvailableQuantity(itemId: string, pending: number, accounts: ClientPaymentAccount[], clientId: number) {
  const assigned = accounts.filter(a => a.id !== clientId && !a.payment)
    .reduce((sum, a) => sum + (a.splitRows[itemId]?.checked ? a.splitRows[itemId].qty : 0), 0);
  return Math.max(0, pending - assigned);
}

export function reconcileClientPayments(accounts: ClientPaymentAccount[], order: OrderDto, payments: OrderPaymentDto[]): ClientPaymentAccount[] {
  const paid = paidItemQuantities(payments);
  const available = Object.fromEntries(order.items.filter(i => i.status !== 'Cancelled')
    .map(i => [i.id, Math.max(0, i.quantity - (paid[i.id] ?? 0))]));
  const result: ClientPaymentAccount[] = accounts.map(account => {
    const payment = account.payment && payments.find(p => p.id === account.payment!.id);
    if (payment) return { ...account, payment };
    const splitRows: Record<string, SplitRow> = {};
    for (const [id, row] of Object.entries(account.splitRows)) {
      if (!row.checked) continue;
      const qty = Math.min(Math.max(0, Math.floor(row.qty)), available[id] ?? 0);
      if (qty > 0) {
        splitRows[id] = { checked: true, qty };
        available[id] -= qty;
      }
    }
    return { ...account, payment: undefined, splitRows };
  });
  let nextId = Math.max(0, ...result.map(a => a.id)) + 1;
  for (const payment of payments) if (!result.some(a => a.payment?.id === payment.id))
    result.push({ ...createClientPaymentAccount(nextId++), payment });
  return result;
}
