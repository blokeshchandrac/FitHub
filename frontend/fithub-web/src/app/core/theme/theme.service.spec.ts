import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    TestBed.configureTestingModule({});
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  it('uses Volt by default', () => {
    const service = TestBed.inject(ThemeService);

    expect(service.theme()).toBe('volt');
    expect(document.documentElement.dataset['theme']).toBe('volt');
  });

  it('restores a valid stored theme', () => {
    localStorage.setItem('fithub.theme.active', 'ocean');

    const service = TestBed.inject(ThemeService);

    expect(service.theme()).toBe('ocean');
    expect(document.documentElement.dataset['theme']).toBe('ocean');
  });

  it('rejects an unknown stored theme and falls back to Volt', () => {
    localStorage.setItem('fithub.theme.active', 'unknown-neon');

    const service = TestBed.inject(ThemeService);

    expect(service.theme()).toBe('volt');
    expect(localStorage.getItem('fithub.theme.active')).toBe('volt');
  });

  it('persists a selected theme identifier', () => {
    const service = TestBed.inject(ThemeService);

    service.select('pulse');

    expect(service.theme()).toBe('pulse');
    expect(localStorage.getItem('fithub.theme.active')).toBe('pulse');
    expect(localStorage.getItem('fithub.theme.guest')).toBe('pulse');
  });

  it('keeps authenticated user preferences separate', () => {
    const service = TestBed.inject(ThemeService);
    service.select('pulse');
    service.setAuthenticatedUser('user-a');
    service.select('ocean');
    service.setAuthenticatedUser('user-b');
    service.select('high-contrast');

    service.setAuthenticatedUser('user-a');
    expect(service.theme()).toBe('ocean');

    service.useGuestPreference();
    expect(service.theme()).toBe('pulse');
  });
});
