export const themeIds = ['volt', 'pulse', 'ocean', 'high-contrast'] as const;

export type ThemeId = (typeof themeIds)[number];

export interface ThemeOption {
  readonly id: ThemeId;
  readonly name: string;
  readonly description: string;
}

export const defaultTheme: ThemeId = 'volt';

export const themeOptions: readonly ThemeOption[] = [
  { id: 'volt', name: 'Volt', description: 'Charcoal surfaces with an electric lime accent.' },
  { id: 'pulse', name: 'Pulse', description: 'Blackberry surfaces with rose and violet accents.' },
  { id: 'ocean', name: 'Ocean', description: 'Midnight blue surfaces with a clear cyan accent.' },
  { id: 'high-contrast', name: 'High Contrast', description: 'Maximum readability with strong borders and focus.' },
];

export function isThemeId(value: unknown): value is ThemeId {
  return typeof value === 'string' && themeIds.includes(value as ThemeId);
}
