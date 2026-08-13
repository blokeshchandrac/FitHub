import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, switchMap } from 'rxjs';
import { AuthenticationService } from '../../../core/authentication/authentication.service';
import { AccountContextService } from '../../../core/membership/account-context.service';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login-page.html',
})
export class LoginPage {
  private readonly authentication = inject(AuthenticationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly accountContext = inject(AccountContextService);

  protected readonly submitting = signal(false);
  protected readonly apiError = signal<string | null>(null);
  protected readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email, Validators.maxLength(254)] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(256)] }),
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.apiError.set(null);
    this.authentication.login(this.form.getRawValue()).pipe(
      switchMap(() => this.accountContext.refresh()),
      finalize(() => this.submitting.set(false)),
    ).subscribe({
      next: () => {
        const requested = this.route.snapshot.queryParamMap.get('returnUrl');
        const safeReturn = requested?.startsWith('/') && !requested.startsWith('//') ? requested : null;
        this.router.navigateByUrl(this.accountContext.hasApplicationAccess() && safeReturn ? safeReturn : this.accountContext.destination());
      },
      error: (error: HttpErrorResponse) => this.apiError.set(error.error?.message ?? 'Sign in failed. Please try again.'),
    });
  }
}
