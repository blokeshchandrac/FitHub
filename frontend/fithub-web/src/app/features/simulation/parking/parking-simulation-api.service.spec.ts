import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthenticationService } from '../../../core/authentication/authentication.service';
import { ParkingSimulationApiService } from './parking-simulation-api.service';

describe('ParkingSimulationApiService', () => {
  let http: HttpTestingController;
  let service: ParkingSimulationApiService;
  const authentication = {
    accessToken: signal<string | null>('admin-token'),
    restoreSession: vi.fn(() => of(true)),
    hasRole: vi.fn(() => true),
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ParkingSimulationApiService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthenticationService, useValue: authentication },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(ParkingSimulationApiService);
    authentication.restoreSession.mockReturnValue(of(true));
    authentication.hasRole.mockReturnValue(true);
  });

  afterEach(() => http.verify());

  it('uses the authenticated parking API when Connected Mode is requested', async () => {
    const resultPromise = service.checkAvailability();
    await Promise.resolve();
    http.expectOne('/api/simulation/parking/status').flush({ enabled: true, hubUrl: '/hubs/parking', cameraId: 'SIM-PARK-CAMERA-01', normalScanSeconds: 60, demoScanSeconds: 5 });
    expect((await resultPromise)?.hubUrl).toBe('/hubs/parking');
    expect(service.backendState()).toBe('connected');
  });

  it('reports Administrator authorization failures instead of silently doing nothing', async () => {
    authentication.hasRole.mockReturnValueOnce(false);
    expect(await service.checkAvailability()).toBeNull();
    expect(service.connectionMessage()).toContain('Administrator permission');
  });
});
