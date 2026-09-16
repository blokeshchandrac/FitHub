import { TestBed } from '@angular/core/testing';
import { SectionCard } from './section-card';

describe('SectionCard', () => {
  it('renders an optional heading and description', () => {
    const fixture = TestBed.createComponent(SectionCard);
    fixture.componentRef.setInput('heading', 'Training overview');
    fixture.componentRef.setInput('description', 'This week at a glance.');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('h2').textContent).toContain('Training overview');
    expect(fixture.nativeElement.textContent).toContain('This week at a glance.');
  });
});
