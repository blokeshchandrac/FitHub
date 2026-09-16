import { TestBed } from '@angular/core/testing';
import { SkeletonLoader } from './skeleton-loader';

describe('SkeletonLoader', () => {
  it('announces loading and limits rendered lines', () => {
    const fixture = TestBed.createComponent(SkeletonLoader);
    fixture.componentRef.setInput('label', 'Loading memberships');
    fixture.componentRef.setInput('lines', 12);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="status"]').getAttribute('aria-label')).toBe('Loading memberships');
    expect(fixture.nativeElement.querySelectorAll('.skeleton-line')).toHaveLength(8);
  });
});
