import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { AuthenticationService } from '../../core/authentication/authentication.service';
import { EquipmentChanged } from '../../core/equipment/equipment.models';
import {
  SimulationBackendStatus,
  SimulationConnectionState,
  SimulationEquipmentMapping,
  SimulationEventRequest,
  SimulationEventResponse,
  SimulationMemberResponse,
} from './simulation.models';

@Injectable()
export class SimulationApiService {
  private readonly http = inject(HttpClient);
  private readonly authentication = inject(AuthenticationService);
  private connection?: HubConnection;
  readonly backendState = signal<SimulationConnectionState>('disconnected');
  readonly signalRState = signal<SimulationConnectionState>('disconnected');
  readonly connectionMessage = signal('Local Mode — no equipment changes are sent to FitHub.');
  readonly lastBroadcast = signal<EquipmentChanged | null>(null);

  async checkAvailability(): Promise<SimulationBackendStatus | null> {
    this.backendState.set('connecting');
    this.connectionMessage.set('Connecting — restoring the Administrator session…');
    const authenticated = await firstValueFrom(this.authentication.restoreSession());
    if (!authenticated) {
      this.backendState.set('connection-failed');
      this.connectionMessage.set('Authentication required — sign in as an Administrator, then try again.');
      return null;
    }
    if (!this.authentication.hasRole('Administrator')) {
      this.backendState.set('connection-failed');
      this.connectionMessage.set('Administrator permission is required for Connected Mode.');
      return null;
    }
    try {
      const status = await firstValueFrom(this.http.get<SimulationBackendStatus>('/api/simulation/status'));
      if (!status.enabled) {
        this.backendState.set('connection-failed');
        this.connectionMessage.set('The development simulation API is disabled.');
        return null;
      }
      this.backendState.set('connected');
      this.connectionMessage.set('API verified. Connecting to live equipment updates…');
      return status;
    } catch (error) {
      this.backendState.set('connection-failed');
      this.connectionMessage.set(connectionFailure(error, 'equipment'));
      return null;
    }
  }

  async connectHub(hubUrl: string): Promise<boolean> {
    if (this.connection?.state === HubConnectionState.Connected) return true;
    await this.disconnectHub(false, true);
    this.signalRState.set('connecting');
    this.connectionMessage.set('API verified. Connecting to EquipmentHub…');
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => this.authentication.accessToken() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    this.connection = connection;
    // Connected Mode listens to the normal feature hub so the simulator and application pages
    // observe the same committed equipment state rather than a simulation-only event stream.
    connection.on('equipmentChanged', (event: EquipmentChanged) => this.lastBroadcast.set(event));
    connection.onreconnecting(() => {
      this.signalRState.set('reconnecting');
      this.connectionMessage.set('Reconnecting to EquipmentHub…');
    });
    connection.onreconnected(() => {
      this.signalRState.set('connected');
      this.connectionMessage.set('Connected — equipment changes are persisted and broadcast live.');
    });
    connection.onclose(error => {
      this.signalRState.set(error ? 'connection-failed' : 'disconnected');
      this.connectionMessage.set(error ? 'EquipmentHub disconnected unexpectedly.' : 'Disconnected from Connected Mode.');
    });
    try {
      await connection.start();
      this.signalRState.set('connected');
      this.connectionMessage.set('Connected — equipment changes are persisted and broadcast live.');
      return true;
    } catch {
      await this.disconnectHub(false, true);
      this.signalRState.set('connection-failed');
      this.connectionMessage.set('SignalR unavailable — the EquipmentHub connection failed.');
      return false;
    }
  }

  send(event: SimulationEventRequest): Promise<SimulationEventResponse> {
    return firstValueFrom(this.http.post<SimulationEventResponse>('/api/simulation/events', event));
  }

  getMembers(): Promise<readonly SimulationMemberResponse[]> {
    return firstValueFrom(this.http.get<readonly SimulationMemberResponse[]>('/api/simulation/members'));
  }

  getEquipment(): Promise<readonly SimulationEquipmentMapping[]> {
    return firstValueFrom(this.http.get<readonly SimulationEquipmentMapping[]>('/api/simulation/equipment'));
  }

  async disconnectHub(showMessage = true, preserveBackend = false): Promise<void> {
    const connection = this.connection;
    this.connection = undefined;
    if (connection) {
      // Remove named handlers before stopping so reconnects cannot multiply subscriptions.
      connection.off('equipmentChanged');
      await connection.stop().catch(() => undefined);
    }
    this.signalRState.set('disconnected');
    if (!preserveBackend) this.backendState.set('disconnected');
    if (showMessage) this.connectionMessage.set('Disconnected — Local Mode remains available.');
  }
}

function connectionFailure(error: unknown, feature: string): string {
  if (!(error instanceof HttpErrorResponse)) return `The ${feature} API is unavailable.`;
  if (error.status === 0) return `API unavailable — the ${feature} backend could not be reached.`;
  if (error.status === 401) return 'Authentication required — sign in as an Administrator, then try again.';
  if (error.status === 403) return 'Administrator permission is required for Connected Mode.';
  if (error.status === 404) return 'Connected Mode is disabled in this environment.';
  return error.error?.detail ?? `The ${feature} API rejected the connection (${error.status}).`;
}
