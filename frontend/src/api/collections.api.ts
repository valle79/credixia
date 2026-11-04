import { apiClient } from './client';
import type {
  CollectionActionDto,
  OverdueLoansResponse,
  PaginatedResponseDto,
} from './types';

export interface RecordCollectionActionRequest {
  loanId: string;
  actionType: string;
  notes?: string | undefined;
  followUpDate?: string | undefined;
}

export interface ListCollectionActionsParams {
  loanId?: string | undefined;
  actionType?: string | undefined;
  pageNumber?: number | undefined;
  pageSize?: number | undefined;
}

export interface ListOverdueLoansParams {
  pageNumber?: number | undefined;
  pageSize?: number | undefined;
}

export const collectionsApi = {
  getOverdueLoans: async (params?: ListOverdueLoansParams): Promise<OverdueLoansResponse> => {
    const { data } = await apiClient.get('/api/v1/collections/overdue', { params });
    return data;
  },

  listActions: async (params?: ListCollectionActionsParams): Promise<PaginatedResponseDto<CollectionActionDto>> => {
    const { data } = await apiClient.get('/api/v1/collections/actions', { params });
    return data;
  },

  recordAction: async (request: RecordCollectionActionRequest): Promise<CollectionActionDto> => {
    const { data } = await apiClient.post('/api/v1/collections/actions', request);
    return data;
  },
};
