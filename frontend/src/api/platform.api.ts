import { apiClient } from './client'
import type { 
  PlatformTenantSummary, 
  PlatformDashboard,
  CreateTenantResponse,
  WhatsAppCredentialsResponse,
  TenantDetails,
  UpdateTenantRequest,
  ResetPasswordResponse
} from './types'

export interface CreateTenantRequest {
  slug: string
  name: string
  ownerEmail: string
  ownerFullName: string
  ownerPhone: string
}

export const platformApi = {
  getDashboard: async (): Promise<PlatformDashboard> => {
    const { data } = await apiClient.get('/api/v1/platform/dashboard')
    return data
  },

  listTenants: async (): Promise<PlatformTenantSummary[]> => {
    const { data } = await apiClient.get('/api/v1/platform/tenants')
    return data
  },

  createTenant: async (request: CreateTenantRequest): Promise<CreateTenantResponse> => {
    const { data } = await apiClient.post('/api/v1/platform/tenants', request)
    return data
  },

  updateTenantStatus: async (id: string, suspend: boolean): Promise<PlatformTenantSummary> => {
    const { data } = await apiClient.patch(`/api/v1/platform/tenants/${id}/status`, { suspend })
    return data
  },
  
  sendCredentialsViaWhatsApp: async (
    id: string, 
    phoneNumber: string, 
    password: string
  ): Promise<WhatsAppCredentialsResponse> => {
    const { data } = await apiClient.post(`/api/v1/platform/tenants/${id}/send-credentials`, { 
      phoneNumber,
      password 
    })
    return data
  },

  getTenantDetails: async (id: string): Promise<TenantDetails> => {
    const { data } = await apiClient.get(`/api/v1/platform/tenants/${id}`)
    return data
  },

  updateTenant: async (id: string, request: UpdateTenantRequest): Promise<TenantDetails> => {
    const { data} = await apiClient.put(`/api/v1/platform/tenants/${id}`, request)
    return data
  },

  resetOwnerPassword: async (id: string, newPassword?: string): Promise<ResetPasswordResponse> => {
    const { data } = await apiClient.post(`/api/v1/platform/tenants/${id}/reset-password`, { newPassword })
    return data
  },
}
