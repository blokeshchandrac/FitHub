import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthenticationService } from '../../../core/authentication/authentication.service';
import { ResetPasswordPage } from './reset-password-page';

describe('ResetPasswordPage', () => {
  const authentication = { resetPassword: vi.fn() };
  const route = {
    snapshot: {
      queryParamMap: convertToParamMap({
        email: 'member@fithub.test',
        token: 'secure-reset-token-with-more-than-32-characters',
      }),
    },
  };

  beforeEach(async () => {
    authentication.resetPassword.mockReset();
    authentication.resetPassword.mockReturnValue(of({ message: 'Your password has been reset. You can now sign in.' }));
    await TestBed.configureTestingModule({
      imports: [ResetPasswordPage],
      providers: [
        provideRouter([]),
        { provide: AuthenticationService, useValue: authentication },
        { provide: ActivatedRoute, useValue: route },
      ],
    }).compileComponents();
  });

  it('rejects mismatched password confirmation without calling the API', () => {
    const fixture = TestBed.createComponent(ResetPasswordPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    setValue(root, '#new-password', 'Replacement-Password-84!');
    setValue(root, '#confirm-new-password', 'Different-Password-85!');
    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    expect(authentication.resetPassword).not.toHaveBeenCalled();
    expect(root.textContent).toContain('Password confirmation must match');
  });

  it('submits the link data and displays the successful result', () => {
    const fixture = TestBed.createComponent(ResetPasswordPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    setValue(root, '#new-password', 'Replacement-Password-84!');
    setValue(root, '#confirm-new-password', 'Replacement-Password-84!');
    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    expect(authentication.resetPassword).toHaveBeenCalledWith({
      email: 'member@fithub.test',
      token: 'secure-reset-token-with-more-than-32-characters',
      newPassword: 'Replacement-Password-84!',
      confirmPassword: 'Replacement-Password-84!',
    });
    expect(root.querySelector('[role="status"]')?.textContent).toContain('password has been reset');
    expect(root.querySelector<HTMLAnchorElement>('a[href="/login"]')).not.toBeNull();
  });

  it('shows the safe invalid-or-expired link message', () => {
    authentication.resetPassword.mockReturnValue(throwError(() => new HttpErrorResponse({
      status: 400,
      error: { message: 'This password-reset link is invalid, expired, or has already been used.' },
    })));
    const fixture = TestBed.createComponent(ResetPasswordPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    setValue(root, '#new-password', 'Replacement-Password-84!');
    setValue(root, '#confirm-new-password', 'Replacement-Password-84!');
    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    expect(root.querySelector('[role="alert"]')?.textContent).toContain('invalid, expired, or has already been used');
  });

  function setValue(root: HTMLElement, selector: string, value: string): void {
    const input = root.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
  }
});
