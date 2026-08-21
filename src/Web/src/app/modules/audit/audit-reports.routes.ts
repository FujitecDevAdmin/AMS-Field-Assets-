import type { Routes } from '@angular/router';

export const AUDIT_REPORTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/audit-reports/audit-reports.page').then(m => m.AuditReportsPage),
  },
];
