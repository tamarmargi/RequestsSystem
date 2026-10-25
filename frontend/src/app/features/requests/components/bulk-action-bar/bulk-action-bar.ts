import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

import { RequestStatus } from '../../../../models/request.model';
import { STATUSES, STATUS_LABELS } from '../../../../shared/request-labels';

/** Shown while rows are selected: count, target status, apply, clear selection. */
@Component({
  selector: 'app-bulk-action-bar',
  template: `
    <div class="bar" role="region" aria-label="פעולה על הפניות שנבחרו">
      <strong>{{ count() }} {{ count() === 1 ? 'פנייה נבחרה' : 'פניות נבחרו' }}</strong>
      <label class="bar__label" for="bulk-status">שינוי סטטוס לכולן:</label>
      <select
        id="bulk-status"
        class="bar__select"
        [value]="target() ?? ''"
        (change)="target.set($any($event.target).value || null)"
      >
        <option value="" disabled>בחירת סטטוס</option>
        @for (status of statuses; track status) {
          <option [value]="status">{{ labels[status] }}</option>
        }
      </select>
      <button type="button" class="bar__apply" [disabled]="!target() || busy()" (click)="applyTarget()">
        {{ busy() ? 'מעדכן…' : 'החלה' }}
      </button>
      <button type="button" class="bar__clear" (click)="clearSelection.emit()">ביטול בחירה</button>
    </div>
  `,
  styleUrl: './bulk-action-bar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BulkActionBar {
  readonly count = input.required<number>();
  readonly busy = input(false);
  readonly apply = output<RequestStatus>();
  readonly clearSelection = output<void>();

  protected readonly statuses = STATUSES;
  protected readonly labels = STATUS_LABELS;
  protected readonly target = signal<RequestStatus | null>(null);

  protected applyTarget(): void {
    const target = this.target();
    if (target) {
      this.apply.emit(target);
    }
  }
}
