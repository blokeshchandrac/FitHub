export type SimulationMode = 'local' | 'connected';

export type SimulationEquipmentState =
  | 'AVAILABLE'
  | 'OCCUPIED'
  | 'SHARING_ALLOWED'
  | 'CLEARANCE_WARNING'
  | 'TEMPORARILY_UNAVAILABLE'
  | 'OFFLINE_UNKNOWN';

export type SimulationMembershipStatus = 'ACTIVE' | 'PENDING_PAYMENT' | 'PAUSED' | 'EXPIRED' | 'CANCELLED' | 'NONE' | 'INACTIVE';
export type SimulationCardStatus = 'ACTIVE' | 'BLOCKED';
export type SimulationAccountStatus = 'ACTIVE' | 'SUSPENDED';
export type SimulationStatusOverride = 'Actual' | 'ActiveMembership' | 'ExpiredMembership' | 'BlockedCard' | 'SuspendedAccount';
export type SimulationSensorResult = 'PERSON_DETECTED' | 'AREA_CLEAR' | 'SENSOR_FAULT';
export type SimulationConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'connection-failed';
export type SimulationMachineType = 'treadmill' | 'bench' | 'bike' | 'cable';
export type SimulationWire =
  | 'power'
  | 'pn532'
  | 'ultrasonic-trigger'
  | 'ultrasonic-echo'
  | 'green-led'
  | 'red-led'
  | 'yellow-led';

export interface SimulationPosition {
  readonly x: number;
  readonly z: number;
}

export interface SimulationMember {
  readonly id: string;
  readonly cardId: string;
  readonly name: string;
  readonly shortName: string;
  readonly membershipStatus: SimulationMembershipStatus;
  readonly cardStatus: SimulationCardStatus;
  readonly accountStatus: SimulationAccountStatus;
  readonly cardSource: 'FIT_HUB_CARD' | 'SIMULATION_TOKEN';
  readonly statusOverride: SimulationStatusOverride;
  readonly shirtColor: number;
  readonly entered: boolean;
  readonly hasCard: boolean;
  readonly machineId: string | null;
  readonly position: SimulationPosition;
}

export interface SimulationMachine {
  readonly id: string;
  readonly deviceId: string;
  readonly name: string;
  readonly type: SimulationMachineType;
  readonly position: SimulationPosition;
  readonly state: SimulationEquipmentState;
  readonly stateBeforeInterruption: SimulationEquipmentState | null;
  readonly activeStateBeforeClearance: Extract<SimulationEquipmentState, 'OCCUPIED' | 'SHARING_ALLOWED'> | null;
  readonly memberIds: readonly string[];
  readonly distanceMetres: number;
  readonly sensorResult: SimulationSensorResult;
  readonly clearanceDeadlineMs: number | null;
  readonly clearanceRemainingSeconds: number | null;
  readonly disconnectedWires: readonly SimulationWire[];
  readonly safetyLocked: boolean;
  readonly restarting: boolean;
}

export interface SimulationEvent {
  readonly id: number;
  readonly occurredAt: string;
  readonly tone: 'info' | 'success' | 'warning' | 'error';
  readonly title: string;
  readonly detail: string;
  readonly action: string;
  readonly result: string;
  readonly mode: SimulationMode;
  readonly memberName?: string;
  readonly machineName?: string;
  readonly machineId?: string;
  readonly memberId?: string;
}

export interface SimulationDecision {
  readonly allowed: boolean;
  readonly code: string;
  readonly message: string;
}

export interface SimulationBackendStatus {
  readonly enabled: boolean;
  readonly hubUrl: string;
  readonly clearanceThresholdMetres: number;
  readonly clearanceSeconds: number;
  readonly equipment: readonly SimulationEquipmentMapping[];
}

export interface SimulationEquipmentMapping {
  readonly simulationEquipmentId: string;
  readonly deviceId: string;
  readonly persistedEquipmentId: string;
  readonly persistedCode: string;
  readonly state: SimulationEquipmentState;
  readonly ownerMemberId: string | null;
  readonly partnerMemberId: string | null;
  readonly presenceDetected: boolean | null;
  readonly presenceChangedAt: string | null;
  readonly clearanceStartedAt: string | null;
  readonly lastUpdatedAt: string;
}

export interface SimulationMemberResponse {
  readonly memberId: string;
  readonly displayName: string;
  readonly membershipStatus: SimulationMembershipStatus;
  readonly cardId: string;
  readonly cardStatus: SimulationCardStatus;
  readonly accountStatus: SimulationAccountStatus;
  readonly cardSource: 'FIT_HUB_CARD' | 'SIMULATION_TOKEN';
}

export interface SimulationEventRequest {
  readonly simulationSessionId: string;
  readonly deviceId: string;
  readonly equipmentId: string;
  readonly eventType: string;
  readonly cardId?: string;
  readonly distanceMetres?: number;
  readonly observedAt: string;
  readonly simulated: true;
  readonly statusOverride: SimulationStatusOverride;
  readonly memberId?: string;
}

export interface SimulationEventResponse extends SimulationDecision {
  readonly simulationSessionId: string;
  readonly equipmentId: string;
  readonly equipmentState: SimulationEquipmentState;
  readonly memberNames: readonly string[];
  readonly clearanceSecondsRemaining: number | null;
  readonly occurredAt: string;
  readonly persistedEquipmentId: string | null;
  readonly ownerMemberId: string | null;
  readonly partnerMemberId: string | null;
  readonly presenceDetected: boolean | null;
  readonly presenceChangedAt: string | null;
}

export interface SceneHit {
  readonly kind: 'machine' | 'machine-reader' | 'entrance-scanner' | 'member';
  readonly id: string;
}

export const SIMULATION_PIN_CONNECTIONS = [
  ['PN532 SDA', 'GPIO 21'],
  ['PN532 SCL', 'GPIO 22'],
  ['OLED SDA', 'GPIO 21'],
  ['OLED SCL', 'GPIO 22'],
  ['HC-SR04 TRIG', 'GPIO 5'],
  ['HC-SR04 ECHO', 'GPIO 18 through divider'],
  ['Green LED', 'GPIO 25 through 220 Ω'],
  ['Red LED', 'GPIO 26 through 220 Ω'],
  ['Yellow LED', 'GPIO 27 through 220 Ω'],
  ['Finish button', 'GPIO 32 / INPUT_PULLUP'],
  ['Sharing button', 'GPIO 33 / INPUT_PULLUP'],
] as const;

export const SIMULATION_WIRE_LABELS: Readonly<Record<SimulationWire, string>> = {
  power: 'ESP32 USB power',
  pn532: 'PN532 I²C connection',
  'ultrasonic-trigger': 'HC-SR04 Trigger',
  'ultrasonic-echo': 'HC-SR04 Echo divider',
  'green-led': 'Green LED / GPIO 25',
  'red-led': 'Red LED / GPIO 26',
  'yellow-led': 'Yellow LED / GPIO 27',
};

export function createSimulationMachines(): readonly SimulationMachine[] {
  return [
    machine('EQ-TREADMILL-01', 'SIM-ESP32-01', 'Treadmill', 'treadmill', -4, -3),
    machine('EQ-BENCH-01', 'SIM-ESP32-02', 'Bench press', 'bench', 2.5, -3),
    machine('EQ-BIKE-01', 'SIM-ESP32-03', 'Exercise bike', 'bike', -4, 3),
    machine('EQ-CABLE-01', 'SIM-ESP32-04', 'Cable machine', 'cable', 2.5, 3),
  ];
}

export function createSimulationMember(source: SimulationMemberResponse, index: number): SimulationMember {
  return {
    id: source.memberId,
    cardId: source.cardId,
    name: source.displayName,
    shortName: shortName(source.displayName),
    membershipStatus: source.membershipStatus,
    cardStatus: source.cardStatus,
    accountStatus: source.accountStatus,
    cardSource: source.cardSource,
    statusOverride: 'Actual',
    shirtColor: shirtColor(source.memberId),
    entered: false,
    hasCard: true,
    machineId: null,
    position: { x: -8, z: -5.6 + (index % 6) * 0.7 },
  };
}

function machine(
  id: string,
  deviceId: string,
  name: string,
  type: SimulationMachineType,
  x: number,
  z: number,
): SimulationMachine {
  return {
    id,
    deviceId,
    name,
    type,
    position: { x, z },
    state: 'AVAILABLE',
    stateBeforeInterruption: null,
    activeStateBeforeClearance: null,
    memberIds: [],
    distanceMetres: 4,
    sensorResult: 'AREA_CLEAR',
    clearanceDeadlineMs: null,
    clearanceRemainingSeconds: null,
    disconnectedWires: [],
    safetyLocked: false,
    restarting: false,
  };
}

function shortName(displayName: string): string {
  return displayName.trim().split(/\s+/u)[0]?.slice(0, 18) || 'Member';
}

function shirtColor(memberId: string): number {
  const palette = [0xc9ff4a, 0x35d7f4, 0xff5ba7, 0xfbbf24, 0xa78bfa, 0x34d399, 0xfb7185];
  let hash = 0;
  for (const character of memberId) hash = ((hash << 5) - hash + character.charCodeAt(0)) | 0;
  return palette[Math.abs(hash) % palette.length];
}
