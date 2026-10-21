import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { AuthenticationService } from '../../../core/authentication/authentication.service';
import { AccountContextService } from '../../../core/membership/account-context.service';
import { RegistrationPage } from './registration-page';

@Component({ template: '' })
class EmptyPage {}

describe('RegistrationPage consent', () => {
  const user = {
    id: 'new-member',
    email: 'consent.member@fithub.test',
    firstName: 'Consent',
    lastName: 'Member',
    roles: ['Member'],
    onboardingCompleted: false,
  };
  const authentication = { register: vi.fn() };
  const accountContext = {
    refresh: vi.fn(),
    destination: vi.fn(() => '/welcome'),
  };

  beforeEach(async () => {
    authentication.register.mockReset();
    accountContext.refresh.mockReset();
    accountContext.destination.mockClear();
    authentication.register.mockReturnValue(of(user));
    accountContext.refresh.mockReturnValue(of({ effectiveState: 'None', currentMembership: null, plans: [], payments: [] }));
    await TestBed.configureTestingModule({
      imports: [RegistrationPage],
      providers: [
        provideRouter([{ path: 'welcome', component: EmptyPage }]),
        { provide: AuthenticationService, useValue: authentication },
        { provide: AccountContextService, useValue: accountContext },
      ],
    }).compileComponents();
  });

  it('starts with consent unselected and blocks submission until it is accepted', () => {
    const fixture = TestBed.createComponent(RegistrationPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const checkbox = root.querySelector<HTMLInputElement>('#registration-consent')!;

    expect(checkbox.checked).toBe(false);
    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    expect(authentication.register).not.toHaveBeenCalled();
    const errors = Array.from(root.querySelectorAll<HTMLElement>('.field-error')).map((item) => item.textContent ?? '');
    expect(errors.some((message) => message.includes('You must accept'))).toBe(true);
  });

  it('includes explicit consent in a valid registration request', async () => {
    const fixture = TestBed.createComponent(RegistrationPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    setValue(root, '#first-name', 'Consent');
    setValue(root, '#last-name', 'Member');
    setValue(root, '#register-email', user.email);
    setValue(root, '#register-password', 'Test-only-Password-42!');
    setValue(root, '#confirm-password', 'Test-only-Password-42!');
    const checkbox = root.querySelector<HTMLInputElement>('#registration-consent')!;
    checkbox.click();

    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    expect(authentication.register).toHaveBeenCalledOnce();
    expect(authentication.register).toHaveBeenCalledWith({
      firstName: 'Consent',
      lastName: 'Member',
      email: user.email,
      password: 'Test-only-Password-42!',
      consentAccepted: true,
    });
    expect(accountContext.refresh).toHaveBeenCalledOnce();
    await fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/welcome');
  });

  it('links to the public Terms and Privacy pages', () => {
    const fixture = TestBed.createComponent(RegistrationPage);
    fixture.detectChanges();
    const links = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLAnchorElement>('.checkbox-row a'));

    expect(links.map((link) => link.getAttribute('href'))).toEqual(['/terms', '/privacy']);
  });

  function setValue(root: HTMLElement, selector: string, value: string): void {
    const input = root.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
  }
});
