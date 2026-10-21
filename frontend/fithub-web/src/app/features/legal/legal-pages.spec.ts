import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { createRoutes } from '../../app.routes';
import { PrivacyPage } from './privacy-page';
import { TermsPage } from './terms-page';

describe('Public legal pages', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideRouter([])] }));

  it('renders the FitHub academic Terms and Privacy notices', () => {
    const terms = TestBed.createComponent(TermsPage);
    const privacy = TestBed.createComponent(PrivacyPage);
    terms.detectChanges();
    privacy.detectChanges();

    expect(terms.nativeElement.textContent).toContain('university software-engineering demonstration');
    expect(privacy.nativeElement.textContent).toContain('Stripe Test Mode');
  });

  it('registers loadable public Terms and Privacy routes', async () => {
    const publicChildren = createRoutes([]).find((route) => route.path === '' && route.children?.some((child) => child.path === 'register'))!.children!;
    const terms = publicChildren.find((route) => route.path === 'terms')!;
    const privacy = publicChildren.find((route) => route.path === 'privacy')!;

    expect(await terms.loadComponent!()).toBe(TermsPage);
    expect(await privacy.loadComponent!()).toBe(PrivacyPage);
  });
});
