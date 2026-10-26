import type { PaymentLineDto } from './index';

export interface CorrectPaymentMethodDto {
  requestId: string;
  paymentLineId?: string;
  expectedMethodId?: string;
  expectedUpdatedAt?: string | null;
  expectedLines?: { id: string; methodId: string; updatedAt: string | null; netAmount: number }[];
  methodId?: string;
  cardPaymentType?: string;
  cardBankId?: string;
  cardBrand?: string;
  authorizationNumber?: string;
  reason: string;
  lines?: PaymentMethodCorrectionLineDto[];
}

export interface PaymentMethodCorrectionLineDto {
  methodId: string;
  amount: number;
  cardPaymentType?: string;
  cardBankId?: string;
  cardBrand?: string;
  authorizationNumber?: string;
}

export interface PaymentMethodCorrectionDto {
  id: string;
  paymentLineId: string;
  correctedAt: string;
  actorName: string;
  reason: string;
  before: PaymentLineDto;
  after: PaymentLineDto;
  afterLines?: PaymentLineDto[];
  beforeLines?: PaymentLineDto[];
}
