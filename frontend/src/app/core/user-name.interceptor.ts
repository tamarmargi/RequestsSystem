import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { CurrentUserService } from './current-user.service';

export const USER_NAME_HEADER = 'X-User-Name';

/**
 * Adds X-User-Name to API requests. Browsers refuse non-ASCII header values (Hebrew names), so the value is
 * percent-encoded; the server decodes it (CurrentUserAccessor, see docs/api-contract.md).
 */
export const userNameInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/')) {
    return next(req);
  }
  const userName = inject(CurrentUserService).userName();
  return next(req.clone({ setHeaders: { [USER_NAME_HEADER]: encodeURIComponent(userName) } }));
};
