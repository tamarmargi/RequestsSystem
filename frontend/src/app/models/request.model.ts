/** Mirrors RequestDto in docs/api-contract.md. */
export type RequestStatus = 'New' | 'InProgress' | 'Waiting' | 'Completed';

export type RequestPriority = 'Low' | 'Medium' | 'High';

export interface RequestDto {
  id: number;
  title: string;
  organizationName: string;
  status: RequestStatus;
  priority: RequestPriority;
  assignedTo: string | null;
  /** UTC ISO 8601. */
  createdAt: string;
  /** UTC ISO 8601. */
  updatedAt: string;
  /** RowVersion as Base64; opaque, sent back unchanged on updates. */
  version: string;
}
