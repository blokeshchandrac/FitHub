export type ParkingSimulationMode = 'local' | 'connected';
export type ParkingSpaceState = 'AVAILABLE' | 'OCCUPIED' | 'MISUSED_OR_BLOCKED' | 'UNKNOWN';
export type ParkingConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'connection-failed';

export interface Point2 { readonly x: number; readonly z: number; }

export interface ParkingSpaceModel {
  readonly id: string;
  readonly center: Point2;
  readonly width: number;
  readonly length: number;
  readonly heading: number;
  readonly state: ParkingSpaceState;
  readonly confidence: number;
  readonly observedAt: string | null;
}

export interface ParkingCar {
  readonly id: string;
  readonly position: Point2;
  readonly heading: number;
  readonly speed: number;
  readonly color: number;
}

export interface ParkingVisionConfiguration {
  readonly minimumInsideRatio: number;
  readonly maximumAdjacentOverlap: number;
  readonly maximumHeadingDifferenceDegrees: number;
  readonly minimumAffectedRatio: number;
  readonly minimumConfidence: number;
  readonly carWidth: number;
  readonly carLength: number;
}

export const PARKING_VISION_DEFAULTS: ParkingVisionConfiguration = {
  minimumInsideRatio: 0.8,
  maximumAdjacentOverlap: 0.1,
  maximumHeadingDifferenceDegrees: 15,
  minimumAffectedRatio: 0.1,
  minimumConfidence: 0.6,
  carWidth: 2,
  carLength: 4.2,
};

export interface ParkingObservation {
  readonly spaceId: string;
  readonly status: ParkingSpaceState;
  readonly confidence: number;
  readonly observedAt: string;
  readonly sourceDeviceId: string;
}

export interface ParkingViolation {
  readonly vehicleSimulationId: string;
  readonly affectedSpaceIds: readonly string[];
  readonly violationType: 'TWO_SPACE_OBSTRUCTION';
  readonly detectedAt: string;
  readonly sourceDeviceId: string;
  readonly reason: string;
}

export interface ParkingVisionResult {
  readonly observations: readonly ParkingObservation[];
  readonly violations: readonly ParkingViolation[];
  readonly detectedCars: number;
}

export interface ParkingAlert {
  readonly key: string;
  readonly vehicleSimulationId: string;
  readonly affectedSpaceIds: readonly string[];
  readonly reason: string;
  readonly status: 'ACTIVE' | 'RESOLVED';
  readonly detectedAt: string;
  readonly resolvedAt: string | null;
}

export interface ParkingSimulationEvent {
  readonly id: number;
  readonly occurredAt: string;
  readonly action: string;
  readonly source: string;
  readonly result: string;
  readonly detail: string;
  readonly tone: 'info' | 'success' | 'warning' | 'error';
  readonly mode: ParkingSimulationMode;
}

export interface ParkingSimulationBackendStatus {
  readonly enabled: boolean;
  readonly hubUrl: string;
  readonly cameraId: string;
  readonly normalScanSeconds: number;
  readonly demoScanSeconds: number;
}

export interface ParkingSimulationScanRequest {
  readonly simulationSessionId: string;
  readonly sourceDeviceId: string;
  readonly observedAt: string;
  readonly observations: readonly { readonly spaceId: string; readonly status: ParkingSpaceState; readonly confidence: number }[];
  readonly violations: readonly { readonly vehicleSimulationId: string; readonly affectedSpaceIds: readonly string[]; readonly violationType: string; readonly reason: string }[];
}

export interface ParkingSimulationScanResponse {
  readonly updatedSpaces: number;
  readonly createdAlerts: number;
  readonly resolvedAlerts: number;
  readonly activeAlerts: readonly unknown[];
  readonly acceptedAt: string;
}

export function createParkingSpaces(): readonly ParkingSpaceModel[] {
  const centers = [-7.2, -2.4, 2.4, 7.2];
  const rows = [{ name: 'A', z: 9 }, { name: 'B', z: 0 }, { name: 'C', z: -9 }];
  return rows.flatMap(row => centers.map((x, index) => ({
    id: `P-${row.name}${index + 1}`,
    center: { x, z: row.z },
    width: 4.4,
    length: 5.8,
    heading: 0,
    state: 'AVAILABLE' as const,
    confidence: 1,
    observedAt: null,
  })));
}
