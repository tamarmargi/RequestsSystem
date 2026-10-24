import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { BulkUpdateItem, BulkUpdateRequest, BulkUpdateResult } from '../models/bulk-update.model';
import { PagedResult } from '../models/paged-result.model';
import { RequestDto, RequestStatus } from '../models/request.model';
import { RequestHistoryItem } from '../models/request-history.model';
import { RequestQuery } from '../models/request-query.model';
import { RequestStats } from '../models/request-stats.model';
import { UpdateStatusRequest } from '../models/update-status.model';

/** Typed client for /api/requests (see docs/api-contract.md). */
@Injectable({ providedIn: 'root' })
export class RequestsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/requests';

  getRequests(query: RequestQuery): Observable<PagedResult<RequestDto>> {
    return this.http.get<PagedResult<RequestDto>>(this.baseUrl, { params: toHttpParams(query) });
  }

  getById(id: number): Observable<RequestDto> {
    return this.http.get<RequestDto>(`${this.baseUrl}/${id}`);
  }

  getStats(): Observable<RequestStats> {
    return this.http.get<RequestStats>(`${this.baseUrl}/stats`);
  }

  getHistory(id: number): Observable<RequestHistoryItem[]> {
    return this.http.get<RequestHistoryItem[]>(`${this.baseUrl}/${id}/history`);
  }

  updateStatus(id: number, newStatus: RequestStatus, version: string): Observable<RequestDto> {
    const body: UpdateStatusRequest = { newStatus, version };
    return this.http.patch<RequestDto>(`${this.baseUrl}/${id}/status`, body);
  }

  bulkUpdateStatus(newStatus: RequestStatus, items: BulkUpdateItem[]): Observable<BulkUpdateResult> {
    const body: BulkUpdateRequest = { newStatus, items };
    return this.http.post<BulkUpdateResult>(`${this.baseUrl}/bulk-status`, body);
  }
}

/** Skips undefined, null and empty-string values so the server defaults apply. */
function toHttpParams(query: RequestQuery): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === '') {
      continue;
    }
    params = params.set(key, String(value));
  }
  return params;
}
