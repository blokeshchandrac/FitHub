import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, finalize, map, Observable, of, shareReplay, tap } from 'rxjs';
import { AuthenticationResponse, CurrentUser, LoginRequest, RegistrationRequest } from './authentication.models';

@Injectable({ providedIn: 'root' })
export class AuthenticationService {
  private readonly http = inject(HttpClient);
  private readonly accessTokenState = signal<string | null>(null);
  private readonly currentUserState = signal<CurrentUser | null>(null);
  private restoreRequest: Observable<boolean> | null = null;

  readonly accessToken = this.accessTokenState.asReadonly();
  readonly currentUser = this.currentUserState.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserState() !== null && this.accessTokenState() !== null);

  login(request: LoginRequest): Observable<CurrentUser> {
    return this.http.post<AuthenticationResponse>('/api/auth/login', request, { withCredentials: true }).pipe(
      tap((response) => this.establishSession(response)),
      map((response) => response.user),
    );
  }

  register(request: RegistrationRequest): Observable<CurrentUser> {
    return this.http.post<AuthenticationResponse>('/api/auth/register', request, { withCredentials: true }).pipe(
      tap((response) => this.establishSession(response)),
      map((response) => response.user),
    );
  }

  restoreSession(): Observable<boolean> {
    if (this.isAuthenticated()) {
      return of(true);
    }

    if (!this.restoreRequest) {
      this.restoreRequest = this.http.post<AuthenticationResponse>('/api/auth/refresh', {}, { withCredentials: true }).pipe(
        tap((response) => this.establishSession(response)),
        map(() => true),
        catchError(() => {
          this.clearSession();
          return of(false);
        }),
        finalize(() => (this.restoreRequest = null)),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    }

    return this.restoreRequest;
  }

  logout(): Observable<void> {
    return this.http.post<void>('/api/auth/logout', {}, { withCredentials: true }).pipe(
      finalize(() => this.clearSession()),
    );
  }

  requestPasswordReset(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>('/api/auth/forgot-password', { email });
  }

  hasRole(role: string): boolean {
    return this.currentUserState()?.roles.includes(role) ?? false;
  }

  private establishSession(response: AuthenticationResponse): void {
    this.accessTokenState.set(response.accessToken);
    this.currentUserState.set(response.user);
  }

  private clearSession(): void {
    this.accessTokenState.set(null);
    this.currentUserState.set(null);
  }
}
