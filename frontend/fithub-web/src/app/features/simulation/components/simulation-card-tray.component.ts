import { Component, input, output } from '@angular/core';
import { SimulationMember, SimulationStatusOverride } from '../simulation.models';

@Component({
  selector: 'app-simulation-card-tray',
  templateUrl: './simulation-card-tray.component.html',
  styleUrl: './simulation-card-tray.component.scss',
})
export class SimulationCardTrayComponent {
  readonly members = input.required<readonly SimulationMember[]>();
  readonly carriedCardId = input<string | null>(null);
  readonly selectedMemberId = input<string | null>(null);
  readonly loading = input(false);
  readonly error = input<string | null>(null);
  readonly entranceScan = output<string>();
  readonly machineScan = output<string>();
  readonly memberActions = output<string>();
  readonly returnCard = output<string>();
  readonly memberSelected = output<string>();
  readonly overrideChanged = output<{ memberId: string; statusOverride: SimulationStatusOverride }>();
  readonly refreshRequested = output<void>();

  protected beginDrag(event: DragEvent, cardId: string): void {
    event.dataTransfer?.setData('application/x-fithub-simulation-card', cardId);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
  }

  protected selectedMember(): SimulationMember | undefined {
    return this.members().find(member => member.id === this.selectedMemberId());
  }

  protected changeOverride(memberId: string, event: Event): void {
    this.overrideChanged.emit({ memberId, statusOverride: (event.target as HTMLSelectElement).value as SimulationStatusOverride });
  }
}
