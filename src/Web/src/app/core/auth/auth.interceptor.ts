import { HttpErrorResponse, type HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthStore } from './auth.store';

/**
 * Attaches the bearer token, and treats a 401 as the end of the session.
 *
 * The sign-in routes are skipped deliberately: they are anonymous, and sending
 * a stale token to them would turn a failed password into a redirect loop
 * through this same interceptor.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  const isSignInCall = request.url.includes('/identity/sign-in');
  const token = auth.session()?.accessToken;

  const outbound =
    token !== undefined && !isSignInCall
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;

  return next(outbound).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !isSignInCall) {
        auth.signOut();
        void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      }
      return throwError(() => error);
    }),
  );
};
