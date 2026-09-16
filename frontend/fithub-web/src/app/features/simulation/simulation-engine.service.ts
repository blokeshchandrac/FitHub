import { computed, Injectable, signal } from '@angular/core';
import {
  createSimulationMachines,
  createSimulationMember,
  SimulationDecision,
  SimulationEquipmentMapping,
  SimulationEquipmentState,
  SimulationEvent,
  SimulationMachine,
  SimulationMember,
  SimulationMemberResponse,
  SimulationMode,
  SimulationPosition,
  SimulationSensorResult,
  SimulationStatusOverride,
  SimulationEventResponse,
  SimulationWire,
} from './simulation.models';

@Injectable()
export class SimulationEngineService {
  readonly clearanceThresholdMetres = signal(2.2);
  readonly clearanceSeconds = signal(30);
  readonly mode = signal<SimulationMode>('local');
  readonly sessionId = signal(createSessionId());
  readonly machines = signal<readonly SimulationMachine[]>(createSimulationMachines());
  readonly members = signal<readonly SimulationMember[]>([]);
  readonly membersLoading = signal(false);
  readonly membersError = signal<string | null>(null);
  readonly selectedMachineId = signal('EQ-TREADMILL-01');
  readonly selectedMemberId = signal<string | null>(null);
  readonly carriedCardId = signal<string | null>(null);
  readonly showTapTargets = signal(false);
  readonly events = signal<readonly SimulationEvent[]>([]);
  readonly message = signal<SimulationEvent | null>(null);
  readonly signalWire = signal<string | null>(null);

  readonly selectedMachine = computed(() => this.machine(this.selectedMachineId()));
  readonly selectedMember = computed(() => this.member(this.selectedMemberId()));
  readonly carriedMember = computed(() => this.members().find(member => member.cardId === this.carriedCardId()) ?? null);
  readonly enteredMembers = computed(() => this.members().filter(member => member.entered));

  private eventSequence = 0;
  private readonly restartTimers = new Map<string, ReturnType<typeof setTimeout>>();
  private signalTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.log('LAB_READY', 'READY', 'info', 'Refresh Members to load development FitHub accounts.');
  }

  configure(clearanceThresholdMetres: number, clearanceSeconds: number): void {
    this.clearanceThresholdMetres.set(Math.min(10, Math.max(0.5, clearanceThresholdMetres)));
    this.clearanceSeconds.set(Math.min(60, Math.max(1, Math.round(clearanceSeconds))));
  }

  setMode(mode: SimulationMode): void {
    this.mode.set(mode);
    this.log('MODE_CHANGED', mode.toUpperCase(), 'info', `${mode === 'local' ? 'Local' : 'Connected'} Mode selected.`);
  }

  setMembersLoading(loading: boolean): void {
    this.membersLoading.set(loading);
    if (loading) this.membersError.set(null);
  }

  setMembersError(message: string): void {
    this.membersLoading.set(false);
    this.membersError.set(message);
  }

  replaceMembers(catalog: readonly SimulationMemberResponse[]): void {
    const currentById = new Map(this.members().map(member => [member.id, member] as const));
    const next = catalog.map((source, index) => {
      const loaded = createSimulationMember(source, index);
      const current = currentById.get(loaded.id);
      if (!current) return loaded;
      return {
        ...loaded,
        statusOverride: current.statusOverride,
        entered: current.entered,
        hasCard: current.cardId === loaded.cardId ? current.hasCard : true,
        machineId: current.machineId,
        position: current.position,
      };
    });

    const validIds = new Set(next.map(member => member.id));
    this.members.set(next);
    this.machines.update(machines => machines.map(machine => {
      const memberIds = machine.memberIds.filter(id => validIds.has(id));
      if (memberIds.length === machine.memberIds.length) return machine;
      return memberIds.length
        ? { ...machine, memberIds, state: memberIds.length > 1 ? 'SHARING_ALLOWED' : 'OCCUPIED', activeStateBeforeClearance: null, clearanceDeadlineMs: null, clearanceRemainingSeconds: null }
        : { ...machine, memberIds: [], state: 'AVAILABLE', activeStateBeforeClearance: null, clearanceDeadlineMs: null, clearanceRemainingSeconds: null };
    }));
    this.membersLoading.set(false);
    this.membersError.set(null);
    if (this.selectedMemberId() && !validIds.has(this.selectedMemberId()!)) this.selectedMemberId.set(null);
    if (this.carriedMember() === null) this.carriedCardId.set(null);
    this.log('MEMBER_LIST_REFRESHED', `${next.length}_MEMBERS`, 'success', next.length
      ? `${next.length} development FitHub Member${next.length === 1 ? '' : 's'} loaded.`
      : 'No FitHub test Members are available. Register a Member or seed the development database.');
  }

  setStatusOverride(memberId: string, statusOverride: SimulationStatusOverride): void {
    const member = this.member(memberId);
    if (!member) return;
    this.updateMember(memberId, current => ({ ...current, statusOverride }));
    this.log('MEMBER_STATUS_OVERRIDE_CHANGED', statusOverride.toUpperCase(), 'warning',
      `${member.name} now uses ${statusOverride === 'Actual' ? 'database status' : `the session-only ${statusOverride} scenario`}.`, undefined, memberId);
  }

  selectMachine(machineId: string): void {
    if (this.machine(machineId)) this.selectedMachineId.set(machineId);
  }

  selectMember(memberId: string): void {
    const member = this.member(memberId);
    if (!member) return;
    this.selectedMemberId.set(memberId);
    this.log('MEMBER_SELECTED', 'SELECTED', 'info', `${member.name} selected.`, undefined, memberId);
  }

  enterGym(cardId: string): SimulationDecision {
    const member = this.memberByCard(cardId);
    if (!member) return this.deny('ENTRY_SCAN_DENIED', 'INVALID_CARD', 'Access denied — NFC card is invalid.');
    const decision = this.validateMember(member);
    if (!decision.allowed) return decision;
    if (member.entered) return this.deny('ENTRY_SCAN_DENIED', 'ALREADY_INSIDE', `${member.name} is already inside the gym.`, member.id);

    this.updateMember(member.id, current => ({ ...current, entered: true, position: { x: -7, z: 0 } }));
    this.selectedMemberId.set(member.id);
    this.pulse('pn532');
    return this.allow('ENTRY_SCAN_APPROVED', 'APPROVED', `Access approved — ${member.name} entered the gym.`, member.id);
  }

  takeCard(memberId: string): SimulationDecision {
    const member = this.member(memberId);
    if (!member?.entered) return this.deny('NFC_CARD_TAKEN', 'MEMBER_NOT_INSIDE', 'The Member must enter the gym first.');
    if (!member.hasCard) return this.deny('NFC_CARD_TAKEN', 'CARD_ALREADY_TAKEN', `${member.name}'s card is already in the inventory.`, member.id);
    if (this.carriedCardId()) return this.deny('NFC_CARD_TAKEN', 'INVENTORY_FULL', 'Return the current NFC card before taking another.', member.id);

    this.updateMember(member.id, current => ({ ...current, hasCard: false }));
    this.carriedCardId.set(member.cardId);
    this.showTapTargets.set(true);
    return this.allow('NFC_CARD_TAKEN', 'READY', `${member.name}'s NFC card is ready to drag to a reader.`, member.id);
  }

  returnCard(memberId: string): SimulationDecision {
    const member = this.member(memberId);
    if (!member) return this.deny('NFC_CARD_RETURNED', 'MEMBER_NOT_FOUND', 'Member not found.');
    if (this.carriedCardId() !== member.cardId) return this.deny('NFC_CARD_RETURNED', 'WRONG_CARD', `The inventory does not contain ${member.name}'s card.`, member.id);

    this.carriedCardId.set(null);
    this.showTapTargets.set(false);
    this.updateMember(member.id, current => ({ ...current, hasCard: true }));
    return this.allow('NFC_CARD_RETURNED', 'RETURNED', `${member.name}'s NFC card was returned to the correct Member.`, member.id);
  }

  sendToMachine(memberId: string, machineId = this.selectedMachineId()): SimulationDecision {
    const member = this.member(memberId);
    const machine = this.machine(machineId);
    if (!member?.entered || !machine) return this.deny('MEMBER_MOVED', 'INVALID_TARGET', 'Select a machine and an entered Member.');
    this.moveMember(member.id, machine.position.x, machine.position.z + 1.6);
    return this.allow('MEMBER_MOVED', 'AT_MACHINE', `${member.name} moved to ${machine.name}.`, member.id, machine.id);
  }

  scanMachine(cardId: string, machineId: string): SimulationDecision {
    const machine = this.machine(machineId);
    const member = this.memberByCard(cardId);
    if (!machine || !member) return this.deny('MACHINE_CLAIMED', 'INVALID_SCAN', 'The card or equipment identifier is invalid.');
    if (this.carriedCardId() !== cardId) return this.deny('MACHINE_CLAIMED', 'CARD_NOT_HELD', `Take ${member.name}'s card before using a machine reader.`, member.id, machine.id);

    const memberDecision = this.validateMember(member);
    if (!memberDecision.allowed) return memberDecision;
    if (!member.entered) return this.deny('MACHINE_CLAIMED', 'MEMBER_NOT_INSIDE', `Access denied — ${member.name} has not entered the gym.`, member.id, machine.id);
    if (!this.powerConnected(machine)) return this.deny('MACHINE_CLAIMED', 'DEVICE_OFFLINE', 'Access denied — equipment controller is offline.', member.id, machine.id);
    if (!this.nfcConnected(machine)) return this.deny('MACHINE_CLAIMED', 'NFC_UNAVAILABLE', 'Scan unavailable — the PN532 reader is disconnected.', member.id, machine.id);
    if (machine.restarting) return this.deny('MACHINE_CLAIMED', 'DEVICE_RESTARTING', 'Access denied — equipment controller is restarting.', member.id, machine.id);
    if (machine.safetyLocked || machine.state === 'TEMPORARILY_UNAVAILABLE') return this.deny('MACHINE_CLAIMED', 'SAFETY_LOCK', 'Access denied — equipment is temporarily unavailable.', member.id, machine.id);
    if (machine.state === 'OFFLINE_UNKNOWN') return this.deny('MACHINE_CLAIMED', 'DEVICE_OFFLINE', 'Access denied — equipment status is unknown.', member.id, machine.id);
    if (machine.memberIds.includes(member.id)) return this.deny('MACHINE_CLAIMED', 'ALREADY_ASSIGNED', `${member.name} is already assigned to ${machine.name}.`, member.id, machine.id);

    if (machine.state === 'AVAILABLE') {
      this.assignMember(machine, member, 'OCCUPIED');
      this.showTapTargets.set(false);
      this.pulse('pn532');
      return this.allow('MACHINE_CLAIMED', 'AUTHORIZED', `${member.name} claimed ${machine.name}.`, member.id, machine.id);
    }
    if (machine.state === 'SHARING_ALLOWED' && machine.memberIds.length === 1) {
      this.assignMember(machine, member, 'SHARING_ALLOWED');
      this.showTapTargets.set(false);
      this.pulse('pn532');
      return this.allow('SECOND_MEMBER_JOINED', 'AUTHORIZED', `${member.name} joined ${machine.name} as the one sharing partner.`, member.id, machine.id);
    }
    return this.deny('MACHINE_CLAIMED', machine.state === 'SHARING_ALLOWED' ? 'SHARING_FULL' : 'EQUIPMENT_OCCUPIED', `Access denied — ${machine.name} is already occupied.`, member.id, machine.id);
  }

  enableSharing(machineId: string): SimulationDecision {
    const machine = this.machine(machineId);
    if (!machine || machine.state !== 'OCCUPIED' || machine.memberIds.length !== 1)
      return this.deny('SHARING_ENABLED', 'INVALID_TRANSITION', 'Sharing can be enabled only by the owner of an occupied machine.');
    this.updateMachine(machine.id, current => ({ ...current, state: 'SHARING_ALLOWED' }));
    this.pulse('sharing-button');
    return this.allow('SHARING_ENABLED', 'ONE_PARTNER_ALLOWED', `${machine.name} now allows one additional Member.`, machine.memberIds[0], machine.id);
  }

  disableSharing(machineId: string): SimulationDecision {
    const machine = this.machine(machineId);
    if (!machine || machine.state !== 'SHARING_ALLOWED') return this.deny('SHARING_DISABLED', 'INVALID_TRANSITION', 'Sharing is not enabled for this machine.');
    if (machine.memberIds.length > 1) return this.deny('SHARING_DISABLED', 'PARTNER_PRESENT', 'The sharing partner must finish before sharing can be disabled.', undefined, machine.id);
    this.updateMachine(machine.id, current => ({ ...current, state: 'OCCUPIED' }));
    return this.allow('SHARING_DISABLED', 'OCCUPIED', `${machine.name} returned to occupied-only mode.`, machine.memberIds[0], machine.id);
  }

  finishSession(memberId: string): SimulationDecision {
    const member = this.member(memberId);
    if (!member?.machineId) return this.deny('SESSION_FINISHED', 'NO_SESSION', 'This Member has no active machine session.', memberId);
    const machine = this.machine(member.machineId);
    if (!machine) return this.deny('SESSION_FINISHED', 'MACHINE_NOT_FOUND', 'Assigned machine was not found.', memberId);
    this.pulse('finish-button');
    if (machine.memberIds.length > 1) {
      const remaining = machine.memberIds.filter(id => id !== member.id);
      const state: SimulationEquipmentState = machine.memberIds[0] === member.id ? 'OCCUPIED' : 'SHARING_ALLOWED';
      this.updateMachine(machine.id, current => ({ ...current, memberIds: remaining, state }));
      this.updateMember(member.id, current => ({ ...current, machineId: null }));
      return this.allow('SESSION_FINISHED', machine.memberIds[0] === member.id ? 'PARTNER_CONTINUES' : 'OWNER_CONTINUES',
        `${member.name} finished; ${machine.memberIds[0] === member.id ? 'the sharing partner' : 'the owner'} may continue.`, member.id, machine.id);
    }
    return this.allow('SESSION_FINISHED', 'AWAITING_CLEARANCE', `${member.name} finished; the sensor will release ${machine.name} only after the area remains clear.`, member.id, machine.id, 'warning');
  }

  reconcileConnectedEquipment(mappings: readonly SimulationEquipmentMapping[]): void {
    for (const mapping of mappings) this.reconcileConnectedMachine(mapping);
  }

  reconcileConnectedMachine(source: SimulationEquipmentMapping | SimulationEventResponse): void {
    const machineId = 'simulationEquipmentId' in source ? source.simulationEquipmentId : source.equipmentId;
    const ownerMemberId = source.ownerMemberId;
    const partnerMemberId = source.partnerMemberId;
    const memberIds = [ownerMemberId, partnerMemberId].filter((id): id is string => Boolean(id));
    const state = 'state' in source ? source.state : source.equipmentState;
    const clearanceStartedAt = 'clearanceStartedAt' in source ? source.clearanceStartedAt : null;
    const remaining = state === 'CLEARANCE_WARNING'
      ? 'clearanceSecondsRemaining' in source && source.clearanceSecondsRemaining !== null
        ? source.clearanceSecondsRemaining
        : Math.max(0, Math.ceil(((clearanceStartedAt ? Date.parse(clearanceStartedAt) : Date.now()) + this.clearanceSeconds() * 1000 - Date.now()) / 1000))
      : null;
    this.updateMachine(machineId, current => ({
      ...current,
      state,
      memberIds,
      safetyLocked: state === 'TEMPORARILY_UNAVAILABLE',
      sensorResult: state === 'CLEARANCE_WARNING' ? 'AREA_CLEAR' : memberIds.length ? 'PERSON_DETECTED' : current.sensorResult,
      activeStateBeforeClearance: state === 'CLEARANCE_WARNING' ? memberIds.length > 1 ? 'SHARING_ALLOWED' : 'OCCUPIED' : null,
      clearanceDeadlineMs: remaining === null ? null : performance.now() + remaining * 1000,
      clearanceRemainingSeconds: remaining,
    }));
    this.members.update(members => members.map(member => {
      if (memberIds.includes(member.id)) {
        const machine = this.machine(machineId);
        return { ...member, entered: true, machineId, position: machine ? { x: machine.position.x, z: machine.position.z + 1.6 } : member.position };
      }
      return member.machineId === machineId ? { ...member, machineId: null } : member;
    }));
  }

  moveMember(memberId: string, x: number, z: number): SimulationPosition | null {
    const member = this.member(memberId);
    if (!member?.entered) return null;
    const position = constrainPosition({ x, z }, member.machineId, this.machines());
    if (position.x === member.position.x && position.z === member.position.z) return position;
    this.updateMember(memberId, current => ({ ...current, position }));
    return position;
  }

  updateSensor(machineId: string, sensorResult: SimulationSensorResult, distanceMetres: number, nowMs: number): SimulationDecision | null {
    const machine = this.machine(machineId);
    if (!machine) return null;
    if (!this.sensorConnected(machine) || !this.powerConnected(machine)) sensorResult = 'SENSOR_FAULT';
    const distance = Math.max(0, Math.min(20, distanceMetres));

    if (sensorResult === 'SENSOR_FAULT') {
      if (machine.sensorResult === 'SENSOR_FAULT' && machine.state === 'OFFLINE_UNKNOWN') return null;
      this.updateMachine(machine.id, current => ({ ...current, sensorResult, distanceMetres: distance, clearanceDeadlineMs: null, clearanceRemainingSeconds: null }));
      return this.allow('SENSOR_FAULT', 'OFFLINE_UNKNOWN', 'The clearance sensor is unavailable; automatic release is disabled.', undefined, machine.id, 'error');
    }

    const activeSession = machine.memberIds.length > 0 && ['OCCUPIED', 'SHARING_ALLOWED', 'CLEARANCE_WARNING'].includes(machine.state);
    const resultChanged = machine.sensorResult !== sensorResult;
    if (resultChanged) {
      this.log(sensorResult === 'PERSON_DETECTED' ? 'SENSOR_ZONE_ENTERED' : 'SENSOR_ZONE_EXITED', sensorResult, 'info',
        sensorResult === 'PERSON_DETECTED' ? 'An assigned Member is inside the clearance zone.' : 'All assigned Members are outside the clearance zone.', machine.id);
    }

    if (!activeSession) {
      this.updateMachine(machine.id, current => ({ ...current, sensorResult, distanceMetres: distance, clearanceDeadlineMs: null, clearanceRemainingSeconds: null }));
      return null;
    }

    if (sensorResult === 'PERSON_DETECTED') {
      if (machine.state === 'CLEARANCE_WARNING') {
        const restored = machine.activeStateBeforeClearance ?? (machine.memberIds.length > 1 ? 'SHARING_ALLOWED' : 'OCCUPIED');
        this.updateMachine(machine.id, current => ({ ...current, state: restored, activeStateBeforeClearance: null, sensorResult, distanceMetres: distance, clearanceDeadlineMs: null, clearanceRemainingSeconds: null }));
        return this.allow('CLEARANCE_COUNTDOWN_CANCELLED', 'MEMBER_RETURNED', 'An assigned Member returned; the automatic release countdown was cancelled.', undefined, machine.id, 'warning');
      }
      if (resultChanged || machine.distanceMetres !== distance)
        this.updateMachine(machine.id, current => ({ ...current, sensorResult, distanceMetres: distance }));
      return null;
    }

    if (machine.state !== 'CLEARANCE_WARNING') {
      const previousState = machine.state as Extract<SimulationEquipmentState, 'OCCUPIED' | 'SHARING_ALLOWED'>;
      const deadline = nowMs + this.clearanceSeconds() * 1000;
      this.updateMachine(machine.id, current => ({ ...current, state: 'CLEARANCE_WARNING', activeStateBeforeClearance: previousState, sensorResult, distanceMetres: distance, clearanceDeadlineMs: deadline, clearanceRemainingSeconds: this.clearanceSeconds() }));
      return this.allow('CLEARANCE_COUNTDOWN_STARTED', 'COUNTDOWN_STARTED', `Area clear. ${machine.name} will release in ${formatSeconds(this.clearanceSeconds())}.`, undefined, machine.id, 'warning');
    }

    const deadline = machine.clearanceDeadlineMs ?? nowMs + this.clearanceSeconds() * 1000;
    const remaining = Math.max(0, Math.ceil((deadline - nowMs) / 1000));
    if (remaining > 0) {
      if (remaining !== machine.clearanceRemainingSeconds || resultChanged || machine.distanceMetres !== distance)
        this.updateMachine(machine.id, current => ({ ...current, sensorResult, distanceMetres: distance, clearanceDeadlineMs: deadline, clearanceRemainingSeconds: remaining }));
      return null;
    }

    for (const memberId of machine.memberIds) this.updateMember(memberId, current => ({ ...current, machineId: null }));
    this.updateMachine(machine.id, current => ({ ...current, state: 'AVAILABLE', activeStateBeforeClearance: null, memberIds: [], sensorResult: 'AREA_CLEAR', distanceMetres: distance, clearanceDeadlineMs: null, clearanceRemainingSeconds: null }));
    return this.allow('AUTOMATIC_RELEASE_COMPLETED', 'AVAILABLE', `${machine.name} remained clear for ${this.clearanceSeconds()} seconds and is now available.`, undefined, machine.id);
  }

  toggleWire(machineId: string, wire: SimulationWire): SimulationDecision {
    const machine = this.machine(machineId);
    if (!machine) return this.deny('HARDWARE_CHANGED', 'MACHINE_NOT_FOUND', 'Select a machine first.');
    const reconnecting = machine.disconnectedWires.includes(wire);
    const wires = reconnecting ? machine.disconnectedWires.filter(item => item !== wire) : [...machine.disconnectedWires, wire];

    if (!reconnecting) {
      const sensorFault = wire === 'ultrasonic-trigger' || wire === 'ultrasonic-echo';
      const affectsSafety = wire === 'power' || sensorFault;
      this.updateMachine(machine.id, current => ({
        ...current,
        disconnectedWires: wires,
        stateBeforeInterruption: affectsSafety && current.state !== 'OFFLINE_UNKNOWN'
          ? current.state === 'CLEARANCE_WARNING' ? current.activeStateBeforeClearance : current.state
          : current.stateBeforeInterruption,
        activeStateBeforeClearance: affectsSafety ? null : current.activeStateBeforeClearance,
        state: affectsSafety ? 'OFFLINE_UNKNOWN' : current.state,
        sensorResult: sensorFault || wire === 'power' ? 'SENSOR_FAULT' : current.sensorResult,
        clearanceDeadlineMs: affectsSafety ? null : current.clearanceDeadlineMs,
        clearanceRemainingSeconds: affectsSafety ? null : current.clearanceRemainingSeconds,
      }));
      const action = wire === 'power' ? 'DEVICE_OFFLINE' : sensorFault ? 'SENSOR_FAULT' : 'HARDWARE_DISPLAY_MISMATCH';
      const detail = wire.endsWith('led')
        ? `${wire.replace('-', ' ')} is dark although software state remains ${machine.state}.`
        : `${wire} disconnected. ${affectsSafety ? 'The machine moved to OFFLINE_UNKNOWN.' : 'The component is unavailable.'}`;
      return this.allow(action, 'DISCONNECTED', detail, undefined, machine.id, wire.endsWith('led') ? 'warning' : 'error');
    }

    const restoresSafety = wire === 'power' || wire === 'ultrasonic-trigger' || wire === 'ultrasonic-echo';
    this.updateMachine(machine.id, current => ({ ...current, disconnectedWires: wires, restarting: restoresSafety, state: restoresSafety ? 'TEMPORARILY_UNAVAILABLE' : current.state }));
    if (restoresSafety) this.scheduleRestart(machine.id);
    return this.allow('HARDWARE_RECONNECTED', 'RESTARTING', `${wire} reconnected.${restoresSafety ? ' Controller restart in progress.' : ''}`, undefined, machine.id);
  }

  setSafetyLock(machineId: string, locked: boolean): SimulationDecision {
    const machine = this.machine(machineId);
    if (!machine) return this.deny('SAFETY_LOCK_CHANGED', 'MACHINE_NOT_FOUND', 'Select a machine first.');
    if (locked === machine.safetyLocked) return this.deny('SAFETY_LOCK_CHANGED', 'NO_CHANGE', `Safety lock is already ${locked ? 'enabled' : 'cleared'}.`);
    if (locked) {
      this.updateMachine(machine.id, current => ({ ...current, safetyLocked: true, stateBeforeInterruption: current.state === 'CLEARANCE_WARNING' ? current.activeStateBeforeClearance : current.state, activeStateBeforeClearance: null, state: 'TEMPORARILY_UNAVAILABLE', clearanceDeadlineMs: null, clearanceRemainingSeconds: null }));
    } else {
      const restored = machine.stateBeforeInterruption ?? (machine.memberIds.length ? 'OCCUPIED' : 'AVAILABLE');
      this.updateMachine(machine.id, current => ({ ...current, safetyLocked: false, state: this.sensorConnected(current) && this.powerConnected(current) ? restored : 'OFFLINE_UNKNOWN', stateBeforeInterruption: null }));
    }
    return this.allow('SAFETY_LOCK_CHANGED', locked ? 'LOCKED' : 'UNLOCKED', `Safety lock ${locked ? 'enabled' : 'cleared'} for ${machine.name}.`, undefined, machine.id, locked ? 'warning' : 'success');
  }

  removeMember(memberId: string): SimulationDecision {
    const member = this.member(memberId);
    if (!member) return this.deny('MEMBER_REMOVED', 'MEMBER_NOT_FOUND', 'Member not found.');
    if (member.machineId) {
      const machine = this.machine(member.machineId);
      if (machine) {
        const remaining = machine.memberIds.filter(id => id !== member.id);
        this.updateMachine(machine.id, current => ({ ...current, memberIds: remaining, state: remaining.length ? 'OCCUPIED' : 'AVAILABLE', activeStateBeforeClearance: null, clearanceDeadlineMs: null, clearanceRemainingSeconds: null }));
      }
    }
    if (this.carriedCardId() === member.cardId) this.carriedCardId.set(null);
    if (this.carriedCardId() === null) this.showTapTargets.set(false);
    this.updateMember(member.id, current => ({ ...current, entered: false, hasCard: true, machineId: null }));
    if (this.selectedMemberId() === member.id) this.selectedMemberId.set(null);
    return this.allow('MEMBER_REMOVED', 'REMOVED', `${member.name} left the virtual gym.`, member.id);
  }

  reset(): void {
    for (const timer of this.restartTimers.values()) clearTimeout(timer);
    this.restartTimers.clear();
    this.machines.set(createSimulationMachines());
    this.members.update(members => members.map((member, index) => ({ ...member, statusOverride: 'Actual', entered: false, hasCard: true, machineId: null, position: { x: -8, z: -5.6 + (index % 6) * 0.7 } })));
    this.selectedMachineId.set('EQ-TREADMILL-01');
    this.selectedMemberId.set(null);
    this.carriedCardId.set(null);
    this.showTapTargets.set(false);
    this.sessionId.set(createSessionId());
    this.log('SIMULATION_RESET', 'RESET', 'info', 'Members retained from the development database; session overrides, assignments, timers, and hardware faults were cleared.');
  }

  recordExternalDecision(decision: SimulationDecision, machineId?: string): void {
    const machine = machineId ? this.machine(machineId) : undefined;
    this.log(decision.code, decision.allowed ? 'ACCEPTED' : 'DENIED', decision.allowed ? 'info' : 'error', decision.message, machineId, undefined, machine?.name);
  }

  dispose(): void {
    for (const timer of this.restartTimers.values()) clearTimeout(timer);
    if (this.signalTimer) clearTimeout(this.signalTimer);
  }

  private validateMember(member: SimulationMember): SimulationDecision {
    const status = effectiveStatus(member);
    if (status.accountStatus !== 'ACTIVE') return this.deny('ENTRY_SCAN_DENIED', 'USER_DISABLED', 'Access denied — the Member account is suspended.', member.id);
    if (status.membershipStatus === 'EXPIRED') return this.deny('ENTRY_SCAN_DENIED', 'MEMBERSHIP_EXPIRED', 'Access denied — membership expired.', member.id);
    if (status.membershipStatus !== 'ACTIVE') return this.deny('ENTRY_SCAN_DENIED', 'MEMBERSHIP_INACTIVE', 'Access denied — an active membership is required.', member.id);
    if (status.cardStatus !== 'ACTIVE') return this.deny('ENTRY_SCAN_DENIED', 'CARD_BLOCKED', 'Access denied — NFC card blocked.', member.id);
    return { allowed: true, code: 'VALID', message: 'Access allowed.' };
  }

  private assignMember(machine: SimulationMachine, member: SimulationMember, state: SimulationEquipmentState): void {
    if (member.machineId && member.machineId !== machine.id) {
      const previous = this.machine(member.machineId);
      if (previous) this.updateMachine(previous.id, current => ({ ...current, memberIds: current.memberIds.filter(id => id !== member.id), state: 'AVAILABLE' }));
    }
    this.updateMachine(machine.id, current => ({ ...current, memberIds: [...current.memberIds, member.id], state, sensorResult: 'PERSON_DETECTED', activeStateBeforeClearance: null, clearanceDeadlineMs: null, clearanceRemainingSeconds: null }));
    this.updateMember(member.id, current => ({ ...current, machineId: machine.id, position: { x: machine.position.x, z: machine.position.z + 1.6 } }));
  }

  private scheduleRestart(machineId: string): void {
    const existing = this.restartTimers.get(machineId);
    if (existing) clearTimeout(existing);
    const timer = setTimeout(() => {
      const current = this.machine(machineId);
      if (!current) return;
      const unsafe = !this.powerConnected(current) || !this.sensorConnected(current);
      const restored = current.stateBeforeInterruption ?? (current.memberIds.length > 1 ? 'SHARING_ALLOWED' : current.memberIds.length ? 'OCCUPIED' : 'AVAILABLE');
      this.updateMachine(machineId, machine => ({ ...machine, restarting: false, state: unsafe ? 'OFFLINE_UNKNOWN' : machine.safetyLocked ? 'TEMPORARILY_UNAVAILABLE' : restored, stateBeforeInterruption: unsafe ? machine.stateBeforeInterruption : null, sensorResult: unsafe ? 'SENSOR_FAULT' : machine.sensorResult === 'SENSOR_FAULT' ? 'AREA_CLEAR' : machine.sensorResult }));
      this.restartTimers.delete(machineId);
    }, 1200);
    this.restartTimers.set(machineId, timer);
  }

  private pulse(signalName: string): void {
    this.signalWire.set(signalName);
    if (this.signalTimer) clearTimeout(this.signalTimer);
    this.signalTimer = setTimeout(() => this.signalWire.set(null), 650);
  }

  private powerConnected(machine: SimulationMachine): boolean { return !machine.disconnectedWires.includes('power'); }
  private nfcConnected(machine: SimulationMachine): boolean { return !machine.disconnectedWires.includes('pn532'); }
  private sensorConnected(machine: SimulationMachine): boolean { return !machine.disconnectedWires.includes('ultrasonic-trigger') && !machine.disconnectedWires.includes('ultrasonic-echo'); }

  private allow(action: string, result: string, message: string, memberId?: string, machineId?: string, tone: SimulationEvent['tone'] = 'success'): SimulationDecision {
    this.log(action, result, tone, message, machineId, memberId);
    return { allowed: true, code: action, message };
  }

  private deny(action: string, result: string, message: string, memberId?: string, machineId?: string): SimulationDecision {
    this.log(action, result, 'error', message, machineId, memberId);
    return { allowed: false, code: result, message };
  }

  private log(action: string, result: string, tone: SimulationEvent['tone'], detail: string, machineId?: string, memberId?: string, suppliedMachineName?: string): void {
    const member = memberId ? this.member(memberId) : undefined;
    const machine = machineId ? this.machine(machineId) : undefined;
    const event: SimulationEvent = {
      id: ++this.eventSequence,
      occurredAt: new Date().toISOString(),
      tone,
      title: action.replaceAll('_', ' '),
      detail,
      action,
      result,
      mode: this.mode(),
      machineId,
      memberId,
      memberName: member?.name,
      machineName: machine?.name ?? suppliedMachineName,
    };
    this.events.update(events => [event, ...events].slice(0, 120));
    this.message.set(event);
  }

  private machine(id: string | null | undefined): SimulationMachine | undefined { return id ? this.machines().find(machine => machine.id === id) : undefined; }
  private member(id: string | null | undefined): SimulationMember | undefined { return id ? this.members().find(member => member.id === id) : undefined; }
  private memberByCard(cardId: string): SimulationMember | undefined { return this.members().find(member => member.cardId === cardId); }
  private updateMachine(id: string, update: (machine: SimulationMachine) => SimulationMachine): void { this.machines.update(machines => machines.map(machine => machine.id === id ? update(machine) : machine)); }
  private updateMember(id: string, update: (member: SimulationMember) => SimulationMember): void { this.members.update(members => members.map(member => member.id === id ? update(member) : member)); }
}

export function effectiveStatus(member: SimulationMember): Pick<SimulationMember, 'membershipStatus' | 'cardStatus' | 'accountStatus'> {
  if (member.statusOverride === 'Actual') return member;
  if (member.statusOverride === 'ActiveMembership') return { membershipStatus: 'ACTIVE', cardStatus: 'ACTIVE', accountStatus: 'ACTIVE' };
  if (member.statusOverride === 'ExpiredMembership') return { membershipStatus: 'EXPIRED', cardStatus: member.cardStatus, accountStatus: 'ACTIVE' };
  if (member.statusOverride === 'BlockedCard') return { membershipStatus: member.membershipStatus, cardStatus: 'BLOCKED', accountStatus: 'ACTIVE' };
  return { membershipStatus: member.membershipStatus, cardStatus: member.cardStatus, accountStatus: 'SUSPENDED' };
}

function constrainPosition(position: SimulationPosition, assignedMachineId: string | null, machines: readonly SimulationMachine[]): SimulationPosition {
  let x = clamp(position.x, -8.7, 8.2);
  let z = clamp(position.z, -6.6, 6.6);
  for (const machine of machines) {
    if (machine.id === assignedMachineId) continue;
    const dx = x - machine.position.x;
    const dz = z - machine.position.z;
    if (Math.abs(dx) >= 1.9 || Math.abs(dz) >= 1.45) continue;
    const pushX = 1.9 - Math.abs(dx);
    const pushZ = 1.45 - Math.abs(dz);
    if (pushX < pushZ) x = machine.position.x + Math.sign(dx || 1) * 1.9;
    else z = machine.position.z + Math.sign(dz || 1) * 1.45;
  }
  return { x, z };
}

function formatSeconds(seconds: number): string { return `00:${seconds.toString().padStart(2, '0')}`; }
function clamp(value: number, minimum: number, maximum: number): number { return Math.min(maximum, Math.max(minimum, value)); }
function createSessionId(): string { return `SIM-${new Date().getUTCFullYear()}-${crypto.randomUUID().slice(0, 8).toUpperCase()}`; }
