import type { Routes } from '@angular/router';

export const MY_AUDITS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/my-audits/my-audits.page').then((component) => component.MyAuditsPage),
  },
];
