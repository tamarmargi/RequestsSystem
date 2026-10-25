import { AsyncPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogClose, MatDialogTitle } from '@angular/material/dialog';
import { Observable, catchError, map, of, startWith } from 'rxjs';

import { RequestDto, RequestStatus } from '../../../../models/request.model';
import { RequestHistoryItem } from '../../../../models/request-history.model';
import { DATE_TIME_FORMAT, TIME_FORMAT, timeAgo } from '../../../../shared/date-formats';
import { Icon } from '../../../../shared/icon/icon';
import { STATUS_CLASSES, STATUS_LABELS, isTransitionAllowed } from '../../../../shared/request-labels';
import { StatusBadge } from '../../../../shared/status-badge/status-badge';
import { RequestsApiService } from '../../../../services/requests-api.service';

export interface ConflictDialogData {
  /** The request as the user saw it (stale version). */
  request: RequestDto;
  /** The status the user tried to set. */
  attempted: RequestStatus;
  /** The current state on the server, from the 409 body. */
  current: RequestDto;
  /** When the user's version was loaded. */
  loadedAt: Date | null;
}

/** keep-theirs: keep the other user's change. retry: resend with the current version. */
export type ConflictChoice = 'keep-theirs' | 'retry';

type LastChange =
  | { state: 'loading' }
  | { state: 'known'; changedBy: string; ago: string }
  | { state: 'unknown' };

/** 409: shows "your change" next to the current state on the server and who changed it, and lets the user decide. */
@Component({
  selector: 'app-conflict-dialog',
  imports: [AsyncPipe, DatePipe, MatDialogTitle, MatDialogClose, Icon, StatusBadge],
  templateUrl: './conflict-dialog.html',
  styleUrl: './conflict-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConflictDialog {
  protected readonly data = inject<ConflictDialogData>(MAT_DIALOG_DATA);
  protected readonly labels = STATUS_LABELS;
  protected readonly timeFormat = TIME_FORMAT;
  protected readonly dateTimeFormat = DATE_TIME_FORMAT;
  protected readonly currentClass = STATUS_CLASSES[this.data.current.status];

  protected readonly alreadyInStatus = this.data.current.status === this.data.attempted;
  protected readonly retryAllowed =
    !this.alreadyInStatus && isTransitionAllowed(this.data.current.status, this.data.attempted);

  /** Who made the change that won, from the newest history entry. */
  protected readonly lastChange$: Observable<LastChange> = inject(RequestsApiService)
    .getHistory(this.data.current.id)
    .pipe(
      map((history: RequestHistoryItem[]): LastChange => {
        const newest = history[0];
        return newest ? { state: 'known', changedBy: newest.changedBy, ago: timeAgo(newest.changedAt) } : { state: 'unknown' };
      }),
      catchError(() => of<LastChange>({ state: 'unknown' })),
      startWith<LastChange>({ state: 'loading' }),
    );

  protected readonly keepTheirs: ConflictChoice = 'keep-theirs';
  protected readonly retry: ConflictChoice = 'retry';
}
