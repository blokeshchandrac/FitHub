import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LandingRevealDirective } from '../landing-reveal.directive';

@Component({
  selector: 'app-landing-close',
  imports: [RouterLink, LandingRevealDirective],
  templateUrl: './landing-close.component.html',
  styleUrl: './landing-close.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingCloseComponent {
  protected readonly year = new Date().getFullYear();
}
