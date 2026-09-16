import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { LandingPageComponent } from './landing-page.component';

describe('LandingPageComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LandingPageComponent],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('renders the complete public journey with working authentication links', () => {
    const fixture = TestBed.createComponent(LandingPageComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const links = [...root.querySelectorAll<HTMLAnchorElement>('a')];

    expect(root.querySelector('h1')?.textContent).toContain('TRAIN SMARTER');
    expect(root.textContent).toContain('FITNESS BUILT AROUND YOU');
    expect(root.textContent).toContain('A SMARTER VISIT, FROM ARRIVAL TO PROGRESS');
    expect(root.textContent).toContain('YOUR PROGRESS BELONGS TO YOU');
    expect(root.textContent).toContain('MORE THAN A MEMBERSHIP');
    expect(links.some((link) => link.getAttribute('href') === '/login')).toBe(true);
    expect(links.some((link) => link.getAttribute('href') === '/register')).toBe(true);
    expect(root.textContent).not.toMatch(/ESP32|NFC|SignalR|PostgreSQL|API|sensor/i);
  });

  it('provides an accessible mobile navigation that closes with Escape', () => {
    const fixture = TestBed.createComponent(LandingPageComponent);
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('.menu-button') as HTMLButtonElement;

    expect(button.getAttribute('aria-expanded')).toBe('false');
    button.click();
    fixture.detectChanges();
    expect(button.getAttribute('aria-expanded')).toBe('true');
    expect(
      fixture.nativeElement.querySelector('nav[aria-label="Mobile public navigation"]'),
    ).not.toBeNull();

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector('nav[aria-label="Mobile public navigation"]'),
    ).toBeNull();
  });

  it('uses an optimized local hero image with an accessible fallback', () => {
    const fixture = TestBed.createComponent(LandingPageComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const sources = [...root.querySelectorAll<HTMLSourceElement>('picture source')];
    const image = root.querySelector('picture img') as HTMLImageElement;

    expect(sources[0].getAttribute('media')).toBe('(max-width: 900px)');
    expect(sources[0].getAttribute('srcset')).toBe(
      '/assets/landing/fithub-hero-community-mobile.webp',
    );
    expect(sources[1].getAttribute('srcset')).toBe(
      '/assets/landing/fithub-hero-community-mobile.jpg',
    );
    expect(sources[2].getAttribute('srcset')).toBe('/assets/landing/fithub-hero-community.webp');
    expect(image.getAttribute('src')).toBe('/assets/landing/fithub-hero-community.jpg');
    expect(image.alt).toContain('Men and women');
    expect(image.getAttribute('loading')).toBeNull();
  });

  it('uses the same media and body structure for all four goal cards', () => {
    const fixture = TestBed.createComponent(LandingPageComponent);
    fixture.detectChanges();
    const cards = [
      ...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLElement>('.goal-card'),
    ];

    expect(cards).toHaveLength(4);
    expect(cards.every((card) => card.querySelector(':scope > .goal-art'))).toBe(true);
    expect(cards.every((card) => card.querySelector(':scope > .goal-card__body'))).toBe(true);
    expect(cards.every((card) => !card.hasAttribute('style'))).toBe(true);
  });

  it('keeps the goals anchor clear of the sticky public header', () => {
    const fixture = TestBed.createComponent(LandingPageComponent);
    fixture.detectChanges();
    const goals = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('#your-goals');

    expect(goals).not.toBeNull();
    expect(getComputedStyle(goals!).scrollMarginTop).not.toBe('0px');
  });
});
