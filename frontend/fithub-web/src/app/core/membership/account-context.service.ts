import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { catchError, finalize, map, Observable, of, shareReplay, tap, throwError } from 'rxjs';
import { AuthenticationService } from '../authentication/authentication.service';
import { EffectiveMembershipState, MembershipOverview } from './membership.models';
import { MembershipService } from './membership.service';

@Injectable({ providedIn: 'root' })
export class AccountContextService {
  private readonly authentication = inject(AuthenticationService);
  private readonly membershipService = inject(MembershipService);
  private readonly overviewState = signal<MembershipOverview | null>(null);
  private readonly membershipStateValue = signal<EffectiveMembershipState | 'Unknown'>('Unknown');
  private readonly loadingValue = signal(false);
  private readonly errorValue = signal<string | null>(null);
  private refreshRequest: Observable<MembershipOverview> | null = null;

  readonly authenticatedUser = this.authentication.currentUser;
  readonly roles = computed(() => this.authenticatedUser()?.roles ?? []);
  readonly overview = this.overviewState.asReadonly();
  readonly currentMembership = computed(() => this.overviewState()?.currentMembership ?? null);
  readonly membershipState = this.membershipStateValue.asReadonly();
  readonly loading = this.loadingValue.asReadonly();
  readonly error = this.errorValue.asReadonly();
  readonly isStaff = computed(() => this.roles().includes('Trainer') || this.roles().includes('Administrator'));
  readonly hasActiveMembership = computed(() => this.membershipStateValue() === 'Active');
  readonly hasApplicationAccess = computed(() => this.isStaff() || this.hasActiveMembership());

  constructor() {
    effect(() => {
      if (!this.authentication.currentUser()) this.reset();
    });
  }

  refresh(): Observable<MembershipOverview> {
    if (this.isStaff()) {
      this.loadingValue.set(false);
      this.errorValue.set(null);
      return of({ effectiveState: 'None', currentMembership: null, plans: [], payments: [] });
    }
    if (this.authentication.hasRole('Member') && !this.authentication.currentUser()?.onboardingCompleted) {
      this.loadingValue.set(false);
      this.errorValue.set(null);
      return of({ effectiveState: 'None', currentMembership: null, plans: [], payments: [] });
    }
    if (!this.refreshRequest) {
      this.loadingValue.set(true);
      this.errorValue.set(null);
      this.refreshRequest = this.membershipService.getOverview().pipe(
        tap((overview) => {
          this.overviewState.set(overview);
          this.membershipStateValue.set(overview.effectiveState);
        }),
        catchError((error) => {
          this.errorValue.set('We could not confirm your membership status.');
          this.membershipStateValue.set('Unknown');
          return throwError(() => error);
        }),
        finalize(() => {
          this.loadingValue.set(false);
          this.refreshRequest = null;
        }),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    }
    return this.refreshRequest;
  }

  ensureLoaded(): Observable<boolean> {
    if (this.isStaff() || (this.membershipStateValue() !== 'Unknown' && !this.errorValue())) return of(true);
    return this.refresh().pipe(map(() => true));
  }

  destination(): string {
    if (this.authentication.hasRole('Administrator')) return '/administration';
    if (this.authentication.hasRole('Trainer')) return '/trainer/classes';
    if (this.authentication.hasRole('Member') && !this.authentication.currentUser()?.onboardingCompleted) return '/welcome';
    if (this.membershipStateValue() === 'Active') return '/dashboard';
    if (this.membershipStateValue() === 'PendingPayment') return '/membership/confirming';
    if (['Expired', 'Cancelled', 'PastDue'].includes(this.membershipStateValue())) return '/membership/renew';
    return '/membership/choose';
  }

  private reset(): void {
    this.overviewState.set(null);
    this.membershipStateValue.set('Unknown');
    this.loadingValue.set(false);
    this.errorValue.set(null);
  }
}
