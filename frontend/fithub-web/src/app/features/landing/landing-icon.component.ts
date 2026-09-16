import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type LandingIconName =
  'clock' | 'direction' | 'unified' | 'strength' | 'weight' | 'endurance' | 'active';

@Component({
  selector: 'app-landing-icon',
  template: `
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.7"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
    >
      @switch (name()) {
        @case ('clock') {
          <circle cx="12" cy="12" r="8.5" />
          <path d="M12 7.5v5l3.4 2" />
        }
        @case ('direction') {
          <path d="M12 3.2 20 7.5v6.8c0 3.1-3 5.6-8 6.8-5-1.2-8-3.7-8-6.8V7.5z" />
          <path d="m8.7 12.2 2.1 2.1 4.7-5" />
        }
        @case ('unified') {
          <rect x="4" y="4" width="16" height="16" rx="3" />
          <path d="M8 9h8M8 13h5M8 17h3" />
        }
        @case ('strength') {
          <path d="M3 9v6M6 7.5v9M18 7.5v9M21 9v6M6 12h12" />
        }
        @case ('weight') {
          <path d="M7 5.5h10l2 14H5z" />
          <path d="M9.5 9a2.5 2.5 0 0 1 5 0M12 9l1.5-1.5" />
        }
        @case ('endurance') {
          <path
            d="M12 20s-7.5-4.3-7.5-10.1A4.1 4.1 0 0 1 12 7.6a4.1 4.1 0 0 1 7.5 2.3C19.5 15.7 12 20 12 20Z"
          />
          <path d="M7.5 12h2l1-2.2 2.2 4.5 1.2-2.3h2.6" />
        }
        @case ('active') {
          <circle cx="13.2" cy="4.8" r="1.7" />
          <path
            d="m10 9.3 3-1.7 2.5 2.1 2.4.3M12.6 9.2l-1.2 4.1 3.4 2.2 1.5 4M11.4 13.3l-3 2.2-2.2 3.3"
          />
        }
      }
    </svg>
  `,
  styles: [':host{display:inline-block;height:1.75rem;width:1.75rem}svg{height:100%;width:100%}'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingIconComponent {
  readonly name = input.required<LandingIconName>();
}
