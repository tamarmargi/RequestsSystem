import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatSort, MatSortHeader, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';

import { RequestDto } from '../../../../models/request.model';
import { RequestSortBy, SortDirection } from '../../../../models/request-query.model';
import { DATE_FORMAT } from '../../../../shared/date-formats';
import { Icon } from '../../../../shared/icon/icon';
import { PriorityLabel } from '../../../../shared/priority-label/priority-label';
import { StatusBadge } from '../../../../shared/status-badge/status-badge';

export interface TableSort {
  sortBy: RequestSortBy;
  sortDirection: SortDirection;
}

/** The requests table: server-side sort (matSort only reports the change), row selection, row actions. */
@Component({
  selector: 'app-requests-table',
  imports: [DatePipe, MatTableModule, MatSort, MatSortHeader, MatCheckbox, Icon, PriorityLabel, StatusBadge],
  templateUrl: './requests-table.html',
  styleUrl: './requests-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsTable {
  readonly items = input.required<RequestDto[]>();
  readonly selectedIds = input.required<ReadonlySet<number>>();
  readonly sortBy = input.required<RequestSortBy>();
  readonly sortDirection = input.required<SortDirection>();

  readonly sortChange = output<TableSort>();
  readonly toggleRow = output<number>();
  readonly togglePage = output<boolean>();
  readonly changeStatus = output<RequestDto>();
  readonly showHistory = output<RequestDto>();

  protected readonly columns = [
    'select',
    'id',
    'title',
    'organizationName',
    'status',
    'priority',
    'assignedTo',
    'createdAt',
    'updatedAt',
    'actions',
  ];
  protected readonly dateFormat = DATE_FORMAT;

  protected readonly allSelected = computed(
    () => this.items().length > 0 && this.items().every((item) => this.selectedIds().has(item.id)),
  );
  protected readonly someSelected = computed(
    () => !this.allSelected() && this.items().some((item) => this.selectedIds().has(item.id)),
  );

  protected readonly trackById = (_: number, item: RequestDto) => item.id;

  protected onSort(sort: Sort): void {
    // matSortDisableClear: the direction is never ''.
    this.sortChange.emit({ sortBy: sort.active as RequestSortBy, sortDirection: sort.direction || 'asc' });
  }
}
