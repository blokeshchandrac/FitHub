import { TestBed } from '@angular/core/testing';
import { EmptyState } from './empty-state';

describe('EmptyState', () => {
  it('renders a labelled empty state', () => {
    const fixture = TestBed.createComponent(EmptyState);
    fixture.componentRef.setInput('title', 'No workouts yet');
    fixture.componentRef.setInput('message', 'Create a workout to get started.');
    fixture.detectChanges();
    const section = fixture.nativeElement.querySelector('section') as HTMLElement;
    expect(section.getAttribute('aria-labelledby')).toBe(fixture.nativeElement.querySelector('h2').id);
    expect(fixture.nativeElement.textContent).toContain('No workouts yet');
  });
});
