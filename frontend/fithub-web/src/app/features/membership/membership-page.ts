import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AccountContextService } from '../../core/membership/account-context.service';
import { MembershipPlan, MembershipPurchaseKind } from '../../core/membership/membership.models';
import { MembershipService } from '../../core/membership/membership.service';

@Component({ selector: 'app-membership-page', imports: [CurrencyPipe, DatePipe, RouterLink], templateUrl: './membership-page.html' })
export class MembershipPage {
  private readonly service = inject(MembershipService);
  private readonly router = inject(Router);
  protected readonly context = inject(AccountContextService);
  protected readonly busyPlan = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly failedPlan = signal<MembershipPlan | null>(null);
  protected readonly isOnboarding = this.router.url.startsWith('/membership/choose') || this.router.url.startsWith('/membership/renew') || this.router.url.includes('/checkout/cancelled');
  protected readonly checkoutCancelled = this.router.url.includes('/checkout/cancelled');

  constructor() {
    if (!this.context.overview()) this.context.refresh().subscribe({ error: () => undefined });
  }

  protected actionFor(plan: MembershipPlan): { label: string; kind: MembershipPurchaseKind } | null {
    const current = this.context.currentMembership();
    if (!this.context.hasActiveMembership()) {
      return { label: this.context.membershipState() === 'None' ? 'Choose this plan' : 'Reactivate membership', kind: 'Purchase' };
    }
    if (current?.plan.id === plan.id) return { label: 'Renew membership', kind: 'Renewal' };
    return current && plan.price > current.plan.price ? { label: 'Upgrade membership', kind: 'Upgrade' } : null;
  }

  protected checkout(plan: MembershipPlan): void {
    if (this.busyPlan()) return;
    const action = this.actionFor(plan);
    if (!action) return;
    this.busyPlan.set(plan.id);
    this.failedPlan.set(null);
    this.error.set(null);
    this.service.createCheckout(plan.id, action.kind).pipe(finalize(() => this.busyPlan.set(null))).subscribe({
      next: session => window.location.assign(session.checkoutUrl),
      error: (_error: HttpErrorResponse) => {
        this.failedPlan.set(plan);
        this.error.set("We couldn't start checkout. Please try again.");
      },
    });
  }

  protected retry(): void {
    const plan = this.failedPlan();
    if (plan) this.checkout(plan);
  }

  protected reload(): void {
    this.error.set(null);
    this.context.refresh().subscribe({ error: () => this.error.set('Membership details could not be loaded. Please try again.') });
  }
}
