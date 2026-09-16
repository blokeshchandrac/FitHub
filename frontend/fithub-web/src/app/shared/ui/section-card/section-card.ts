import { Component, input } from '@angular/core';

@Component({
  selector: 'fit-section-card',
  templateUrl: './section-card.html',
  styleUrl: './section-card.scss',
})
export class SectionCard {
  readonly heading = input<string | null>(null);
  readonly description = input<string | null>(null);
  readonly elevated = input(false);
}
