import { type AxiosResponse } from 'axios';
import type {
  SpecialDateDto,
  CreateSpecialDateDto,
  UpdateSpecialDateDto,
} from '../types/SpecialDate';
import apiClient from './api';

export const specialDateApi = {
  getAll: (branchId: string): Promise<AxiosResponse<SpecialDateDto[]>> =>
    apiClient.get<SpecialDateDto[]>('/scheduling/special-dates', { params: { branchId } }),
  
  getById: (id: string): Promise<AxiosResponse<SpecialDateDto>> =>
    apiClient.get<SpecialDateDto>(`/scheduling/special-dates/${id}`),
  
  create: (data: CreateSpecialDateDto): Promise<AxiosResponse<SpecialDateDto>> =>
    apiClient.post<SpecialDateDto>('/scheduling/special-dates', data),
  
  update: (id: string, data: UpdateSpecialDateDto): Promise<AxiosResponse<SpecialDateDto>> =>
    apiClient.put<SpecialDateDto>(`/scheduling/special-dates/${id}`, data),
  
  delete: (id: string): Promise<AxiosResponse<void>> =>
    apiClient.delete<void>(`/scheduling/special-dates/${id}`),
};
