import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LandingIconComponent } from '../landing-icon.component';
import { LandingRevealDirective } from '../landing-reveal.directive';

@Component({
  selector: 'app-landing-discovery',
  imports: [RouterLink, LandingIconComponent, LandingRevealDirective],
  templateUrl: './landing-discovery.component.html',
  styleUrl: './landing-discovery.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingDiscoveryComponent {}
