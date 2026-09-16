import { Component, input } from '@angular/core';

export type AppIconName =
  | 'alert-triangle'
  | 'check'
  | 'circle'
  | 'circle-alert'
  | 'info'
  | 'palette'
  | 'sparkles';

export type AppIconSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'fit-app-icon',
  templateUrl: './app-icon.html',
  styleUrl: './app-icon.scss',
})
export class AppIcon {
  readonly name = input.required<AppIconName>();
  readonly size = input<AppIconSize>('md');
  readonly label = input<string | null>(null);
}
