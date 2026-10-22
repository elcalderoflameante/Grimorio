import api from './api';

export interface ResponsibilityNamed { id: string; name: string; isActive: boolean }
export interface ResponsibilityCheck { key: string; label: string; options: string[]; attentionOptions: string[] }
export interface ResponsibilityTask {
  id: string; areaId: string; positionId: string; name: string; instructions: string;
  frequency: string; stage: string; photoMode: string; anchorDate: string;
  daysOfWeekMask: number; everyNDays: number; intervalMinutes?: number | null;
  windowMinutes: number; isActive: boolean; checklist: ResponsibilityCheck[];
}
export interface ResponsibilityAssignment {
  id: string; positionId: string; employeeId: string; fromDate: string; toDate: string;
  shiftStartTime: string; isActive: boolean;
}
export interface ResponsibilityEmployee { id: string; name: string; hasUser: boolean }
export interface ResponsibilityEvidence { id: string; phase: string; capturedAtUtc?: string; receivedAtUtc?: string }
export interface ResponsibilityActivity {
  id: string; employeeId: string; employeeName: string; positionName: string; areaName: string;
  taskName: string; instructions: string; stage: string; photoMode: string; status: string;
  workDate: string; availableAtUtc: string; dueAtUtc: string; completedAtUtc?: string;
  notes?: string; checklist: ResponsibilityCheck[]; answers: { key: string; value: string }[];
  evidence: ResponsibilityEvidence[];
}
export interface ResponsibilityCatalog {
  areas: ResponsibilityNamed[]; positions: ResponsibilityNamed[]; employees: ResponsibilityEmployee[];
  tasks: ResponsibilityTask[]; assignments: ResponsibilityAssignment[];
}
export interface ResponsibilityDay {
  workDate: string; message?: string; activities: ResponsibilityActivity[];
  warnings: { positionId: string; positionName: string; workDate: string; shiftStartTime: string; message: string }[];
}
const base = '/operations/responsibilities';
export const responsibilityApi = {
  catalog: () => api.get<ResponsibilityCatalog>(`${base}/catalog`),
  day: (date: string) => api.get<ResponsibilityDay>(`${base}/day`, { params: { date } }),
  saveNamed: (kind: 'areas' | 'positions', value: Partial<ResponsibilityNamed>) => value.id
    ? api.put(`${base}/${kind}/${value.id}`, value) : api.post(`${base}/${kind}`, value),
  saveTask: (value: Partial<ResponsibilityTask>) => value.id
    ? api.put(`${base}/tasks/${value.id}`, value) : api.post(`${base}/tasks`, value),
  saveAssignment: (value: Partial<ResponsibilityAssignment>) => value.id
    ? api.put(`${base}/assignments/${value.id}`, value) : api.post(`${base}/assignments`, value),
  transfer: (id: string, employeeId: string, reason: string) => api.post(`${base}/activities/${id}/transfer`, { employeeId, reason }),
  evidence: (id: string) => api.get<Blob>(`${base}/evidence/${id}`, { responseType: 'blob' }),
};
