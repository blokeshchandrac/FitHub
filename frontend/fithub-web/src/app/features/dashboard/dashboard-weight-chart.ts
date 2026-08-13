import { Component, computed, input } from '@angular/core';
import { WeightTrendPoint } from '../../core/dashboard/dashboard.models';

@Component({ selector: 'app-dashboard-weight-chart', template: `<div class="chart compact-chart" role="img" aria-label="Recent weight trend">@if (line()) { <svg viewBox="0 0 600 160" preserveAspectRatio="none"><polyline [attr.points]="line()"/></svg> } @else { <p>More weight entries are needed.</p> }</div>` })
export class DashboardWeightChart {
  readonly points = input.required<WeightTrendPoint[]>();
  readonly line = computed(() => { const values = this.points().map((item) => item.weightKg); if (values.length < 2) return ''; const min = Math.min(...values); const range = Math.max(...values) - min || 1; return values.map((value, index) => `${20 + index * 560 / (values.length - 1)},${140 - (value - min) * 120 / range}`).join(' '); });
}
