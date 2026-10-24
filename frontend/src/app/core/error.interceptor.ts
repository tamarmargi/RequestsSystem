import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

import { toApiError } from './api-error';

/**
 * Converts HttpErrorResponse into ApiError and rethrows it.
 * It never shows messages or swallows errors: the calling component decides how to handle them
 * (409 conflicts in particular).
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: unknown) =>
      throwError(() => (error instanceof HttpErrorResponse ? toApiError(error) : error)),
    ),
  );
