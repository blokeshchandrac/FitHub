import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthenticationService } from '../authentication/authentication.service';

@Component({
  selector: 'app-membership-onboarding-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="onboarding-shell">
      <header>
        <a class="brand" routerLink="/membership/choose"><span class="brand-mark">FH</span><span>FitHub</span></a>
        <nav aria-label="Account navigation">
          <a routerLink="/membership/choose" routerLinkActive="active">Membership</a>
          <a routerLink="/payments" routerLinkActive="active">Payment history</a>
          <a routerLink="/account" routerLinkActive="active">Profile</a>
          <button type="button" (click)="logout()">Sign out</button>
        </nav>
      </header>
      <main><router-outlet /></main>
    </div>
  `,
  styles: [`
    :host,.onboarding-shell { display:block; min-height:100vh; }
    .onboarding-shell { background:linear-gradient(145deg,#effcf7 0,#f4f7fb 50%,#eef2ff 100%); }
    header { align-items:center; background:#0b1324; color:#fff; display:flex; gap:1rem; min-height:4.5rem; padding:.75rem clamp(1rem,4vw,3rem); }
    .brand { align-items:center; color:inherit; display:flex; font-size:1.25rem; font-weight:800; gap:.75rem; text-decoration:none; }
    .brand-mark { align-items:center; background:#34d399; border-radius:.65rem; color:#052e24; display:inline-flex; font-size:.8rem; height:2.25rem; justify-content:center; width:2.25rem; }
    nav { align-items:center; display:flex; gap:.5rem; margin-left:auto; }
    nav a,button { background:transparent; border:1px solid transparent; border-radius:.5rem; color:#dbeafe; cursor:pointer; padding:.55rem .7rem; text-decoration:none; }
    nav a:hover,nav a.active { background:#1e2d49; color:#fff; }
    button { border-color:#475569; color:#fff; }
    main { margin:0 auto; max-width:80rem; min-height:calc(100vh - 4.5rem); padding:clamp(1.25rem,4vw,3.5rem); }
    @media(max-width:720px){header{align-items:flex-start;flex-direction:column}nav{flex-wrap:wrap;margin-left:0}nav a,button{font-size:.83rem;padding:.45rem .55rem}main{min-height:calc(100vh - 8rem)}}
  `],
})
export class MembershipOnboardingLayout {
  private readonly authentication = inject(AuthenticationService);
  private readonly router = inject(Router);
  protected logout(): void {
    this.authentication.logout().subscribe({ next: () => this.router.navigateByUrl('/login'), error: () => this.router.navigateByUrl('/login') });
  }
}
