import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { RequestStatus } from '../../models/request.model';
import { STATUS_CLASSES, STATUS_LABELS } from '../request-labels';

/** Status pill: colored dot + text, so the meaning never depends on color alone. */
@Component({
  selector: 'app-status-badge',
  template: `<span class="badge" [class]="cssClass()"><span class="badge__dot" aria-hidden="true"></span>{{ label() }}</span>`,
  styleUrl: './status-badge.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadge {
  readonly status = input.required<RequestStatus>();

  protected readonly label = computed(() => STATUS_LABELS[this.status()]);
  protected readonly cssClass = computed(() => STATUS_CLASSES[this.status()]);
}
