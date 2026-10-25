import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthenticationService } from '../../core/authentication/authentication.service';
import { SimulationApiService } from './simulation-api.service';

describe('SimulationApiService', () => {
  let http: HttpTestingController;
  let service: SimulationApiService;
  const authentication = {
    accessToken: signal<string | null>('admin-token'),
    restoreSession: vi.fn(() => of(true)),
    hasRole: vi.fn(() => true),
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        SimulationApiService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthenticationService, useValue: authentication },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(SimulationApiService);
    authentication.restoreSession.mockReturnValue(of(true));
    authentication.hasRole.mockReturnValue(true);
  });

  afterEach(() => http.verify());

  it('restores the Administrator session and loads persisted equipment mappings', async () => {
    const resultPromise = service.checkAvailability();
    await Promise.resolve();
    http.expectOne('/api/simulation/status').flush({
      enabled: true,
      hubUrl: '/hubs/equipment',
      clearanceThresholdMetres: 2.2,
      clearanceSeconds: 30,
      equipment: [{ simulationEquipmentId: 'EQ-TREADMILL-01', deviceId: 'SIM-ESP32-01', persistedEquipmentId: 'real-id', persistedCode: 'TREAD-001', state: 'AVAILABLE', ownerMemberId: null, partnerMemberId: null, presenceDetected: null, presenceChangedAt: null, clearanceStartedAt: null, lastUpdatedAt: new Date().toISOString() }],
    });
    expect((await resultPromise)?.hubUrl).toBe('/hubs/equipment');
    expect(service.backendState()).toBe('connected');
    expect(authentication.restoreSession).toHaveBeenCalledOnce();
  });

  it('shows a useful failure without calling the API when authentication is absent', async () => {
    authentication.restoreSession.mockReturnValueOnce(of(false));
    expect(await service.checkAvailability()).toBeNull();
    expect(service.backendState()).toBe('connection-failed');
    expect(service.connectionMessage()).toContain('Authentication required');
  });

  it('distinguishes an unreachable backend from an API rejection', () => {
    expect(service.submissionFailure(new HttpErrorResponse({ status: 0 }))).toMatchObject({ code: 'BACKEND_UNAVAILABLE' });
    expect(service.submissionFailure(new HttpErrorResponse({ status: 400 }))).toMatchObject({ code: 'BACKEND_REJECTED' });
  });
});
