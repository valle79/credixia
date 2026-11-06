import { apiClient } from './client'
import type {
  LoanSummary,
  LoanDetails,
  CreateLoanRequest,
  RegisterPaymentRequest,
  PaymentScheduleItem,
  PaginatedResponse,
  CalculateSettlementResponse,
  SettleLoanRequest,
  SettleLoanResponse,
  SimulateRefinancingRequest,
  SimulateRefinancingResponse,
  RefinanceLoanRequest,
  RefinanceLoanResponse,
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

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/v1/loans/${id}`)
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

  getSchedulePdf: async (id: string): Promise<Blob> => {
    const response = await apiClient.get(`/api/v1/loans/${id}/schedule/pdf`, {
      responseType: 'blob',
    })
    return response.data
  },

  // Early Settlement
  calculateSettlement: async (
    id: string,
    settlementDate?: string
  ): Promise<CalculateSettlementResponse> => {
    const params = settlementDate ? { settlementDate } : undefined
    const response = await apiClient.post<CalculateSettlementResponse>(
      `/api/v1/loans/${id}/settlement/calculate`,
      null,
      { params }
    )
    return response.data
  },

  settleLoan: async (id: string, data: SettleLoanRequest): Promise<SettleLoanResponse> => {
    const response = await apiClient.post<SettleLoanResponse>(
      `/api/v1/loans/${id}/settlement`,
      data
    )
    return response.data
  },

  // Refinancing
  simulateRefinancing: async (
    id: string,
    data: SimulateRefinancingRequest
  ): Promise<SimulateRefinancingResponse> => {
    const response = await apiClient.post<SimulateRefinancingResponse>(
      `/api/v1/loans/${id}/refinance/simulate`,
      data
    )
    return response.data
  },

  refinanceLoan: async (
    id: string,
    data: RefinanceLoanRequest
  ): Promise<RefinanceLoanResponse> => {
    const response = await apiClient.post<RefinanceLoanResponse>(
      `/api/v1/loans/${id}/refinance`,
      data
    )
    return response.data
  },
}
