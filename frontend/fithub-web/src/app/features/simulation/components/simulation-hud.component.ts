import { SlicePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { SimulationConnectionState, SimulationEvent, SimulationMachine, SimulationMember, SimulationMode } from '../simulation.models';

@Component({
  selector: 'app-simulation-hud',
  imports: [SlicePipe],
  templateUrl: './simulation-hud.component.html',
  styleUrl: './simulation-hud.component.scss',
})
export class SimulationHudComponent {
  readonly machine = input.required<SimulationMachine>();
  readonly members = input.required<readonly SimulationMember[]>();
  readonly events = input.required<readonly SimulationEvent[]>();
  readonly mode = input.required<SimulationMode>();
  readonly backendState = input.required<SimulationConnectionState>();
  readonly signalRState = input.required<SimulationConnectionState>();
  readonly clearanceSeconds = input.required<number>();
  readonly helpRequested = output<void>();
  readonly resetRequested = output<void>();

  protected memberNames(): string {
    const names = this.machine().memberIds
      .map(id => this.members().find(member => member.id === id)?.name)
      .filter((name): name is string => Boolean(name));
    return names.length ? names.join(' + ') : 'None';
  }

  protected nfcStatus(): string {
    const machine = this.machine();
    if (machine.disconnectedWires.includes('power')) return 'Offline';
    return machine.disconnectedWires.includes('pn532') ? 'Disconnected' : machine.restarting ? 'Restarting' : 'Ready';
  }

  protected sensorDescription(): string {
    const machine = this.machine();
    if (machine.sensorResult === 'SENSOR_FAULT') return 'Sensor fault — automatic release disabled';
    if (machine.sensorResult === 'PERSON_DETECTED') return 'Person detected';
    return machine.clearanceRemainingSeconds === null ? 'Area clear' : `Area clear · releasing in 00:${machine.clearanceRemainingSeconds.toString().padStart(2, '0')}`;
  }
}
