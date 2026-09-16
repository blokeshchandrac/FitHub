import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { MembershipConfirmationPage } from './membership-confirmation-page';

describe('MembershipConfirmationPage', () => {
  beforeEach(async () => {
    vi.useFakeTimers();
    await TestBed.configureTestingModule({
      imports: [MembershipConfirmationPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({ session_id: 'cs_test_paid' }) } },
        },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    vi.useRealTimers();
  });

  it('verifies the returned Stripe session before revealing member access', async () => {
    const fixture = TestBed.createComponent(MembershipConfirmationPage);
    const navigation = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture.detectChanges();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/membership/checkout/cs_test_paid/reconcile')
      .flush({ confirmed: true, changed: true, message: 'Payment confirmed.' });
    await vi.advanceTimersByTimeAsync(0);
    TestBed.inject(HttpTestingController).expectOne('/api/membership').flush(activeOverview());
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Payment successful!');
    await vi.advanceTimersByTimeAsync(1200);
    expect(navigation).toHaveBeenCalledWith('/dashboard');
  });

  it('shows a safe retryable message when server-side verification is unavailable', async () => {
    const fixture = TestBed.createComponent(MembershipConfirmationPage);
    fixture.detectChanges();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/membership/checkout/cs_test_paid/reconcile')
      .flush(
        { detail: 'StripeException: secret detail' },
        { status: 503, statusText: 'Service Unavailable' },
      );
    await vi.advanceTimersByTimeAsync(0);
    TestBed.inject(HttpTestingController).expectOne('/api/membership').flush(pendingOverview());
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'We could not confirm the payment yet. Please try again shortly.',
    );
    expect(fixture.nativeElement.textContent).not.toContain('StripeException');
  });

  function activeOverview() {
    return {
      effectiveState: 'Active',
      currentMembership: {
        id: 'membership-1',
        plan: { id: 'plan-1', name: 'Monthly', description: null, price: 49.9, currency: 'EUR', billingPeriodMonths: 1, isActive: true },
        status: 'Active',
        startsAt: '2026-09-01T13:05:00Z',
        endsAt: '2026-10-01T13:05:00Z',
        isActive: true,
      },
      plans: [],
      payments: [],
    };
  }

  function pendingOverview() {
    return {
      effectiveState: 'PendingPayment',
      currentMembership: null,
      plans: [],
      payments: [],
    };
  }
});
