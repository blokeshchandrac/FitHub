import { TestBed } from '@angular/core/testing';
import { CanActivateFn, RedirectCommand, Router, UrlTree, provideRouter } from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { AuthenticationService } from '../authentication/authentication.service';
import { AccountContextService } from '../membership/account-context.service';
import { anonymousLandingGuard } from './anonymous-landing.guard';

describe('anonymousLandingGuard', () => {
  const authentication = { restoreSession: vi.fn<() => Observable<boolean>>() };
  const account = {
    destination: vi.fn<() => string>(),
    ensureLoaded: vi.fn<() => Observable<boolean>>(),
  };

  beforeEach(() => {
    authentication.restoreSession.mockReset();
    account.destination.mockReset();
    account.ensureLoaded.mockReset();
    account.ensureLoaded.mockReturnValue(of(true));

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthenticationService, useValue: authentication },
        { provide: AccountContextService, useValue: account },
      ],
    });
  });

  it('allows an anonymous visitor to see the landing page', async () => {
    authentication.restoreSession.mockReturnValue(of(false));

    expect(await runGuard()).toBe(true);
    expect(account.ensureLoaded).not.toHaveBeenCalled();
  });

  it.each([
    ['/welcome', 'a Member with incomplete onboarding'],
    ['/membership/choose', 'a Member without active membership'],
    ['/dashboard', 'an active Member'],
    ['/trainer/classes', 'a Trainer'],
    ['/administration', 'an Administrator'],
  ])('redirects %s for %s using the centralized account context', async (destination) => {
    authentication.restoreSession.mockReturnValue(of(true));
    account.destination.mockReturnValue(destination);

    const result = await runGuard();
    expect(result).toBeInstanceOf(RedirectCommand);
    expect(TestBed.inject(Router).serializeUrl((result as RedirectCommand).redirectTo)).toBe(
      destination,
    );
    expect((result as RedirectCommand).navigationBehaviorOptions?.replaceUrl).toBe(true);
  });

  it('uses the existing retryable membership error route when account state cannot be loaded', async () => {
    authentication.restoreSession.mockReturnValue(of(true));
    account.ensureLoaded.mockReturnValue(throwError(() => new Error('unavailable')));

    const result = await runGuard();
    expect(TestBed.inject(Router).serializeUrl((result as RedirectCommand).redirectTo)).toBe(
      '/membership/status-error',
    );
  });

  function runGuard(): Promise<boolean | UrlTree | RedirectCommand> {
    const result = TestBed.runInInjectionContext(() =>
      (anonymousLandingGuard as CanActivateFn)({} as never, {} as never),
    );
    return result instanceof Observable ? firstValueFrom(result) : Promise.resolve(result);
  }
});
