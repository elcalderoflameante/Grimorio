import api from './api';

export interface LoyaltyProduct { menuItemId: string; name: string; quantity: number }
export interface LoyaltyTier {
  visitNumber: number; repeatEvery?: number | null; name: string; kind: 'Bundle' | 'Choice';
  isEnabled: boolean; maximumPrice?: number | null; products: LoyaltyProduct[];
}
export interface LoyaltyProgram {
  name: string; isActive: boolean; minimumSpend: number; includeTax: boolean; rewardValidityDays: number;
  excludedItemIds: string[]; tiers: LoyaltyTier[];
}
export interface LoyaltyMember {
  id: string; customerId?: string | null; customerName: string; billingCustomerName?: string | null;
  phone?: string | null; email?: string | null; birthDay?: number | null; birthMonth?: number | null; marketingConsent: boolean;
  cardCode: string; isActive: boolean; visits: number; rewards: number;
}
export interface LoyaltyMemberProfile {
  customerId?: string | null; name: string; phone: string; email?: string | null;
  birthDay?: number | null; birthMonth?: number | null; marketingConsent: boolean;
}
export type LoyaltyMemberReference = Pick<LoyaltyMember, 'id' | 'customerName' | 'phone' | 'cardCode' | 'isActive' | 'visits' | 'rewards'>;
export interface LoyaltyReward {
  id: string; visitNumber: number; name: string; issuedAt: string; availableAt: string; expiresAt: string;
  status: 'Available' | 'Upcoming' | 'Expired'; rule: LoyaltyTier;
}
export interface LoyaltyVisit { id: string; businessDate: string; qualifiedSpend: number; minimumSpend: number }
export interface LoyaltyDetail { member: LoyaltyMember; publicToken: string; visits: LoyaltyVisit[]; rewards: LoyaltyReward[] }
export interface PublicLoyaltyCard {
  programName: string; customerName: string; cardCode: string; visits: number; nextVisit?: number; nextReward?: string; rewards: LoyaltyReward[];
}
export interface LoyaltySummary { members: number; activeMembers: number; visits: number; availableRewards: number; expiredRewards: number }
const base = '/marketing/loyalty';
export const loyaltyApi = {
  program: () => api.get<LoyaltyProgram>(`${base}/program`),
  saveProgram: (program: LoyaltyProgram) => api.put<LoyaltyProgram>(`${base}/program`, program),
  members: (search?: string) => api.get<LoyaltyMember[]>(`${base}/members`, { params: { search } }),
  lookup: (search?: string) => api.get<LoyaltyMemberReference[]>(`${base}/lookup`, { params: { search } }),
  member: (id: string) => api.get<LoyaltyDetail>(`${base}/members/${id}`),
  enroll: (profile: LoyaltyMemberProfile & { acceptsProgram: boolean }) => api.post<LoyaltyDetail>(`${base}/members`, profile),
  updateMember: (id: string, profile: LoyaltyMemberProfile) => api.put<LoyaltyDetail>(`${base}/members/${id}`, profile),
  summary: () => api.get<LoyaltySummary>(`${base}/summary`),
  card: (token: string) => api.get<PublicLoyaltyCard>(`${base}/card/${encodeURIComponent(token)}`),
};
