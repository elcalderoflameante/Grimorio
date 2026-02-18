import type { AxiosResponse } from 'axios';
import apiClient from './api';
import type {
  SpecialDateTemplateDto,
  CreateSpecialDateTemplateDto,
  UpdateSpecialDateTemplateDto,
} from '../types';

export const specialDateTemplateApi = {
  /**
   * Get all templates for a specific special date
   */
  async getBySpecialDateId(specialDateId: string): Promise<AxiosResponse<SpecialDateTemplateDto[]>> {
    return apiClient.get(`/scheduling/special-dates/${specialDateId}/templates`);
  },

  /**
   * Get a specific template by ID
   */
  async getById(id: string): Promise<AxiosResponse<SpecialDateTemplateDto>> {
    return apiClient.get(`/scheduling/special-date-templates/${id}`);
  },

  /**
   * Create a new template for a special date
   */
  async create(data: CreateSpecialDateTemplateDto): Promise<AxiosResponse<SpecialDateTemplateDto>> {
    return apiClient.post(`/scheduling/special-date-templates`, data);
  },

  /**
   * Update an existing special date template
   */
  async update(id: string, data: UpdateSpecialDateTemplateDto): Promise<AxiosResponse<SpecialDateTemplateDto>> {
    return apiClient.put(`/scheduling/special-date-templates/${id}`, data);
  },

  /**
   * Delete a special date template
   */
  async delete(id: string): Promise<AxiosResponse<void>> {
    return apiClient.delete(`/scheduling/special-date-templates/${id}`);
  },
};
