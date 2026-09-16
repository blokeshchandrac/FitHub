import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LandingRevealDirective } from '../landing-reveal.directive';

@Component({
  selector: 'app-landing-experience',
  imports: [LandingRevealDirective],
  templateUrl: './landing-experience.component.html',
  styleUrl: './landing-experience.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingExperienceComponent {}
