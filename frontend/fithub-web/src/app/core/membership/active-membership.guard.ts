import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthenticationService } from '../authentication/authentication.service';
import { AccountContextService } from './account-context.service';

export const activeMembershipGuard: CanActivateFn = (_route, state) => {
  const authentication = inject(AuthenticationService);
  const router = inject(Router);
  if (authentication.hasRole('Trainer') || authentication.hasRole('Administrator')) return true;

  const context = inject(AccountContextService);
  return context.ensureLoaded().pipe(
    map(() => context.hasActiveMembership()
      ? true
      : router.createUrlTree([context.destination()], { queryParams: { returnUrl: safeReturnUrl(state.url) } })),
    catchError(() => of(router.createUrlTree(['/membership/status-error']))),
  );
};

export const membershipEntryGuard: CanActivateFn = () => {
  const context = inject(AccountContextService);
  const router = inject(Router);
  if (context.isStaff()) return true;
  return context.ensureLoaded().pipe(
    map(() => context.hasActiveMembership() ? true : router.createUrlTree([context.destination()])),
    catchError(() => of(router.createUrlTree(['/membership/status-error']))),
  );
};

export const membershipOnboardingGuard: CanActivateFn = () => {
  const context = inject(AccountContextService);
  const router = inject(Router);
  return context.ensureLoaded().pipe(
    map(() => context.hasApplicationAccess() ? router.createUrlTree([context.destination()]) : true),
    catchError(() => of(router.createUrlTree(['/membership/status-error']))),
  );
};

function safeReturnUrl(url: string): string | undefined {
  return url.startsWith('/') && !url.startsWith('//') ? url : undefined;
}
