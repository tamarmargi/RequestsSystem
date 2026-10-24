import { RequestPriority, RequestStatus } from './request.model';

export type RequestSortBy = 'createdAt' | 'updatedAt' | 'priority' | 'title' | 'organizationName';

export type SortDirection = 'asc' | 'desc';

/** Query parameters of GET /api/requests. Omitted values fall back to the server defaults. */
export interface RequestQuery {
  /** Default 1. */
  page?: number;
  /** Default 20, max 100. */
  pageSize?: number;
  /** Matches title or organizationName. */
  search?: string;
  status?: RequestStatus;
  priority?: RequestPriority;
  organizationName?: string;
  assignedTo?: string;
  /** ISO 8601 date, inclusive. */
  createdFrom?: string;
  /** ISO 8601 date, inclusive. */
  createdTo?: string;
  /** Default createdAt. */
  sortBy?: RequestSortBy;
  /** Default desc. */
  sortDirection?: SortDirection;
}
