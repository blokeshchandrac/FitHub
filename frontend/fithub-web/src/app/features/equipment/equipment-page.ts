import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { EquipmentSummary } from '../../core/equipment/equipment.models';
import { EquipmentService } from '../../core/equipment/equipment.service';
@Component({ selector: 'app-equipment-page', imports: [RouterLink, DatePipe], templateUrl: './equipment-page.html' })
export class EquipmentPage implements OnInit {
  private readonly service = inject(EquipmentService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly items = signal<readonly EquipmentSummary[]>([]);
  protected readonly error = signal('');
  ngOnInit(): void {
    this.load();
    this.service.changes.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.load());
    void this.service.connect();
  }
  private load(): void { this.service.list().subscribe({ next: x => this.items.set(x), error: e => this.error.set(e.status === 403 ? 'Check in at the gym to see live machine details.' : 'Equipment could not be loaded.') }); }
}
