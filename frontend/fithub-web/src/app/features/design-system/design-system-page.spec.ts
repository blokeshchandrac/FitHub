import { TestBed } from '@angular/core/testing';
import { DesignSystemPage } from './design-system-page';

describe('DesignSystemPage', () => {
  beforeEach(() => localStorage.clear());

  it('renders all theme names and core state previews', () => {
    const fixture = TestBed.createComponent(DesignSystemPage);
    fixture.detectChanges();
    for (const name of ['Volt', 'Pulse', 'Ocean', 'High Contrast']) {
      expect(fixture.nativeElement.textContent).toContain(name);
    }
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeTruthy();
  });
});
