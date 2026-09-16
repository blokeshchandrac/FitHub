import { Component, input, output } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'quiet' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'fit-button',
  templateUrl: './button.html',
  styleUrl: './button.scss',
})
export class Button {
  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<ButtonSize>('md');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly disabled = input(false);
  readonly busy = input(false);
  readonly accessibleLabel = input<string | null>(null);
  readonly pressed = output<MouseEvent>();

  protected activate(event: MouseEvent): void {
    if (!this.disabled() && !this.busy()) this.pressed.emit(event);
  }
}
