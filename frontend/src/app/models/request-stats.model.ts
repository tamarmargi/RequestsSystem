import { RequestPriority, RequestStatus } from './request.model';

export interface RequestStats {
  byStatus: Record<RequestStatus, number>;
  byPriority: Record<RequestPriority, number>;
  total: number;
}
