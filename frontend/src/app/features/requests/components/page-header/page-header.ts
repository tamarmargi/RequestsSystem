import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { Icon } from '../../../../shared/icon/icon';

/** App header: orange top stripe, product name, and the current user chip (click to change the name). */
@Component({
  selector: 'app-page-header',
  imports: [Icon],
  template: `
    <header class="header">
      <div class="header__inner">
        <div class="brand">
          <div class="brand__mark"><app-icon name="leaf" [size]="20" /></div>
          <div class="brand__name">ניהול פניות</div>
        </div>
        <div class="user">
          <span class="muted" id="user-label">משתמש:</span>
          <button
            type="button"
            class="user__chip"
            aria-describedby="user-label"
            title="שינוי שם המשתמש"
            (click)="editUserName.emit()"
          >
            <app-icon name="user" />
            {{ userName() }}
            <span class="visually-hidden">(לחצו לשינוי השם)</span>
          </button>
        </div>
      </div>
    </header>
  `,
  styleUrl: './page-header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageHeader {
  readonly userName = input.required<string>();
  readonly editUserName = output<void>();
}
