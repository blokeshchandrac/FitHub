import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { BodyProgress } from '../../core/progress/progress.models';
import { ProgressService } from '../../core/progress/progress.service';
import { ProgressChart } from './progress-chart';

@Component({ selector: 'app-progress-page', imports: [DatePipe, ProgressChart, ReactiveFormsModule], templateUrl: './progress-page.html' })
export class ProgressPage {
  private readonly service = inject(ProgressService); protected readonly progress = signal<BodyProgress | null>(null); protected readonly error = signal<string | null>(null); protected readonly message = signal<string | null>(null);
  protected readonly form = new FormGroup({ weightKg: new FormControl<number | null>(null, [Validators.min(20), Validators.max(400)]), heightCm: new FormControl<number | null>(null, [Validators.min(100), Validators.max(250)]), bodyFatPercentage: new FormControl<number | null>(null, [Validators.min(2), Validators.max(70)]), chestCm: new FormControl<number | null>(null, [Validators.min(30), Validators.max(300)]), waistCm: new FormControl<number | null>(null, [Validators.min(30), Validators.max(300)]), hipCm: new FormControl<number | null>(null, [Validators.min(30), Validators.max(300)]), notes: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(1000)] }) });
  constructor() { this.load(); }
  protected save(): void { if (this.form.invalid) { this.form.markAllAsTouched(); return; } this.service.logMeasurement(this.form.getRawValue()).subscribe({ next: () => { this.message.set('Measurement saved.'); this.form.reset({ notes: '' }); this.load(); }, error: (error) => this.error.set(Object.values(error.error?.errors ?? {}).flat().join(' ') || 'Measurement could not be saved.') }); }
  private load(): void { this.service.getProgress().subscribe({ next: (value) => this.progress.set(value), error: () => this.error.set('Progress could not be loaded.') }); }
}
