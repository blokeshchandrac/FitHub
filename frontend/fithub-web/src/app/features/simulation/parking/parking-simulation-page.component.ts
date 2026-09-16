import { DatePipe } from '@angular/common';
import { AfterViewInit, Component, ElementRef, OnDestroy, computed, effect, inject, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ParkingSimulationApiService } from './parking-simulation-api.service';
import { ParkingSimulationEngineService } from './parking-simulation-engine.service';
import { ParkingSimulationSceneService } from './parking-simulation-scene.service';
import { ParkingSimulationScanRequest, ParkingVisionResult } from './parking-simulation.models';
import { VirtualParkingVisionService } from './virtual-parking-vision.service';
import { buildParkingEventCsv, parkingEventLogFilename } from './parking-event-log';

@Component({
  selector: 'app-parking-simulation-page',
  imports: [RouterLink, DatePipe],
  providers: [VirtualParkingVisionService, ParkingSimulationEngineService, ParkingSimulationSceneService, ParkingSimulationApiService],
  templateUrl: './parking-simulation-page.component.html',
  styleUrl: './parking-simulation-page.component.scss',
})
export class ParkingSimulationPageComponent implements AfterViewInit, OnDestroy {
  protected readonly engine = inject(ParkingSimulationEngineService);
  protected readonly api = inject(ParkingSimulationApiService);
  private readonly scene = inject(ParkingSimulationSceneService);
  private readonly sceneHost = viewChild.required<ElementRef<HTMLElement>>('sceneHost');
  protected readonly webGlAvailable = signal(true);
  protected readonly instructionsOpen = signal(true);
  protected readonly resetPending = signal(false);
  protected readonly modeChanging = signal(false);
  protected readonly connectedAvailable = signal(false);
  protected readonly pressedTouchControls = signal<ReadonlySet<string>>(new Set());
  protected readonly activeAlert = computed(() => this.engine.alerts().find(alert => alert.status === 'ACTIVE') ?? null);
  private readonly pressedKeys = new Set<string>();
  private lastPoseSyncMs = 0;
  private scheduledScanRunning = false;
  private lastSignalRMessage: string | null = null;

  constructor() {
    effect(() => {
      this.scene.sync(this.engine.spaces(), this.engine.cars(), this.engine.selectedCarId(), this.engine.selectedSpaceId());
      this.scene.setFollowCar(this.engine.followCamera() ? this.engine.selectedCarId() : null);
    });
    effect(() => {
      const message = this.api.lastRealtimeEvent();
      if (message && message !== this.lastSignalRMessage) {
        this.lastSignalRMessage = message;
        this.engine.recordSignalRUpdate(message);
      }
    });
  }

  ngAfterViewInit(): void {
    this.webGlAvailable.set(this.scene.initialize(this.sceneHost().nativeElement));
    this.scene.setFrameHandler(frame => this.onFrame(frame.nowMs, frame.deltaSeconds));
    this.scene.sync(this.engine.spaces(), this.engine.cars(), null, null);
    window.addEventListener('keydown', this.onKeyDown);
    window.addEventListener('keyup', this.onKeyUp);
    window.addEventListener('blur', this.clearKeys);
  }

  ngOnDestroy(): void {
    window.removeEventListener('keydown', this.onKeyDown);
    window.removeEventListener('keyup', this.onKeyUp);
    window.removeEventListener('blur', this.clearKeys);
    this.scene.dispose();
    void this.api.disconnectHub();
  }

  protected addCar(): void { this.engine.addCar(); this.scene.focusCanvas(); }

  protected removeCar(): void { this.engine.removeSelectedCar(); }

  protected resetCar(): void {
    this.engine.resetSelectedCar();
    const car = this.engine.selectedCar();
    if (car) this.scene.resetCar(car);
  }

  protected confirmReset(): void {
    this.engine.resetSimulation();
    this.resetPending.set(false);
  }

  protected onSceneClick(event: MouseEvent): void {
    const hit = this.scene.hitTest(event.clientX, event.clientY);
    if (hit?.kind === 'car') {
      this.engine.selectCar(hit.id);
      this.scene.focusCanvas();
    } else if (hit?.kind === 'space') this.engine.selectSpace(hit.id);
  }

  protected setCameraOnline(value: boolean): void { this.engine.setCameraOnline(value); }
  protected setLowConfidence(value: boolean): void { this.engine.setLowConfidence(value); }
  protected setDemoSpeed(value: boolean): void { this.engine.setDemoSpeed(value); }

  protected async scanNow(): Promise<void> { await this.performScan(); }

  protected async toggleMode(): Promise<void> {
    if (this.modeChanging()) return;
    this.modeChanging.set(true);
    try {
      if (this.engine.mode() === 'connected') {
        await this.api.disconnectHub();
        this.engine.setMode('local');
        this.connectedAvailable.set(false);
        this.engine.recordConnection('SIGNALR_DISCONNECTED', 'DISCONNECTED', 'ParkingHub subscriptions were closed cleanly.', 'info');
        return;
      }
      this.engine.recordConnection('CONNECTION_REQUESTED', 'CONNECTING', 'Administrator Connected Mode was requested.', 'info');
      const status = await this.api.checkAvailability();
      if (!status) {
        this.engine.recordBackendResult(false, this.api.connectionMessage());
        return;
      }
      this.engine.recordConnection('API_VERIFIED', 'AUTHORIZED', 'The development parking simulation API accepted the Administrator session.', 'success');
      this.engine.configureIntervals(status.normalScanSeconds, status.demoScanSeconds);
      const connected = await this.api.connectHub(status.hubUrl);
      if (!connected) {
        this.engine.recordBackendResult(false, this.api.connectionMessage());
        return;
      }
      this.connectedAvailable.set(true);
      this.engine.setMode('connected');
      this.engine.recordConnection('SIGNALR_CONNECTED', 'CONNECTED', 'Normal ParkingHub updates are active.', 'success');
    } finally {
      this.modeChanging.set(false);
    }
  }

  protected pressTouch(code: string, pressed: boolean): void {
    const next = new Set(this.pressedTouchControls());
    if (pressed) next.add(code); else next.delete(code);
    this.pressedTouchControls.set(next);
    this.scene.focusCanvas();
  }

  protected formatCountdown(seconds: number): string {
    return `00:${Math.max(0, seconds).toString().padStart(2, '0')}`;
  }

  protected formatPosition(value: number): string { return value.toFixed(1); }

  protected clearLog(): void { this.engine.clearLog(); }

  protected downloadLog(): void {
    const events = this.engine.events();
    if (events.length === 0) return;
    const blob = new Blob(['\uFEFF', buildParkingEventCsv(events)], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = parkingEventLogFilename();
    anchor.click();
    URL.revokeObjectURL(url);
  }

  private onFrame(nowMs: number, deltaSeconds: number): void {
    const selectedId = this.engine.selectedCarId();
    if (selectedId) {
      const combined = new Set([...this.pressedKeys, ...this.pressedTouchControls()]);
      const updated = this.scene.driveCar(selectedId, combined, deltaSeconds);
      if (updated && nowMs - this.lastPoseSyncMs >= 100) {
        this.lastPoseSyncMs = nowMs;
        this.engine.updateCarPose(updated.id, updated.position, updated.heading, updated.speed);
      }
    }
    if (this.engine.tick(nowMs) && !this.scheduledScanRunning) void this.performScan();
  }

  private async performScan(): Promise<void> {
    if (this.engine.scanning() || this.scheduledScanRunning) return;
    this.scheduledScanRunning = true;
    this.engine.beginScan();
    this.scene.pulseCamera();
    await new Promise(resolve => window.setTimeout(resolve, 280));
    const cars = this.scene.getCarSnapshots(this.engine.cars());
    for (const car of cars) this.engine.updateCarPose(car.id, car.position, car.heading, car.speed);
    const preview = this.scene.captureCameraPreview();
    const result = this.engine.completeScan(cars, preview);
    if (this.engine.mode() === 'connected') await this.submitConnected(result);
    this.scheduledScanRunning = false;
  }

  private async submitConnected(result: ParkingVisionResult): Promise<void> {
    const request: ParkingSimulationScanRequest = {
      simulationSessionId: this.engine.sessionId,
      sourceDeviceId: this.engine.cameraId,
      observedAt: result.observations[0]?.observedAt ?? new Date().toISOString(),
      observations: result.observations.map(item => ({ spaceId: item.spaceId, status: item.status, confidence: item.confidence })),
      violations: result.violations.map(item => ({ vehicleSimulationId: item.vehicleSimulationId,
        affectedSpaceIds: item.affectedSpaceIds, violationType: item.violationType, reason: item.reason })),
    };
    try {
      const response = await this.api.submit(request);
      this.engine.recordPersistedScan(response.updatedSpaces, response.createdAlerts, response.resolvedAlerts);
      this.engine.recordBackendResult(true, `${response.updatedSpaces} space observations persisted; ${response.activeAlerts.length} active administrator alert(s).`);
    } catch {
      this.engine.recordBackendResult(false, 'The scan remains visible locally, but the development backend did not accept it.');
    }
  }

  private readonly onKeyDown = (event: KeyboardEvent): void => {
    if (isEditableTarget(event.target)) return;
    const supported = ['KeyW', 'KeyA', 'KeyS', 'KeyD', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Space', 'ShiftLeft', 'ShiftRight', 'KeyR'];
    if (!supported.includes(event.code) || !this.engine.selectedCarId()) return;
    event.preventDefault();
    if (event.code === 'KeyR') {
      if (!event.repeat) this.resetCar();
      return;
    }
    this.pressedKeys.add(event.code);
  };

  private readonly onKeyUp = (event: KeyboardEvent): void => { this.pressedKeys.delete(event.code); };
  private readonly clearKeys = (): void => { this.pressedKeys.clear(); this.pressedTouchControls.set(new Set()); };
}

function isEditableTarget(target: EventTarget | null): boolean {
  return target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement || target instanceof HTMLSelectElement ||
    (target instanceof HTMLElement && (target.isContentEditable || target.closest('[role="menu"]') !== null));
}
