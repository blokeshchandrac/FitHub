import { Component, input } from '@angular/core';

export type MetricTone = 'accent' | 'neutral' | 'success' | 'warning';

@Component({
  selector: 'fit-metric-card',
  templateUrl: './metric-card.html',
  styleUrl: './metric-card.scss',
})
export class MetricCard {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly supportingText = input<string | null>(null);
  readonly tone = input<MetricTone>('accent');
}
