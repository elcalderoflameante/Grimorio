import assert from 'node:assert/strict';
import { test } from 'node:test';
import { clientAvailableQuantity, createClientPaymentAccount, paidItemQuantities, reconcileClientPayments } from '../src/utils/clientPaymentAccounts.ts';

const order = { items: [{ id: 'dish', quantity: 3, status: 'Ready' }, { id: 'drink', quantity: 1, status: 'Pending' }] };
const account = (id, qty = 0) => ({ ...createClientPaymentAccount(id), splitRows: qty ? { dish: { checked: true, qty } } : {} });
const payment = { id: 'payment-1', items: [{ orderItemId: 'dish', quantity: 1 }] };

test('clients have independent customer, document, payment methods and selections', () => {
  const first = account(1), second = account(2);
  first.docType = 'Factura'; first.customer = { id: 'customer' }; first.payLines.push({ methodId: 'cash', amountTendered: 5 });
  first.splitRows.dish = { checked: true, qty: 1 };
  first.loyaltyMember = { id: 'member-1', customerId: 'different-from-invoice' };
  assert.equal(second.docType, 'NotaDeVenta'); assert.equal(second.customer, null);
  assert.equal(second.loyaltyMember, null);
  assert.deepEqual(second.payLines, []); assert.deepEqual(second.splitRows, {});
});

test('units assigned to another unpaid client cannot be assigned twice', () => {
  const accounts = [account(1, 2), account(2, 1)];
  assert.equal(clientAvailableQuantity('dish', 3, accounts, 1), 2);
  assert.equal(clientAvailableQuantity('dish', 3, accounts, 2), 1);
  assert.equal(clientAvailableQuantity('dish', 3, accounts, 3), 0);
  assert.equal(clientAvailableQuantity('drink', 1, accounts, 2), 1);
});

test('deleting an unpaid client releases their assigned units', () => {
  assert.equal(clientAvailableQuantity('dish', 3, [account(2, 1)], 2), 3);
});

test('paid quantities aggregate by exact order line, not menu item', () => {
  assert.deepEqual(paidItemQuantities([payment, { items: [{ orderItemId: 'dish', quantity: 1 }, { orderItemId: 'drink', quantity: 1 }] }]), { dish: 2, drink: 1 });
});

test('confirmed payments stay locked and do not reserve their units twice', () => {
  const first = { ...account(1, 1), payment };
  const result = reconcileClientPayments([first, account(2, 2)], order, [payment]);
  assert.equal(result[0].payment.id, payment.id);
  assert.equal(result[1].splitRows.dish.qty, 2);
  assert.equal(clientAvailableQuantity('dish', 2, result, 2), 2);
});

test('external payments shrink conflicting drafts without changing captured payments', () => {
  const before = [account(1, 2), account(2, 1)];
  const snapshot = JSON.stringify(before);
  const result = reconcileClientPayments(before, order, [payment]);
  assert.equal(result[0].splitRows.dish.qty, 2);
  assert.deepEqual(result[1].splitRows, {});
  assert.equal(result[2].payment.id, payment.id);
  assert.equal(JSON.stringify(before), snapshot);
});

test('cancelled or deleted order lines are removed from unpaid client selections', () => {
  const result = reconcileClientPayments([account(1, 2)], { items: [{ id: 'dish', quantity: 3, status: 'Cancelled' }] }, []);
  assert.deepEqual(result[0].splitRows, {});
});

test('history is reconstructed without duplicate paid tabs on repeated refresh', () => {
  const result = reconcileClientPayments([], order, [payment]);
  assert.equal(result.length, 1);
  assert.deepEqual(reconcileClientPayments(result, order, [payment]), result);
});

test('fully paid items are never kept in a pending tab', () => {
  const full = { ...payment, items: [{ orderItemId: 'dish', quantity: 3 }] };
  const result = reconcileClientPayments([account(1, 3)], order, [full]);
  assert.deepEqual(result[0].splitRows, {});
});
