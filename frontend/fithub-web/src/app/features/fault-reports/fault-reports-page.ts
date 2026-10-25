import { Component, inject, OnInit, signal } from '@angular/core'; import { FormsModule } from '@angular/forms'; import { EquipmentSummary } from '../../core/equipment/equipment.models'; import { EquipmentService } from '../../core/equipment/equipment.service'; import { FaultReport } from '../../core/fault-reports/fault-report.models'; import { FaultReportService } from '../../core/fault-reports/fault-report.service';
@Component({ selector: 'app-fault-reports-page', imports: [FormsModule], templateUrl: './fault-reports-page.html' })
export class FaultReportsPage implements OnInit {
  private readonly faults = inject(FaultReportService); private readonly equipmentService = inject(EquipmentService); protected readonly reports = signal<readonly FaultReport[]>([]); protected readonly equipment = signal<readonly EquipmentSummary[]>([]); protected readonly error = signal(''); protected readonly message = signal('');
  protected equipmentId=''; protected category='BrokenPart'; protected comment=''; protected serious=false; protected photo?: File;
  ngOnInit(): void { this.load(); this.equipmentService.list().subscribe(x => this.equipment.set(x)); }
  protected selectFile(event: Event): void { this.photo = (event.target as HTMLInputElement).files?.[0]; }
  protected submit(): void { if (!this.comment.trim()) { this.error.set('A comment is required.'); return; } const data = new FormData(); data.append('equipmentId', this.equipmentId); data.append('category', this.category); data.append('comment', this.comment); data.append('seriousSafetyConcern', String(this.serious)); if (this.photo) data.append('photo', this.photo); this.faults.create(data).subscribe({ next: () => { this.message.set('Fault report submitted.'); this.error.set(''); this.load(); }, error: e => this.error.set(e.error?.detail ?? 'Report could not be submitted.') }); }
  private load(): void { this.faults.mine().subscribe(x => this.reports.set(x)); }
}
