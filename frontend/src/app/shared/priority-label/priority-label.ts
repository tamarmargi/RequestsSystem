import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { RequestPriority } from '../../models/request.model';
import { PRIORITY_CLASSES, PRIORITY_LABELS } from '../request-labels';

/** Priority as colored text (the word itself carries the meaning), optionally followed by projected content. */
@Component({
  selector: 'app-priority-label',
  template: `<span class="priority" [class]="cssClass()">{{ label() }}<ng-content /></span>`,
  styleUrl: './priority-label.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PriorityLabel {
  readonly priority = input.required<RequestPriority>();

  protected readonly label = computed(() => PRIORITY_LABELS[this.priority()]);
  protected readonly cssClass = computed(() => PRIORITY_CLASSES[this.priority()]);
}
