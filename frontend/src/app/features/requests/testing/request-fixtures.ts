import { PagedResult } from '../../../models/paged-result.model';
import { RequestDto } from '../../../models/request.model';

/** Test data builders shared by the feature specs. */
export function requestDto(id: number, overrides: Partial<RequestDto> = {}): RequestDto {
  return {
    id,
    title: `פנייה ${id}`,
    organizationName: 'עיריית חיפה',
    status: 'InProgress',
    priority: 'High',
    assignedTo: null,
    createdAt: '2026-09-01T08:00:00Z',
    updatedAt: '2026-09-02T08:00:00Z',
    version: 'AAAAAAAAB9E=',
    ...overrides,
  };
}

export function pagedResult(items: RequestDto[], totalCount = items.length): PagedResult<RequestDto> {
  return { items, totalCount, page: 1, pageSize: 20 };
}
