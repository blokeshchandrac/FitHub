import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AttendanceSummary, CheckInResult, CheckInToken } from './attendance.models';

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly http = inject(HttpClient);

  getSummary(): Observable<AttendanceSummary> { return this.http.get<AttendanceSummary>('/api/attendance'); }
  generateToken(): Observable<CheckInToken> { return this.http.post<CheckInToken>('/api/attendance/check-in-token', {}); }
  checkIn(token: string): Observable<CheckInResult> { return this.http.post<CheckInResult>('/api/attendance/check-in', { token }); }
}
