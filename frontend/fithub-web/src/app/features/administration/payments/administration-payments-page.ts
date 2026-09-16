import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { AdminPayment } from '../../../core/administration/administration-management.models';
import { AdministrationManagementService } from '../../../core/administration/administration-management.service';
import { AdministratorDashboardService } from '../../../core/administration/administrator-dashboard.service';

@Component({
  selector: 'app-administration-payments-page',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './administration-payments-page.html',
})
export class AdministrationPaymentsPage implements OnInit {
  private readonly management = inject(AdministrationManagementService);
  private readonly dashboard = inject(AdministratorDashboardService);

  protected readonly payments = signal<readonly AdminPayment[]>([]);
  protected readonly error = signal('');
  protected readonly confirmedRevenue = signal(0);
  protected readonly revenueCurrency = signal('EUR');

  ngOnInit(): void {
    forkJoin({ payments: this.management.payments(), summary: this.dashboard.get() }).subscribe({
      next: ({ payments, summary }) => {
        this.payments.set(payments);
        this.confirmedRevenue.set(summary.completedRevenue);
        this.revenueCurrency.set(summary.revenueCurrency);
      },
      error: () => this.error.set('Payments could not be loaded.'),
    });
  }
}
