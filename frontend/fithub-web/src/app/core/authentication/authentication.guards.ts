import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthenticationService } from './authentication.service';

export const authenticationGuard: CanActivateFn = (_route, state) => {
  const authentication = inject(AuthenticationService);
  const router = inject(Router);

  return authentication.restoreSession().pipe(
    map((authenticated) => authenticated
      ? true
      : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } })),
  );
};

/**
 * Restores the HttpOnly refresh-cookie session before evaluating the Administrator role.
 * This is essential for protected links opened in a new tab because access tokens remain in memory
 * and therefore are not copied into the new JavaScript context.
 */
export const administratorGuard: CanActivateFn = (_route, state) => {
  const authentication = inject(AuthenticationService);
  const router = inject(Router);

  return authentication.restoreSession().pipe(
    map((authenticated) => {
      if (!authenticated) {
        return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
      }

      return authentication.hasRole('Administrator')
        ? true
        : router.createUrlTree(['/unauthorized']);
    }),
  );
};

export function roleGuard(role: string): CanActivateFn {
  return () => {
    const authentication = inject(AuthenticationService);
    return authentication.hasRole(role) ? true : inject(Router).createUrlTree(['/unauthorized']);
  };
}

export function anyRoleGuard(...roles: string[]): CanActivateFn {
  return () => {
    const authentication = inject(AuthenticationService);
    return roles.some((role) => authentication.hasRole(role))
      ? true
      : inject(Router).createUrlTree(['/unauthorized']);
  };
}
