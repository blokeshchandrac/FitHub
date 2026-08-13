import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { firstValueFrom, Observable } from 'rxjs';
import { authenticationGuard } from './authentication.guards';

describe('authenticationGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

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
});
