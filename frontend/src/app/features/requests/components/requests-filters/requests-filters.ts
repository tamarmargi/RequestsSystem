import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';

import { PRIORITIES, PRIORITY_LABELS, STATUSES, STATUS_LABELS } from '../../../../shared/request-labels';
import { FiltersForm } from '../../requests-list.state';

/**
 * Sidebar filters. The form belongs to RequestsListState (which debounces the text inputs and validates the
 * date range); this component only renders it.
 */
@Component({
  selector: 'app-requests-filters',
  imports: [ReactiveFormsModule],
  templateUrl: './requests-filters.html',
  styleUrl: './requests-filters.scss',
  // Not OnPush: the form is also changed from outside (chips, "clear all"), and the date-range error must follow.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class RequestsFilters {
  readonly form = input.required<FiltersForm>();
  readonly clearAll = output<void>();

  protected readonly statuses = STATUSES;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly priorities = PRIORITIES;
  protected readonly priorityLabels = PRIORITY_LABELS;
}
