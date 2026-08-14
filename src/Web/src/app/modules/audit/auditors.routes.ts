import { Routes } from '@angular/router';

export const AUDITORS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/auditors/auditors.page').then((component) => component.AuditorsPage),
  },
];
