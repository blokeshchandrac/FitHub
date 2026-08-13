import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { MemberDashboard } from './dashboard.models';
@Injectable({ providedIn: 'root' })
export class DashboardService { private readonly http = inject(HttpClient); get(): Observable<MemberDashboard> { return this.http.get<MemberDashboard>('/api/dashboard'); } }
