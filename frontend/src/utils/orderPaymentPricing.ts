import type { OrderDto, PromotionDto } from '../types';

// Match decimal rounding in the existing promotion engine (midpoints to even).
function roundDiscount(value: number) {
  const scaled = value * 100;
  const lower = Math.floor(scaled);
  return (Math.abs(scaled - lower - 0.5) < 1e-8
    ? (lower % 2 === 0 ? lower : lower + 1)
    : Math.round(scaled)) / 100;
}

export function getPaymentLineTotal(
  item: Pick<OrderDto['items'][number], 'unitPrice' | 'quantity' | 'totalPrice'>,
  promotion: PromotionDto | undefined,
  useCardPrice: boolean,
) {
  if (!promotion) return item.totalPrice;
  const gross = item.unitPrice * item.quantity;
  let discount = 0;
  if (useCardPrice && promotion.paymentPolicy === 'CashTransferOnly') discount = 0;
  else if (useCardPrice && promotion.paymentPolicy === 'CardAlternativePrice' && promotion.cardPrice != null)
    discount = gross - promotion.cardPrice * item.quantity;
  else if (promotion.type === 'Percentage') discount = gross * (promotion.discountPercent ?? 0) / 100;
  else if (promotion.type === 'FixedAmount') discount = promotion.discountAmount ?? 0;
  else if (promotion.type === 'FixedPrice') discount = gross - (promotion.fixedPrice ?? item.unitPrice) * item.quantity;
  else if (promotion.type === 'BuyXPayY') {
    const buy = promotion.buyQuantity ?? 0;
    const pay = promotion.payQuantity ?? 0;
    if (buy > 1 && pay > 0 && pay < buy)
      discount = Math.floor(item.quantity / buy) * (buy - pay) * item.unitPrice;
  }
  return gross - Math.min(gross, Math.max(0, roundDiscount(discount)));
}

export function allocatePaymentTotal(total: number, quantity: number, paidQuantity: number, selectedQuantity: number) {
  if (quantity <= 0 || paidQuantity < 0 || selectedQuantity < 0 || paidQuantity + selectedQuantity > quantity)
    throw new Error('Cantidad de cobro invalida.');
  const cents = (value: number) => Math.round(value * 100 + 1e-8);
  return (cents(total * (paidQuantity + selectedQuantity) / quantity)
    - cents(total * paidQuantity / quantity)) / 100;
}
