import { AsyncPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogClose, MatDialogTitle } from '@angular/material/dialog';
import { Observable, Subject, catchError, map, of, startWith, switchMap } from 'rxjs';

import { ApiError, isApiError } from '../../../../core/api-error';
import { RequestDto } from '../../../../models/request.model';
import { RequestHistoryItem } from '../../../../models/request-history.model';
import { DATE_FORMAT, DATE_TIME_FORMAT } from '../../../../shared/date-formats';
import { errorMessage } from '../../../../shared/error-messages';
import { Icon } from '../../../../shared/icon/icon';
import { StatusBadge } from '../../../../shared/status-badge/status-badge';
import { RequestsApiService } from '../../../../services/requests-api.service';

export interface HistoryPanelData {
  request: RequestDto;
}

type HistoryState =
  | { state: 'loading' }
  | { state: 'ready'; items: RequestHistoryItem[] }
  | { state: 'error'; message: string };

/** Side panel (a dialog at the inline-start edge, full height): request summary + status timeline, newest first. */
@Component({
  selector: 'app-history-panel',
  imports: [AsyncPipe, DatePipe, MatDialogTitle, MatDialogClose, Icon, StatusBadge],
  templateUrl: './history-panel.html',
  styleUrl: './history-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoryPanel {
  private readonly api = inject(RequestsApiService);
  private readonly retry$ = new Subject<void>();

  protected readonly request = inject<HistoryPanelData>(MAT_DIALOG_DATA).request;
  protected readonly dateFormat = DATE_FORMAT;
  protected readonly dateTimeFormat = DATE_TIME_FORMAT;

  protected readonly history$: Observable<HistoryState> = this.retry$.pipe(
    startWith(undefined),
    switchMap(() =>
      this.api.getHistory(this.request.id).pipe(
        map((items): HistoryState => ({ state: 'ready', items })),
        catchError((error: unknown) =>
          of<HistoryState>({ state: 'error', message: isApiError(error) ? describe(error) : errorMessage(error) }),
        ),
        startWith<HistoryState>({ state: 'loading' }),
      ),
    ),
  );

  protected retry(): void {
    this.retry$.next();
  }
}

function describe(error: ApiError): string {
  return error.status === 404 ? 'הפנייה לא נמצאה.' : errorMessage(error);
}
