import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, switchMap } from 'rxjs';
import { AuthenticationService } from '../../../core/authentication/authentication.service';
import { AccountContextService } from '../../../core/membership/account-context.service';

function matchingPasswords(control: AbstractControl): ValidationErrors | null {
  return control.get('password')?.value === control.get('confirmPassword')?.value ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-registration-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './registration-page.html',
})
export class RegistrationPage {
  private readonly authentication = inject(AuthenticationService);
  private readonly router = inject(Router);
  private readonly accountContext = inject(AccountContextService);
  protected readonly submitting = signal(false);
  protected readonly apiError = signal<string | null>(null);
  protected readonly form = new FormGroup({
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    lastName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email, Validators.maxLength(254)] }),
    password: new FormControl('', { nonNullable: true, validators: [
      Validators.required,
      Validators.minLength(12),
      Validators.maxLength(256),
      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$/),
    ] }),
    confirmPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  }, { validators: matchingPasswords });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { confirmPassword: _, ...request } = this.form.getRawValue();
    this.submitting.set(true);
    this.apiError.set(null);
    this.authentication.register(request).pipe(
      switchMap(() => this.accountContext.refresh()),
      finalize(() => this.submitting.set(false)),
    ).subscribe({
      next: () => this.router.navigateByUrl(this.accountContext.destination()),
      error: (error: HttpErrorResponse) => this.apiError.set(error.error?.message ?? 'Registration failed. Please review your details.'),
    });
  }
}
