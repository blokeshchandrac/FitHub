import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { AccountContextService } from '../../core/membership/account-context.service';
import { MembershipService } from '../../core/membership/membership.service';

@Component({ selector: 'app-payments-page', imports: [CurrencyPipe, DatePipe], templateUrl: './payments-page.html' })
export class PaymentsPage {
  private readonly service = inject(MembershipService);
  protected readonly context = inject(AccountContextService);
  protected readonly overview = this.context.overview;
  protected readonly error = signal<string | null>(null);
  constructor() { this.context.refresh().subscribe({ error: () => this.error.set('Payment history could not be loaded.') }); }

  protected printReceipt(paymentId: string): void {
    const receipt = window.open('about:blank', '_blank');
    if (receipt) receipt.opener = null;
    this.service.getReceipt(paymentId).subscribe({
      next: html => { if (receipt) { receipt.document.open(); receipt.document.write(html); receipt.document.close(); } },
      error: () => { receipt?.close(); this.error.set('The receipt could not be opened.'); },
    });
  }
}
