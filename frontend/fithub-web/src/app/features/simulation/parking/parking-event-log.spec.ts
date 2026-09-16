import { buildParkingEventCsv, parkingEventLogFilename, restoreParkingEvents } from './parking-event-log';
import { ParkingSimulationEvent } from './parking-simulation.models';

describe('parking event log', () => {
  const event: ParkingSimulationEvent = {
    id: 1,
    occurredAt: '2026-09-04T12:34:56Z',
    action: 'CAMERA_SCAN_COMPLETED',
    source: 'SIM-PARK-CAMERA-01',
    detail: 'All 12 spaces analysed, including "A1".',
    result: 'Success',
    tone: 'success',
    mode: 'local',
  };

  it('exports the displayed columns and safely quotes CSV details', () => {
    const csv = buildParkingEventCsv([event]);
    expect(csv).toContain('Time,Event,Source,Details,Result');
    expect(csv).toContain('CAMERA SCAN COMPLETED');
    expect(csv).toContain('SIM-PARK-CAMERA-01');
    expect(csv).toContain('"All 12 spaces analysed, including ""A1""."');
  });

  it('creates the requested timestamped filename', () => {
    expect(parkingEventLogFilename(new Date(2026, 8, 4, 7, 8, 9)))
      .toBe('FitHub_Parking_Simulation_Log_2026-09-04_07-08-09.csv');
  });

  it('rejects malformed persisted entries', () => {
    const storage = { getItem: () => JSON.stringify([event, { id: 'unsafe' }]) } as Pick<Storage, 'getItem'> as Storage;
    expect(restoreParkingEvents(storage)).toEqual([event]);
  });
});
