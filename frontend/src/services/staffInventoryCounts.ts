import api from './api';
import type { CountArea, CountCatalog, StaffCount } from '../types/staffInventoryCounts';

const root = '/inventory/conteos';
export const staffInventoryCountsApi = {
  catalog: () => api.get<CountCatalog>(`${root}/configuracion`),
  saveArea: (value: CountArea) => value.id
    ? api.put<CountArea>(`${root}/areas/${value.id}`, value)
    : api.post<CountArea>(`${root}/areas`, { ...value, id: undefined }),
  list: (params: { from: string; to: string; status?: string }) => api.get<StaffCount[]>(root, { params }),
  detail: (id: string) => api.get<StaffCount>(`${root}/${id}`),
  review: (id: string, decision: string, notes: string) => api.post<StaffCount>(`${root}/${id}/revisar`, { decision, notes }),
};
