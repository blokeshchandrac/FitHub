import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthenticationService } from '../../../core/authentication/authentication.service';
import { ForgotPasswordPage } from './forgot-password-page';

describe('ForgotPasswordPage', () => {
  const authentication = { requestPasswordReset: vi.fn() };

  beforeEach(async () => {
    authentication.requestPasswordReset.mockReset();
    authentication.requestPasswordReset.mockReturnValue(of({
      message: 'If an account exists for this email address, a password-reset link has been sent.',
    }));
    await TestBed.configureTestingModule({
      imports: [ForgotPasswordPage],
      providers: [provideRouter([]), { provide: AuthenticationService, useValue: authentication }],
    }).compileComponents();
  });

  it('submits a valid email and shows the neutral response', () => {
    const fixture = TestBed.createComponent(ForgotPasswordPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const email = root.querySelector<HTMLInputElement>('#recovery-email')!;
    email.value = 'member@fithub.test';
    email.dispatchEvent(new Event('input', { bubbles: true }));
    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    expect(authentication.requestPasswordReset).toHaveBeenCalledOnce();
    expect(authentication.requestPasswordReset).toHaveBeenCalledWith('member@fithub.test');
    expect(root.querySelector('[role="status"]')?.textContent).toContain('If an account exists');
  });

  it('does not submit an invalid email', () => {
    const fixture = TestBed.createComponent(ForgotPasswordPage);
    fixture.detectChanges();
    (fixture.nativeElement as HTMLElement).querySelector<HTMLFormElement>('form')!
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    expect(authentication.requestPasswordReset).not.toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).querySelector('.field-error')).not.toBeNull();
  });
});
