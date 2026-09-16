import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { MembershipOverview, MembershipPlan, MembershipPlanCommand, MembershipPurchaseKind, PaymentReconciliationResult } from './membership.models';

@Injectable({ providedIn: 'root' })
export class MembershipService {
  private readonly http = inject(HttpClient);
  getOverview(): Observable<MembershipOverview> {
    return this.http.get<MembershipOverview>('/api/membership');
  }

  createCheckout(planId: string, purchaseKind: MembershipPurchaseKind): Observable<{ paymentId: string; checkoutUrl: string }> {
    return this.http.post<{ paymentId: string; checkoutUrl: string }>('/api/membership/checkout', { planId, purchaseKind });
  }

  reconcileCheckout(checkoutSessionId?: string): Observable<PaymentReconciliationResult> {
    const endpoint = checkoutSessionId
      ? `/api/membership/checkout/${encodeURIComponent(checkoutSessionId)}/reconcile`
      : '/api/membership/checkout/reconcile';
    return this.http.post<PaymentReconciliationResult>(
      endpoint,
      {},
    );
  }

  getAdministrativePlans(): Observable<MembershipPlan[]> {
    return this.http.get<MembershipPlan[]>('/api/administration/membership-plans');
  }

  createPlan(command: MembershipPlanCommand): Observable<MembershipPlan> {
    return this.http.post<MembershipPlan>('/api/administration/membership-plans', command);
  }

  updatePlan(planId: string, command: MembershipPlanCommand): Observable<MembershipPlan> {
    return this.http.put<MembershipPlan>(`/api/administration/membership-plans/${planId}`, command);
  }

  setPlanActive(planId: string, active: boolean): Observable<void> {
    return this.http.put<void>(`/api/administration/membership-plans/${planId}/active?active=${active}`, {});
  }

  getReceipt(paymentId: string): Observable<string> {
    return this.http.get(`/api/membership/payments/${paymentId}/receipt`, { responseType: 'text' });
  }
}
