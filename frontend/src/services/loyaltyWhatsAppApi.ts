import api from './api';

export interface LoyaltyWhatsAppSetup {
  mode: string; enabled: boolean; configured: boolean; ready: boolean; programActive: boolean;
  restricted: boolean; dailySendLimit: number; businessPhone: string; authenticationTemplate: string; templateLanguage: string;
  businessAppCoexistenceConfirmed: boolean; missing: string[]; canCheckConnection: boolean;
  webhookUrl?: string; enrollmentUrl?: string; privacyUrl?: string; deletionUrl?: string;
  developerUrl: string; managerUrl: string;
}
export const loyaltyWhatsAppApi = {
  setup: () => api.get<LoyaltyWhatsAppSetup>('/marketing/loyalty/whatsapp/setup'),
  check: () => api.post<{ success: boolean; message: string; phone: string; name: string; template: string }>('/marketing/loyalty/whatsapp/check'),
};
