import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { AttendanceSummary, CheckInToken } from '../../core/attendance/attendance.models';
import { AttendanceService } from '../../core/attendance/attendance.service';
import QRCode from 'qrcode';

@Component({ selector: 'app-attendance-page', imports: [CurrencyPipe, DatePipe], templateUrl: './attendance-page.html' })
export class AttendancePage {
  private readonly service = inject(AttendanceService);
  protected readonly summary = signal<AttendanceSummary | null>(null);
  protected readonly checkInToken = signal<CheckInToken | null>(null);
  protected readonly qrImage = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly generating = signal(false);

  constructor() { this.load(); }

  protected generateToken(): void {
    this.generating.set(true);
    this.error.set(null);
    this.service.generateToken().subscribe({
      next: (token) => {
        this.checkInToken.set(token);
        void QRCode.toDataURL(token.token, { errorCorrectionLevel: 'M', margin: 2, width: 280 })
          .then((image) => this.qrImage.set(image))
          .catch(() => this.error.set('The QR image could not be rendered; use the token text instead.'));
        this.generating.set(false);
      },
      error: (error: HttpErrorResponse) => { this.error.set(error.error?.message ?? 'An active membership is required to generate a check-in token.'); this.generating.set(false); },
    });
  }

  private load(): void {
    this.service.getSummary().subscribe({ next: (value) => this.summary.set(value), error: () => this.error.set('Attendance history could not be loaded.') });
  }
}
