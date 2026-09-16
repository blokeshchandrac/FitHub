import { Component, computed, input } from '@angular/core';
import { AppIcon, AppIconName } from '../app-icon/app-icon';

export type StatusTone = 'success' | 'warning' | 'error' | 'information' | 'offline' | 'maintenance' | 'neutral';

@Component({
  selector: 'fit-status-badge',
  imports: [AppIcon],
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.scss',
})
export class StatusBadge {
  readonly label = input.required<string>();
  readonly tone = input<StatusTone>('neutral');
  protected readonly icon = computed<AppIconName>(() => {
    switch (this.tone()) {
      case 'success': return 'check';
      case 'warning': return 'alert-triangle';
      case 'error': return 'circle-alert';
      case 'information':
      case 'maintenance': return 'info';
      default: return 'circle';
    }
  });
}
