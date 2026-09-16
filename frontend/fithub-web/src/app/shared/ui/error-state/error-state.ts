import { Component, input, output } from '@angular/core';
import { AppIcon } from '../app-icon/app-icon';
import { Button } from '../button/button';

let nextErrorStateId = 0;

@Component({
  selector: 'fit-error-state',
  imports: [AppIcon, Button],
  templateUrl: './error-state.html',
  styleUrl: './error-state.scss',
})
export class ErrorState {
  readonly title = input<string>('Something went wrong');
  readonly message = input.required<string>();
  readonly retryLabel = input<string | null>('Try again');
  readonly retry = output<void>();
  protected readonly headingId = `fit-error-state-${nextErrorStateId++}`;
}
