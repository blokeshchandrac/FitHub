import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AttendanceService } from '../../../core/attendance/attendance.service';
import { ScannerPage } from './scanner-page';

describe('ScannerPage submission', () => {
  const service = {
    checkIn: vi.fn(),
    validateAccess: vi.fn(),
  };

  beforeEach(async () => {
    service.checkIn.mockReset();
    service.validateAccess.mockReset();
    await TestBed.configureTestingModule({
      imports: [ScannerPage],
      providers: [{ provide: AttendanceService, useValue: service }],
    }).compileComponents();
  });

  it('checks in once, prevents navigation, and resets validation state after success', () => {
    service.checkIn.mockReturnValue(of({
      attendanceId: 'attendance-1',
      userId: 'member-1',
      checkedInAt: '2026-09-17T05:36:25.102869Z',
    }));
    const fixture = TestBed.createComponent(ScannerPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const token = root.querySelector<HTMLTextAreaElement>('#scanner-token')!;
    token.value = 'QR-VALID-TEST';
    token.dispatchEvent(new Event('input', { bubbles: true }));

    const submit = new Event('submit', { bubbles: true, cancelable: true });
    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(submit);
    fixture.detectChanges();

    expect(submit.defaultPrevented).toBe(true);
    expect(service.checkIn).toHaveBeenCalledOnce();
    expect(service.checkIn).toHaveBeenCalledWith('QR-VALID-TEST');
    expect(token.value).toBe('');
    const component = fixture.componentInstance as unknown as {
      token: { touched: boolean; value: string };
    };
    expect(component.token.value).toBe('');
    expect(component.token.touched).toBe(false);
    expect(root.querySelector('.success-message')?.textContent).toContain('Check-in accepted');
    expect(root.querySelector('.field-error')).toBeNull();
  });

  it('shows the backend rejection message', () => {
    service.checkIn.mockReturnValue(throwError(() => new HttpErrorResponse({
      status: 403,
      statusText: 'Forbidden',
      error: { message: 'Membership is inactive.' },
    })));
    const fixture = TestBed.createComponent(ScannerPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const token = root.querySelector<HTMLTextAreaElement>('#scanner-token')!;
    token.value = 'QR-REJECTED-TEST';
    token.dispatchEvent(new Event('input', { bubbles: true }));

    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    expect(service.checkIn).toHaveBeenCalledOnce();
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('Membership is inactive.');
  });

  it('does not call the API for an empty token', () => {
    const fixture = TestBed.createComponent(ScannerPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const submit = new Event('submit', { bubbles: true, cancelable: true });

    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(submit);
    fixture.detectChanges();

    expect(submit.defaultPrevented).toBe(true);
    expect(service.checkIn).not.toHaveBeenCalled();
    expect(root.querySelector('.field-error')?.textContent).toContain('Enter a valid token or card UID.');
  });
});
