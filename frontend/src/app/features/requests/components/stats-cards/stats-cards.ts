import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { RequestStatus } from '../../../../models/request.model';
import { PRIORITIES, STATUSES } from '../../../../shared/request-labels';
import { PriorityLabel } from '../../../../shared/priority-label/priority-label';
import { StatusBadge } from '../../../../shared/status-badge/status-badge';
import { StatsViewModel } from '../../requests-list.state';

/** Total with the priority breakdown, and one card per status with its count and share. */
@Component({
  selector: 'app-stats-cards',
  imports: [DecimalPipe, PriorityLabel, StatusBadge],
  templateUrl: './stats-cards.html',
  styleUrl: './stats-cards.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatsCards {
  readonly state = input.required<StatsViewModel>();

  protected readonly statuses = STATUSES;
  protected readonly priorities = PRIORITIES;
  protected readonly stats = computed(() => this.state().stats);

  protected share(status: RequestStatus): number {
    const stats = this.stats();
    return stats && stats.total > 0 ? Math.round((stats.byStatus[status] / stats.total) * 100) : 0;
  }
}
