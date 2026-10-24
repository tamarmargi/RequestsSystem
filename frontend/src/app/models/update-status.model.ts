import { RequestStatus } from './request.model';

/** Body of PATCH /api/requests/{id}/status. */
export interface UpdateStatusRequest {
  newStatus: RequestStatus;
  version: string;
}
