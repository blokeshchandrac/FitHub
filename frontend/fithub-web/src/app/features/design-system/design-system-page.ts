import { Component } from '@angular/core';
import { themeOptions } from '../../core/theme/theme.models';
import { Button } from '../../shared/ui/button/button';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { ErrorState } from '../../shared/ui/error-state/error-state';
import { MetricCard } from '../../shared/ui/metric-card/metric-card';
import { PageHeader } from '../../shared/ui/page-header/page-header';
import { SectionCard } from '../../shared/ui/section-card/section-card';
import { SkeletonLoader } from '../../shared/ui/skeleton-loader/skeleton-loader';
import { StatusBadge } from '../../shared/ui/status-badge/status-badge';
import { ThemeSelector } from '../../shared/ui/theme-selector/theme-selector';

@Component({
  selector: 'app-design-system-page',
  imports: [Button, EmptyState, ErrorState, MetricCard, PageHeader, SectionCard, SkeletonLoader, StatusBadge, ThemeSelector],
  templateUrl: './design-system-page.html',
  styleUrl: './design-system-page.scss',
})
export class DesignSystemPage {
  protected readonly themeOptions = themeOptions;
}
