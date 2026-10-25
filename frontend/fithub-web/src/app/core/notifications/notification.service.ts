import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Notification, NotificationPreference, NotificationPreferenceUpdate } from './notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);

  list() { return this.http.get<readonly Notification[]>('/api/notifications'); }
  read(id: string) { return this.http.post<void>(`/api/notifications/${id}/read`, {}); }
  preferences() { return this.http.get<readonly NotificationPreference[]>('/api/notifications/preferences'); }
  updatePreferences(preferences: readonly NotificationPreferenceUpdate[]) {
    return this.http.put<readonly NotificationPreference[]>('/api/notifications/preferences', { preferences });
  }
}
