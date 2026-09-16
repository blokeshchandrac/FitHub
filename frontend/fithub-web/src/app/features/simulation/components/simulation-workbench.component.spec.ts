import { TestBed } from '@angular/core/testing';
import { createSimulationMachines, SimulationMachine } from '../simulation.models';
import { SimulationWorkbenchComponent } from './simulation-workbench.component';

describe('SimulationWorkbenchComponent', () => {
  it('uses yellow only for sharing and orange only for clearance warning', async () => {
    await TestBed.configureTestingModule({ imports: [SimulationWorkbenchComponent] }).compileComponents();
    const fixture = TestBed.createComponent(SimulationWorkbenchComponent);
    fixture.componentRef.setInput('machine', withState('SHARING_ALLOWED'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.led.yellow').classList.contains('on')).toBe(true);
    expect(fixture.nativeElement.querySelector('.led.red').classList.contains('on')).toBe(false);
    expect(fixture.nativeElement.querySelector('.led.orange').classList.contains('on')).toBe(false);

    fixture.componentRef.setInput('machine', withState('CLEARANCE_WARNING'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.led.yellow').classList.contains('on')).toBe(false);
    expect(fixture.nativeElement.querySelector('.led.orange').classList.contains('on')).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Clearance warning');
  });
});

function withState(state: SimulationMachine['state']): SimulationMachine {
  return { ...createSimulationMachines()[0], state };
}
