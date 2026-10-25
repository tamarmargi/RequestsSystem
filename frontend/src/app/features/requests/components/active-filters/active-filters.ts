import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { formatIsoDate } from '../../../../shared/date-formats';
import { Icon } from '../../../../shared/icon/icon';
import { PRIORITY_LABELS, STATUS_LABELS } from '../../../../shared/request-labels';
import { FilterKey, ListQuery } from '../../requests-list.state';

interface Chip {
  key: FilterKey;
  text: string;
  removeLabel: string;
}

/** One removable chip per filter applied to the current query. */
@Component({
  selector: 'app-active-filters',
  imports: [Icon],
  template: `
    @if (chips().length > 0) {
      <ul class="chips" aria-label="מסננים פעילים">
        @for (chip of chips(); track chip.key) {
          <li class="chip">
            {{ chip.text }}
            <button type="button" class="chip__remove" [attr.aria-label]="chip.removeLabel" (click)="remove.emit(chip.key)">
              <app-icon name="close" [size]="12" [strokeWidth]="2.5" />
            </button>
          </li>
        }
      </ul>
    }
  `,
  styleUrl: './active-filters.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActiveFilters {
  readonly query = input.required<ListQuery>();
  readonly remove = output<FilterKey>();

  protected readonly chips = computed(() => {
    const q = this.query();
    const chips: Chip[] = [];
    if (q.status) {
      chips.push({ key: 'status', text: `סטטוס: ${STATUS_LABELS[q.status]}`, removeLabel: 'הסר סינון סטטוס' });
    }
    if (q.priority) {
      chips.push({ key: 'priority', text: `עדיפות: ${PRIORITY_LABELS[q.priority]}`, removeLabel: 'הסר סינון עדיפות' });
    }
    if (q.organizationName) {
      chips.push({ key: 'organizationName', text: `ארגון: ${q.organizationName}`, removeLabel: 'הסר סינון ארגון' });
    }
    if (q.assignedTo) {
      chips.push({ key: 'assignedTo', text: `מטפל: ${q.assignedTo}`, removeLabel: 'הסר סינון מטפל' });
    }
    if (q.createdFrom || q.createdTo) {
      const text =
        q.createdFrom && q.createdTo
          ? `${formatIsoDate(q.createdFrom)} – ${formatIsoDate(q.createdTo)}`
          : q.createdFrom
            ? `מתאריך ${formatIsoDate(q.createdFrom)}`
            : `עד ${formatIsoDate(q.createdTo)}`;
      chips.push({ key: 'dateRange', text, removeLabel: 'הסר סינון תאריכים' });
    }
    return chips;
  });
}
