import type { PaymentLineDto } from './index';

export interface CorrectPaymentMethodDto {
  requestId: string;
  paymentLineId: string;
  expectedMethodId: string;
  expectedUpdatedAt: string | null;
  methodId: string;
  cardPaymentType?: string;
  cardBankId?: string;
  cardBrand?: string;
  authorizationNumber?: string;
  reason: string;
}

export interface PaymentMethodCorrectionDto {
  id: string;
  paymentLineId: string;
  correctedAt: string;
  actorName: string;
  reason: string;
  before: PaymentLineDto;
  after: PaymentLineDto;
}
