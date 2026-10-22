import { TestBed } from '@angular/core/testing';
import { RuntimeConfigService } from './runtime-config.service';

describe('RuntimeConfigService', () => {
  afterEach(() => {
    delete window.__FITHUB_CONFIG__;
    TestBed.resetTestingModule();
  });

  it('keeps local same-origin paths when no hosted API is configured', () => {
    window.__FITHUB_CONFIG__ = { apiBaseUrl: '' };
    expect(TestBed.inject(RuntimeConfigService).apiUrl('/api/health')).toBe('/api/health');
  });

  it('resolves API and SignalR paths against the hosted API without duplicating slashes', () => {
    window.__FITHUB_CONFIG__ = { apiBaseUrl: 'https://fithub-api-10252426.onrender.com/' };
    const config = TestBed.inject(RuntimeConfigService);
    expect(config.apiUrl('/api/health')).toBe('https://fithub-api-10252426.onrender.com/api/health');
    expect(config.hubUrl('/hubs/equipment')).toBe('https://fithub-api-10252426.onrender.com/hubs/equipment');
  });
});
