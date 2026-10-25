import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ApiError } from './api-error';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let client: HttpClient;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([errorInterceptor])), provideHttpClientTesting()],
    });
    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('converts a 409 ProblemDetails response into an ApiError and keeps "current"', () => {
    const current = { id: 42, status: 'Waiting', version: 'AAAAAAAAB+A=' };
    let error: ApiError | undefined;

    client.patch('/api/requests/42/status', {}).subscribe({ error: (e: ApiError) => (error = e) });

    http.expectOne('/api/requests/42/status').flush(
      {
        type: 'https://tools.ietf.org/html/rfc9110#section-15.5.10',
        title: 'The request was modified by another user.',
        status: 409,
        detail: 'The version you sent is out of date.',
        current,
      },
      { status: 409, statusText: 'Conflict' },
    );

    expect(error).toEqual({
      status: 409,
      title: 'The request was modified by another user.',
      detail: 'The version you sent is out of date.',
      current,
    });
  });

  it('maps a network failure to status 0 "server unreachable"', () => {
    let error: ApiError | undefined;

    client.get('/api/health').subscribe({ error: (e: ApiError) => (error = e) });
    http.expectOne('/api/health').error(new ProgressEvent('error'));

    expect(error).toEqual({ status: 0, title: 'server unreachable' });
  });

  it('maps a 5xx without a ProblemDetails body (dev proxy, API down) to status 0', () => {
    let error: ApiError | undefined;

    client.get('/api/requests').subscribe({ error: (e: ApiError) => (error = e) });
    http.expectOne('/api/requests').flush(null, { status: 500, statusText: 'Internal Server Error' });

    expect(error).toEqual({ status: 0, title: 'server unreachable' });
  });

  it('keeps a 500 that comes from the API (ProblemDetails body)', () => {
    let error: ApiError | undefined;

    client.get('/api/requests').subscribe({ error: (e: ApiError) => (error = e) });
    http
      .expectOne('/api/requests')
      .flush({ title: 'An unexpected error occurred.', status: 500 }, { status: 500, statusText: 'Internal Server Error' });

    expect(error).toEqual({ status: 500, title: 'An unexpected error occurred.' });
  });
});
