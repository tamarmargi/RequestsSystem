import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { provideRouter } from '@angular/router';
import { Subscription, of } from 'rxjs';

import { errorInterceptor } from '../../core/error.interceptor';
import { ConflictChoice, ConflictDialog, ConflictDialogData } from './components/conflict-dialog/conflict-dialog';
import { RequestStatusActions, StatusUpdateOutcome } from './request-status-actions';
import { RequestsListState } from './requests-list.state';
import { pagedResult, requestDto } from './testing/request-fixtures';

const STATS = {
  byStatus: { New: 1, InProgress: 1, Waiting: 0, Completed: 0 },
  byPriority: { Low: 0, Medium: 1, High: 1 },
  total: 2,
};

describe('RequestStatusActions', () => {
  let actions: RequestStatusActions;
  let state: RequestsListState;
  let http: HttpTestingController;
  let subscriptions: Subscription;
  let dialogChoice: ConflictChoice | undefined;
  const dialogOpen = vi.fn();

  beforeEach(() => {
    dialogOpen.mockReset();
    dialogOpen.mockImplementation(() => ({ afterClosed: () => of(dialogChoice) }));
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        RequestsListState,
        RequestStatusActions,
        { provide: MatDialog, useValue: { open: dialogOpen } },
        { provide: MatSnackBar, useValue: { open: vi.fn() } },
      ],
    });
    actions = TestBed.inject(RequestStatusActions);
    state = TestBed.inject(RequestsListState);
    http = TestBed.inject(HttpTestingController);

    subscriptions = state.vm$.subscribe();
    subscriptions.add(state.stats$.subscribe());
    http.expectOne((r) => r.url === '/api/requests').flush(pagedResult([requestDto(42)]));
    http.expectOne('/api/requests/stats').flush(STATS);
  });

  afterEach(() => {
    subscriptions.unsubscribe();
    http.verify();
  });

  it('reloads the stats (and the list) after a successful update', () => {
    let outcome: StatusUpdateOutcome | undefined;
    actions.updateStatus(requestDto(42), 'Completed', null).subscribe((o) => (outcome = o));

    http.expectOne('/api/requests/42/status').flush(requestDto(42, { status: 'Completed', version: 'AAAAAAAAB+A=' }));

    expect(outcome).toBe('updated');
    http.expectOne('/api/requests/stats').flush(STATS);
    http.expectOne((r) => r.url === '/api/requests').flush(pagedResult([requestDto(42, { status: 'Completed' })]));
  });

  it('on 409 opens the conflict dialog with "current"; "עדכון בכל זאת" resends with the current version', () => {
    const mine = requestDto(42, { status: 'InProgress', version: 'AAAAAAAAB9E=' });
    const current = requestDto(42, { status: 'Waiting', version: 'AAAAAAAAB+A=', updatedAt: '2026-10-25T10:00:00Z' });
    dialogChoice = 'retry';

    let outcome: StatusUpdateOutcome | undefined;
    actions.updateStatus(mine, 'Completed', new Date()).subscribe((o) => (outcome = o));

    const first = http.expectOne('/api/requests/42/status');
    expect(first.request.body).toEqual({ newStatus: 'Completed', version: 'AAAAAAAAB9E=' });
    first.flush(
      { title: 'The request was modified by another user', status: 409, current },
      { status: 409, statusText: 'Conflict' },
    );

    expect(dialogOpen).toHaveBeenCalledTimes(1);
    const [component, config] = dialogOpen.mock.calls[0] as [unknown, { data: ConflictDialogData }];
    expect(component).toBe(ConflictDialog);
    expect(config.data.current).toEqual(current);
    expect(config.data.attempted).toBe('Completed');

    const retry = http.expectOne('/api/requests/42/status');
    expect(retry.request.method).toBe('PATCH');
    expect(retry.request.body).toEqual({ newStatus: 'Completed', version: 'AAAAAAAAB+A=' });
    retry.flush(requestDto(42, { status: 'Completed', version: 'AAAAAAAAB+I=' }));

    expect(outcome).toBe('updated');
    http.expectOne('/api/requests/stats').flush(STATS);
    http.expectOne((r) => r.url === '/api/requests').flush(pagedResult([requestDto(42, { status: 'Completed' })]));
  });

  it('on 409 "keep the other change" sends nothing more and shows the current state', () => {
    const current = requestDto(42, { status: 'Waiting', version: 'AAAAAAAAB+A=' });
    dialogChoice = 'keep-theirs';
    let outcome: StatusUpdateOutcome | undefined;

    actions.updateStatus(requestDto(42), 'Completed', null).subscribe((o) => (outcome = o));
    http
      .expectOne('/api/requests/42/status')
      .flush({ title: 'Conflict', status: 409, current }, { status: 409, statusText: 'Conflict' });

    expect(outcome).toBe('kept-theirs');
    http.expectNone('/api/requests/42/status');
  });
});
