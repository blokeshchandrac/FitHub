import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'fit-skeleton-loader',
  templateUrl: './skeleton-loader.html',
  styleUrl: './skeleton-loader.scss',
})
export class SkeletonLoader {
  readonly label = input('Loading content');
  readonly lines = input(3);
  protected readonly lineItems = computed(() => Array.from({ length: Math.max(1, Math.min(this.lines(), 8)) }));
}
