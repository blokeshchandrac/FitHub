import { Component, input } from '@angular/core';
import { AppIcon, AppIconName } from '../app-icon/app-icon';

let nextEmptyStateId = 0;

@Component({
  selector: 'fit-empty-state',
  imports: [AppIcon],
  templateUrl: './empty-state.html',
  styleUrl: './empty-state.scss',
})
export class EmptyState {
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly icon = input<AppIconName>('sparkles');
  protected readonly headingId = `fit-empty-state-${nextEmptyStateId++}`;
}
