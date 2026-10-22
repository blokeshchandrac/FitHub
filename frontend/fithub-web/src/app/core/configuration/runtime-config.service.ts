import { Injectable } from '@angular/core';

interface FitHubRuntimeConfig {
  readonly apiBaseUrl?: string;
}

declare global {
  interface Window {
    __FITHUB_CONFIG__?: FitHubRuntimeConfig;
  }
}

@Injectable({ providedIn: 'root' })
export class RuntimeConfigService {
  private readonly apiBaseUrl = normalizeBaseUrl(globalThis.window?.__FITHUB_CONFIG__?.apiBaseUrl ?? '');

  apiUrl(path: string): string {
    if (/^https?:\/\//i.test(path)) return path;
    return `${this.apiBaseUrl}${ensureLeadingSlash(path)}`;
  }

  hubUrl(path: string): string {
    return this.apiUrl(path);
  }
}

function normalizeBaseUrl(value: string): string {
  return value.trim().replace(/\/+$/, '');
}

function ensureLeadingSlash(value: string): string {
  return value.startsWith('/') ? value : `/${value}`;
}
