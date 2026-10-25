import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { AuthenticationService } from '../authentication/authentication.service';
import { RuntimeConfigService } from '../configuration/runtime-config.service';
import { EquipmentChanged, EquipmentDetail, EquipmentSummary } from './equipment.models';
@Injectable({ providedIn: 'root' })
export class EquipmentService {
  private readonly http = inject(HttpClient); private readonly auth = inject(AuthenticationService); private readonly runtimeConfig = inject(RuntimeConfigService); private connection?: HubConnection;
  private readonly changesSubject = new Subject<EquipmentChanged>(); readonly changes = this.changesSubject.asObservable();
  list(): Observable<readonly EquipmentSummary[]> { return this.http.get<readonly EquipmentSummary[]>('/api/equipment'); }
  detail(id: string): Observable<EquipmentDetail> { return this.http.get<EquipmentDetail>(`/api/equipment/${id}`); }
  claim(id: string) { return this.http.post<EquipmentDetail>(`/api/equipment/${id}/claim`, {}); }
  sharing(id: string) { return this.http.post<EquipmentDetail>(`/api/equipment/${id}/sharing-allowed`, {}); }
  occupied(id: string) { return this.http.post<EquipmentDetail>(`/api/equipment/${id}/occupied`, {}); }
  release(id: string) { return this.http.post<EquipmentDetail>(`/api/equipment/${id}/release`, { clearanceConfirmed: false }); }
  override(id: string, state: string, reason: string) { return this.http.post<EquipmentDetail>(`/api/equipment/${id}/override`, { state, reason }); }
  async connect(): Promise<void> {
    if (this.connection) return;
    this.connection = new HubConnectionBuilder().withUrl(this.runtimeConfig.hubUrl('/hubs/equipment'), { accessTokenFactory: () => this.auth.accessToken() ?? '', withCredentials: true }).withAutomaticReconnect().configureLogging(LogLevel.Warning).build();
    this.connection.on('equipmentChanged', change => this.changesSubject.next(change));
    try { await this.connection.start(); } catch { this.connection = undefined; }
  }
}
