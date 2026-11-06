import { apiClient } from './client'
import type {
  CustomerSummary,
  CustomerDetails,
  CreateCustomerRequest,
  PaginatedResponse,
} from './types'

export interface ListCustomersParams {
  search?: string | undefined
  status?: string | undefined
  sortBy?: string | undefined
  sortDirection?: 'asc' | 'desc' | undefined
  pageNumber?: number | undefined
  pageSize?: number | undefined
}

export const customersApi = {
  list: async (params?: ListCustomersParams): Promise<PaginatedResponse<CustomerSummary>> => {
    const response = await apiClient.get<PaginatedResponse<CustomerSummary>>('/api/v1/customers', { params })
    return response.data
  },

  get: async (id: string): Promise<CustomerDetails> => {
    const response = await apiClient.get<CustomerDetails>(`/api/v1/customers/${id}`)
    return response.data
  },

  create: async (data: CreateCustomerRequest): Promise<{ id: string }> => {
    const response = await apiClient.post<{ id: string }>('/api/v1/customers', data)
    return response.data
  },

  update: async (id: string, data: Partial<CreateCustomerRequest>): Promise<void> => {
    await apiClient.put(`/api/v1/customers/${id}`, data)
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/v1/customers/${id}`)
  },

  updateStatus: async (id: string, status: 'Active' | 'Suspended' | 'Blocked'): Promise<void> => {
    await apiClient.patch(`/api/v1/customers/${id}/status`, { status })
  },

  addReference: async (id: string, data: { fullName: string; relationship?: string | undefined; phone: string }): Promise<void> => {
    await apiClient.post(`/api/v1/customers/${id}/references`, data)
  },

  removeReference: async (id: string, referenceId: string): Promise<void> => {
    await apiClient.delete(`/api/v1/customers/${id}/references/${referenceId}`)
  },

  uploadPhoto: async (id: string, file: File): Promise<{ photoUrl: string }> => {
    const formData = new FormData()
    formData.append('photo', file)
    const response = await apiClient.post<{ photoUrl: string }>(
      `/api/v1/customers/${id}/photo`,
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    )
    return response.data
  },

  checkDocumentAvailability: async (
    documentType: string,
    documentNumber: string
  ): Promise<{ available: boolean; message: string }> => {
    const response = await apiClient.get<{ available: boolean; message: string }>(
      '/api/v1/customers/check-document',
      { params: { documentType, documentNumber } }
    )
    return response.data
  },
}
