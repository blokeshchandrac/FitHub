import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { MembershipPage } from './membership-page';

describe('MembershipPage checkout', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports:[MembershipPage], providers:[provideRouter([]),provideHttpClient(),provideHttpClientTesting()] }).compileComponents();
  });
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('sends the documented string purchaseKind and prevents duplicate clicks', () => {
    const fixture=TestBed.createComponent(MembershipPage);fixture.detectChanges();
    TestBed.inject(HttpTestingController).expectOne('/api/membership').flush(overview());fixture.detectChanges();
    const button=fixture.nativeElement.querySelector('.plan-card .primary-button') as HTMLButtonElement;button.click();button.click();
    fixture.detectChanges();
    const request=TestBed.inject(HttpTestingController).expectOne('/api/membership/checkout');
    expect(request.request.body).toEqual({planId:'plan-1',purchaseKind:'Purchase'});
    expect(button.disabled).toBe(true);
  });

  it('shows a safe retryable message instead of raw backend details', () => {
    const fixture=TestBed.createComponent(MembershipPage);fixture.detectChanges();
    TestBed.inject(HttpTestingController).expectOne('/api/membership').flush(overview());fixture.detectChanges();
    (fixture.nativeElement.querySelector('.plan-card .primary-button') as HTMLButtonElement).click();
    TestBed.inject(HttpTestingController).expectOne('/api/membership/checkout').flush({errors:{purchaseKind:['The JSON value could not be converted to CreateCheckoutRequest.']}},{status:400,statusText:'Bad Request'});
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain("We couldn't start checkout. Please try again.");
    expect(fixture.nativeElement.textContent).not.toContain('CreateCheckoutRequest');
    expect(fixture.nativeElement.querySelector('.checkout-error button').textContent).toContain('Retry');
  });

  function overview(){return{effectiveState:'None',currentMembership:null,plans:[{id:'plan-1',name:'Monthly',description:'Flexible',price:49.9,currency:'EUR',billingPeriodMonths:1,isActive:true}],payments:[]};}
});
