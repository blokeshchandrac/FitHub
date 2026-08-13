import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { AuthenticationResponse } from './authentication.models';
import { AuthenticationService } from './authentication.service';

describe('AuthenticationService', () => {
  const response: AuthenticationResponse = {
    accessToken: 'memory-only-access-token',
    expiresAt: '2026-08-13T00:00:00Z',
    user: { id: 'user-1', email: 'member@fithub.test', firstName: 'Test', lastName: 'Member', roles: ['Member'] },
  };

  beforeEach(() => TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting()],
  }));

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('logs in with the refresh cookie enabled and keeps the access token in memory', async () => {
    const service = TestBed.inject(AuthenticationService);
    const result = firstValueFrom(service.login({ email: 'member@fithub.test', password: 'Password-42!' }));
    const request = TestBed.inject(HttpTestingController).expectOne('/api/auth/login');

    expect(request.request.withCredentials).toBe(true);
    request.flush(response);

    expect((await result).email).toBe('member@fithub.test');
    expect(service.accessToken()).toBe('memory-only-access-token');
    expect(service.hasRole('Member')).toBe(true);
  });

  it('coalesces simultaneous session restoration into one refresh request', async () => {
    const service = TestBed.inject(AuthenticationService);
    const first = firstValueFrom(service.restoreSession());
    const second = firstValueFrom(service.restoreSession());
    const request = TestBed.inject(HttpTestingController).expectOne('/api/auth/refresh');

    expect(request.request.withCredentials).toBe(true);
    request.flush(response);

    expect(await first).toBe(true);
    expect(await second).toBe(true);
    expect(service.isAuthenticated()).toBe(true);
  });

  it('clears the in-memory session after logout even when the API fails', async () => {
    const service = TestBed.inject(AuthenticationService);
    const login = firstValueFrom(service.login({ email: 'member@fithub.test', password: 'Password-42!' }));
    TestBed.inject(HttpTestingController).expectOne('/api/auth/login').flush(response);
    await login;

    const logout = firstValueFrom(service.logout()).catch(() => undefined);
    TestBed.inject(HttpTestingController).expectOne('/api/auth/logout').flush(
      { message: 'temporary failure' },
      { status: 500, statusText: 'Server Error' },
    );
    await logout;

    expect(service.accessToken()).toBeNull();
    expect(service.currentUser()).toBeNull();
  });
});
