import type { CashSessionDto } from './index';

export interface SeparateCashSessionDto {
  requestId: string;
  cutoff: string;
  newOpeningBalance: number;
  previousActualCash: number | null;
  reason: string;
  previewToken: string;
}

export interface CashSessionSeparationMovementDto {
  id: string;
  kind: 'Payment' | 'Expense';
  reference: string;
  date: string;
  amount: number;
  movesToNewSession: boolean;
  isCancelled: boolean;
}

export interface CashSessionSeparationPreviewDto {
  token: string;
  cutoffUtc: string;
  previousSession: CashSessionDto;
  newSession: CashSessionDto;
  movements: CashSessionSeparationMovementDto[];
}

export interface CashSessionSeparationResultDto {
  id: string;
  previousSessionId: string;
  newSessionId: string;
}

export interface CashSessionSeparationRecordDto extends CashSessionSeparationResultDto {
  cutoffUtc: string;
  recordedAt: string;
  actorName: string;
  reason: string;
  preview: CashSessionSeparationPreviewDto;
}
