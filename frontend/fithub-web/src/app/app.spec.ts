import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { ApplicationLayout } from './core/layouts/application-layout';
import { MembershipOnboardingLayout } from './core/layouts/membership-onboarding-layout';

describe('FitHub layouts', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] }));

  it('creates the root router host', () => {
    const fixture = TestBed.createComponent(App); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('router-outlet')).toBeTruthy();
  });

  it('does not render the member sidebar in membership onboarding', () => {
    const fixture = TestBed.createComponent(MembershipOnboardingLayout); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('aside')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Payment history');
  });

  it('renders the complete sidebar in the active application layout', () => {
    const fixture = TestBed.createComponent(ApplicationLayout); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('aside')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Dashboard');
    expect(fixture.nativeElement.textContent).toContain('Notifications');
  });
});
