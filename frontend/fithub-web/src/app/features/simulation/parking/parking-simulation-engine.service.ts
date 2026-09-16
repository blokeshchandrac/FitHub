import { computed, Injectable, signal } from '@angular/core';
import {
  ParkingAlert,
  ParkingCar,
  ParkingSimulationEvent,
  ParkingSimulationMode,
  ParkingSpaceModel,
  ParkingVisionResult,
  createParkingSpaces,
} from './parking-simulation.models';
import { VirtualParkingVisionService } from './virtual-parking-vision.service';
import { PARKING_EVENT_STORAGE_KEY, persistParkingEvents, restoreParkingEvents } from './parking-event-log';

@Injectable()
export class ParkingSimulationEngineService {
  readonly sessionId = `SIM-PARK-${crypto.randomUUID().slice(0, 8).toUpperCase()}`;
  readonly cameraId = 'SIM-PARK-CAMERA-01';
  readonly spaces = signal<readonly ParkingSpaceModel[]>(createParkingSpaces());
  readonly cars = signal<readonly ParkingCar[]>([]);
  readonly selectedCarId = signal<string | null>(null);
  readonly selectedSpaceId = signal<string | null>(null);
  readonly mode = signal<ParkingSimulationMode>('local');
  readonly cameraOnline = signal(true);
  readonly lowConfidence = signal(false);
  readonly demoSpeed = signal(false);
  readonly followCamera = signal(true);
  readonly latestScanAt = signal<string | null>(null);
  readonly latestCapture = signal<string | null>(null);
  readonly secondsUntilScan = signal(60);
  readonly scanning = signal(false);
  readonly detectedCars = signal(0);
  readonly alerts = signal<readonly ParkingAlert[]>([]);
  readonly events = signal<readonly ParkingSimulationEvent[]>([]);
  readonly lastVisionResult = signal<ParkingVisionResult | null>(null);
  readonly selectedCar = computed(() => this.cars().find(car => car.id === this.selectedCarId()) ?? null);
  readonly selectedSpace = computed(() => this.spaces().find(space => space.id === this.selectedSpaceId()) ?? null);
  readonly summary = computed(() => {
    const spaces = this.spaces();
    return {
      total: spaces.length,
      available: spaces.filter(x => x.state === 'AVAILABLE').length,
      occupied: spaces.filter(x => x.state === 'OCCUPIED').length,
      blocked: spaces.filter(x => x.state === 'MISUSED_OR_BLOCKED').length,
      unknown: spaces.filter(x => x.state === 'UNKNOWN').length,
      detectedCars: this.detectedCars(),
      activeAlerts: this.alerts().filter(x => x.status === 'ACTIVE').length,
    };
  });

  private nextScanAtMs = performance.now() + 60_000;
  private lastDisplayedSecond = 60;
  private eventSequence = 0;
  private nextCarNumber = 1;
  private normalScanSeconds = 60;
  private demoScanSeconds = 5;
  private readonly eventStorage = typeof sessionStorage === 'undefined' ? null : sessionStorage;

  constructor(private readonly vision: VirtualParkingVisionService) {
    const restored = restoreParkingEvents(this.eventStorage);
    this.events.set(restored);
    this.eventSequence = Math.max(0, ...restored.map(event => event.id));
    this.record('SIMULATION_OPENED', 'READY', 'Parking simulation opened with twelve monitored spaces.', 'info', 'FitHub Virtual Lab');
  }

  configureIntervals(normalSeconds: number, demoSeconds: number): void {
    this.normalScanSeconds = normalSeconds;
    this.demoScanSeconds = demoSeconds;
    this.scheduleNextScan(performance.now());
  }

  addCar(): ParkingCar | null {
    if (this.cars().length >= 6) {
      this.record('CAR_ADD_REJECTED', 'LIMIT_REACHED', 'The six-car simulation limit has been reached.', 'warning', 'Vehicle controller');
      return null;
    }
    const number = this.nextCarNumber++;
    const entrance = entrancePosition(number);
    const car: ParkingCar = {
      id: `Car ${number.toString().padStart(2, '0')}`,
      position: entrance,
      heading: Math.PI,
      speed: 0,
      color: [0x35d7f4, 0xc9ff4a, 0xfb7185, 0xfbbf24, 0xa78bfa, 0xf8fafc][(number - 1) % 6],
    };
    this.cars.update(cars => [...cars, car]);
    this.selectCar(car.id);
    this.record('CAR_ADDED', 'CREATED', `${car.id} entered on the access road.`, 'success', car.id);
    return car;
  }

  selectCar(id: string): void {
    if (this.selectedCarId() === id) return;
    this.selectedCarId.set(id);
    this.selectedSpaceId.set(null);
    this.record('CAR_SELECTED', 'SELECTED', `${id} is ready for keyboard control.`, 'info', id);
  }

  selectSpace(id: string): void {
    this.selectedSpaceId.set(id);
    this.selectedCarId.set(null);
  }

  removeSelectedCar(): void {
    const selected = this.selectedCar();
    if (!selected) return;
    this.cars.update(cars => cars.filter(car => car.id !== selected.id));
    this.selectedCarId.set(null);
    this.record('CAR_REMOVED', 'REMOVED', `${selected.id} was removed from the simulation.`, 'info', selected.id);
  }

  resetSelectedCar(): void {
    const selected = this.selectedCar();
    if (!selected) return;
    const number = Number.parseInt(selected.id.slice(-2), 10) || 1;
    this.cars.update(cars => cars.map(car => car.id === selected.id
      ? { ...car, position: entrancePosition(number), heading: Math.PI, speed: 0 }
      : car));
    this.record('CAR_RESET', 'ENTRANCE', `${selected.id} returned to the entrance road.`, 'info', selected.id);
  }

  updateCarPose(id: string, position: { x: number; z: number }, heading: number, speed: number): void {
    this.cars.update(cars => cars.map(car => car.id === id ? { ...car, position, heading, speed } : car));
  }

  resetSimulation(): void {
    this.spaces.set(createParkingSpaces());
    this.cars.set([]);
    this.alerts.set([]);
    this.detectedCars.set(0);
    this.selectedCarId.set(null);
    this.selectedSpaceId.set(null);
    this.latestScanAt.set(null);
    this.latestCapture.set(null);
    this.lastVisionResult.set(null);
    this.nextCarNumber = 1;
    this.scheduleNextScan(performance.now());
    this.record('SIMULATION_RESET', 'RESET', 'Cars, observations and local alerts were cleared.', 'info', 'FitHub Virtual Lab');
  }

  setCameraOnline(value: boolean): void {
    if (this.cameraOnline() === value) return;
    this.cameraOnline.set(value);
    this.record(value ? 'CAMERA_ONLINE' : 'CAMERA_OFFLINE', value ? 'ONLINE' : 'OFFLINE',
      value ? 'The parking camera is ready for its next observation.' : 'The next attempted scan will mark monitored spaces unknown.',
      value ? 'success' : 'error', this.cameraId);
  }

  setLowConfidence(value: boolean): void {
    if (this.lowConfidence() === value) return;
    this.lowConfidence.set(value);
    this.record('CAMERA_CONFIDENCE_CHANGED', value ? 'OBSTRUCTED' : 'CLEAR',
      value ? 'Camera observations will be unknown until the obstruction is cleared.' : 'Normal confidence restored.',
      value ? 'warning' : 'success', this.cameraId);
  }

  setDemoSpeed(value: boolean, nowMs = performance.now()): void {
    this.demoSpeed.set(value);
    this.scheduleNextScan(nowMs);
    this.record('SCAN_INTERVAL_CHANGED', value ? 'DEMO_5_SECONDS' : 'NORMAL_60_SECONDS',
      value ? 'Demo Speed scans every five seconds.' : 'Normal camera scans occur every sixty seconds.', 'info', this.cameraId);
  }

  tick(nowMs: number): boolean {
    const remaining = Math.max(0, Math.ceil((this.nextScanAtMs - nowMs) / 1000));
    if (remaining !== this.lastDisplayedSecond) {
      this.lastDisplayedSecond = remaining;
      this.secondsUntilScan.set(remaining);
    }
    if (nowMs < this.nextScanAtMs || this.scanning()) return false;
    this.scheduleNextScan(nowMs);
    return true;
  }

  beginScan(): void {
    this.scanning.set(true);
    this.record('CAMERA_SCAN_STARTED', 'SCANNING', 'The virtual camera capture started.', 'info', this.cameraId);
  }

  completeScan(cars: readonly ParkingCar[], captureDataUrl: string | null): ParkingVisionResult {
    const observedAt = new Date().toISOString();
    const result = this.vision.analyse(this.spaces(), cars, observedAt, this.cameraOnline(), this.lowConfidence());
    const previousStates = new Map(this.spaces().map(space => [space.id, space.state]));
    this.lastVisionResult.set(result);
    this.latestScanAt.set(observedAt);
    this.latestCapture.set(captureDataUrl);
    this.detectedCars.set(result.detectedCars);
    this.spaces.update(spaces => spaces.map(space => {
      const observation = result.observations.find(item => item.spaceId === space.id)!;
      return { ...space, state: observation.status, confidence: observation.confidence, observedAt };
    }));
    for (const observation of result.observations) {
      const previousState = previousStates.get(observation.spaceId);
      if (observation.status === 'OCCUPIED' && previousState !== 'OCCUPIED') {
        this.record('CORRECT_PARKING_DETECTED', 'OCCUPIED', `A vehicle was detected correctly inside ${observation.spaceId}.`, 'success', this.cameraId);
        this.record('PARKING_SPACE_OCCUPIED', `${observation.spaceId} OCCUPIED`, `${observation.spaceId} changed from ${previousState ?? 'UNKNOWN'} to occupied.`, 'success', observation.spaceId);
      }
      if (observation.status === 'AVAILABLE' && previousState && previousState !== 'AVAILABLE')
        this.record('PARKING_SPACE_AVAILABLE', `${observation.spaceId} AVAILABLE`, `${observation.spaceId} changed from ${previousState} to available.`, 'success', observation.spaceId);
    }
    if (this.lowConfidence()) this.record('LOW_CONFIDENCE_OBSERVATION', 'UNKNOWN', 'Confidence was below the safe detection threshold; monitored spaces were not guessed.', 'warning', this.cameraId);
    this.reconcileAlerts(result, observedAt);
    this.scanning.set(false);
    this.record('CAMERA_SCAN_COMPLETED', this.cameraOnline() ? 'COMPLETED' : 'UNKNOWN',
      this.cameraOnline()
        ? `${result.detectedCars} car${result.detectedCars === 1 ? '' : 's'} affected parking spaces.`
        : 'Camera offline: all monitored spaces are now unknown.',
      this.cameraOnline() ? 'success' : 'warning', this.cameraId);
    return result;
  }

  recordBackendResult(succeeded: boolean, detail: string): void {
    this.record(succeeded ? 'BACKEND_SUBMISSION_SUCCEEDED' : 'BACKEND_SUBMISSION_FAILED',
      succeeded ? 'ACCEPTED' : 'NOT_SENT', detail, succeeded ? 'success' : 'error', 'FitHub API');
  }

  recordConnection(action: string, result: string, detail: string, tone: ParkingSimulationEvent['tone']): void {
    this.record(action, result, detail, tone, 'Connected Mode');
  }

  recordPersistedScan(updatedSpaces: number, createdAlerts: number, resolvedAlerts: number): void {
    this.record('PARKING_OBSERVATIONS_PERSISTED', 'COMMITTED', `${updatedSpaces} observations were committed to FitHub.`, 'success', 'FitHub API');
    if (createdAlerts > 0) this.record('PARKING_ALERT_CREATED', 'COMMITTED', `${createdAlerts} Administrator parking alert(s) were created.`, 'warning', 'FitHub API');
    if (resolvedAlerts > 0) this.record('PARKING_ALERT_RESOLVED', 'COMMITTED', `${resolvedAlerts} Administrator parking alert(s) were resolved.`, 'success', 'FitHub API');
  }

  setMode(mode: ParkingSimulationMode): void {
    if (this.mode() === mode) return;
    this.mode.set(mode);
    this.record(mode === 'connected' ? 'CONNECTED_MODE_ENABLED' : 'LOCAL_MODE_ENABLED', mode.toUpperCase(),
      mode === 'local' ? 'No parking data is being sent to FitHub.' : 'Accepted scans are persisted through the development API.', 'info', 'Simulation mode');
  }

  clearLog(): void {
    this.events.set([]);
    this.eventStorage?.removeItem(PARKING_EVENT_STORAGE_KEY);
  }

  recordSignalRUpdate(detail: string): void {
    this.record('SIGNALR_UPDATE_RECEIVED', 'RECEIVED', detail, 'info', 'Parking SignalR');
  }

  private reconcileAlerts(result: ParkingVisionResult, observedAt: string): void {
    const currentKeys = new Set(result.violations.map(violation => alertKey(violation.vehicleSimulationId, violation.affectedSpaceIds)));
    const previous = this.alerts();
    const next = previous.map(alert => {
      if (alert.status === 'ACTIVE' && !currentKeys.has(alert.key)) {
        this.record('PARKING_VIOLATION_RESOLVED', 'ALERT RESOLVED', `${alert.vehicleSimulationId} no longer obstructs ${alert.affectedSpaceIds.join(' and ')}.`, 'success', alert.vehicleSimulationId);
        return { ...alert, status: 'RESOLVED' as const, resolvedAt: observedAt };
      }
      return alert;
    });
    for (const violation of result.violations) {
      const key = alertKey(violation.vehicleSimulationId, violation.affectedSpaceIds);
      const existingIndex = next.findIndex(alert => alert.key === key);
      if (existingIndex >= 0) {
        if (next[existingIndex].status === 'RESOLVED') {
          next[existingIndex] = { ...next[existingIndex], status: 'ACTIVE', detectedAt: observedAt, resolvedAt: null };
          this.record('PARKING_ALERT_CREATED', 'ADMIN ALERT CREATED', violation.reason, 'warning', violation.vehicleSimulationId);
        }
        continue;
      }
      next.unshift({ key, vehicleSimulationId: violation.vehicleSimulationId, affectedSpaceIds: violation.affectedSpaceIds,
        reason: violation.reason, status: 'ACTIVE', detectedAt: observedAt, resolvedAt: null });
      this.record('TWO_SPACE_OBSTRUCTION_DETECTED', 'TWO SPACES BLOCKED', violation.reason, 'warning', violation.vehicleSimulationId);
      this.record('PARKING_ALERT_CREATED', 'ADMIN ALERT CREATED', violation.reason, 'warning', violation.vehicleSimulationId);
    }
    this.alerts.set(next.slice(0, 50));
  }

  private scheduleNextScan(nowMs: number): void {
    const seconds = this.demoSpeed() ? this.demoScanSeconds : this.normalScanSeconds;
    this.nextScanAtMs = nowMs + seconds * 1000;
    this.lastDisplayedSecond = seconds;
    this.secondsUntilScan.set(seconds);
  }

  private record(action: string, result: string, detail: string, tone: ParkingSimulationEvent['tone'], source: string): void {
    const event: ParkingSimulationEvent = {
      id: ++this.eventSequence,
      occurredAt: new Date().toISOString(),
      action,
      source,
      result,
      detail,
      tone,
      mode: this.mode(),
    };
    this.events.update(events => [event, ...events].slice(0, 100));
    persistParkingEvents(this.eventStorage, this.events());
  }
}

function alertKey(vehicleId: string, spaceIds: readonly string[]): string {
  return `${vehicleId}:${[...spaceIds].sort().join('|')}`;
}

function entrancePosition(number: number): { x: number; z: number } {
  return { x: [-7.2, -2.4, 2.4, 7.2, -10.5, 10.5][(number - 1) % 6], z: 19.5 };
}
