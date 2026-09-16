import { TestBed } from '@angular/core/testing';
import { MetricCard } from './metric-card';

describe('MetricCard', () => {
  it('renders typed metric content', () => {
    const fixture = TestBed.createComponent(MetricCard);
    fixture.componentRef.setInput('label', 'Current streak');
    fixture.componentRef.setInput('value', '4 weeks');
    fixture.componentRef.setInput('supportingText', 'Personal best');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Current streak');
    expect(fixture.nativeElement.querySelector('strong').textContent).toContain('4 weeks');
  });
});
