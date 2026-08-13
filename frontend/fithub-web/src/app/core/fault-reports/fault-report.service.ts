import { HttpClient } from '@angular/common/http'; import { inject, Injectable } from '@angular/core'; import { FaultReport } from './fault-report.models';
@Injectable({ providedIn: 'root' }) export class FaultReportService {
  private readonly http = inject(HttpClient);
  mine() { return this.http.get<readonly FaultReport[]>('/api/fault-reports'); }
  administration() { return this.http.get<readonly FaultReport[]>('/api/administration/fault-reports'); }
  create(data: FormData) { return this.http.post<FaultReport>('/api/fault-reports', data); }
  update(id: string, status: string, maintenanceNotes: string) { return this.http.put<FaultReport>(`/api/administration/fault-reports/${id}`, { status, maintenanceNotes }); }
}
