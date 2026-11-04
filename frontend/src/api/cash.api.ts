import { apiClient } from './client';
import type {
  CashSessionDto,
  CashTransactionDto,
  PaginatedResponseDto,
} from './types';

export interface OpenCashSessionRequest {
  openingBalance: number;
  notes?: string | undefined;
}

export interface CloseCashSessionRequest {
  closingBalance: number;
  notes?: string | undefined;
}

export interface ListCashSessionsParams {
  status?: string | undefined;
  pageNumber?: number | undefined;
  pageSize?: number | undefined;
}

export const cashApi = {
  list: async (params?: ListCashSessionsParams): Promise<PaginatedResponseDto<CashSessionDto>> => {
    const { data } = await apiClient.get('/api/v1/cash', { params });
    return data;
  },

  getById: async (id: string): Promise<CashSessionDto> => {
    const { data } = await apiClient.get(`/api/v1/cash/${id}`);
    return data;
  },

  getCurrent: async (): Promise<CashSessionDto | null> => {
    const { data } = await apiClient.get('/api/v1/cash/current');
    return data;
  },

  open: async (request: OpenCashSessionRequest): Promise<CashSessionDto> => {
    const { data } = await apiClient.post('/api/v1/cash/open', request);
    return data;
  },

  close: async (id: string, request: CloseCashSessionRequest): Promise<CashSessionDto> => {
    const { data } = await apiClient.post(`/api/v1/cash/${id}/close`, request);
    return data;
  },

  getTransactions: async (id: string): Promise<CashTransactionDto[]> => {
    const { data } = await apiClient.get(`/api/v1/cash/${id}/transactions`);
    return data;
  },
};
