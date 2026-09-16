import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';
import { EquipmentChanged } from '../../core/equipment/equipment.models';
import { EquipmentService } from '../../core/equipment/equipment.service';
import { EquipmentPage } from './equipment-page';

describe('EquipmentPage realtime updates', () => {
  it('reloads persisted equipment when EquipmentHub emits equipmentChanged', () => {
    const changes = new Subject<EquipmentChanged>();
    const service = {
      changes: changes.asObservable(),
      list: vi.fn(() => of([])),
      connect: vi.fn(() => Promise.resolve()),
    };
    TestBed.configureTestingModule({
      imports: [EquipmentPage],
      providers: [provideRouter([]), { provide: EquipmentService, useValue: service }],
    });

    TestBed.createComponent(EquipmentPage).detectChanges();
    changes.next({ equipmentId: 'equipment-1', state: 'SharingAllowed', version: 2, changedAt: new Date().toISOString() });

    expect(service.list).toHaveBeenCalledTimes(2);
    expect(service.connect).toHaveBeenCalledOnce();
  });
});
