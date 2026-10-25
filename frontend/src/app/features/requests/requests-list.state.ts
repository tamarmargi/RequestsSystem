import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, ParamMap, Params, Router } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  filter,
  map,
  merge,
  of,
  scan,
  shareReplay,
  startWith,
  switchMap,
  tap,
  withLatestFrom,
} from 'rxjs';

import { ApiError, isApiError } from '../../core/api-error';
import { PagedResult } from '../../models/paged-result.model';
import { RequestDto, RequestPriority, RequestStatus } from '../../models/request.model';
import { RequestQuery, RequestSortBy, SortDirection } from '../../models/request-query.model';
import { RequestStats } from '../../models/request-stats.model';
import { PRIORITIES, STATUSES } from '../../shared/request-labels';
import { RequestsApiService } from '../../services/requests-api.service';

export const SEARCH_DEBOUNCE_MS = 300;
export const PAGE_SIZES = [20, 50, 100] as const;
export const DEFAULT_PAGE_SIZE = 20;
export const SORTABLE_FIELDS: readonly RequestSortBy[] = ['createdAt', 'updatedAt', 'priority', 'title', 'organizationName'];

/**
 * The full list query. Every field is always present ('' / null = no filter), so two queries can be compared
 * field by field.
 */
export interface ListQuery {
  page: number;
  pageSize: number;
  search: string;
  status: RequestStatus | null;
  priority: RequestPriority | null;
  organizationName: string;
  assignedTo: string;
  /** yyyy-MM-dd or ''. */
  createdFrom: string;
  /** yyyy-MM-dd or ''. */
  createdTo: string;
  sortBy: RequestSortBy;
  sortDirection: SortDirection;
}

export const DEFAULT_QUERY: ListQuery = {
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  search: '',
  status: null,
  priority: null,
  organizationName: '',
  assignedTo: '',
  createdFrom: '',
  createdTo: '',
  sortBy: 'createdAt',
  sortDirection: 'desc',
};

/** A filter that can be removed on its own (one chip each). */
export type FilterKey = 'status' | 'priority' | 'organizationName' | 'assignedTo' | 'dateRange';

export interface ListViewModel {
  status: 'loading' | 'ready' | 'error';
  /** The query this state belongs to (set as soon as loading starts). */
  query: ListQuery;
  items: RequestDto[];
  totalCount: number;
  error: ApiError | null;
  /** Loading after refresh(): the previous rows stay visible. */
  refreshing: boolean;
  /** When the rows were loaded from the server (shown in the conflict dialog). */
  loadedAt: Date | null;
}

export interface StatsViewModel {
  status: 'loading' | 'ready' | 'error';
  /** The last loaded stats; kept while reloading. */
  stats: RequestStats | null;
  error: ApiError | null;
}

type QueryUpdate = (query: ListQuery) => ListQuery;

type LoadEvent =
  | { type: 'loading'; query: ListQuery; refresh: boolean }
  | { type: 'loaded'; query: ListQuery; page: PagedResult<RequestDto> }
  | { type: 'failed'; query: ListQuery; error: ApiError }
  | { type: 'patched'; item: RequestDto };

export function createFiltersForm() {
  return new FormGroup(
    {
      status: new FormControl<RequestStatus | null>(null),
      priority: new FormControl<RequestPriority | null>(null),
      organizationName: new FormControl('', { nonNullable: true }),
      assignedTo: new FormControl('', { nonNullable: true }),
      createdFrom: new FormControl('', { nonNullable: true }),
      createdTo: new FormControl('', { nonNullable: true }),
    },
    { validators: dateRangeValidator },
  );
}

export type FiltersForm = ReturnType<typeof createFiltersForm>;

/**
 * State of the requests screen (provided in the page component, no NgRx).
 *
 * Inputs (search, filters, sort, page) are folded into one ListQuery by a scan reducer:
 *   text inputs → trim → debounceTime(300) → distinctUntilChanged
 *   any criteria change resets page to 1 in the same step, so one change = one query
 * The query then goes through distinctUntilChanged (field-by-field) → switchMap(getRequests) so a newer query
 * cancels the request in flight; each request has its own catchError + startWith(loading), so the stream never
 * dies. The query is mirrored to the URL.
 */
@Injectable()
export class RequestsListState {
  private readonly api = inject(RequestsApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly filtersForm: FiltersForm = createFiltersForm();

  /** Selected request ids; selection is per page and is cleared when the query changes. */
  readonly selectedIds = signal<ReadonlySet<number>>(new Set());

  private readonly commands$ = new Subject<QueryUpdate>();
  private readonly refresh$ = new Subject<void>();
  private readonly refreshStats$ = new Subject<void>();
  private readonly patches$ = new Subject<RequestDto>();

  readonly query$: Observable<ListQuery>;
  readonly vm$: Observable<ListViewModel>;
  readonly stats$: Observable<StatsViewModel>;

  constructor() {
    const initial = queryFromParams(this.route.snapshot.queryParamMap);
    this.syncControls(initial);

    this.query$ = this.queryUpdates().pipe(
      scan((query, update) => update(query), initial),
      startWith(initial),
      distinctUntilChanged(queryEquals),
      tap(() => this.clearSelection()),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

    const loads$ = merge(
      this.query$.pipe(map((query) => ({ query, refresh: false }))),
      this.refresh$.pipe(
        withLatestFrom(this.query$),
        map(([, query]) => ({ query, refresh: true })),
      ),
    ).pipe(
      switchMap(({ query, refresh }) =>
        this.api.getRequests(toRequestQuery(query)).pipe(
          map((page): LoadEvent => ({ type: 'loaded', query, page })),
          catchError((error: unknown) => of<LoadEvent>({ type: 'failed', query, error: toApiError(error) })),
          startWith<LoadEvent>({ type: 'loading', query, refresh }),
        ),
      ),
    );

    this.vm$ = merge(loads$, this.patches$.pipe(map((item): LoadEvent => ({ type: 'patched', item })))).pipe(
      tap((event) => {
        if (event.type === 'loaded') {
          this.afterLoad(event.query, event.page);
        }
      }),
      scan(reduceViewModel, initialViewModel(initial)),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

    this.stats$ = this.refreshStats$.pipe(
      startWith(undefined),
      switchMap(() =>
        this.api.getStats().pipe(
          map((stats): StatsViewModel => ({ status: 'ready', stats, error: null })),
          catchError((error: unknown) => of<StatsViewModel>({ status: 'error', stats: null, error: toApiError(error) })),
          startWith<StatsViewModel>({ status: 'loading', stats: null, error: null }),
        ),
      ),
      // While reloading, keep showing the previous numbers.
      scan((previous: StatsViewModel, next: StatsViewModel): StatsViewModel =>
        next.status === 'loading' ? { ...previous, status: 'loading' } : next,
      ),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

    // Keep the query in the URL so refresh and shared links keep the state.
    this.query$.pipe(takeUntilDestroyed()).subscribe((query) => {
      void this.router.navigate([], { relativeTo: this.route, queryParams: queryToParams(query), replaceUrl: true });
    });
  }

  setSort(sortBy: RequestSortBy, sortDirection: SortDirection): void {
    this.commands$.next(withCriteria({ sortBy, sortDirection }));
  }

  setPage(page: number): void {
    this.commands$.next((query) => ({ ...query, page }));
  }

  setPageSize(pageSize: number): void {
    this.commands$.next((query) => (query.pageSize === pageSize ? query : { ...query, pageSize, page: 1 }));
  }

  /** Clears the search and all filters in one step (keeps sort and page size). */
  clearFilters(): void {
    const { search, status, priority, organizationName, assignedTo, createdFrom, createdTo } = DEFAULT_QUERY;
    this.commands$.next(withCriteria({ search, status, priority, organizationName, assignedTo, createdFrom, createdTo }));
    this.searchControl.setValue('');
    this.filtersForm.reset();
  }

  removeFilter(key: FilterKey): void {
    const controls = this.filtersForm.controls;
    switch (key) {
      case 'status':
        this.commands$.next(withCriteria({ status: null }));
        controls.status.setValue(null);
        break;
      case 'priority':
        this.commands$.next(withCriteria({ priority: null }));
        controls.priority.setValue(null);
        break;
      case 'organizationName':
        this.commands$.next(withCriteria({ organizationName: '' }));
        controls.organizationName.setValue('');
        break;
      case 'assignedTo':
        this.commands$.next(withCriteria({ assignedTo: '' }));
        controls.assignedTo.setValue('');
        break;
      case 'dateRange':
        this.commands$.next(withCriteria({ createdFrom: '', createdTo: '' }));
        this.filtersForm.patchValue({ createdFrom: '', createdTo: '' });
        break;
    }
  }

  /** Reloads the current query (after updates); the current rows stay visible meanwhile. */
  refresh(): void {
    this.refresh$.next();
  }

  refreshStats(): void {
    this.refreshStats$.next();
  }

  /** Replaces one row with a newer version from the server (e.g. the "current" of a 409). */
  patchItem(item: RequestDto): void {
    this.patches$.next(item);
  }

  toggleSelection(id: number): void {
    const next = new Set(this.selectedIds());
    if (!next.delete(id)) {
      next.add(id);
    }
    this.selectedIds.set(next);
  }

  setPageSelection(items: readonly RequestDto[], selected: boolean): void {
    this.selectedIds.set(selected ? new Set(items.map((item) => item.id)) : new Set());
  }

  select(ids: Iterable<number>): void {
    this.selectedIds.set(new Set(ids));
  }

  clearSelection(): void {
    if (this.selectedIds().size > 0) {
      this.selectedIds.set(new Set());
    }
  }

  private queryUpdates(): Observable<QueryUpdate> {
    const controls = this.filtersForm.controls;
    const text$ = (control: FormControl<string>) =>
      control.valueChanges.pipe(
        map((value) => value.trim()),
        debounceTime(SEARCH_DEBOUNCE_MS),
        distinctUntilChanged(),
      );

    // The form-level validity is updated after the child control emits, so dates are read from the group.
    const dateRange$ = this.filtersForm.valueChanges.pipe(
      filter(() => !this.filtersForm.hasError('dateRange')),
      map(() => ({ createdFrom: controls.createdFrom.value, createdTo: controls.createdTo.value })),
      distinctUntilChanged((a, b) => a.createdFrom === b.createdFrom && a.createdTo === b.createdTo),
    );

    return merge(
      text$(this.searchControl).pipe(map((search) => withCriteria({ search }))),
      text$(controls.organizationName).pipe(map((organizationName) => withCriteria({ organizationName }))),
      text$(controls.assignedTo).pipe(map((assignedTo) => withCriteria({ assignedTo }))),
      controls.status.valueChanges.pipe(map((status) => withCriteria({ status }))),
      controls.priority.valueChanges.pipe(map((priority) => withCriteria({ priority }))),
      dateRange$.pipe(map((range) => withCriteria(range))),
      this.commands$,
    );
  }

  private syncControls(query: ListQuery): void {
    this.searchControl.setValue(query.search, { emitEvent: false });
    this.filtersForm.setValue(
      {
        status: query.status,
        priority: query.priority,
        organizationName: query.organizationName,
        assignedTo: query.assignedTo,
        createdFrom: query.createdFrom,
        createdTo: query.createdTo,
      },
      { emitEvent: false },
    );
  }

  private afterLoad(query: ListQuery, page: PagedResult<RequestDto>): void {
    // Keep only selected rows that are still on the page (e.g. after "reload the failed ones").
    const ids = new Set(page.items.map((item) => item.id));
    const kept = [...this.selectedIds()].filter((id) => ids.has(id));
    if (kept.length !== this.selectedIds().size) {
      this.selectedIds.set(new Set(kept));
    }

    // A page past the end (old link, or rows that left the filter after an update): go to the last page.
    const lastPage = Math.max(1, Math.ceil(page.totalCount / query.pageSize));
    if (page.items.length === 0 && query.page > lastPage) {
      this.setPage(lastPage);
    }
  }
}

/** Applies criteria (search, filters, sort); when something actually changed, goes back to page 1. */
function withCriteria(patch: Partial<ListQuery>): QueryUpdate {
  return (query) => {
    const next = { ...query, ...patch };
    return queryEquals(next, query) ? query : { ...next, page: 1 };
  };
}

export function queryEquals(a: ListQuery, b: ListQuery): boolean {
  return (Object.keys(DEFAULT_QUERY) as (keyof ListQuery)[]).every((key) => a[key] === b[key]);
}

export function toRequestQuery(query: ListQuery): RequestQuery {
  return {
    page: query.page,
    pageSize: query.pageSize,
    search: query.search,
    status: query.status ?? undefined,
    priority: query.priority ?? undefined,
    organizationName: query.organizationName,
    assignedTo: query.assignedTo,
    createdFrom: query.createdFrom,
    createdTo: query.createdTo,
    sortBy: query.sortBy,
    sortDirection: query.sortDirection,
  };
}

/** URL query parameters: only values that differ from the defaults. */
export function queryToParams(query: ListQuery): Params {
  const params: Params = {};
  for (const key of Object.keys(DEFAULT_QUERY) as (keyof ListQuery)[]) {
    const value = query[key];
    if (value !== DEFAULT_QUERY[key] && value !== null && value !== '') {
      params[key] = value;
    }
  }
  return params;
}

/** Reads the query from the URL; invalid values fall back to the defaults. */
export function queryFromParams(params: ParamMap): ListQuery {
  const text = (key: string) => params.get(key)?.trim() ?? '';
  const oneOf = <T extends string>(key: string, allowed: readonly T[]): T | null => {
    const value = params.get(key);
    return allowed.includes(value as T) ? (value as T) : null;
  };
  const date = (key: string) => (/^\d{4}-\d{2}-\d{2}$/.test(text(key)) ? text(key) : '');
  const page = Number(params.get('page'));
  const pageSize = Number(params.get('pageSize'));

  const query: ListQuery = {
    page: Number.isInteger(page) && page >= 1 ? page : DEFAULT_QUERY.page,
    pageSize: (PAGE_SIZES as readonly number[]).includes(pageSize) ? pageSize : DEFAULT_QUERY.pageSize,
    search: text('search'),
    status: oneOf('status', STATUSES),
    priority: oneOf('priority', PRIORITIES),
    organizationName: text('organizationName'),
    assignedTo: text('assignedTo'),
    createdFrom: date('createdFrom'),
    createdTo: date('createdTo'),
    sortBy: oneOf('sortBy', SORTABLE_FIELDS) ?? DEFAULT_QUERY.sortBy,
    sortDirection: oneOf('sortDirection', ['asc', 'desc'] as const) ?? DEFAULT_QUERY.sortDirection,
  };
  if (query.createdFrom && query.createdTo && query.createdFrom > query.createdTo) {
    query.createdTo = '';
  }
  return query;
}

/** createdFrom must not be after createdTo (yyyy-MM-dd strings compare correctly as text). */
export function dateRangeValidator(group: AbstractControl): ValidationErrors | null {
  const from = group.get('createdFrom')?.value as string;
  const to = group.get('createdTo')?.value as string;
  return from && to && from > to ? { dateRange: true } : null;
}

function initialViewModel(query: ListQuery): ListViewModel {
  return { status: 'loading', query, items: [], totalCount: 0, error: null, refreshing: false, loadedAt: null };
}

function reduceViewModel(vm: ListViewModel, event: LoadEvent): ListViewModel {
  switch (event.type) {
    case 'loading':
      return event.refresh
        ? { ...vm, status: 'loading', refreshing: true }
        : { ...vm, status: 'loading', query: event.query, items: [], error: null, refreshing: false };
    case 'loaded':
      return {
        status: 'ready',
        query: event.query,
        items: event.page.items,
        totalCount: event.page.totalCount,
        error: null,
        refreshing: false,
        loadedAt: new Date(),
      };
    case 'failed':
      return { ...vm, status: 'error', query: event.query, items: [], error: event.error, refreshing: false };
    case 'patched':
      return { ...vm, items: vm.items.map((item) => (item.id === event.item.id ? event.item : item)) };
  }
}

function toApiError(error: unknown): ApiError {
  return isApiError(error) ? error : { status: -1, title: error instanceof Error ? error.message : 'Unexpected error' };
}
