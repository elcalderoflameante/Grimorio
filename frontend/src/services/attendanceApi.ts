import type { AxiosResponse } from 'axios';
import apiClient from './api';

export interface AttendanceKioskDto {
  id: string;
  name: string;
  deviceIdentifier: string;
  status: 'Pending' | 'Active' | 'Revoked';
  activatedAtUtc?: string;
  lastSeenAtUtc?: string;
  appVersion?: string;
}

export interface KioskRegistrationDto {
  kioskId: string;
  name: string;
  deviceIdentifier: string;
  apiKey: string;
}

export interface FacialEnrollmentDto {
  employeeId: string;
  employeeName: string;
  modelVersion: string;
  sampleCount: number;
  enrolledAtUtc: string;
}

export interface AttendanceAdminRowDto {
  id: string;
  employeeId: string;
  employeeName: string;
  workDate: string;
  status: 1 | 2 | 3;
  clockInTimeUtc: string;
  clockOutTimeUtc?: string;
  breakStartedAtUtc?: string;
  breakEndedAtUtc?: string;
  clockInMethod: 1 | 2 | 3;
  clockOutMethod?: 1 | 2 | 3;
  breakMinutes: number;
  lateMinutes: number;
  earlyArrivalMinutes: number;
  workedMinutes: number;
  overtimeMinutes: number;
  administrativeNotes?: string;
  correctionCount: number;
}

export interface AttendanceCorrectionDto {
  id: string;
  correctedByUserId: string;
  correctedAtUtc: string;
  reason: string;
  beforeJson: string;
  afterJson: string;
}

export const attendanceApi = {
  getKiosks: (): Promise<AxiosResponse<AttendanceKioskDto[]>> =>
    apiClient.get('/attendance/admin/kiosks'),
  registerKiosk: (data: { name: string; deviceIdentifier: string }): Promise<AxiosResponse<KioskRegistrationDto>> =>
    apiClient.post('/attendance/admin/kiosks', data),
  revokeKiosk: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.post(`/attendance/admin/kiosks/${id}/revoke`),
  getFacialEnrollments: (): Promise<AxiosResponse<FacialEnrollmentDto[]>> =>
    apiClient.get('/attendance/admin/facial-enrollments'),
  enrollEmployeeFace: (employeeId: string, samples: Blob[]): Promise<AxiosResponse<FacialEnrollmentDto>> => {
    const form = new FormData();
    samples.forEach((sample, index) => form.append('samples', sample, `sample-${index + 1}.jpg`));
    return apiClient.post(`/attendance/admin/employees/${employeeId}/face`, form, {
      // Dejar que el navegador agregue el boundary multipart. El cliente global
      // usa application/json por defecto, lo que impide a ASP.NET enlazar los archivos.
      headers: { 'Content-Type': undefined },
    });
  },
  revokeEmployeeFace: (employeeId: string): Promise<AxiosResponse<void>> =>
    apiClient.delete(`/attendance/admin/employees/${employeeId}/face`),
  getClockings: (from: string, to: string, employeeId?: string): Promise<AxiosResponse<AttendanceAdminRowDto[]>> =>
    apiClient.get('/attendance/admin/clockings', { params: { from, to, employeeId } }),
  correctClocking: (id: string, data: {
    clockInTimeUtc: string;
    clockOutTimeUtc?: string;
    breakStartedAtUtc?: string;
    breakEndedAtUtc?: string;
    reason: string;
  }): Promise<AxiosResponse<AttendanceAdminRowDto>> =>
    apiClient.put(`/attendance/admin/clockings/${id}`, data),
  createManualClocking: (data: {
    employeeId: string;
    clockInTimeUtc: string;
    clockOutTimeUtc?: string;
    breakStartedAtUtc?: string;
    breakEndedAtUtc?: string;
    reason: string;
  }): Promise<AxiosResponse<AttendanceAdminRowDto>> =>
    apiClient.post('/attendance/admin/clockings/manual', data),
  getCorrections: (id: string): Promise<AxiosResponse<AttendanceCorrectionDto[]>> =>
    apiClient.get(`/attendance/admin/clockings/${id}/corrections`),
};
