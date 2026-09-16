import { ParkingCar, createParkingSpaces } from './parking-simulation.models';
import { VirtualParkingVisionService } from './virtual-parking-vision.service';

describe('VirtualParkingVisionService', () => {
  const service = new VirtualParkingVisionService();
  const spaces = createParkingSpaces();
  const observedAt = '2026-09-04T12:00:00Z';

  it('marks exactly one space occupied for a correctly parked car', () => {
    const result = service.analyse(spaces, [car('Car 01', -7.2, 9, 0)], observedAt, true, false);
    expect(result.observations.find(x => x.spaceId === 'P-A1')?.status).toBe('OCCUPIED');
    expect(result.observations.filter(x => x.status === 'OCCUPIED')).toHaveLength(1);
    expect(result.detectedCars).toBe(1);
    expect(result.violations).toHaveLength(0);
  });

  it('marks two spaces misused while counting one obstructing car', () => {
    const result = service.analyse(spaces, [car('Car 02', 0, 9, 0)], observedAt, true, false);
    expect(result.observations.filter(x => x.status === 'MISUSED_OR_BLOCKED').map(x => x.spaceId)).toEqual(['P-A2', 'P-A3']);
    expect(result.detectedCars).toBe(1);
    expect(result.violations).toHaveLength(1);
    expect(result.violations[0].affectedSpaceIds).toEqual(['P-A2', 'P-A3']);
  });

  it('does not treat a car on the entrance road as parked', () => {
    const result = service.analyse(spaces, [car('Car 01', -8, 19, Math.PI)], observedAt, true, false);
    expect(result.detectedCars).toBe(0);
    expect(result.observations.every(x => x.status === 'AVAILABLE')).toBe(true);
  });

  it('returns unknown rather than guessing for offline or obstructed captures', () => {
    const offline = service.analyse(spaces, [], observedAt, false, false);
    const obstructed = service.analyse(spaces, [], observedAt, true, true);
    expect(offline.observations.every(x => x.status === 'UNKNOWN' && x.confidence === 0)).toBe(true);
    expect(obstructed.observations.every(x => x.status === 'UNKNOWN' && x.confidence < 0.6)).toBe(true);
  });
});

function car(id: string, x: number, z: number, heading: number): ParkingCar {
  return { id, position: { x, z }, heading, speed: 0, color: 0x35d7f4 };
}
