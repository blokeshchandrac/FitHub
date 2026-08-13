import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { firstValueFrom, Observable } from 'rxjs';
import { AuthenticationService } from '../authentication/authentication.service';
import { activeMembershipGuard } from './active-membership.guard';

describe('activeMembershipGuard', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] }));
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('redirects a no-membership member to plan selection and preserves the active-only URL', async () => {
    await authenticate('Member');
    const result = evaluate('/equipment');
    const resultPromise = firstValueFrom(result as Observable<boolean | UrlTree>);
    TestBed.inject(HttpTestingController).expectOne('/api/membership').flush(overview('None'));
    expect(TestBed.inject(Router).serializeUrl(await resultPromise as UrlTree)).toBe('/membership/choose?returnUrl=%2Fequipment');
  });

  it('allows an active member to navigate to active-only routes', async () => {
    await authenticate('Member');
    const result = evaluate('/dashboard');
    const resultPromise = firstValueFrom(result as Observable<boolean | UrlTree>);
    TestBed.inject(HttpTestingController).expectOne('/api/membership').flush(overview('Active'));
    expect(await resultPromise).toBe(true);
  });

  it('sends an expired member to renewal', async () => {
    await authenticate('Member');
    const result = evaluate('/workouts');
    const resultPromise = firstValueFrom(result as Observable<boolean | UrlTree>);
    TestBed.inject(HttpTestingController).expectOne('/api/membership').flush(overview('Expired'));
    expect(TestBed.inject(Router).serializeUrl(await resultPromise as UrlTree)).toContain('/membership/renew');
  });

  it.each(['Trainer', 'Administrator'])('does not membership-gate %s', async (role) => {
    await authenticate(role);
    expect(evaluate('/dashboard')).toBe(true);
    TestBed.inject(HttpTestingController).expectNone('/api/membership');
  });

  function evaluate(url: string): Observable<boolean | UrlTree> | boolean | UrlTree {
    return TestBed.runInInjectionContext(() => activeMembershipGuard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot)) as Observable<boolean | UrlTree> | boolean | UrlTree;
  }

  async function authenticate(role: string): Promise<void> {
    const result = firstValueFrom(TestBed.inject(AuthenticationService).login({ email: `${role}@test.local`, password: 'Password-42!' }));
    TestBed.inject(HttpTestingController).expectOne('/api/auth/login').flush({ accessToken: 'token', expiresAt: '2026-08-14T00:00:00Z', user: { id: '1', email: `${role}@test.local`, firstName: role, lastName: 'Test', roles: [role] } });
    await result;
  }

  function overview(state: 'None' | 'Active' | 'Expired') {
    return { effectiveState: state, currentMembership: state === 'None' ? null : { id:'m1', plan:{id:'p1',name:'Monthly',description:null,price:49.9,currency:'EUR',billingPeriodMonths:1,isActive:true}, status:state, startsAt:'2026-08-01T00:00:00Z', endsAt:'2026-09-01T00:00:00Z', isActive:state==='Active' }, plans:[], payments:[] };
  }
});
