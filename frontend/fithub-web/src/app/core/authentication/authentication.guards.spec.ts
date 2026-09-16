import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { firstValueFrom, Observable } from 'rxjs';
import { administratorGuard, authenticationGuard } from './authentication.guards';

describe('authenticationGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('redirects an unauthenticated user to login', async () => {
    const result = TestBed.runInInjectionContext(() => authenticationGuard(
      {} as ActivatedRouteSnapshot,
      { url: '/dashboard' } as RouterStateSnapshot,
    )) as Observable<boolean | UrlTree>;
    const resultPromise = firstValueFrom(result);
    const request = TestBed.inject(HttpTestingController).expectOne('/api/auth/refresh');
    expect(request.request.withCredentials).toBe(true);
    request.flush({ message: 'No active session.' }, { status: 401, statusText: 'Unauthorized' });

    const urlTree = await resultPromise as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(urlTree)).toBe('/login?returnUrl=%2Fdashboard');
  });

  it.each(['/simulation', '/simulation/equipment', '/simulation/parking'])(
    'redirects an anonymous visitor away from %s',
    async (url) => {
      const resultPromise = runAdministratorGuard(url);
      TestBed.inject(HttpTestingController).expectOne('/api/auth/refresh').flush(
        { message: 'No active session.' },
        { status: 401, statusText: 'Unauthorized' },
      );

      const result = (await resultPromise) as UrlTree;
      expect(TestBed.inject(Router).serializeUrl(result)).toBe(
        `/login?returnUrl=${encodeURIComponent(url)}`,
      );
    },
  );

  it.each([
    ['Member', '/simulation'],
    ['Member', '/simulation/equipment'],
    ['Member', '/simulation/parking'],
    ['Trainer', '/simulation'],
    ['Trainer', '/simulation/equipment'],
    ['Trainer', '/simulation/parking'],
  ])('rejects an authenticated %s opening %s', async (role, url) => {
    const resultPromise = runAdministratorGuard(url);
    flushRefresh(role);

    const result = (await resultPromise) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/unauthorized');
  });

  it.each(['/simulation', '/simulation/equipment', '/simulation/parking'])(
    'restores a valid Administrator session before allowing a new tab to open %s',
    async (url) => {
      const resultPromise = runAdministratorGuard(url);
      flushRefresh('Administrator');

      expect(await resultPromise).toBe(true);
    },
  );

  function runAdministratorGuard(url: string): Promise<boolean | UrlTree> {
    const result = TestBed.runInInjectionContext(() => administratorGuard(
      {} as ActivatedRouteSnapshot,
      { url } as RouterStateSnapshot,
    )) as Observable<boolean | UrlTree>;
    return firstValueFrom(result);
  }

  function flushRefresh(role: string): void {
    TestBed.inject(HttpTestingController).expectOne('/api/auth/refresh').flush({
      accessToken: 'restored-access-token',
      expiresAt: '2026-09-16T12:15:00Z',
      user: {
        id: `${role.toLowerCase()}-id`,
        email: `${role.toLowerCase()}@fithub.test`,
        firstName: role,
        lastName: 'Test',
        roles: [role],
        onboardingCompleted: true,
      },
    });
  }
});
