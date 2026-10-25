import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { ApiError } from '../../../../core/api-error';
import { isServerUnreachable } from '../../../../shared/error-messages';
import { Icon } from '../../../../shared/icon/icon';

/** Loading skeleton, empty result and error states of the list, as in docs/design/States.dc.html. */
@Component({
  selector: 'app-list-state',
  imports: [Icon],
  templateUrl: './list-state.html',
  styleUrl: './list-state.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListState {
  readonly kind = input.required<'loading' | 'empty' | 'error'>();
  readonly error = input<ApiError | null>(null);
  readonly rows = input(8);

  readonly retry = output<void>();
  readonly clearFilters = output<void>();

  protected readonly skeletonRows = computed(() => Array.from({ length: this.rows() }, (_, i) => i));
  protected readonly unreachable = computed(() => isServerUnreachable(this.error()));
  /** " (500: detail)" for other errors. */
  protected readonly errorSuffix = computed(() => {
    const error = this.error();
    return error && error.status > 0 ? ` (${error.status}${error.detail ? `: ${error.detail}` : ''})` : '';
  });
}
