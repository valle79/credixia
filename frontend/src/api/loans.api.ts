import { apiClient } from './client'
import type {
  LoanSummary,
  LoanDetails,
  CreateLoanRequest,
  RegisterPaymentRequest,
  PaymentScheduleItem,
  PaginatedResponse,
} from './types'

export interface ListLoansParams {
  customerId?: string | undefined
  productId?: string | undefined
  status?: string | undefined
  search?: string | undefined
  fromDate?: string | undefined
  toDate?: string | undefined
  overdueOnly?: boolean | undefined
  sortBy?: string | undefined
  sortDirection?: 'asc' | 'desc' | undefined
  pageNumber?: number | undefined
  pageSize?: number | undefined
}

export const loansApi = {
  list: async (params?: ListLoansParams): Promise<PaginatedResponse<LoanSummary>> => {
    const response = await apiClient.get<PaginatedResponse<LoanSummary>>('/api/v1/loans', { params })
    return response.data
  },

  get: async (id: string): Promise<LoanDetails> => {
    const response = await apiClient.get<LoanDetails>(`/api/v1/loans/${id}`)
    return response.data
  },

  create: async (data: CreateLoanRequest): Promise<{ id: string; loanNumber: string }> => {
    const response = await apiClient.post<{ id: string; loanNumber: string }>('/api/v1/loans', data)
    return response.data
  },

  approve: async (id: string): Promise<void> => {
    await apiClient.post(`/api/v1/loans/${id}/approve`)
  },

  reject: async (id: string): Promise<void> => {
    await apiClient.post(`/api/v1/loans/${id}/reject`)
  },

  disburse: async (id: string): Promise<void> => {
    await apiClient.post(`/api/v1/loans/${id}/disburse`)
  },

  registerPayment: async (id: string, data: RegisterPaymentRequest): Promise<void> => {
    await apiClient.post(`/api/v1/loans/${id}/payments`, data)
  },

  getSchedule: async (id: string): Promise<PaymentScheduleItem[]> => {
    const response = await apiClient.get<PaymentScheduleItem[]>(`/api/v1/loans/${id}/schedule`)
    return response.data
  },
}
