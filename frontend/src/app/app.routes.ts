import { Routes } from '@angular/router';

import { RequestsPage } from './features/requests/requests-page/requests-page';

export const routes: Routes = [
  { path: '', component: RequestsPage, title: 'ניהול פניות' },
  { path: '**', redirectTo: '' },
];
