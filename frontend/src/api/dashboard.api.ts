import { apiClient } from './client'
import type { DashboardSummary } from './types'

export const dashboardApi = {
  getSummary: async (): Promise<DashboardSummary> => {
    const response = await apiClient.get<DashboardSummary>('/api/v1/dashboard/summary')
    return response.data
  },
}
