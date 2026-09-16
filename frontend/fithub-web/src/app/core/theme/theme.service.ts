import { DOCUMENT } from '@angular/common';
import { Inject, Injectable, signal } from '@angular/core';
import { defaultTheme, isThemeId, ThemeId } from './theme.models';

const activeThemeKey = 'fithub.theme.active';
const guestThemeKey = 'fithub.theme.guest';
const userThemeKeyPrefix = 'fithub.theme.user.';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly selectedTheme = signal<ThemeId>(defaultTheme);
  private preferenceKey = guestThemeKey;

  readonly theme = this.selectedTheme.asReadonly();

  constructor(@Inject(DOCUMENT) private readonly document: Document) {
    this.restoreInitialTheme();
  }

  select(theme: ThemeId): void {
    this.apply(theme);
    this.write(this.preferenceKey, theme);
    this.write(activeThemeKey, theme);
  }

  setAuthenticatedUser(userId: string): void {
    this.preferenceKey = `${userThemeKeyPrefix}${userId}`;
    const stored = this.read(this.preferenceKey);
    this.apply(isThemeId(stored) ? stored : defaultTheme);
    this.write(activeThemeKey, this.selectedTheme());
  }

  useGuestPreference(): void {
    this.preferenceKey = guestThemeKey;
    const stored = this.read(guestThemeKey);
    this.apply(isThemeId(stored) ? stored : defaultTheme);
    this.write(activeThemeKey, this.selectedTheme());
  }

  private restoreInitialTheme(): void {
    const stored = this.read(activeThemeKey);
    const preRendered = this.document.documentElement.dataset['theme'];
    this.apply(isThemeId(stored) ? stored : isThemeId(preRendered) ? preRendered : defaultTheme);
    if (!isThemeId(stored)) this.write(activeThemeKey, this.selectedTheme());
  }

  private apply(theme: ThemeId): void {
    this.selectedTheme.set(theme);
    this.document.documentElement.dataset['theme'] = theme;
  }

  private read(key: string): string | null {
    try {
      return this.document.defaultView?.localStorage.getItem(key) ?? null;
    } catch {
      return null;
    }
  }

  private write(key: string, value: ThemeId): void {
    try {
      this.document.defaultView?.localStorage.setItem(key, value);
    } catch {
      // Theme selection remains functional when storage is unavailable.
    }
  }
}
