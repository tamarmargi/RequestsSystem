import { AsyncPipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressBar } from '@angular/material/progress-bar';
import { filter, finalize, switchMap } from 'rxjs';

import { CurrentUserService } from '../../../../core/current-user.service';
import { RequestDto, RequestStatus } from '../../../../models/request.model';
import { Icon } from '../../../../shared/icon/icon';
import { RequestStatusActions } from '../../request-status-actions';
import { RequestsListState } from '../../requests-list.state';
import { ActiveFilters } from '../active-filters/active-filters';
import { BulkActionBar } from '../bulk-action-bar/bulk-action-bar';
import { HistoryPanel, HistoryPanelData } from '../history-panel/history-panel';
import { ListState } from '../list-state/list-state';
import { PageHeader } from '../page-header/page-header';
import { RequestsFilters } from '../requests-filters/requests-filters';
import { RequestsPaginator } from '../requests-paginator/requests-paginator';
import { RequestsTable } from '../requests-table/requests-table';
import { StatsCards } from '../stats-cards/stats-cards';
import { StatusChangeDialog, StatusChangeDialogData } from '../status-change-dialog/status-change-dialog';
import { UserNameDialog, UserNameDialogData } from '../user-name-dialog/user-name-dialog';

/**
 * Container of the requests screen: wires RequestsListState (query, list, stats, selection) and
 * RequestStatusActions (updates, conflicts, bulk) to the presentational components and dialogs.
 */
@Component({
  selector: 'app-requests-page',
  imports: [
    AsyncPipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatProgressBar,
    Icon,
    PageHeader,
    StatsCards,
    RequestsFilters,
    ActiveFilters,
    BulkActionBar,
    RequestsTable,
    RequestsPaginator,
    ListState,
  ],
  providers: [RequestsListState, RequestStatusActions],
  templateUrl: './requests-page.html',
  styleUrl: './requests-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsPage {
  protected readonly state = inject(RequestsListState);
  protected readonly currentUser = inject(CurrentUserService);
  private readonly actions = inject(RequestStatusActions);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly vm$ = this.state.vm$;
  protected readonly stats$ = this.state.stats$;
  protected readonly bulkBusy = signal(false);

  protected refreshAll(): void {
    this.state.refresh();
    this.state.refreshStats();
  }

  protected editUserName(): void {
    this.dialog
      .open<UserNameDialog, UserNameDialogData, string>(UserNameDialog, {
        data: { userName: this.currentUser.userName() },
        width: 'var(--dialog-width)',
      })
      .afterClosed()
      .pipe(
        filter((name): name is string => !!name),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((name) => this.currentUser.setUserName(name));
  }

  protected openStatusDialog(request: RequestDto, loadedAt: Date | null): void {
    this.dialog
      .open<StatusChangeDialog, StatusChangeDialogData, RequestStatus>(StatusChangeDialog, {
        data: { request },
        width: 'var(--dialog-width)',
      })
      .afterClosed()
      .pipe(
        filter((status): status is RequestStatus => !!status),
        switchMap((status) => this.actions.updateStatus(request, status, loadedAt)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  protected openHistory(request: RequestDto): void {
    this.dialog.open<HistoryPanel, HistoryPanelData>(HistoryPanel, {
      data: { request },
      panelClass: 'side-panel',
      position: { right: '0', top: '0' },
      height: '100vh',
      width: 'var(--side-panel-width)',
      maxWidth: '100vw',
    });
  }

  protected applyBulk(newStatus: RequestStatus, items: RequestDto[]): void {
    const selected = items.filter((item) => this.state.selectedIds().has(item.id));
    if (selected.length === 0) {
      return;
    }
    this.bulkBusy.set(true);
    this.actions
      .bulkUpdateStatus(newStatus, selected)
      .pipe(
        finalize(() => this.bulkBusy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }
}
