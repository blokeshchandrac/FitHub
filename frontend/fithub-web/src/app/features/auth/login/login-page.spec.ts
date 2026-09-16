import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('shows validation feedback and does not call the API for an invalid form', () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelectorAll('.field-error').length).toBe(2);
    TestBed.inject(HttpTestingController).expectNone('/api/auth/login');
  });

  it('renders the safe API error returned for failed login', () => {
    const fixture = TestBed.createComponent(LoginPage);
    const component = fixture.componentInstance as unknown as { form: { setValue(value: object): void }; submit(): void };
    component.form.setValue({ email: 'member@fithub.test', password: 'Wrong-password-42!' });
    component.submit();
    TestBed.inject(HttpTestingController).expectOne('/api/auth/login').flush(
      { message: 'Email or password is incorrect.' },
      { status: 401, statusText: 'Unauthorized' },
    );
    fixture.detectChanges();

    const alert = (fixture.nativeElement as HTMLElement).querySelector('[role="alert"]');
    expect(alert?.textContent).toContain('Email or password is incorrect.');
  });

  it('navigates a no-membership Member to plan selection', () => {
    const fixture = TestBed.createComponent(LoginPage);
    const router = TestBed.inject(Router);
    const navigation = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    const component = fixture.componentInstance as unknown as { form: { setValue(value: object): void }; submit(): void };
    component.form.setValue({ email: 'member-without-plan@fithub.test', password: 'Test-only-Password-42!' });
    component.submit();
    TestBed.inject(HttpTestingController).expectOne('/api/auth/login').flush({ accessToken:'token',expiresAt:'2026-09-01T00:00:00Z',user:{id:'u1',email:'member-without-plan@fithub.test',firstName:'No Membership',lastName:'Member',roles:['Member'],onboardingCompleted:true} });
    TestBed.inject(HttpTestingController).expectOne('/api/membership').flush({ effectiveState:'None',currentMembership:null,plans:[],payments:[] });
    expect(navigation).toHaveBeenCalledWith('/membership/choose');
  });

  it('navigates an active Member to the dashboard', () => {
    const fixture = TestBed.createComponent(LoginPage);
    const navigation = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const component = fixture.componentInstance as unknown as { form: { setValue(value: object): void }; submit(): void };
    component.form.setValue({ email: 'active-member@fithub.test', password: 'Test-only-Password-42!' }); component.submit();
    TestBed.inject(HttpTestingController).expectOne('/api/auth/login').flush({ accessToken:'token',expiresAt:'2026-09-01T00:00:00Z',user:{id:'u2',email:'active-member@fithub.test',firstName:'Active',lastName:'Member',roles:['Member'],onboardingCompleted:true} });
    TestBed.inject(HttpTestingController).expectOne('/api/membership').flush({ effectiveState:'Active',currentMembership:{id:'m1',plan:{id:'p1',name:'Monthly',description:null,price:49.9,currency:'EUR',billingPeriodMonths:1,isActive:true},status:'Active',startsAt:'2026-08-01T00:00:00Z',endsAt:'2026-09-01T00:00:00Z',isActive:true},plans:[],payments:[] });
    expect(navigation).toHaveBeenCalledWith('/dashboard');
  });

  it('navigates a newly registered Member to profile onboarding before membership lookup', () => {
    const fixture = TestBed.createComponent(LoginPage);
    const navigation = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const component = fixture.componentInstance as unknown as { form: { setValue(value: object): void }; submit(): void };
    component.form.setValue({ email: 'new-member@fithub.test', password: 'Test-only-Password-42!' });
    component.submit();
    TestBed.inject(HttpTestingController).expectOne('/api/auth/login').flush({
      accessToken: 'token', expiresAt: '2026-09-01T00:00:00Z',
      user: { id: 'new', email: 'new-member@fithub.test', firstName: 'New', lastName: 'Member', roles: ['Member'], onboardingCompleted: false },
    });
    TestBed.inject(HttpTestingController).expectNone('/api/membership');
    expect(navigation).toHaveBeenCalledWith('/welcome');
  });

  it('renders only the public authentication controls without demonstration or Virtual Lab content', () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const links = [...root.querySelectorAll<HTMLAnchorElement>('a')];

    expect(root.querySelector('input[type="email"]')).not.toBeNull();
    expect(root.querySelector('input[type="password"]')).not.toBeNull();
    expect(root.querySelector('button[type="submit"]')?.textContent).toContain('Sign in');
    expect(links.some((link) => link.getAttribute('href') === '/forgot-password')).toBe(true);
    expect(links.some((link) => link.getAttribute('href') === '/register')).toBe(true);
    expect(root.textContent).not.toContain('Demonstration accounts');
    expect(root.textContent).not.toContain('no-membership@fithub.demo');
    expect(root.textContent).not.toContain('active-member@fithub.demo');
    expect(root.textContent).not.toContain('shared development password');
    expect(root.textContent).not.toContain('Virtual Lab');
    expect(root.querySelector('.demo-accounts')).toBeNull();
    expect(root.querySelector('.virtual-lab-entry')).toBeNull();
  });
});
