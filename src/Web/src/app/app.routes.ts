import { Routes } from '@angular/router';

import { anonymousOnlyGuard, authGuard } from './core/auth/auth.guard';
import { NotBuiltPage } from './core/layout/not-built.page';
import { ShellPage } from './core/layout/shell.page';
import { SignInPage } from './modules/identity/features/sign-in/sign-in.page';

/**
 * Sign-in sits OUTSIDE the shell: there is no navigation to show somebody who
 * has not signed in.
 *
 * Everything else renders inside it and requires a session. Module routes are
 * lazy-loaded as children with loadChildren, one line per module, each carrying
 * the capability its screens require (docs/04 §1) — e.g.
 *
 *   {
 *     path: 'allocations',
 *     loadChildren: () => import('./modules/allocations/allocations.routes')
 *       .then((m) => m.ALLOCATIONS_ROUTES),
 *   }
 */
export const routes: Routes = [
  {
    path: 'login',
    component: SignInPage,
    canActivate: [anonymousOnlyGuard],
  },
  {
    path: '',
    component: ShellPage,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadChildren: () =>
          import('./modules/assets/dashboard.routes').then((m) => m.ASSET_DASHBOARD_ROUTES),
      },
      {
        path: 'field-assets',
        loadChildren: () =>
          import('./modules/assets/field-assets.routes').then((m) => m.FIELD_ASSETS_ROUTES),
      },
      {
        path: 'auditors',
        loadChildren: () =>
          import('./modules/audit/auditors.routes').then((m) => m.AUDITORS_ROUTES),
      },
      {
        path: 'my-audits',
        loadChildren: () =>
          import('./modules/audit/my-audits.routes').then((m) => m.MY_AUDITS_ROUTES),
      },
      {
        path: 'audit',
        loadChildren: () => import('./modules/audit/audit.routes').then((m) => m.AUDIT_ROUTES),
      },
      {
        path: 'audit-reports',
        loadChildren: () =>
          import('./modules/audit/audit-reports.routes').then((m) => m.AUDIT_REPORTS_ROUTES),
      },
      {
        path: 'reports',
        loadChildren: () =>
          import('./modules/assets/reports.routes').then((m) => m.ASSET_REPORTS_ROUTES),
      },
      /* Every module route the menu lists, until that module has its own. A
         lazy loadChildren line above this one takes precedence, so modules
         drop out of the placeholder as they land. */
      { path: '**', component: NotBuiltPage },
    ],
  },
];
