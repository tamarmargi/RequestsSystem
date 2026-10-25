import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { Icon } from '../../../../shared/icon/icon';
import { PAGE_SIZES } from '../../requests-list.state';

type PageItem = { kind: 'page'; page: number } | { kind: 'gap'; key: string };

/**
 * Page numbers as in the design: first, current ±1 (three pages at the edges), last, with ellipses; prev/next;
 * page size; "מציג X–Y מתוך N". A small custom component because mat-paginator has no page numbers.
 */
@Component({
  selector: 'app-requests-paginator',
  imports: [DecimalPipe, Icon],
  templateUrl: './requests-paginator.html',
  styleUrl: './requests-paginator.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsPaginator {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  protected readonly pageSizes = PAGE_SIZES;
  protected readonly lastPage = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));
  protected readonly from = computed(() => (this.totalCount() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1));
  protected readonly to = computed(() => Math.min(this.page() * this.pageSize(), this.totalCount()));
  protected readonly items = computed(() => pageItems(this.page(), this.lastPage()));

  protected goTo(page: number): void {
    if (page >= 1 && page <= this.lastPage() && page !== this.page()) {
      this.pageChange.emit(page);
    }
  }

  protected onPageSize(value: string): void {
    this.pageSizeChange.emit(Number(value));
  }
}

export function pageItems(current: number, last: number): PageItem[] {
  // The window around the current page; at the edges it is widened to three pages (1 2 3 … last).
  let start = Math.max(1, current - 1);
  let end = Math.min(last, current + 1);
  if (current <= 2) end = Math.min(last, 3);
  if (current >= last - 1) start = Math.max(1, last - 2);

  const items: PageItem[] = [];
  if (start > 1) {
    items.push({ kind: 'page', page: 1 });
    if (start > 2) items.push({ kind: 'gap', key: 'start' });
  }
  for (let page = start; page <= end; page++) {
    items.push({ kind: 'page', page });
  }
  if (end < last) {
    if (end < last - 1) items.push({ kind: 'gap', key: 'end' });
    items.push({ kind: 'page', page: last });
  }
  return items;
}
