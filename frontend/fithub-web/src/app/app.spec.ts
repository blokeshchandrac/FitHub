import { provideHttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { ApplicationLayout } from './core/layouts/application-layout';
import { MembershipOnboardingLayout } from './core/layouts/membership-onboarding-layout';
import { AuthenticationService } from './core/authentication/authentication.service';

describe('FitHub layouts', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), {provide:AuthenticationService,useValue:{currentUser:signal({id:'member',email:'member@test',firstName:'Test',lastName:'Member',roles:['Member'],onboardingCompleted:true}),hasRole:(role:string)=>role==='Member',logout:()=>of(undefined)}}] }));

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

  it('renders administration navigation without personal member features', () => {
    useRole('Administrator');
    const fixture = TestBed.createComponent(ApplicationLayout); fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Admin dashboard');
    expect(text).toContain('Users');
    expect(text).toContain('Class management');
    expect(text).not.toContain('Class booking');
    expect(text).not.toContain('Recommendations');
    expect(text).not.toContain('Progress');
    const virtualLab = fixture.nativeElement.querySelector('.virtual-lab-action') as HTMLAnchorElement;
    expect(virtualLab).not.toBeNull();
    expect(virtualLab.getAttribute('href')).toBe('/simulation');
    expect(virtualLab.getAttribute('target')).toBe('_blank');
    expect(virtualLab.getAttribute('rel')).toBe('noopener noreferrer');
  });

  it('renders trainer workflow navigation without administration powers', () => {
    useRole('Trainer');
    const fixture = TestBed.createComponent(ApplicationLayout); fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Trainer dashboard');
    expect(text).toContain('Workout plans');
    expect(text).toContain('Scanner / validator');
    expect(text).not.toContain('Users');
    expect(text).not.toContain('Payments / Revenue');
    expect(fixture.nativeElement.querySelector('.virtual-lab-action')).toBeNull();
  });

  it('does not show the Virtual Lab action to a Member', () => {
    const fixture = TestBed.createComponent(ApplicationLayout); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.virtual-lab-action')).toBeNull();
  });

  function useRole(role: 'Trainer' | 'Administrator'): void {
    TestBed.overrideProvider(AuthenticationService, { useValue: {
      currentUser: signal({ id: role, email: `${role}@test`, firstName: role, lastName: 'Test', roles: [role], onboardingCompleted: true }),
      hasRole: (candidate: string) => candidate === role,
      logout: () => of(undefined),
    }});
  }
});
