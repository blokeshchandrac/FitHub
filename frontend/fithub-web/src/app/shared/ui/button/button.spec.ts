import { TestBed } from '@angular/core/testing';
import { Button } from './button';

describe('Button', () => {
  it('renders its variant and prevents activation while disabled', () => {
    const fixture = TestBed.createComponent(Button);
    fixture.componentRef.setInput('variant', 'secondary');
    fixture.componentRef.setInput('disabled', true);
    const pressed = vi.fn();
    fixture.componentInstance.pressed.subscribe(pressed);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.click();
    expect(button.classList).toContain('button-secondary');
    expect(button.disabled).toBe(true);
    expect(pressed).not.toHaveBeenCalled();
  });
});
