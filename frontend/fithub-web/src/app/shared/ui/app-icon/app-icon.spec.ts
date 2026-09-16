import { TestBed } from '@angular/core/testing';
import { AppIcon } from './app-icon';

describe('AppIcon', () => {
  it('is decorative by default and can expose an accessible label', () => {
    const fixture = TestBed.createComponent(AppIcon);
    fixture.componentRef.setInput('name', 'check');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('svg').getAttribute('aria-hidden')).toBe('true');

    fixture.componentRef.setInput('label', 'Selected');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('svg').getAttribute('aria-label')).toBe('Selected');
  });
});
