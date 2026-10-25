import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, catchError, map, of, switchMap, tap } from 'rxjs';

import { isApiError } from '../../core/api-error';
import { BulkUpdateResult } from '../../models/bulk-update.model';
import { RequestDto, RequestStatus } from '../../models/request.model';
import { errorMessage } from '../../shared/error-messages';
import { STATUS_LABELS } from '../../shared/request-labels';
import { RequestsApiService } from '../../services/requests-api.service';
import {
  BulkResultChoice,
  BulkResultDialog,
  BulkResultDialogData,
} from './components/bulk-result-dialog/bulk-result-dialog';
import { ConflictChoice, ConflictDialog, ConflictDialogData } from './components/conflict-dialog/conflict-dialog';
import { RequestsListState } from './requests-list.state';

export type StatusUpdateOutcome = 'updated' | 'kept-theirs' | 'cancelled' | 'failed';

/**
 * Status changes with their side effects: success snackbar + list and stats refresh, error snackbars, and the
 * 409 conflict flow (conflict dialog → keep the other user's change, or resend with the current version).
 * Provided in the requests page next to RequestsListState.
 */
@Injectable()
export class RequestStatusActions {
  private readonly api = inject(RequestsApiService);
  private readonly state = inject(RequestsListState);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  /** Sends the change with the version the user saw. On 409 the user decides in the conflict dialog. */
  updateStatus(request: RequestDto, newStatus: RequestStatus, loadedAt: Date | null): Observable<StatusUpdateOutcome> {
    return this.api.updateStatus(request.id, newStatus, request.version).pipe(
      map((updated): StatusUpdateOutcome => {
        this.state.patchItem(updated);
        this.afterSuccessfulUpdate(`הסטטוס של פנייה ${updated.id} עודכן ל"${STATUS_LABELS[updated.status]}"`);
        return 'updated';
      }),
      catchError((error: unknown) => {
        if (isApiError(error) && error.status === 409 && isRequestDto(error.current)) {
          return this.resolveConflict(request, newStatus, error.current, loadedAt);
        }
        this.snackBar.open(errorMessage(error), 'סגירה');
        if (isApiError(error) && error.status === 404) {
          this.state.refresh();
        }
        return of<StatusUpdateOutcome>('failed');
      }),
    );
  }

  /** Bulk change; shows the per-item result dialog. "Reload the failed ones" keeps them selected. */
  bulkUpdateStatus(newStatus: RequestStatus, requests: RequestDto[]): Observable<BulkUpdateResult | null> {
    const items = requests.map(({ id, version }) => ({ id, version }));
    return this.api.bulkUpdateStatus(newStatus, items).pipe(
      tap((result) => {
        if (result.succeeded > 0) {
          this.state.refresh();
          this.state.refreshStats();
        }
      }),
      switchMap((result) =>
        this.dialog
          .open<BulkResultDialog, BulkResultDialogData, BulkResultChoice>(BulkResultDialog, {
            data: { result, newStatus, requests },
            width: 'var(--dialog-width-wide)',
          })
          .afterClosed()
          .pipe(
            map((choice) => {
              if (choice === 'reload-failed') {
                this.state.select(result.results.filter((r) => r.outcome !== 'Updated').map((r) => r.id));
                this.state.refresh();
              } else {
                this.state.clearSelection();
              }
              return result;
            }),
          ),
      ),
      catchError((error: unknown) => {
        this.snackBar.open(errorMessage(error), 'סגירה');
        return of(null);
      }),
    );
  }

  private resolveConflict(
    request: RequestDto,
    attempted: RequestStatus,
    current: RequestDto,
    loadedAt: Date | null,
  ): Observable<StatusUpdateOutcome> {
    return this.dialog
      .open<ConflictDialog, ConflictDialogData, ConflictChoice>(ConflictDialog, {
        data: { request, attempted, current, loadedAt },
        role: 'alertdialog',
        ariaDescribedBy: 'conflict-description',
        width: 'var(--dialog-width-wide)',
      })
      .afterClosed()
      .pipe(
        switchMap((choice): Observable<StatusUpdateOutcome> => {
          if (choice === 'retry') {
            // Resend against the version the server returned; another 409 opens the dialog again.
            return this.updateStatus(current, attempted, new Date());
          }
          // Keep the other user's change (or the dialog was dismissed): show the current state in the row.
          this.state.patchItem(current);
          return of(choice === 'keep-theirs' ? 'kept-theirs' : 'cancelled');
        }),
      );
  }

  private afterSuccessfulUpdate(message: string): void {
    this.state.refresh();
    this.state.refreshStats();
    this.snackBar.open(message, undefined, { duration: 3000 });
  }
}

function isRequestDto(value: unknown): value is RequestDto {
  return (
    typeof value === 'object' &&
    value !== null &&
    typeof (value as RequestDto).id === 'number' &&
    typeof (value as RequestDto).status === 'string' &&
    typeof (value as RequestDto).version === 'string'
  );
}
