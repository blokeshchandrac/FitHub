import { AfterViewInit, Component, ElementRef, OnDestroy, computed, effect, inject, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SimulationCardTrayComponent } from './components/simulation-card-tray.component';
import { SimulationHudComponent } from './components/simulation-hud.component';
import { SimulationWorkbenchComponent } from './components/simulation-workbench.component';
import { SimulationApiService } from './simulation-api.service';
import { SimulationEngineService } from './simulation-engine.service';
import { SimulationSceneService } from './simulation-scene.service';
import { SimulationDecision, SimulationEventRequest, SimulationMachine, SimulationStatusOverride, SimulationWire } from './simulation.models';

@Component({
  selector: 'app-simulation-page',
  imports: [RouterLink, SimulationCardTrayComponent, SimulationHudComponent, SimulationWorkbenchComponent],
  providers: [SimulationEngineService, SimulationSceneService, SimulationApiService],
  templateUrl: './simulation-page.component.html',
  styleUrl: './simulation-page.component.scss',
})
export class SimulationPageComponent implements AfterViewInit, OnDestroy {
  protected readonly engine = inject(SimulationEngineService);
  protected readonly api = inject(SimulationApiService);
  private readonly scene = inject(SimulationSceneService);
  private readonly sceneHost = viewChild.required<ElementRef<HTMLElement>>('sceneHost');
  private readonly memberMenuElement = viewChild<ElementRef<HTMLElement>>('memberMenuElement');
  protected readonly webGlAvailable = signal(true);
  protected readonly helpOpen = signal(true);
  protected readonly resetOpen = signal(false);
  protected readonly memberMenu = signal<{ memberId: string; left: number; top: number } | null>(null);
  protected readonly menuMember = computed(() => {
    const menu = this.memberMenu();
    return menu ? this.engine.members().find(member => member.id === menu.memberId) ?? null : null;
  });
  protected readonly connectedAvailable = signal(false);
  protected readonly modeChanging = signal(false);
  protected readonly soundEnabled = signal(false);
  private draggingMemberId: string | null = null;
  private pointerMoved = false;
  private readonly pressedKeys = new Set<string>();
  private readonly connectedEventQueues = new Map<string, Promise<void>>();
  private lastEquipmentBroadcast = '';

  constructor() {
    effect(() => {
      const machines = this.engine.machines();
      const members = this.engine.members();
      const selectedId = this.engine.selectedMachineId();
      const selectedMemberId = this.engine.selectedMemberId();
      const showTapTargets = this.engine.showTapTargets();
      this.scene.sync(machines, members, selectedId, selectedMemberId, showTapTargets);
    });
    effect(() => {
      const change = this.api.lastBroadcast();
      const identity = change ? `${change.equipmentId}:${change.version}` : '';
      if (change && identity !== this.lastEquipmentBroadcast && this.engine.mode() === 'connected') {
        this.lastEquipmentBroadcast = identity;
        void this.refreshConnectedEquipment();
      }
    });
  }

  async ngAfterViewInit(): Promise<void> {
    this.webGlAvailable.set(this.scene.initialize(this.sceneHost().nativeElement));
    this.scene.setFrameHandler(frame => this.onFrame(frame.nowMs, frame.deltaSeconds));
    this.scene.sync(this.engine.machines(), this.engine.members(), this.engine.selectedMachineId(), this.engine.selectedMemberId(), false);
    window.addEventListener('keydown', this.onKeyDown);
    window.addEventListener('keyup', this.onKeyUp);
    window.addEventListener('blur', this.clearPressedKeys);
    await this.refreshMembers();
  }

  ngOnDestroy(): void {
    window.removeEventListener('keydown', this.onKeyDown);
    window.removeEventListener('keyup', this.onKeyUp);
    window.removeEventListener('blur', this.clearPressedKeys);
    this.engine.dispose();
    this.scene.dispose();
    void this.api.disconnectHub();
  }

  protected selectMachine(machine: SimulationMachine): void {
    this.engine.selectMachine(machine.id);
    this.scene.focusMachine(machine);
  }

  protected machineMemberNames(machine: SimulationMachine): readonly string[] {
    return machine.memberIds
      .map(id => this.engine.members().find(member => member.id === id)?.name)
      .filter((name): name is string => Boolean(name));
  }

  protected async toggleMode(): Promise<void> {
    if (this.modeChanging()) return;
    this.modeChanging.set(true);
    try {
      if (this.engine.mode() === 'connected') {
        await this.api.disconnectHub();
        this.engine.setMode('local');
        this.connectedAvailable.set(false);
        this.engine.recordExternalDecision({ allowed: true, code: 'SIGNALR_DISCONNECTED', message: 'EquipmentHub subscriptions were closed cleanly.' });
        return;
      }
      this.engine.recordExternalDecision({ allowed: true, code: 'CONNECTION_REQUESTED', message: 'Administrator Connected Mode was requested.' });
      const status = await this.api.checkAvailability();
      if (!status) {
        this.connectedAvailable.set(false);
        this.engine.recordExternalDecision({ allowed: false, code: 'CONNECTED_MODE_UNAVAILABLE', message: this.api.connectionMessage() });
        return;
      }
      this.engine.reset();
      this.engine.configure(status.clearanceThresholdMetres, status.clearanceSeconds);
      await this.refreshMembers();
      if (status.equipment.length !== this.engine.machines().length) {
        this.engine.recordExternalDecision({ allowed: false, code: 'EQUIPMENT_MAPPING_MISSING', message: `Connected Mode found ${status.equipment.length} of ${this.engine.machines().length} required persisted equipment mappings.` });
        return;
      }
      this.engine.reconcileConnectedEquipment(status.equipment);
      this.engine.recordExternalDecision({ allowed: true, code: 'EQUIPMENT_MAPPED', message: `${status.equipment.length} virtual machines mapped to persisted FitHub equipment records.` });
      const connected = await this.api.connectHub(status.hubUrl);
      if (!connected) {
        this.engine.recordExternalDecision({ allowed: false, code: 'SIGNALR_UNAVAILABLE', message: this.api.connectionMessage() });
        return;
      }
      this.connectedAvailable.set(true);
      this.engine.setMode('connected');
      this.engine.recordExternalDecision({ allowed: true, code: 'SIGNALR_CONNECTED', message: 'Normal EquipmentHub updates are active.' });
    } finally {
      this.modeChanging.set(false);
    }
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    const hit = this.scene.hitTest(event.clientX, event.clientY);
    this.scene.setDropHighlight(hit?.kind === 'entrance-scanner' || hit?.kind === 'machine-reader' ? hit : null);
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
  }

  protected onDragLeave(): void {
    this.scene.setDropHighlight(null);
  }

  protected async onCardDrop(event: DragEvent): Promise<void> {
    event.preventDefault();
    const cardId = event.dataTransfer?.getData('application/x-fithub-simulation-card');
    const hit = this.scene.hitTest(event.clientX, event.clientY);
    this.scene.setDropHighlight(null);
    if (!cardId || !hit) return;
    if (hit.kind === 'entrance-scanner') await this.scanEntrance(cardId);
    if (hit.kind === 'machine-reader') await this.scanMachine(cardId, hit.id);
  }

  protected onSceneClick(event: MouseEvent): void {
    if (this.pointerMoved) return;
    const hit = this.scene.hitTest(event.clientX, event.clientY);
    if (hit?.kind === 'member') {
      this.engine.selectMember(hit.id);
      this.scene.focusCanvas();
      return;
    }
    if (hit?.kind === 'machine' || hit?.kind === 'machine-reader') {
      const machine = this.engine.machines().find(item => item.id === hit.id);
      if (machine) this.selectMachine(machine);
    }
  }

  protected onSceneDoubleClick(event: MouseEvent): void {
    const hit = this.scene.hitTest(event.clientX, event.clientY);
    if (hit?.kind !== 'member') return;
    this.engine.selectMember(hit.id);
    this.openMemberActions(hit.id, event.clientX, event.clientY);
  }

  protected openMemberActions(memberId: string, left = Math.max(8, window.innerWidth / 2 - 120), top = Math.max(8, window.innerHeight / 2 - 140)): void {
    this.engine.selectMember(memberId);
    this.memberMenu.set({ memberId, left: Math.max(8, Math.min(left, window.innerWidth - 256)), top: Math.max(8, Math.min(top, window.innerHeight - 390)) });
    window.setTimeout(() => this.memberMenuElement()?.nativeElement.querySelector<HTMLButtonElement>('button')?.focus());
  }

  protected onPointerDown(event: PointerEvent): void {
    const hit = this.scene.hitTest(event.clientX, event.clientY);
    if (hit?.kind !== 'member') return;
    this.engine.selectMember(hit.id);
    this.draggingMemberId = hit.id;
    this.pointerMoved = false;
    this.scene.setOrbitEnabled(false);
    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
  }

  protected onPointerMove(event: PointerEvent): void {
    if (!this.draggingMemberId) return;
    const point = this.scene.groundPosition(event.clientX, event.clientY);
    if (point) {
      this.pointerMoved = true;
      this.engine.moveMember(this.draggingMemberId, point.x, point.z);
    }
  }

  protected onPointerUp(event: PointerEvent): void {
    if (!this.draggingMemberId) return;
    const target = event.currentTarget as HTMLElement;
    if (target.hasPointerCapture(event.pointerId)) target.releasePointerCapture(event.pointerId);
    this.draggingMemberId = null;
    this.scene.setOrbitEnabled(true);
    window.setTimeout(() => { this.pointerMoved = false; });
  }

  protected async scanEntrance(cardId: string): Promise<void> {
    this.scene.flashNfc('ENTRANCE');
    if (this.engine.mode() === 'connected') {
      const remote = await this.tryRemote('ENTRANCE_SCAN', entranceMachine(), cardId);
      if (!remote?.allowed) return;
    }
    this.engine.enterGym(cardId);
  }

  protected async scanMachine(cardId: string, machineId = this.engine.selectedMachineId()): Promise<void> {
    const machine = this.engine.machines().find(item => item.id === machineId);
    if (!machine) return;
    this.selectMachine(machine);
    this.scene.flashNfc(machine.id);
    if (this.engine.mode() === 'connected') {
      const remote = await this.tryRemote('NFC_TAPPED', machine, cardId);
      if (!remote?.allowed) return;
      this.engine.showTapTargets.set(false);
      return;
    }
    this.engine.scanMachine(cardId, machine.id);
  }

  protected takeCard(memberId: string): void {
    this.engine.takeCard(memberId);
    this.memberMenu.set(null);
  }

  protected returnCard(memberId: string): void {
    this.engine.returnCard(memberId);
    this.memberMenu.set(null);
  }

  protected sendMember(memberId: string): void {
    this.engine.sendToMachine(memberId);
    this.memberMenu.set(null);
  }

  protected moveMemberToOpenFloor(memberId: string): void {
    const member = this.engine.members().find(item => item.id === memberId);
    if (member) {
      this.engine.moveMember(member.id, 7, 5.5);
      this.engine.recordExternalDecision({ allowed: true, code: 'MEMBER_MOVED', message: `${member.name} moved outside the selected machine's clearance zone.` }, member.machineId ?? undefined);
    }
    this.memberMenu.set(null);
  }

  protected async finishMember(memberId: string): Promise<void> {
    const member = this.engine.members().find(item => item.id === memberId);
    const machine = member?.machineId ? this.engine.machines().find(item => item.id === member.machineId) : undefined;
    if (this.engine.mode() === 'connected' && machine) {
      await this.tryRemote('SESSION_FINISHED', machine, member?.cardId);
      this.memberMenu.set(null);
      return;
    }
    this.engine.finishSession(memberId);
    this.memberMenu.set(null);
  }

  protected finishSelected(): void {
    const machine = this.engine.selectedMachine();
    const memberId = machine?.memberIds[0];
    if (!machine || !memberId) {
      this.engine.recordExternalDecision({ allowed: false, code: 'NO_SESSION', message: 'No Member is currently using the selected machine.' }, machine?.id);
      return;
    }
    void this.finishMember(memberId);
  }

  protected async toggleSharing(): Promise<void> {
    const machine = this.engine.selectedMachine();
    if (!machine) return;
    if (this.engine.mode() === 'connected') {
      await this.tryRemote(machine.state === 'SHARING_ALLOWED' ? 'SHARING_DISABLED' : 'SHARING_ENABLED', machine, this.cardForMember(machine.memberIds[0]));
      return;
    }
    const decision = machine.state === 'SHARING_ALLOWED' ? this.engine.disableSharing(machine.id) : this.engine.enableSharing(machine.id);
    if (!decision.allowed) return;
  }

  protected toggleWire(wire: SimulationWire): void {
    const machine = this.engine.selectedMachine();
    if (!machine) return;
    const wasDisconnected = machine.disconnectedWires.includes(wire);
    const decision = this.engine.toggleWire(machine.id, wire);
    if (decision.allowed) void this.report(wireEvent(wire, wasDisconnected), machine);
  }

  protected changeSafetyLock(locked: boolean): void {
    const machine = this.engine.selectedMachine();
    if (!machine) return;
    const decision = this.engine.setSafetyLock(machine.id, locked);
    if (decision.allowed) void this.report(locked ? 'SAFETY_LOCKED' : 'SAFETY_UNLOCKED', machine);
  }

  protected removeMember(memberId: string): void {
    this.engine.removeMember(memberId);
    this.memberMenu.set(null);
  }

  protected showMemberStatus(memberId: string): void {
    const member = this.engine.members().find(item => item.id === memberId);
    if (member) this.engine.recordExternalDecision({ allowed: true, code: 'MEMBER_STATUS', message: `${member.name}: membership ${member.membershipStatus}, NFC card ${member.cardStatus}, account ${member.accountStatus}, override ${member.statusOverride}, machine ${member.machineId ?? 'none'}.` });
    this.memberMenu.set(null);
  }

  protected changeMemberOverride(memberId: string, statusOverride: SimulationStatusOverride): void {
    this.engine.setStatusOverride(memberId, statusOverride);
  }

  protected async refreshMembers(): Promise<void> {
    if (this.engine.membersLoading()) return;
    this.engine.setMembersLoading(true);
    try {
      this.engine.replaceMembers(await this.api.getMembers());
    } catch {
      this.engine.setMembersError('No FitHub test Members are available. Start the development API, register a Member, or seed the development database.');
    }
  }

  protected async confirmReset(): Promise<void> {
    const machine = this.engine.selectedMachine() ?? entranceMachine();
    await this.report('RESET', machine);
    this.engine.reset();
    this.resetOpen.set(false);
  }

  private onFrame(nowMs: number, deltaSeconds: number): void {
    this.moveSelectedMember(deltaSeconds);
    for (const machine of this.engine.machines()) {
      if (!machine.memberIds.length && machine.sensorResult !== 'SENSOR_FAULT') continue;
      const observation = this.scene.observeSensor(machine, machine.sensorResult);
      if (this.engine.mode() === 'connected') {
        const decision = this.engine.observeConnectedSensor(machine.id, observation.result, observation.distanceMetres, nowMs);
        if (decision?.allowed) this.queueConnectedReport(decision.code, machine, observation.distanceMetres, new Date().toISOString());
        continue;
      }
      this.engine.updateSensor(machine.id, observation.result, observation.distanceMetres, nowMs);
    }
  }

  private async tryRemote(eventType: string, machine: SimulationMachine, cardId?: string, distanceMetres?: number): Promise<SimulationDecision | null> {
    try {
      const response = await this.api.send(this.request(eventType, machine, cardId, distanceMetres));
      if (response.persistedEquipmentId) this.engine.reconcileConnectedMachine(response);
      this.engine.recordExternalDecision(response, machine.id);
      return response;
    } catch (error) {
      this.engine.recordExternalDecision(this.api.submissionFailure(error), machine.id);
      return null;
    }
  }

  private async report(eventType: string, machine: SimulationMachine, cardId?: string, distanceMetres?: number): Promise<void> {
    if (this.engine.mode() !== 'connected') return;
    await this.tryRemote(eventType, machine, cardId, distanceMetres);
  }

  private request(eventType: string, machine: SimulationMachine, cardId?: string, distanceMetres?: number, observedAt = new Date().toISOString()): SimulationEventRequest {
    return {
      simulationSessionId: this.engine.sessionId(),
      deviceId: machine.deviceId,
      equipmentId: machine.id,
      eventType,
      cardId,
      distanceMetres,
      observedAt,
      simulated: true,
      statusOverride: cardId ? this.engine.members().find(member => member.cardId === cardId)?.statusOverride ?? 'Actual' : 'Actual',
      memberId: cardId ? this.engine.members().find(member => member.cardId === cardId)?.id : undefined,
    };
  }

  private queueConnectedReport(eventType: string, machine: SimulationMachine, distanceMetres: number, observedAt: string): void {
    const previous = this.connectedEventQueues.get(machine.id) ?? Promise.resolve();
    // Preserve physical observation order per machine. Concurrent exit/start/cancel requests can
    // otherwise race at the database and make a transport failure look like a Member returned.
    const queued = previous.catch(() => undefined).then(async () => {
      if (this.engine.mode() !== 'connected') return;
      try {
        const response = await this.api.send(this.request(eventType, machine, undefined, distanceMetres, observedAt));
        if (response.persistedEquipmentId) this.engine.reconcileConnectedMachine(response);
        this.engine.recordExternalDecision(response, machine.id);
      } catch (error) {
        this.engine.recordExternalDecision(this.api.submissionFailure(error), machine.id);
      }
    });
    this.connectedEventQueues.set(machine.id, queued);
    void queued.finally(() => {
      if (this.connectedEventQueues.get(machine.id) === queued) this.connectedEventQueues.delete(machine.id);
    });
  }

  private async refreshConnectedEquipment(): Promise<void> {
    try {
      this.engine.reconcileConnectedEquipment(await this.api.getEquipment());
    } catch {
      this.engine.recordExternalDecision({ allowed: false, code: 'STATE_RECONCILIATION_FAILED', message: 'The latest persisted equipment state could not be loaded.' });
    }
  }

  private cardForMember(memberId: string | undefined): string | undefined {
    return this.engine.members().find(member => member.id === memberId)?.cardId;
  }

  private moveSelectedMember(deltaSeconds: number): void {
    const member = this.engine.selectedMember();
    if (!member?.entered || this.draggingMemberId || !this.pressedKeys.size) return;
    const forward = Number(this.pressedKeys.has('arrowup') || this.pressedKeys.has('w')) - Number(this.pressedKeys.has('arrowdown') || this.pressedKeys.has('s'));
    const right = Number(this.pressedKeys.has('arrowright') || this.pressedKeys.has('d')) - Number(this.pressedKeys.has('arrowleft') || this.pressedKeys.has('a'));
    if (!forward && !right) return;
    const direction = this.scene.cameraRelativeMovement(forward, right);
    const speed = this.pressedKeys.has('shift') ? 4.6 : 3.2;
    this.engine.moveMember(member.id, member.position.x + direction.x * speed * deltaSeconds, member.position.z + direction.z * speed * deltaSeconds);
  }

  private readonly onKeyDown = (event: KeyboardEvent): void => {
    if (isEditableTarget(event.target)) return;
    const key = event.key.toLowerCase();
    if (!MOVEMENT_KEYS.has(key)) return;
    event.preventDefault();
    this.pressedKeys.add(key);
  };

  private readonly onKeyUp = (event: KeyboardEvent): void => { this.pressedKeys.delete(event.key.toLowerCase()); };
  private readonly clearPressedKeys = (): void => { this.pressedKeys.clear(); };
}

const MOVEMENT_KEYS = new Set(['arrowup', 'arrowdown', 'arrowleft', 'arrowright', 'w', 'a', 's', 'd', 'shift']);

function isEditableTarget(target: EventTarget | null): boolean {
  const element = target as HTMLElement | null;
  return Boolean(element?.closest('input, select, textarea, button, [contenteditable="true"], [role="menu"]'));
}

function entranceMachine(): SimulationMachine {
  return { id: 'ENTRANCE-01', deviceId: 'SIM-ENTRY-01', name: 'Gym entrance', type: 'treadmill', position: { x: -8, z: 0 }, state: 'AVAILABLE', stateBeforeInterruption: null, activeStateBeforeClearance: null, memberIds: [], distanceMetres: 0, sensorResult: 'AREA_CLEAR', clearanceDeadlineMs: null, clearanceRemainingSeconds: null, disconnectedWires: [], safetyLocked: false, restarting: false };
}

function wireEvent(wire: SimulationWire, reconnecting: boolean): string {
  if (wire === 'power') return reconnecting ? 'POWER_RECONNECTED' : 'POWER_DISCONNECTED';
  if (wire === 'ultrasonic-trigger' || wire === 'ultrasonic-echo') return reconnecting ? 'SENSOR_RECONNECTED' : 'SENSOR_DISCONNECTED';
  if (wire === 'pn532') return reconnecting ? 'NFC_RECONNECTED' : 'NFC_DISCONNECTED';
  return reconnecting ? 'LED_RECONNECTED' : 'LED_DISCONNECTED';
}
