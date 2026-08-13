import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { catchError, EMPTY, switchMap, take, tap, timer } from 'rxjs';
import { AccountContextService } from '../../core/membership/account-context.service';

@Component({
  selector: 'app-membership-confirmation-page',
  imports: [RouterLink],
  template: `
    <section class="confirmation-card">
      <div class="confirmation-mark" aria-hidden="true">✓</div>
      <p class="eyebrow">Payment confirmation</p><h1>{{ confirmed() ? 'Payment confirmed' : 'Confirming your payment' }}</h1>
      <p>{{ confirmed() ? 'Your membership is active. FitHub member features are now available.' : 'Stripe has returned you to FitHub. Access becomes available only after the verified payment webhook confirms your membership.' }}</p>
      @if (checking()) { <p class="checking" aria-live="polite">Checking membership status…</p> }
      @if (message()) { <div class="api-error" role="alert">{{ message() }}</div> }
      <div class="button-row"><button class="primary-button" type="button" [disabled]="checking()" (click)="checkAgain()">Check again</button><a class="secondary-button" routerLink="/membership/choose">Return to plans</a></div>
    </section>
  `,
  styles: [`.confirmation-card{background:#fff;border:1px solid #dbe4ef;border-radius:1.25rem;box-shadow:0 1.25rem 3rem rgb(15 23 42/10%);margin:clamp(1rem,7vh,5rem) auto;max-width:42rem;padding:clamp(1.5rem,5vw,3rem);text-align:center}.confirmation-card h1{font-size:clamp(2rem,5vw,3rem);margin:.4rem 0 1rem}.confirmation-card>p{color:#64748b;line-height:1.6}.confirmation-mark{align-items:center;background:#d1fae5;border-radius:50%;color:#047857;display:flex;font-size:1.6rem;height:3.5rem;justify-content:center;margin:0 auto 1rem;width:3.5rem}.checking{font-weight:700}.button-row{justify-content:center;margin-top:1.5rem}`],
})
export class MembershipConfirmationPage {
  private readonly context = inject(AccountContextService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly checking = signal(true);
  protected readonly message = signal<string | null>(null);
  protected readonly confirmed = signal(false);

  constructor() { this.poll(); }

  protected checkAgain(): void { this.poll(1); }

  private poll(attempts = 15): void {
    this.checking.set(true); this.message.set(null);
    timer(0, 2000).pipe(
      take(attempts), switchMap(() => this.context.refresh()),
      tap(() => {
        if (this.context.hasActiveMembership() && !this.confirmed()) {
          this.confirmed.set(true);
          this.checking.set(false);
          setTimeout(() => this.router.navigateByUrl('/dashboard'), 1200);
        }
      }),
      catchError(() => { this.message.set('We could not check your membership. Please try again.'); return EMPTY; }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({ complete: () => { this.checking.set(false); if (!this.context.hasActiveMembership() && !this.message()) this.message.set('Payment is still being confirmed. You can check again shortly.'); } });
  }
}
