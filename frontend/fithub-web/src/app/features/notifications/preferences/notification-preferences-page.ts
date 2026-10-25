import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { NotificationPreference } from '../../../core/notifications/notification.models';
import { NotificationService } from '../../../core/notifications/notification.service';

@Component({
  selector: 'app-notification-preferences-page',
  imports: [RouterLink],
  templateUrl: './notification-preferences-page.html',
  styleUrl: './notification-preferences-page.scss',
})
export class NotificationPreferencesPage implements OnInit {
  private readonly service = inject(NotificationService);
  protected readonly items = signal<readonly NotificationPreference[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly message = signal('');
  protected readonly error = signal('');

  ngOnInit(): void { this.load(); }

  protected toggle(category: string, enabled: boolean): void {
    this.items.update(items => items.map(item => item.category === category ? { ...item, enabled } : item));
    this.message.set('');
  }

  protected save(): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.message.set('');
    this.error.set('');
    this.service.updatePreferences(this.items().map(item => ({ category: item.category, enabled: item.enabled })))
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: items => {
          this.items.set(items);
          this.message.set('Notification preferences saved.');
        },
        error: () => this.error.set('Notification preferences could not be saved. Please try again.'),
      });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set('');
    this.service.preferences().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: items => this.items.set(items),
      error: () => this.error.set('Notification preferences could not be loaded.'),
    });
  }
}
