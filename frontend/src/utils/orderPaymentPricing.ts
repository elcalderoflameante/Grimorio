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
  item: Pick<OrderDto['items'][number], 'unitPrice' | 'quantity' | 'totalPrice'> &
    Partial<Pick<OrderDto['items'][number], 'promotionCashDiscountAmount' | 'promotionCardDiscountAmount'>>,
  promotion: PromotionDto | undefined,
  useCardPrice: boolean,
) {
  const snapshot = useCardPrice ? item.promotionCardDiscountAmount : item.promotionCashDiscountAmount;
  if (snapshot != null) return item.unitPrice * item.quantity - snapshot;
  if (!promotion) return item.totalPrice;
  const gross = item.unitPrice * item.quantity;
  const percentage = useCardPrice && promotion.paymentPolicy === 'CardAlternativePercentage'
    ? promotion.cardDiscountPercent ?? 0
    : promotion.discountPercent ?? 0;
  let discount = 0;
  if (useCardPrice && promotion.paymentPolicy === 'CashTransferOnly') discount = 0;
  else if (useCardPrice && promotion.paymentPolicy === 'CardAlternativePrice' && promotion.cardPrice != null)
    discount = gross - promotion.cardPrice * item.quantity;
  else if (promotion.type === 'Percentage') discount = gross * percentage / 100;
  else if (promotion.type === 'SecondUnitPercentage')
    discount = Math.floor(item.quantity / 2) * item.unitPrice * percentage / 100;
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

export function getPromotionDiscount(unitPrice: number, quantity: number, promotion: PromotionDto) {
  const gross = unitPrice * quantity;
  return roundDiscount(gross - getPaymentLineTotal({ unitPrice, quantity, totalPrice: gross }, promotion, false));
}

export function getGroupedPromotionDiscounts(
  lines: { menuItemId: string; promotionId?: string; unitPrice: number; quantity: number }[],
  promotions: PromotionDto[],
  useCardPrice = false,
) {
  const discounts = lines.map(line => {
    const promotion = promotions.find(p => p.id === line.promotionId);
    return promotion ? roundDiscount(line.unitPrice * line.quantity - getPaymentLineTotal(
      {...line,totalPrice:line.unitPrice*line.quantity},promotion,useCardPrice)) : 0;
  });
  const groups = new Map<string, number[]>();
  lines.forEach((line,index) => {
    const p = promotions.find(p => p.id === line.promotionId);
    if (p?.type !== 'SecondUnitPercentage' || (useCardPrice && p.paymentPolicy === 'CardAlternativePrice')) return;
    const key = JSON.stringify([line.menuItemId,line.promotionId]);
    groups.set(key,[...(groups.get(key) ?? []),index]);
  });
  for (const indexes of groups.values()) {
    const sorted = [...indexes].sort((a,b) => lines[b].unitPrice-lines[a].unitPrice || a-b);
    const p = promotions.find(p => p.id === lines[sorted[0]].promotionId)!;
    let position=0, base=0;
    for (const index of sorted) {
      const line=lines[index];
      base += (Math.floor((position+line.quantity)/2)-Math.floor(position/2))*line.unitPrice;
      position += line.quantity;
    }
    const percentage = useCardPrice && p.paymentPolicy === 'CashTransferOnly' ? 0
      : useCardPrice && p.paymentPolicy === 'CardAlternativePercentage' ? p.cardDiscountPercent ?? 0 : p.discountPercent ?? 0;
    const total = roundDiscount(base*percentage/100);
    const gross = sorted.reduce((sum,index)=>sum+lines[index].unitPrice*lines[index].quantity,0);
    let cumulative=0, previous=0;
    for (const index of sorted) {
      cumulative += lines[index].unitPrice*lines[index].quantity;
      const next = gross === 0 ? 0 : roundDiscount(total*cumulative/gross);
      discounts[index] = roundDiscount(next-previous); previous=next;
    }
  }
  return discounts;
}
