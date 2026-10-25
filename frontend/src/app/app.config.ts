import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localeHe from '@angular/common/locales/he';
import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { MAT_DIALOG_DEFAULT_OPTIONS, MatDialogConfig } from '@angular/material/dialog';
import { MAT_SNACK_BAR_DEFAULT_OPTIONS, MatSnackBarConfig } from '@angular/material/snack-bar';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { errorInterceptor } from './core/error.interceptor';
import { userNameInterceptor } from './core/user-name.interceptor';

registerLocaleData(localeHe);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([userNameInterceptor, errorInterceptor])),
    { provide: LOCALE_ID, useValue: 'he' },
    {
      provide: MAT_DIALOG_DEFAULT_OPTIONS,
      useValue: {
        direction: 'rtl',
        hasBackdrop: true,
        // Focus the dialog title: screen readers announce it, and no focus ring lands on the close button.
        autoFocus: 'first-heading',
        // Focus goes back to the button that opened the dialog.
        restoreFocus: true,
        maxWidth: 'calc(100vw - 32px)',
      } satisfies MatDialogConfig,
    },
    {
      provide: MAT_SNACK_BAR_DEFAULT_OPTIONS,
      useValue: { duration: 4000, direction: 'rtl', politeness: 'polite' } satisfies MatSnackBarConfig,
    },
  ],
};
