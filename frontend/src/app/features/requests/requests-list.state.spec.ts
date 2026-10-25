import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subscription } from 'rxjs';

import { errorInterceptor } from '../../core/error.interceptor';
import { ListViewModel, RequestsListState, SEARCH_DEBOUNCE_MS } from './requests-list.state';
import { pagedResult, requestDto } from './testing/request-fixtures';

const isList = (req: { url: string; method: string }) => req.method === 'GET' && req.url === '/api/requests';

/**
 * Debounce, cancellation, page reset and error recovery of the list pipeline. Vitest fake timers drive
 * debounceTime (the app is zoneless, so fakeAsync/zone.js is not used).
 */
describe('RequestsListState', () => {
  let state: RequestsListState;
  let http: HttpTestingController;
  let subscription: Subscription;
  let vm: ListViewModel | undefined;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        RequestsListState,
      ],
    });
    state = TestBed.inject(RequestsListState);
    http = TestBed.inject(HttpTestingController);
    subscription = state.vm$.subscribe((value) => (vm = value));
  });

  afterEach(() => {
    subscription.unsubscribe();
    http.verify();
    vi.useRealTimers();
  });

  function flushInitialLoad(): void {
    http.expectOne(isList).flush(pagedResult([requestDto(1)]));
  }

  it('typing "ב", "בנ", "בנק" within 300ms sends exactly one request with search=בנק', () => {
    flushInitialLoad();

    state.searchControl.setValue('ב');
    vi.advanceTimersByTime(100);
    state.searchControl.setValue('בנ');
    vi.advanceTimersByTime(100);
    state.searchControl.setValue('בנק');
    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS - 1);
    http.expectNone(isList);

    vi.advanceTimersByTime(1);
    const requests = http.match(isList);
    expect(requests.length).toBe(1);
    expect(requests[0].request.params.get('search')).toBe('בנק');
    requests[0].flush(pagedResult([]));
  });

  it('a new query while a request is pending cancels it, and only the latest result is shown', () => {
    flushInitialLoad();

    state.filtersForm.controls.status.setValue('New');
    const first: TestRequest = http.expectOne(isList);
    expect(first.request.params.get('status')).toBe('New');

    state.filtersForm.controls.status.setValue('Waiting');
    const second = http.expectOne(isList);
    expect(first.cancelled).toBe(true);
    expect(second.request.params.get('status')).toBe('Waiting');

    second.flush(pagedResult([requestDto(7, { status: 'Waiting' })]));
    expect(vm?.status).toBe('ready');
    expect(vm?.items.map((item) => item.id)).toEqual([7]);
    expect(vm?.query.status).toBe('Waiting');
  });

  it('changing a filter resets page to 1', () => {
    flushInitialLoad();

    state.setPage(3);
    const pageThree = http.expectOne(isList);
    expect(pageThree.request.params.get('page')).toBe('3');
    pageThree.flush(pagedResult([requestDto(41)], 100));

    state.filtersForm.controls.priority.setValue('High');
    const filtered = http.expectOne(isList);
    expect(filtered.request.params.get('page')).toBe('1');
    expect(filtered.request.params.get('priority')).toBe('High');
    filtered.flush(pagedResult([requestDto(2)]));
    expect(vm?.query.page).toBe(1);
  });

  it('an HTTP error produces status "error", and a later query still works', () => {
    http.expectOne(isList).flush(
      { title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );
    expect(vm?.status).toBe('error');
    expect(vm?.error?.status).toBe(500);

    state.filtersForm.controls.status.setValue('Completed');
    http.expectOne(isList).flush(pagedResult([requestDto(9, { status: 'Completed' })]));
    expect(vm?.status).toBe('ready');
    expect(vm?.error).toBeNull();
    expect(vm?.items.map((item) => item.id)).toEqual([9]);
  });

  it('refresh() reloads the same query and keeps the current rows visible meanwhile', () => {
    flushInitialLoad();

    state.refresh();
    expect(vm?.status).toBe('loading');
    expect(vm?.refreshing).toBe(true);
    expect(vm?.items.map((item) => item.id)).toEqual([1]);
    http.expectOne(isList).flush(pagedResult([requestDto(1, { status: 'Completed' })]));
    expect(vm?.items[0].status).toBe('Completed');
  });
});
