import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthenticationService } from '../../../core/authentication/authentication.service';

@Component({
  selector: 'app-forgot-password-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password-page.html',
})
export class ForgotPasswordPage {
  private readonly authentication = inject(AuthenticationService);
  protected readonly submitting = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly apiError = signal<string | null>(null);
  protected readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email, Validators.maxLength(254)] }),
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.message.set(null);
    this.apiError.set(null);
    this.authentication.requestPasswordReset(this.form.controls.email.value).pipe(
      finalize(() => this.submitting.set(false)),
    ).subscribe({
      next: (response) => this.message.set(response.message),
      error: (error: HttpErrorResponse) => this.apiError.set(error.error?.message ?? 'The request could not be completed. Please try again later.'),
    });
  }
}
