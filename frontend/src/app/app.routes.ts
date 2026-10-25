import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/requests/components/requests-page/requests-page').then((m) => m.RequestsPage),
    title: 'ניהול פניות',
  },
  { path: '**', redirectTo: '' },
];
