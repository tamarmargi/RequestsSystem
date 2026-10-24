import { RequestStatus } from './request.model';

export interface RequestHistoryItem {
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  /** UTC ISO 8601. */
  changedAt: string;
  changedBy: string;
}
