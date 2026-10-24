import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { Observable, catchError, map, of, startWith } from 'rxjs';

import { isApiError } from '../../../core/api-error';
import { HealthService } from '../../../services/health.service';

type ConnectionState =
  | { kind: 'loading' }
  | { kind: 'available' }
  | { kind: 'unavailable'; message: string };

@Component({
  selector: 'app-requests-page',
  imports: [AsyncPipe, MatProgressSpinner],
  templateUrl: './requests-page.html',
  styleUrl: './requests-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsPage {
  private readonly health = inject(HealthService);

  protected readonly connection$: Observable<ConnectionState> = this.health.check().pipe(
    map((): ConnectionState => ({ kind: 'available' })),
    catchError((error: unknown) => of<ConnectionState>({ kind: 'unavailable', message: describeError(error) })),
    startWith<ConnectionState>({ kind: 'loading' }),
  );
}

function describeError(error: unknown): string {
  if (!isApiError(error)) {
    return 'שגיאה לא צפויה';
  }
  if (error.status === 0) {
    return 'לא ניתן להתחבר לשרת';
  }
  return `${error.status} ${error.detail ?? error.title}`;
}
