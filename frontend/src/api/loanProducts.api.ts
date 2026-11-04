import { apiClient } from './client'
import type { LoanProductSummary, LoanProductDetails, CreateLoanProductRequest } from './types'

export const loanProductsApi = {
  list: async (active?: boolean): Promise<LoanProductSummary[]> => {
    const response = await apiClient.get<LoanProductSummary[]>('/api/v1/loan-products', {
      params: { active },
    })
    return response.data
  },

  get: async (id: string): Promise<LoanProductDetails> => {
    const response = await apiClient.get<LoanProductDetails>(`/api/v1/loan-products/${id}`)
    return response.data
  },

  create: async (data: CreateLoanProductRequest): Promise<{ id: string }> => {
    const response = await apiClient.post<{ id: string }>('/api/v1/loan-products', data)
    return response.data
  },

  update: async (id: string, data: Partial<CreateLoanProductRequest>): Promise<void> => {
    await apiClient.put(`/api/v1/loan-products/${id}`, data)
  },

  updateStatus: async (id: string, active: boolean): Promise<void> => {
    await apiClient.patch(`/api/v1/loan-products/${id}/status`, { active })
  },
}
