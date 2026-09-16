import { inject } from '@angular/core';
import { CanActivateFn, RedirectCommand, Router } from '@angular/router';
import { catchError, map, of, switchMap } from 'rxjs';
import { AuthenticationService } from '../authentication/authentication.service';
import { AccountContextService } from '../membership/account-context.service';

/** Keeps the public home page anonymous and delegates all account routing decisions to the existing context. */
export const anonymousLandingGuard: CanActivateFn = () => {
  const authentication = inject(AuthenticationService);
  const account = inject(AccountContextService);
  const router = inject(Router);

  const redirect = (destination: string): RedirectCommand =>
    new RedirectCommand(router.parseUrl(destination), { replaceUrl: true });

  return authentication.restoreSession().pipe(
    switchMap((authenticated) => {
      if (!authenticated) return of(true);

      return account.ensureLoaded().pipe(
        map(() => redirect(account.destination())),
        catchError(() => of(redirect('/membership/status-error'))),
      );
    }),
  );
};
