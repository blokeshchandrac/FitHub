import { Component, input, output, signal } from '@angular/core';
import { SIMULATION_PIN_CONNECTIONS, SIMULATION_WIRE_LABELS, SimulationMachine, SimulationWire } from '../simulation.models';

@Component({
  selector: 'app-simulation-workbench',
  templateUrl: './simulation-workbench.component.html',
  styleUrl: './simulation-workbench.component.scss',
})
export class SimulationWorkbenchComponent {
  readonly machine = input.required<SimulationMachine>();
  readonly memberNames = input<readonly string[]>([]);
  readonly activeSignal = input<string | null>(null);
  readonly wireToggled = output<SimulationWire>();
  readonly finishPressed = output<void>();
  readonly sharingPressed = output<void>();
  readonly safetyChanged = output<boolean>();
  readonly selectedPin = signal<string | null>(null);

  protected readonly pinConnections = SIMULATION_PIN_CONNECTIONS;
  protected readonly wireLabels = SIMULATION_WIRE_LABELS;
  protected readonly wires = Object.keys(SIMULATION_WIRE_LABELS) as SimulationWire[];

  protected disconnected(wire: SimulationWire): boolean {
    return this.machine().disconnectedWires.includes(wire);
  }

  protected nfcOnline(): boolean {
    return !this.disconnected('power') && !this.disconnected('pn532') && !this.machine().restarting;
  }

  protected sensorOnline(): boolean {
    return !this.disconnected('power') && !this.disconnected('ultrasonic-trigger') && !this.disconnected('ultrasonic-echo') && !this.machine().restarting;
  }
}
