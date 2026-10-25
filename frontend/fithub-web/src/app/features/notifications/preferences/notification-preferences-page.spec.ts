import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { NotificationPreference } from '../../../core/notifications/notification.models';
import { NotificationService } from '../../../core/notifications/notification.service';
import { NotificationPreferencesPage } from './notification-preferences-page';

describe('NotificationPreferencesPage', () => {
  const values: readonly NotificationPreference[] = [
    { category: 'PersonalRecord', displayName: 'Personal records', description: 'Recorded best updates.', enabled: true, canDisable: true },
    { category: 'FaultReportStatusUpdate', displayName: 'Equipment report updates', description: 'Fault report changes.', enabled: true, canDisable: true },
  ];
  const service = { preferences: vi.fn(), updatePreferences: vi.fn() };

  beforeEach(async () => {
    service.preferences.mockReset();
    service.updatePreferences.mockReset();
    service.preferences.mockReturnValue(of(values));
    service.updatePreferences.mockReturnValue(of(values));
    await TestBed.configureTestingModule({
      imports: [NotificationPreferencesPage],
      providers: [provideRouter([]), { provide: NotificationService, useValue: service }],
    }).compileComponents();
  });

  it('loads preferences and renders accessible switches', () => {
    const fixture = TestBed.createComponent(NotificationPreferencesPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    expect(service.preferences).toHaveBeenCalledOnce();
    expect(root.querySelectorAll('input[role="switch"]').length).toBe(2);
    expect(root.textContent).toContain('Personal records');
  });

  it('saves a changed preference once and displays confirmation', () => {
    service.updatePreferences.mockReturnValue(of([{ ...values[0], enabled: false }, values[1]]));
    const fixture = TestBed.createComponent(NotificationPreferencesPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    root.querySelector<HTMLInputElement>('#preference-PersonalRecord')!.click();
    root.querySelector<HTMLButtonElement>('button.primary-button')!.click();
    fixture.detectChanges();

    expect(service.updatePreferences).toHaveBeenCalledOnce();
    expect(service.updatePreferences.mock.calls[0][0]).toContainEqual({ category: 'PersonalRecord', enabled: false });
    expect(root.querySelector('[role="status"]')?.textContent).toContain('saved');
    expect(root.querySelector<HTMLInputElement>('#preference-PersonalRecord')!.checked).toBe(false);
  });

  it('prevents duplicate save requests while one is in progress', () => {
    const pending = new Subject<readonly NotificationPreference[]>();
    service.updatePreferences.mockReturnValue(pending);
    const fixture = TestBed.createComponent(NotificationPreferencesPage);
    fixture.detectChanges();
    const button = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button.primary-button')!;
    button.click();
    button.click();
    expect(service.updatePreferences).toHaveBeenCalledOnce();
    pending.next(values);
    pending.complete();
  });

  it('shows load and save failures without discarding the current values', () => {
    service.preferences.mockReturnValue(throwError(() => new Error('load')));
    const loadFixture = TestBed.createComponent(NotificationPreferencesPage);
    loadFixture.detectChanges();
    expect((loadFixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain('could not be loaded');

    service.preferences.mockReturnValue(of(values));
    service.updatePreferences.mockReturnValue(throwError(() => new Error('save')));
    const saveFixture = TestBed.createComponent(NotificationPreferencesPage);
    saveFixture.detectChanges();
    (saveFixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button.primary-button')!.click();
    saveFixture.detectChanges();
    expect((saveFixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain('could not be saved');
    expect((saveFixture.nativeElement as HTMLElement).querySelectorAll('input[role="switch"]').length).toBe(2);
  });
});
