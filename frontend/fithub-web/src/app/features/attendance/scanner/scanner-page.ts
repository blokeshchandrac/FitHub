import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { AttendanceService } from '../../../core/attendance/attendance.service';

@Component({ selector: 'app-scanner-page', imports: [ReactiveFormsModule], templateUrl: './scanner-page.html' })
export class ScannerPage {
  private readonly service = inject(AttendanceService);
  protected readonly token = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] });
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);

  protected scan(): void {
    if (this.token.invalid) { this.token.markAsTouched(); return; }
    this.message.set(null); this.error.set(null);
    this.service.checkIn(this.token.value.trim()).subscribe({
      next: (result) => { this.message.set(`Check-in accepted at ${new Date(result.checkedInAt).toLocaleString()}.`); this.token.setValue(''); },
      error: (error: HttpErrorResponse) => this.error.set(error.error?.message ?? 'Check-in was rejected.'),
    });
  }
}
