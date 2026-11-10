import api from './api';

export const helpEnabled = import.meta.env.DEV || import.meta.env.VITE_HELP_ENABLED === 'true';

export interface HelpArticle {
  id: string;
  title: string;
  summary: string;
  section: string;
  audience: string;
  kind: string;
  verifiedOn: string;
  verification: string;
}
export interface HelpIndex { edition: string; articles: HelpArticle[] }
export interface HelpDetail { article: HelpArticle; markdown: string }

export const helpApi = {
  index: (query: string, signal: AbortSignal) => api.get<HelpIndex>('/help/articles', { params: { q: query }, signal }),
  detail: (id: string, signal: AbortSignal) => api.get<HelpDetail>(`/help/articles/${encodeURIComponent(id)}`, { signal }),
  image: (id: string, signal: AbortSignal) => api.get<Blob>(`/help/assets/${encodeURIComponent(id)}`, { responseType: 'blob', signal }),
};
