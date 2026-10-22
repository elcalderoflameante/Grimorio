import axios from 'axios';

// Public requests must not carry staff credentials or log verification codes.
const client = axios.create({ baseURL: import.meta.env.VITE_API_URL || '/api', timeout: 20000 });
const base = '/marketing/loyalty/public';
export interface PublicLoyaltyInfo {
  enabled: boolean; testMode: boolean; programName: string; minimumSpend: number; includeTax: boolean;
  rewardValidityDays: number; termsVersion: string;
  rewards: { visitNumber: number; repeatEvery?: number; name: string }[];
}
export interface PublicEnrollmentProfile {
  name?: string; email?: string; birthDay?: number; birthMonth?: number;
  acceptsProgram?: boolean; marketingConsent?: boolean;
}
export interface LoyaltyChallenge { challengeId: string; expiresAt: string; retryAfterSeconds: number }
export const publicLoyaltyApi = {
  info: () => client.get<PublicLoyaltyInfo>(`${base}/program`),
  request: (phone: string, purpose: 'Enroll' | 'Recover') => client.post<LoyaltyChallenge>(`${base}/code`, { phone, purpose }),
  verify: (challengeId: string, code: string, profile: PublicEnrollmentProfile, termsVersion: string) =>
    client.post<{ publicToken: string }>(`${base}/verify`, { challengeId, code, ...profile, termsVersion }),
};
export function publicLoyaltyError(error: unknown): string {
  if (axios.isAxiosError(error)) {
    if (error.response?.status === 429) return 'Demasiados intentos. Espera un minuto antes de continuar.';
    const message: unknown = error.response?.data?.message;
    if (typeof message === 'string') return message;
  }
  return 'No pudimos completar la solicitud. Intenta nuevamente.';
}
