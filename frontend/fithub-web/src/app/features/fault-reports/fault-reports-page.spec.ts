import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { EquipmentService } from '../../core/equipment/equipment.service';
import { FaultReportService } from '../../core/fault-reports/fault-report.service';
import { FaultReportsPage } from './fault-reports-page';

describe('FaultReportsPage', () => {
  const faultService = { mine: vi.fn(), create: vi.fn() };
  const equipmentService = { list: vi.fn() };

  beforeEach(async () => {
    faultService.mine.mockReset();
    faultService.create.mockReset();
    equipmentService.list.mockReset();
    faultService.mine.mockReturnValue(of([]));
    equipmentService.list.mockReturnValue(of([{ id: 'equipment-1', name: 'Treadmill' }]));
    await TestBed.configureTestingModule({
      imports: [FaultReportsPage],
      providers: [
        { provide: FaultReportService, useValue: faultService },
        { provide: EquipmentService, useValue: equipmentService },
      ],
    }).compileComponents();
  });

  it('requires a comment for every fault category and does not call the API when empty', () => {
    const fixture = TestBed.createComponent(FaultReportsPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const equipment = root.querySelector<HTMLSelectElement>('select[name="equipment"]')!;
    equipment.value = 'equipment-1';
    equipment.dispatchEvent(new Event('change', { bubbles: true }));
    const category = root.querySelector<HTMLSelectElement>('select[name="category"]')!;
    category.value = 'SafetyConcern';
    category.dispatchEvent(new Event('change', { bubbles: true }));

    const submit = new Event('submit', { bubbles: true, cancelable: true });
    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(submit);
    fixture.detectChanges();

    expect(submit.defaultPrevented).toBe(true);
    expect(faultService.create).not.toHaveBeenCalled();
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('A comment is required.');
    expect(root.querySelector<HTMLTextAreaElement>('#fault-comment')?.required).toBe(true);
  });
});
