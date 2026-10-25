import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Inline SVG icons from docs/design (stroke icons on a 24×24 grid). Decorative: always aria-hidden. */
const ICONS = {
  leaf: ['M5 19c8 0 14-6 14-14-8 0-14 6-14 14z', 'M5 19l7-7'],
  user: ['M4 21c1.5-4 4.5-6 8-6s6.5 2 8 6'],
  refresh: ['M21 12a9 9 0 1 1-3-6.7', 'M21 4v5h-5'],
  search: ['M20 20l-3.5-3.5'],
  close: ['M6 6l12 12M18 6L6 18'],
  edit: ['M4 20h4L19 9l-4-4L4 16v4z'],
  history: ['M12 7v5l3 2'],
  info: ['M12 11v5M12 8h.01'],
  error: ['M12 7v6M12 16.5h.01'],
  warning: ['M12 3l9 16H3l9-16z', 'M12 10v4M12 17h.01'],
  /** Points left: "next" in RTL, and the "to" arrow in the history timeline. */
  chevronLeft: ['M15 6l-6 6 6 6'],
  /** Points right: "previous" in RTL. */
  chevronRight: ['M9 6l6 6-6 6'],
} as const;

export type IconName = keyof typeof ICONS;

/** Circles drawn in addition to the paths. */
const CIRCLES: Partial<Record<IconName, { cx: number; cy: number; r: number }>> = {
  user: { cx: 12, cy: 8, r: 4 },
  search: { cx: 11, cy: 11, r: 7 },
  history: { cx: 12, cy: 12, r: 9 },
  info: { cx: 12, cy: 12, r: 9 },
  error: { cx: 12, cy: 12, r: 9 },
};

@Component({
  selector: 'app-icon',
  template: `
    <svg
      [attr.width]="size()"
      [attr.height]="size()"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      [attr.stroke-width]="strokeWidth()"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      @if (circle(); as c) {
        <circle [attr.cx]="c.cx" [attr.cy]="c.cy" [attr.r]="c.r" />
      }
      @for (d of paths(); track $index) {
        <path [attr.d]="d" />
      }
    </svg>
  `,
  styles: `
    :host {
      display: inline-flex;
      flex: none;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Icon {
  readonly name = input.required<IconName>();
  /** Pixels, as in the design's SVG width/height attributes. */
  readonly size = input(16);
  readonly strokeWidth = input(2);

  protected readonly paths = computed(() => ICONS[this.name()]);
  protected readonly circle = computed(() => CIRCLES[this.name()]);
}
