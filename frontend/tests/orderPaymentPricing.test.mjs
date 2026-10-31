import assert from 'node:assert/strict';
import { test } from 'node:test';
import { allocatePaymentTotal, getPaymentLineTotal, getPromotionDiscount, getGroupedPromotionDiscounts } from '../src/utils/orderPaymentPricing.ts';

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

const secondUnit = { type: 'SecondUnitPercentage', discountPercent: 50, paymentPolicy: 'AnyPayment' };
const differentCard = { ...secondUnit, paymentPolicy: 'CardAlternativePercentage', cardDiscountPercent: 30 };
test('separate modifier lines form pairs and distribute cash/card savings without losing cents', () => {
  const promo={...differentCard,id:'p'};
  for(const [count,totalCash,totalCard] of [[1,0,0],[2,5.75,3.45],[3,5.75,3.45],[4,11.5,6.9]]) {
    const lines=Array.from({length:count},()=>({menuItemId:'ribeye',promotionId:'p',unitPrice:11.5,quantity:1}));
    assert.equal(getGroupedPromotionDiscounts(lines,[promo]).reduce((s,d)=>s+Math.round(d*100),0),Math.round(totalCash*100));
    assert.equal(getGroupedPromotionDiscounts(lines,[promo],true).reduce((s,d)=>s+Math.round(d*100),0),Math.round(totalCard*100));
  }
});
test('different products/promotions never form a pair, cheaper unit is discounted and snapshots remain authoritative', () => {
  const promo={...differentCard,id:'p'};
  const lines=[{menuItemId:'ribeye',promotionId:'p',unitPrice:25,quantity:1},{menuItemId:'ribeye',promotionId:'p',unitPrice:20,quantity:1}];
  assert.deepEqual(getGroupedPromotionDiscounts(lines,[promo]),[5.56,4.44]);
  assert.deepEqual(getGroupedPromotionDiscounts([lines[0],{...lines[1],menuItemId:'other'}],[promo]),[0,0]);
  assert.deepEqual(getGroupedPromotionDiscounts([lines[0],{...lines[1],promotionId:undefined}],[promo]),[0,0]);
  assert.equal(getPaymentLineTotal({unitPrice:11.5,quantity:1,totalPrice:8.62,promotionCashDiscountAmount:2.88,promotionCardDiscountAmount:1.72},promo,true),9.78);
});
test('different card percentage applies to the same second units as cash and transfer', () => {
  for (const [quantity, cash, card] of [[1,20,20],[2,30,34],[3,50,54],[4,60,68],[5,80,88],[6,90,102]]) {
    const line = {unitPrice:20,quantity,totalPrice:cash};
    assert.equal(getPaymentLineTotal(line,differentCard,false),cash);
    assert.equal(getPaymentLineTotal(line,differentCard,true),card);
  }
});
test('different card percentage works for ordinary percentages, zero, full discount and rounding', () => {
  const line = {unitPrice:20,quantity:2,totalPrice:30};
  assert.equal(getPaymentLineTotal(line,{...differentCard,type:'Percentage'},false),20);
  assert.equal(getPaymentLineTotal(line,{...differentCard,type:'Percentage'},true),28);
  assert.equal(getPaymentLineTotal(line,{...differentCard,cardDiscountPercent:0},true),40);
  assert.equal(getPaymentLineTotal(line,{...differentCard,cardDiscountPercent:100},true),20);
  assert.equal(getPaymentLineTotal({unitPrice:.05,quantity:2,totalPrice:.1},{...differentCard,cardDiscountPercent:50},true),.08);
});
test('cash and card partial payments keep their own percentage in either order', () => {
  const line = {unitPrice:20,quantity:2,totalPrice:30};
  for (const cardFirst of [false,true]) {
    const first=allocatePaymentTotal(getPaymentLineTotal(line,differentCard,cardFirst),2,0,1);
    const second=allocatePaymentTotal(getPaymentLineTotal(line,differentCard,!cardFirst),2,1,1);
    assert.equal(first,cardFirst?17:15);
    assert.equal(second,cardFirst?15:17);
    assert.equal(first+second,32);
  }
});
test('second unit 50% applies once per complete pair, never to the first or an unpaired unit', () => {
  for (const [quantity, expected] of [[1, 20], [2, 30], [3, 50], [4, 60], [5, 80], [6, 90]]) {
    const line = { unitPrice: 20, quantity, totalPrice: 20 * quantity };
    assert.equal(getPaymentLineTotal(line, secondUnit, false), expected);
    assert.equal(getPaymentLineTotal(line, secondUnit, true), expected);
    assert.equal(getPromotionDiscount(20, quantity, secondUnit), 20 * quantity - expected);
    assert.equal(Array.from({length:quantity}, (_,paid)=>allocatePaymentTotal(expected,quantity,paid,1))
      .reduce((sum,value)=>sum+Math.round(value*100),0),expected*100);
  }
});
test('second unit policy restrictions and card prices match server behavior', () => {
  const line = { unitPrice: 20, quantity: 2, totalPrice: 30 };
  assert.equal(getPaymentLineTotal(line, {...secondUnit,paymentPolicy:'CashTransferOnly'},true),40);
  assert.equal(getPaymentLineTotal(line, {...secondUnit,paymentPolicy:'CashTransferOnly'},false),30);
  assert.equal(getPaymentLineTotal(line, {...secondUnit,paymentPolicy:'CardAlternativePrice',cardPrice:18},true),36);
  assert.equal(getPaymentLineTotal(line, {...secondUnit,paymentPolicy:'CardAlternativePrice',cardPrice:18},false),30);
});
test('second unit rounding, full discount and partial payments preserve the full promotion total', () => {
  assert.equal(getPaymentLineTotal({unitPrice:.05,quantity:2,totalPrice:.1},secondUnit,false),.08);
  assert.equal(getPaymentLineTotal({unitPrice:20,quantity:3,totalPrice:60},{...secondUnit,discountPercent:100},false),40);
  const total = getPaymentLineTotal({unitPrice:20,quantity:2,totalPrice:40},secondUnit,false);
  assert.deepEqual([0,1].map(paid=>allocatePaymentTotal(total,2,paid,1)),[15,15]);
});
