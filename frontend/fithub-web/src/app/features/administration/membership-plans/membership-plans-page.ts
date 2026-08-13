import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MembershipPlan, MembershipPlanCommand } from '../../../core/membership/membership.models';
import { MembershipService } from '../../../core/membership/membership.service';

@Component({ selector: 'app-membership-plans-page', imports: [CurrencyPipe, ReactiveFormsModule], templateUrl: './membership-plans-page.html' })
export class MembershipPlansPage {
  private readonly service = inject(MembershipService);
  protected readonly plans = signal<MembershipPlan[]>([]);
  protected readonly editingId = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(120)] }),
    description: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(1000)] }),
    price: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0.01)] }),
    billingPeriodMonths: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(1), Validators.max(36)] }),
  });

  constructor() { this.load(); }

  protected edit(plan: MembershipPlan): void {
    this.editingId.set(plan.id);
    this.form.setValue({ name: plan.name, description: plan.description ?? '', price: plan.price, billingPeriodMonths: plan.billingPeriodMonths });
  }

  protected cancelEdit(): void { this.editingId.set(null); this.form.reset({ name: '', description: '', price: 0, billingPeriodMonths: 1 }); }

  protected save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const command = this.form.getRawValue() as MembershipPlanCommand;
    const request = this.editingId() ? this.service.updatePlan(this.editingId()!, command) : this.service.createPlan(command);
    request.subscribe({ next: () => { this.message.set('Membership plan saved.'); this.cancelEdit(); this.load(); }, error: (error) => this.showError(error) });
  }

  protected toggle(plan: MembershipPlan): void {
    this.service.setPlanActive(plan.id, !plan.isActive).subscribe({ next: () => this.load(), error: (error) => this.showError(error) });
  }

  private load(): void { this.service.getAdministrativePlans().subscribe({ next: (plans) => this.plans.set(plans), error: (error) => this.showError(error) }); }
  private showError(error: HttpErrorResponse): void {
    const validation = error.error?.errors ? Object.values(error.error.errors).flat().join(' ') : null;
    this.error.set(validation || error.error?.message || 'The plan could not be saved.');
  }
}
