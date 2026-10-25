import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { NotificationPreference } from './notification.models';
import { NotificationService } from './notification.service';

describe('NotificationService preferences', () => {
  const preferences: readonly NotificationPreference[] = [{
    category: 'PersonalRecord',
    displayName: 'Personal records',
    description: 'Updates when a completed workout establishes a recorded best.',
    enabled: true,
    canDisable: true,
  }];

  beforeEach(() => TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting()],
  }));
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('loads the current Member preferences', async () => {
    const result = firstValueFrom(TestBed.inject(NotificationService).preferences());
    const request = TestBed.inject(HttpTestingController).expectOne('/api/notifications/preferences');
    expect(request.request.method).toBe('GET');
    request.flush(preferences);
    expect(await result).toEqual(preferences);
  });

  it('saves the selected values and uses returned persisted values', async () => {
    const updates = [{ category: 'PersonalRecord', enabled: false }];
    const result = firstValueFrom(TestBed.inject(NotificationService).updatePreferences(updates));
    const request = TestBed.inject(HttpTestingController).expectOne('/api/notifications/preferences');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ preferences: updates });
    request.flush([{ ...preferences[0], enabled: false }]);
    expect((await result)[0].enabled).toBe(false);
  });
});
