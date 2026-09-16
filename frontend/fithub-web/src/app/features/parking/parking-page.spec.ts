import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { AuthenticationService } from '../../core/authentication/authentication.service';
import { ParkingChanged } from '../../core/parking/parking.models';
import { ParkingService } from '../../core/parking/parking.service';
import { ParkingPage } from './parking-page';

describe('ParkingPage realtime updates', () => {
  it('coalesces a scan batch into one read-only overview reload', async () => {
    vi.useFakeTimers();
    const changes = new Subject<ParkingChanged | null>();
    const service = {
      changed: changes.asObservable(),
      overview: vi.fn(() => of({ total: 12, available: 12, occupied: 0, misusedOrBlocked: 0, unknown: 0, disabled: 0, spaces: [] })),
      alerts: vi.fn(() => of([])),
      connect: vi.fn(() => Promise.resolve()),
    };
    const authentication = { hasRole: vi.fn(() => false) };
    TestBed.configureTestingModule({
      imports: [ParkingPage],
      providers: [
        { provide: ParkingService, useValue: service },
        { provide: AuthenticationService, useValue: authentication },
      ],
    });

    TestBed.createComponent(ParkingPage).detectChanges();
    changes.next({ spaceId: 'space-1', code: 'P-A1', state: 'Occupied', confidence: 0.99, source: 'SIM-PARK-CAMERA-01', eventTime: new Date().toISOString() });
    changes.next({ spaceId: 'space-2', code: 'P-A2', state: 'Available', confidence: 0.99, source: 'SIM-PARK-CAMERA-01', eventTime: new Date().toISOString() });
    await vi.advanceTimersByTimeAsync(80);

    expect(service.overview).toHaveBeenCalledTimes(2);
    expect(service.connect).toHaveBeenCalledOnce();
    vi.useRealTimers();
  });
});
