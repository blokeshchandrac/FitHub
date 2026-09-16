import { TestBed } from '@angular/core/testing';
import { ThemeService } from '../../../core/theme/theme.service';
import { ThemeSelector } from './theme-selector';

describe('ThemeSelector', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  it('uses native radio semantics with visible theme names', () => {
    const fixture = TestBed.createComponent(ThemeSelector);
    fixture.detectChanges();
    const fieldset = fixture.nativeElement.querySelector('fieldset') as HTMLFieldSetElement;
    const radios = fixture.nativeElement.querySelectorAll('input[type="radio"]') as NodeListOf<HTMLInputElement>;
    expect(fieldset.querySelector('legend')?.textContent).toContain('Theme preference');
    expect(radios).toHaveLength(4);
    expect(fixture.nativeElement.textContent).toContain('High Contrast');
    expect(radios[0].checked).toBe(true);
  });

  it('selects and persists a theme through a keyboard-operable radio', () => {
    const fixture = TestBed.createComponent(ThemeSelector);
    fixture.detectChanges();
    const ocean = fixture.nativeElement.querySelector('input[value="ocean"]') as HTMLInputElement;
    ocean.click();
    fixture.detectChanges();
    expect(TestBed.inject(ThemeService).theme()).toBe('ocean');
    expect(localStorage.getItem('fithub.theme.active')).toBe('ocean');
    expect(ocean.closest('label')?.classList).toContain('selected');
    expect(ocean.closest('label')?.querySelector('svg')?.getAttribute('aria-label')).toBe('Selected');
  });
});
