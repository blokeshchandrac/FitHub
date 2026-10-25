import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthenticationService } from '../../../core/authentication/authentication.service';

const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$/;

const matchingPasswords: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('newPassword')?.value;
  const confirmation = control.get('confirmPassword')?.value;
  return password && confirmation && password !== confirmation ? { passwordMismatch: true } : null;
};

@Component({
  selector: 'app-reset-password-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password-page.html',
})
export class ResetPasswordPage {
  private readonly authentication = inject(AuthenticationService);
  private readonly route = inject(ActivatedRoute);
  private readonly email = this.route.snapshot.queryParamMap.get('email') ?? '';
  private readonly token = this.route.snapshot.queryParamMap.get('token') ?? '';

  protected readonly submitting = signal(false);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly linkIsComplete = this.email.length > 0 && this.token.length >= 32;
  protected readonly form = new FormGroup({
    newPassword: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(12), Validators.maxLength(256), Validators.pattern(PASSWORD_PATTERN)],
    }),
    confirmPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  }, { validators: matchingPasswords });

  protected submit(): void {
    if (!this.linkIsComplete || this.form.invalid) {
      this.form.markAllAsTouched();
      if (!this.linkIsComplete) this.errorMessage.set('This password-reset link is incomplete or invalid.');
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.authentication.resetPassword({
      email: this.email,
      token: this.token,
      newPassword: this.form.controls.newPassword.value,
      confirmPassword: this.form.controls.confirmPassword.value,
    }).pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: (response) => this.successMessage.set(response.message),
      error: (error: HttpErrorResponse) => this.errorMessage.set(
        error.error?.message ?? 'The password could not be reset. Request a new link and try again.',
      ),
    });
  }
}
