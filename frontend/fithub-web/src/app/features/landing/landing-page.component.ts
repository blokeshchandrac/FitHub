import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LandingCloseComponent } from './sections/landing-close.component';
import { LandingDiscoveryComponent } from './sections/landing-discovery.component';
import { LandingExperienceComponent } from './sections/landing-experience.component';
import { LandingHeroComponent } from './sections/landing-hero.component';
import { PublicLandingHeaderComponent } from './sections/public-landing-header.component';

@Component({
  selector: 'app-landing-page',
  imports: [
    PublicLandingHeaderComponent,
    LandingHeroComponent,
    LandingDiscoveryComponent,
    LandingExperienceComponent,
    LandingCloseComponent,
  ],
  templateUrl: './landing-page.component.html',
  styleUrl: './landing-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingPageComponent {}
