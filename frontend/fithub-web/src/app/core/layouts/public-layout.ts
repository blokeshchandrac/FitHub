import { Component, DestroyRef, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';

@Component({
  selector: 'app-public-layout',
  imports: [RouterLink, RouterOutlet],
  template: `
    @if (!isLandingPage()) {
      <header class="public-header">
        <a class="brand" routerLink="/login" aria-label="FitHub home">
          <span class="brand-mark" aria-hidden="true">FH</span><span>FitHub</span>
        </a>
      </header>
    }
    <main><router-outlet /></main>
  `,
  styles: [`
    :host { display:block; min-height:100vh; }
    .public-header { align-items:center; background:#0b1324; color:#fff; display:flex; min-height:4.5rem; padding:0 1.5rem; }
    .brand { align-items:center; color:inherit; display:flex; font-size:1.25rem; font-weight:800; gap:.75rem; text-decoration:none; }
    .brand-mark { align-items:center; background:#34d399; border-radius:.65rem; color:#052e24; display:inline-flex; font-size:.8rem; height:2.25rem; justify-content:center; width:2.25rem; }
    @media (max-height: 560px), (max-width: 480px) { .public-header { min-height:3.75rem; padding:0 1rem; } }
  `],
})
export class PublicLayout {
  private readonly router = inject(Router);
  readonly isLandingPage = signal(this.isRootUrl(this.router.url));

  constructor() {
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      takeUntilDestroyed(inject(DestroyRef)),
    ).subscribe(event => this.isLandingPage.set(this.isRootUrl(event.urlAfterRedirects)));
  }

  private isRootUrl(url: string): boolean {
    return url.split(/[?#]/, 1)[0] === '/';
  }
}
