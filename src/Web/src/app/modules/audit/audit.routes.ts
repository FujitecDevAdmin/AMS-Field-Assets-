import type { Routes } from '@angular/router';

export const AUDIT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/audit-overview/audit-overview.page').then(
        (component) => component.AuditOverviewPage,
      ),
  },
  {
    path: 'my',
    loadComponent: () =>
      import('./features/my-audits/my-audits.page').then((m) => m.MyAuditsPage),
  },
];
