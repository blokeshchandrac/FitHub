import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { authenticationInterceptor } from './authentication.interceptor';
import { AuthenticationService } from './authentication.service';

describe('authenticationInterceptor', () => {
  beforeEach(() => TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([authenticationInterceptor])),
      provideHttpClientTesting(),
    ],
  }));

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('adds the bearer token only to FitHub API requests', async () => {
    const controller = TestBed.inject(HttpTestingController);
    const authentication = TestBed.inject(AuthenticationService);
    const login = firstValueFrom(authentication.login({ email: 'member@fithub.test', password: 'Password-42!' }));
    controller.expectOne('/api/auth/login').flush({
      accessToken: 'access-token',
      expiresAt: '2026-08-13T00:00:00Z',
      user: { id: '1', email: 'member@fithub.test', firstName: 'Test', lastName: 'Member', roles: ['Member'] },
    });
    await login;

    const http = TestBed.inject(HttpClient);
    const apiCall = firstValueFrom(http.get('/api/health'));
    const apiRequest = controller.expectOne('/api/health');
    expect(apiRequest.request.headers.get('Authorization')).toBe('Bearer access-token');
    apiRequest.flush({ status: 'Healthy' });
    await apiCall;

    const externalCall = firstValueFrom(http.get('https://example.test/public'));
    const externalRequest = controller.expectOne('https://example.test/public');
    expect(externalRequest.request.headers.has('Authorization')).toBe(false);
    externalRequest.flush({});
    await externalCall;
  });
});
