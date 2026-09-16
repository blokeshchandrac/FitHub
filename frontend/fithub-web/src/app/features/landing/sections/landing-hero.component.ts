import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LandingHeroMediaComponent } from './landing-hero-media.component';

@Component({
  selector: 'app-landing-hero',
  imports: [RouterLink, LandingHeroMediaComponent],
  templateUrl: './landing-hero.component.html',
  styleUrl: './landing-hero.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingHeroComponent {}
