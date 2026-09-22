import assert from 'node:assert/strict';
import { beforeEach, test } from 'node:test';
import { clearPendingPayment, pendingPaymentStorageKey, readPendingPayment } from '../src/utils/pendingPayment.ts';

const key = pendingPaymentStorageKey('branch', 'order');
const request = {
  idempotencyKey: 'attempt-1', orderAmount: 4, documentType: 'NotaDeVenta',
  items: [{ orderItemId: 'item', quantity: 1 }],
  lines: [{ methodId: 'cash', amountTendered: 5 }],
};

beforeEach(() => {
  const values = new Map();
  globalThis.localStorage = {
    getItem: key => values.get(key) ?? null,
    setItem: (key, value) => values.set(key, value),
    removeItem: key => values.delete(key),
  };
});

test('restores the exact request including tendered amount and partial quantities', () => {
  localStorage.setItem(key, JSON.stringify(request));
  assert.deepEqual(readPendingPayment(key), request);
  assert.deepEqual(readPendingPayment(key), request);
});

test('isolates orders and branches', () => {
  localStorage.setItem(key, JSON.stringify(request));
  assert.equal(readPendingPayment(pendingPaymentStorageKey('other', 'order')), null);
  assert.equal(readPendingPayment(pendingPaymentStorageKey('branch', 'other')), null);
});

test('removes only the confirmed attempt', () => {
  localStorage.setItem(key, JSON.stringify(request));
  clearPendingPayment(key, 'another-attempt');
  assert.deepEqual(readPendingPayment(key), request);
  clearPendingPayment(key, request.idempotencyKey);
  assert.equal(readPendingPayment(key), null);
});

test('does not silently discard corrupt or incomplete attempts', () => {
  for (const value of ['{broken', '{}', 'null', JSON.stringify({ ...request, orderAmount: 0 })]) {
    localStorage.setItem(key, value);
    assert.throws(() => readPendingPayment(key));
    assert.equal(localStorage.getItem(key), value);
  }
});

test('storage errors are propagated instead of treating the order as a new payment', () => {
  localStorage.getItem = () => { throw new Error('Storage unavailable'); };
  assert.throws(() => readPendingPayment(key), /Storage unavailable/);
});
