import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { RequestsApiService } from './requests-api.service';

describe('RequestsApiService', () => {
  let service: RequestsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(RequestsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getRequests sends only the parameters that have values', () => {
    service.getRequests({ page: 2, pageSize: 50, status: 'Waiting', search: '' }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/requests');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().sort()).toEqual(['page', 'pageSize', 'status']);
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.params.get('status')).toBe('Waiting');
    expect(req.request.params.has('search')).toBe(false);
    req.flush({ items: [], totalCount: 0, page: 2, pageSize: 50 });
  });

  it('updateStatus sends PATCH to /api/requests/{id}/status with newStatus and version', () => {
    service.updateStatus(42, 'Completed', 'AAAAAAAAB9E=').subscribe();

    const req = http.expectOne('/api/requests/42/status');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ newStatus: 'Completed', version: 'AAAAAAAAB9E=' });
    req.flush({});
  });
});
