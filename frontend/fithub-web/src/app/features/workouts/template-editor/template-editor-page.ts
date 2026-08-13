import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { Exercise } from '../../../core/workouts/workout.models';
import { WorkoutService } from '../../../core/workouts/workout.service';

@Component({ selector: 'app-template-editor-page', imports: [ReactiveFormsModule], templateUrl: './template-editor-page.html' })
export class TemplateEditorPage {
  private readonly service = inject(WorkoutService); private readonly route = inject(ActivatedRoute); private readonly router = inject(Router);
  protected readonly exercises = signal<Exercise[]>([]); protected readonly selected = signal<Set<string>>(new Set()); protected readonly error = signal<string | null>(null);
  protected readonly form = new FormGroup({ name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(150)] }), description: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(2000)] }), isPublic: new FormControl(false, { nonNullable: true }) });
  protected readonly templateId = this.route.snapshot.paramMap.get('id');
  constructor() { forkJoin({ exercises: this.service.searchExercises(), templates: this.service.getTemplates() }).subscribe(({ exercises, templates }) => { this.exercises.set(exercises); const template = templates.find((item) => item.id === this.templateId); if (template) { this.form.setValue({ name: template.name, description: template.description ?? '', isPublic: template.isPublic }); this.selected.set(new Set(template.exercises.map((item) => item.id))); } }); }
  protected toggle(id: string): void { const copy = new Set(this.selected()); copy.has(id) ? copy.delete(id) : copy.add(id); this.selected.set(copy); }
  protected save(): void { if (this.form.invalid || this.selected().size === 0) { this.form.markAllAsTouched(); this.error.set('Enter a name and select at least one exercise.'); return; } const command = { ...this.form.getRawValue(), exerciseIds: [...this.selected()] }; const request = this.templateId ? this.service.updateTemplate(this.templateId, command) : this.service.createTemplate(command); request.subscribe({ next: () => this.router.navigateByUrl('/workouts'), error: (error) => this.error.set(Object.values(error.error?.errors ?? {}).flat().join(' ') || 'Template could not be saved.') }); }
}
