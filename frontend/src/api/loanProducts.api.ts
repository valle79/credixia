import { apiClient } from './client'
import type { LoanProductSummary, LoanProductDetails, CreateLoanProductRequest } from './types'

export interface ListLoanProductsParams {
  active?: boolean
}

export const loanProductsApi = {
  list: async (params?: ListLoanProductsParams): Promise<LoanProductSummary[]> => {
    const response = await apiClient.get<LoanProductSummary[]>('/api/v1/loan-products', {
      params,
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

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/v1/loan-products/${id}`)
  },
}
