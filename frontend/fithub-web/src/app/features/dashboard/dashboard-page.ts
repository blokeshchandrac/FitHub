import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MemberDashboard } from '../../core/dashboard/dashboard.models';
import { DashboardService } from '../../core/dashboard/dashboard.service';
import { DashboardWeightChart } from './dashboard-weight-chart';

@Component({ selector: 'app-dashboard-page', imports: [CurrencyPipe, DashboardWeightChart, DatePipe, DecimalPipe], templateUrl: './dashboard-page.html' })
export class DashboardPage { private readonly service = inject(DashboardService); protected readonly dashboard = signal<MemberDashboard | null>(null); protected readonly error = signal<string | null>(null); constructor() { this.service.get().subscribe({ next: (value) => this.dashboard.set(value), error: () => this.error.set('Dashboard analytics could not be loaded. An active membership may be required.') }); } }
