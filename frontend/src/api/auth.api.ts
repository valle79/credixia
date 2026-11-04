import { apiClient } from './client'
import type { LoginRequest, LoginResponse, MeResponse, SessionResponse } from './types'

export const authApi = {
  login: async (data: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/api/v1/auth/login', data)
    return response.data
  },

  logout: async (): Promise<void> => {
    await apiClient.post('/api/v1/auth/logout')
  },

  refresh: async (): Promise<SessionResponse> => {
    const response = await apiClient.post<SessionResponse>('/api/v1/auth/refresh')
    return response.data
  },

  me: async (): Promise<MeResponse> => {
    const response = await apiClient.get<MeResponse>('/api/v1/auth/me')
    return response.data
  },

  switchTenant: async (tenantId: string): Promise<SessionResponse> => {
    const response = await apiClient.post<SessionResponse>('/api/v1/auth/tenant-context', { tenantId })
    return response.data
  },

  forgotPassword: async (email: string): Promise<void> => {
    await apiClient.post('/api/v1/auth/forgot-password', { email })
  },

  resetPassword: async (token: string, newPassword: string): Promise<void> => {
    await apiClient.post('/api/v1/auth/reset-password', { token, newPassword })
  },
}
