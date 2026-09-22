import assert from 'node:assert/strict';
import { test } from 'node:test';
import { allocatePaymentTotal, getPaymentLineTotal } from '../src/utils/orderPaymentPricing.ts';

const promotion = { type: 'FixedPrice', fixedPrice: 7.99, cardPrice: 8.99, paymentPolicy: 'CardAlternativePrice' };
const item = { unitPrice: 11, quantity: 2, totalPrice: 15.98 };

test('cash then card preserves the first payment and prices only the remaining unit', () => {
  const cash = allocatePaymentTotal(getPaymentLineTotal(item, promotion, false), 2, 0, 1);
  const card = allocatePaymentTotal(getPaymentLineTotal(item, promotion, true), 2, 1, 1);
  assert.equal(cash, 7.99);
  assert.equal(card, 8.99);
  assert.equal(cash + card, 16.98);
});

test('card then cash does not reuse the blended order line price', () => {
  const blended = { ...item, totalPrice: 16.98 };
  assert.equal(allocatePaymentTotal(getPaymentLineTotal(blended, promotion, false), 2, 1, 1), 7.99);
});

test('cash-only promotion is still available after a card payment', () => {
  const cashOnly = { ...promotion, paymentPolicy: 'CashTransferOnly' };
  assert.equal(allocatePaymentTotal(getPaymentLineTotal(item, cashOnly, true), 2, 0, 1), 11);
  assert.equal(allocatePaymentTotal(getPaymentLineTotal(item, cashOnly, false), 2, 1, 1), 7.99);
});

test('3x2 divided into three payments preserves the offer and its cents', () => {
  const total = getPaymentLineTotal({ unitPrice: 10, quantity: 3, totalPrice: 20 },
    { type: 'BuyXPayY', buyQuantity: 3, payQuantity: 2, paymentPolicy: 'Any' }, false);
  const amounts = [0, 1, 2].map(paid => allocatePaymentTotal(total, 3, paid, 1));
  assert.deepEqual(amounts, [6.67, 6.66, 6.67]);
  assert.equal(amounts.reduce((sum, amount) => sum + amount, 0), 20);
});

test('fixed amount discount is allocated rather than repeated per partial payment', () => {
  const total = getPaymentLineTotal(item, { type: 'FixedAmount', discountAmount: 5 }, false);
  assert.equal(allocatePaymentTotal(total, 2, 0, 1), 8.5);
  assert.equal(allocatePaymentTotal(total, 2, 1, 1), 8.5);
});

test('percentage rounding matches the backend midpoint rule', () => {
  assert.equal(getPaymentLineTotal({ unitPrice: 0.05, quantity: 1, totalPrice: 0.05 },
    { type: 'Percentage', discountPercent: 10 }, false), 0.05);
});

test('rejects quantities beyond the remaining balance', () => {
  assert.throws(() => allocatePaymentTotal(20, 3, 2, 2));
});
