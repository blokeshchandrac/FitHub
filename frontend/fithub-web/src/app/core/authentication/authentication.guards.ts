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
