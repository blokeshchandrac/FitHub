import { Component, computed, input } from '@angular/core';
import { ProgressPoint } from '../../core/progress/progress.models';

@Component({ selector: 'app-progress-chart', template: `<div class="chart" role="img" [attr.aria-label]="label()">@if (polyline()) { <svg viewBox="0 0 600 200" preserveAspectRatio="none"><line x1="20" y1="180" x2="580" y2="180"/><polyline [attr.points]="polyline()"/></svg><div class="chart-scale"><span>{{ maximum() }}</span><span>{{ minimum() }}</span></div> } @else { <p>Log at least two values to display a trend.</p> }</div>` })
export class ProgressChart {
  readonly points = input.required<ProgressPoint[]>();
  readonly metric = input.required<'weightKg' | 'bodyFatPercentage' | 'waistCm'>();
  readonly label = input.required<string>();
  private readonly values = computed(() => this.points().map((point) => point[this.metric()]).filter((value): value is number => value !== null));
  readonly minimum = computed(() => this.values().length ? Math.min(...this.values()) : 0);
  readonly maximum = computed(() => this.values().length ? Math.max(...this.values()) : 0);
  readonly polyline = computed(() => { const values = this.values(); if (values.length < 2) return ''; const min = this.minimum(); const range = this.maximum() - min || 1; return values.map((value, index) => `${20 + index * 560 / (values.length - 1)},${180 - (value - min) * 150 / range}`).join(' '); });
}
