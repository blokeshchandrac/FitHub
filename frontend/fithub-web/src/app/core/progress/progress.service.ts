import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BodyMeasurement, BodyMeasurementCommand, BodyProgress } from './progress.models';

@Injectable({ providedIn: 'root' })
export class ProgressService {
  private readonly http = inject(HttpClient);
  getProgress(): Observable<BodyProgress> { return this.http.get<BodyProgress>('/api/progress'); }
  logMeasurement(command: BodyMeasurementCommand): Observable<BodyMeasurement> { return this.http.post<BodyMeasurement>('/api/progress', command); }
}
