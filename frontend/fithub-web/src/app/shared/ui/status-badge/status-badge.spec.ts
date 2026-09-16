import { TestBed } from '@angular/core/testing';
import { StatusBadge } from './status-badge';

describe('StatusBadge', () => {
  it('communicates status with visible text and an icon instead of colour alone', () => {
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('label', 'Sharing allowed');
    fixture.componentRef.setInput('tone', 'warning');
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('.badge') as HTMLElement;
    expect(badge.textContent).toContain('Status:');
    expect(badge.textContent).toContain('Sharing allowed');
    expect(badge.querySelector('svg')).toBeTruthy();
    expect(badge.classList).toContain('badge-warning');
  });
});
