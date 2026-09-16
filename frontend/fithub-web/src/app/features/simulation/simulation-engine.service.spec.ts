import { SimulationEngineService } from './simulation-engine.service';
import { SimulationMemberResponse } from './simulation.models';

describe('SimulationEngineService', () => {
  let engine: SimulationEngineService;

  beforeEach(() => {
    engine = new SimulationEngineService();
    engine.replaceMembers(MEMBERS);
  });
  afterEach(() => engine.dispose());

  it('loads database Member identities without assigning outcomes by name', () => {
    expect(engine.members().map(member => member.name)).toEqual(['Alex Existing', 'Blair Existing', 'Casey Existing']);
    expect(engine.members().every(member => member.statusOverride === 'Actual')).toBe(true);
  });

  it('replaces repeated Member refreshes without creating duplicates', () => {
    engine.replaceMembers(MEMBERS);
    engine.replaceMembers(MEMBERS);
    expect(engine.members()).toHaveLength(3);
    expect(new Set(engine.members().map(member => member.id)).size).toBe(3);
  });

  it('uses a 30-second elapsed-time countdown before automatic release', () => {
    claim(engine, MEMBERS[0]);
    engine.updateSensor('EQ-TREADMILL-01', 'AREA_CLEAR', 4, 100);
    expect(engine.selectedMachine()).toMatchObject({ state: 'CLEARANCE_WARNING', clearanceRemainingSeconds: 30 });

    engine.updateSensor('EQ-TREADMILL-01', 'AREA_CLEAR', 4, 29_100);
    expect(engine.selectedMachine()).toMatchObject({ state: 'CLEARANCE_WARNING', clearanceRemainingSeconds: 1 });
    engine.updateSensor('EQ-TREADMILL-01', 'AREA_CLEAR', 4, 30_100);
    expect(engine.selectedMachine()).toMatchObject({ state: 'AVAILABLE', clearanceRemainingSeconds: null, memberIds: [] });
    expect(engine.events()[0].action).toBe('AUTOMATIC_RELEASE_COMPLETED');
  });

  it('cancels clearance and restores the active state when a Member returns', () => {
    claim(engine, MEMBERS[0]);
    engine.enableSharing('EQ-TREADMILL-01');
    engine.updateSensor('EQ-TREADMILL-01', 'AREA_CLEAR', 4, 100);
    engine.updateSensor('EQ-TREADMILL-01', 'PERSON_DETECTED', 1.2, 3_000);
    expect(engine.selectedMachine()).toMatchObject({ state: 'SHARING_ALLOWED', clearanceDeadlineMs: null });
    expect(engine.events()[0].action).toBe('CLEARANCE_COUNTDOWN_CANCELLED');
  });

  it('waits until all Members on a shared machine are outside', () => {
    claim(engine, MEMBERS[0]);
    engine.returnCard(MEMBERS[0].memberId);
    engine.enableSharing('EQ-TREADMILL-01');
    enterAndTake(engine, MEMBERS[1]);
    engine.scanMachine(MEMBERS[1].cardId, 'EQ-TREADMILL-01');

    engine.updateSensor('EQ-TREADMILL-01', 'PERSON_DETECTED', 1.5, 100);
    expect(engine.selectedMachine()?.state).toBe('SHARING_ALLOWED');
    engine.updateSensor('EQ-TREADMILL-01', 'AREA_CLEAR', 4, 200);
    expect(engine.selectedMachine()?.state).toBe('CLEARANCE_WARNING');
  });

  it('lets a sharing partner finish while the owner continues in sharing mode', () => {
    claim(engine, MEMBERS[0]);
    engine.returnCard(MEMBERS[0].memberId);
    engine.enableSharing('EQ-TREADMILL-01');
    enterAndTake(engine, MEMBERS[1]);
    engine.scanMachine(MEMBERS[1].cardId, 'EQ-TREADMILL-01');

    expect(engine.finishSession(MEMBERS[1].memberId).allowed).toBe(true);
    expect(engine.selectedMachine()).toMatchObject({ state: 'SHARING_ALLOWED', memberIds: [MEMBERS[0].memberId] });
    expect(engine.members().find(member => member.id === MEMBERS[1].memberId)?.machineId).toBeNull();
  });

  it('rejects a third Member from a shared machine', () => {
    claim(engine, MEMBERS[0]);
    engine.returnCard(MEMBERS[0].memberId);
    engine.enableSharing('EQ-TREADMILL-01');
    enterAndTake(engine, MEMBERS[1]);
    engine.scanMachine(MEMBERS[1].cardId, 'EQ-TREADMILL-01');
    engine.returnCard(MEMBERS[1].memberId);
    enterAndTake(engine, MEMBERS[2]);

    expect(engine.scanMachine(MEMBERS[2].cardId, 'EQ-TREADMILL-01')).toMatchObject({ allowed: false, code: 'SHARING_FULL' });
  });

  it('applies status overrides only to the current session', () => {
    engine.setStatusOverride(MEMBERS[0].memberId, 'ExpiredMembership');
    expect(engine.enterGym(MEMBERS[0].cardId)).toMatchObject({ allowed: false, code: 'MEMBERSHIP_EXPIRED' });
    expect(engine.members()[0].membershipStatus).toBe('ACTIVE');
    engine.setStatusOverride(MEMBERS[0].memberId, 'Actual');
    expect(engine.enterGym(MEMBERS[0].cardId).allowed).toBe(true);
  });

  it('moves to a safe unknown state and cancels release when the sensor fails', () => {
    claim(engine, MEMBERS[0]);
    engine.updateSensor('EQ-TREADMILL-01', 'AREA_CLEAR', 4, 100);
    engine.toggleWire('EQ-TREADMILL-01', 'ultrasonic-echo');
    expect(engine.selectedMachine()).toMatchObject({ state: 'OFFLINE_UNKNOWN', sensorResult: 'SENSOR_FAULT', clearanceDeadlineMs: null });
  });

  it('keeps timers and states independent for all four machines', () => {
    claim(engine, MEMBERS[0]);
    engine.updateSensor('EQ-TREADMILL-01', 'AREA_CLEAR', 4, 100);
    expect(engine.machines().find(machine => machine.id === 'EQ-TREADMILL-01')?.state).toBe('CLEARANCE_WARNING');
    expect(engine.machines().filter(machine => machine.id !== 'EQ-TREADMILL-01').every(machine => machine.state === 'AVAILABLE' && machine.clearanceDeadlineMs === null)).toBe(true);
  });
});

const MEMBERS: readonly SimulationMemberResponse[] = [
  member('11111111-1111-1111-1111-111111111111', 'CARD-A', 'Alex Existing'),
  member('22222222-2222-2222-2222-222222222222', 'CARD-B', 'Blair Existing'),
  member('33333333-3333-3333-3333-333333333333', 'CARD-C', 'Casey Existing'),
];

function member(memberId: string, cardId: string, displayName: string): SimulationMemberResponse {
  return { memberId, cardId, displayName, membershipStatus: 'ACTIVE', cardStatus: 'ACTIVE', accountStatus: 'ACTIVE', cardSource: 'FIT_HUB_CARD' };
}

function enterAndTake(engine: SimulationEngineService, member: SimulationMemberResponse): void {
  expect(engine.enterGym(member.cardId).allowed).toBe(true);
  expect(engine.takeCard(member.memberId).allowed).toBe(true);
}

function claim(engine: SimulationEngineService, member: SimulationMemberResponse): void {
  enterAndTake(engine, member);
  expect(engine.scanMachine(member.cardId, 'EQ-TREADMILL-01').allowed).toBe(true);
}
