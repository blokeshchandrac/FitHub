import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, finalize, map, Observable, of, shareReplay, tap } from 'rxjs';
import { AuthenticationMessage, AuthenticationResponse, CurrentUser, LoginRequest, RegistrationRequest, ResetPasswordRequest } from './authentication.models';
import { ThemeService } from '../theme/theme.service';

@Injectable({ providedIn: 'root' })
export class AuthenticationService {
  private readonly http = inject(HttpClient);
  private readonly themes = inject(ThemeService);
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

  /**
   * Rehydrates the in-memory access token from the server-managed refresh cookie.
   * The shared observable prevents route guards in the same navigation from issuing duplicate refreshes.
   */
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

  requestPasswordReset(email: string): Observable<AuthenticationMessage> {
    return this.http.post<AuthenticationMessage>('/api/auth/forgot-password', { email });
  }

  resetPassword(request: ResetPasswordRequest): Observable<AuthenticationMessage> {
    return this.http.post<AuthenticationMessage>('/api/auth/reset-password', request);
  }

  refreshCurrentUser(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>('/api/auth/me').pipe(
      tap((user) => {
        this.themes.setAuthenticatedUser(user.id);
        this.currentUserState.set(user);
      }),
    );
  }

  hasRole(role: string): boolean {
    return this.currentUserState()?.roles.includes(role) ?? false;
  }

  private establishSession(response: AuthenticationResponse): void {
    this.themes.setAuthenticatedUser(response.user.id);
    this.accessTokenState.set(response.accessToken);
    this.currentUserState.set(response.user);
  }

  private clearSession(): void {
    this.accessTokenState.set(null);
    this.currentUserState.set(null);
    this.themes.useGuestPreference();
  }
}
