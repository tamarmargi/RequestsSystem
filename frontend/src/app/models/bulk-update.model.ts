import { RequestStatus } from './request.model';

export interface BulkUpdateItem {
  id: number;
  version: string;
}

export interface BulkUpdateRequest {
  newStatus: RequestStatus;
  /** 1–100 items, no duplicate ids. */
  items: BulkUpdateItem[];
}

export type BulkUpdateOutcome = 'Updated' | 'NotFound' | 'Conflict' | 'InvalidTransition';

export interface BulkUpdateItemResult {
  id: number;
  outcome: BulkUpdateOutcome;
  message?: string;
}

export interface BulkUpdateResult {
  succeeded: number;
  failed: number;
  results: BulkUpdateItemResult[];
}
