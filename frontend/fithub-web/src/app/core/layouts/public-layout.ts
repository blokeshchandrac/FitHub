import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-public-layout',
  imports: [RouterLink, RouterOutlet],
  template: `
    <header class="public-header">
      <a class="brand" routerLink="/login"><span class="brand-mark">FH</span><span>FitHub</span></a>
    </header>
    <main><router-outlet /></main>
  `,
  styles: [`
    :host { display:block; min-height:100vh; }
    .public-header { align-items:center; background:#0b1324; color:#fff; display:flex; min-height:4.5rem; padding:0 1.5rem; }
    .brand { align-items:center; color:inherit; display:flex; font-size:1.25rem; font-weight:800; gap:.75rem; text-decoration:none; }
    .brand-mark { align-items:center; background:#34d399; border-radius:.65rem; color:#052e24; display:inline-flex; font-size:.8rem; height:2.25rem; justify-content:center; width:2.25rem; }
  `],
})
export class PublicLayout {}
