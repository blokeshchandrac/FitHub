import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { AuthenticationService } from '../../../core/authentication/authentication.service';
import {
  ParkingConnectionState,
  ParkingSimulationBackendStatus,
  ParkingSimulationScanRequest,
  ParkingSimulationScanResponse,
} from './parking-simulation.models';

@Injectable()
export class ParkingSimulationApiService {
  private readonly http = inject(HttpClient);
  private readonly authentication = inject(AuthenticationService);
  private connection?: HubConnection;
  readonly backendState = signal<ParkingConnectionState>('disconnected');
  readonly signalRState = signal<ParkingConnectionState>('disconnected');
  readonly connectionMessage = signal('Local Mode — no parking observations are sent to FitHub.');
  readonly lastRealtimeEvent = signal<string | null>(null);

  async checkAvailability(): Promise<ParkingSimulationBackendStatus | null> {
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
      const status = await firstValueFrom(this.http.get<ParkingSimulationBackendStatus>('/api/simulation/parking/status'));
      if (!status.enabled) {
        this.backendState.set('connection-failed');
        this.connectionMessage.set('The development parking simulation API is disabled.');
        return null;
      }
      this.backendState.set('connected');
      this.connectionMessage.set('API verified. Connecting to live parking updates…');
      return status;
    } catch (error) {
      this.backendState.set('connection-failed');
      this.connectionMessage.set(connectionFailure(error));
      return null;
    }
  }

  async connectHub(hubUrl: string): Promise<boolean> {
    if (this.connection?.state === HubConnectionState.Connected) return true;
    await this.disconnectHub(false, true);
    this.signalRState.set('connecting');
    this.connectionMessage.set('API verified. Connecting to ParkingHub…');
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => this.authentication.accessToken() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    this.connection = connection;
    // Use the production-facing ParkingHub contract so committed simulation observations reach
    // the existing Administrator and Member parking pages.
    connection.on('parkingChanged', event => this.lastRealtimeEvent.set(`Space ${String(event.code)} updated`));
    connection.on('parkingAlertChanged', event => this.lastRealtimeEvent.set(`Parking alert ${String(event.status).toLowerCase()}`));
    connection.onreconnecting(() => {
      this.signalRState.set('reconnecting');
      this.connectionMessage.set('Reconnecting to ParkingHub…');
    });
    connection.onreconnected(() => {
      this.signalRState.set('connected');
      this.connectionMessage.set('Connected — accepted scans are persisted and broadcast live.');
    });
    connection.onclose(error => {
      this.signalRState.set(error ? 'connection-failed' : 'disconnected');
      this.connectionMessage.set(error ? 'ParkingHub disconnected unexpectedly.' : 'Disconnected from Connected Mode.');
    });
    try {
      await connection.start();
      this.signalRState.set('connected');
      this.connectionMessage.set('Connected — accepted scans are persisted and broadcast live.');
      return true;
    } catch {
      await this.disconnectHub(false, true);
      this.signalRState.set('connection-failed');
      this.connectionMessage.set('SignalR unavailable — the ParkingHub connection failed.');
      return false;
    }
  }

  submit(request: ParkingSimulationScanRequest): Promise<ParkingSimulationScanResponse> {
    return firstValueFrom(this.http.post<ParkingSimulationScanResponse>('/api/simulation/parking/scans', request));
  }

  async disconnectHub(showMessage = true, preserveBackend = false): Promise<void> {
    const connection = this.connection;
    this.connection = undefined;
    if (connection) {
      // Explicitly detach both handlers before reconnecting or destroying the simulator service.
      connection.off('parkingChanged');
      connection.off('parkingAlertChanged');
      await connection.stop().catch(() => undefined);
    }
    this.signalRState.set('disconnected');
    if (!preserveBackend) this.backendState.set('disconnected');
    if (showMessage) this.connectionMessage.set('Disconnected — Local Mode remains available.');
  }
}

function connectionFailure(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return 'The parking API is unavailable.';
  if (error.status === 0) return 'API unavailable — the parking backend could not be reached.';
  if (error.status === 401) return 'Authentication required — sign in as an Administrator, then try again.';
  if (error.status === 403) return 'Administrator permission is required for Connected Mode.';
  if (error.status === 404) return 'Connected Mode is disabled in this environment.';
  return error.error?.detail ?? `The parking API rejected the connection (${error.status}).`;
}
