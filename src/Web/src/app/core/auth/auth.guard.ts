import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';

import { AuthStore } from './auth.store';

/**
 * Everything inside the shell requires a session. The attempted URL is carried
 * on the redirect so signing in lands where the user was going, not on a
 * dashboard they did not ask for.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  if (auth.isSignedIn()) {
    return true;
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** Keeps a signed-in user off the sign-in screen. */
export const anonymousOnlyGuard: CanActivateFn = () => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  return auth.isSignedIn() ? router.createUrlTree(['/']) : true;
};
