import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, HostListener, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-public-landing-header',
  imports: [RouterLink],
  templateUrl: './public-landing-header.component.html',
  styleUrl: './public-landing-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicLandingHeaderComponent {
  private readonly document = inject(DOCUMENT);
  protected readonly menuOpen = signal(false);

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  protected scrollToSection(event: Event, sectionId: string): void {
    const target = this.document.getElementById(sectionId);
    if (!target) return;

    event.preventDefault();
    this.closeMenu();
    const reducedMotion =
      this.document.defaultView?.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    target.scrollIntoView({ behavior: reducedMotion ? 'auto' : 'smooth', block: 'start' });
  }

  @HostListener('document:keydown.escape')
  protected handleEscape(): void {
    this.closeMenu();
  }
}
