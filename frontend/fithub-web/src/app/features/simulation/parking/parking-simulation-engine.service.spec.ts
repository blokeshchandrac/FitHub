import { ParkingCar } from './parking-simulation.models';
import { ParkingSimulationEngineService } from './parking-simulation-engine.service';
import { VirtualParkingVisionService } from './virtual-parking-vision.service';
import { PARKING_EVENT_STORAGE_KEY } from './parking-event-log';

describe('ParkingSimulationEngineService', () => {
  beforeEach(() => sessionStorage.removeItem(PARKING_EVENT_STORAGE_KEY));

  it('deduplicates an unchanged violation and resolves it after correct parking', () => {
    const engine = new ParkingSimulationEngineService(new VirtualParkingVisionService());
    const obstructing = car('Car 01', 0, 9);
    engine.beginScan();
    engine.completeScan([obstructing], null);
    expect(engine.summary().activeAlerts).toBe(1);
    expect(engine.alerts()).toHaveLength(1);

    engine.beginScan();
    engine.completeScan([obstructing], null);
    expect(engine.alerts()).toHaveLength(1);
    expect(engine.events().filter(x => x.action === 'PARKING_ALERT_CREATED')).toHaveLength(1);

    engine.beginScan();
    engine.completeScan([car('Car 01', -2.4, 9)], null);
    expect(engine.summary().activeAlerts).toBe(0);
    expect(engine.alerts()[0].status).toBe('RESOLVED');
    expect(engine.events().some(x => x.action === 'PARKING_VIOLATION_RESOLVED' && x.source === 'Car 01')).toBe(true);
    expect(engine.events().some(x => x.action === 'PARKING_SPACE_AVAILABLE')).toBe(true);
  });

  it('uses 60-second normal scans and five-second demo scans', () => {
    const engine = new ParkingSimulationEngineService(new VirtualParkingVisionService());
    const now = performance.now();
    engine.configureIntervals(60, 5);
    expect(engine.secondsUntilScan()).toBe(60);
    engine.setDemoSpeed(true, now);
    expect(engine.secondsUntilScan()).toBe(5);
    expect(engine.tick(now + 4_000)).toBe(false);
    expect(engine.tick(now + 5_100)).toBe(true);
  });

  it('keeps cars on the road out of parking totals', () => {
    const engine = new ParkingSimulationEngineService(new VirtualParkingVisionService());
    engine.beginScan();
    engine.completeScan([car('Car 01', -9, 19)], null);
    expect(engine.summary().available).toBe(12);
    expect(engine.summary().detectedCars).toBe(0);
  });

  it('stores event sources and clears only the browser event log', () => {
    const engine = new ParkingSimulationEngineService(new VirtualParkingVisionService());
    engine.addCar();
    expect(engine.events()[0].source).toBe('Car 01');
    expect(sessionStorage.getItem(PARKING_EVENT_STORAGE_KEY)).toContain('CAR_ADDED');

    engine.clearLog();
    expect(engine.events()).toEqual([]);
    expect(sessionStorage.getItem(PARKING_EVENT_STORAGE_KEY)).toBeNull();
    expect(engine.cars()).toHaveLength(1);
  });
});

function car(id: string, x: number, z: number): ParkingCar {
  return { id, position: { x, z }, heading: 0, speed: 0, color: 0xc9ff4a };
}
