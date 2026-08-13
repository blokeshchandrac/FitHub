import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthenticationService } from '../authentication/authentication.service';

interface NavigationItem { label:string; path:string; icon:string; roles?:readonly string[]; }

@Component({
  selector: 'app-application-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="app-shell">
      <header>
        <button class="menu-button" type="button" (click)="navigationOpen.update(open => !open)" aria-label="Toggle navigation"><span></span><span></span><span></span></button>
        <a class="brand" routerLink="/dashboard"><span class="brand-mark">FH</span><span>FitHub</span></a>
        @if (authentication.currentUser(); as user) {
          <div class="header-actions"><span class="status-dot"></span><span>{{ user.firstName }} · {{ user.roles.join(', ') }}</span><button type="button" (click)="logout()">Sign out</button></div>
        }
      </header>
      <aside [class.open]="navigationOpen()"><nav aria-label="Primary navigation">
        @for (item of navigationItems; track item.path) {
          @if (!item.roles || hasAnyRole(item.roles)) {
            <a [routerLink]="item.path" routerLinkActive="active" (click)="navigationOpen.set(false)"><span class="nav-icon">{{ item.icon }}</span>{{ item.label }}</a>
          }
        }
      </nav></aside>
      <main><router-outlet /></main>
    </div>
  `,
  styles: [`
    :host,.app-shell{display:block;min-height:100vh}.app-shell{display:grid;grid-template:4.5rem 1fr/16rem 1fr}
    header{align-items:center;background:#0b1324;color:#fff;display:flex;gap:1rem;grid-column:1/-1;padding:0 1.5rem;position:sticky;top:0;z-index:10}
    .brand{align-items:center;color:inherit;display:flex;font-size:1.25rem;font-weight:800;gap:.75rem;text-decoration:none}.brand-mark{align-items:center;background:#34d399;border-radius:.65rem;color:#052e24;display:inline-flex;font-size:.8rem;height:2.25rem;justify-content:center;width:2.25rem}
    .header-actions{align-items:center;color:#cbd5e1;display:flex;font-size:.875rem;gap:.5rem;margin-left:auto}.status-dot{background:#34d399;border-radius:50%;height:.5rem;width:.5rem}.header-actions button{background:transparent;border:1px solid #475569;border-radius:.5rem;color:#fff;cursor:pointer;padding:.45rem .75rem}
    aside{background:#111c31;padding:1.25rem .75rem}nav{display:grid;gap:.25rem}nav a{align-items:center;border-radius:.65rem;color:#cbd5e1;display:flex;font-size:.925rem;gap:.75rem;padding:.7rem .8rem;text-decoration:none}nav a:hover,nav a.active{background:#1e2d49;color:#fff}.nav-icon{align-items:center;background:#263958;border-radius:.4rem;display:inline-flex;font-size:.7rem;font-weight:800;height:1.6rem;justify-content:center;width:1.6rem}main{min-width:0;padding:clamp(1.25rem,3vw,2.5rem)}
    .menu-button{background:transparent;border:0;display:none;padding:.5rem}.menu-button span{background:#fff;display:block;height:2px;margin:4px 0;width:1.25rem}
    @media(max-width:800px){.app-shell{grid-template:4rem 1fr/1fr}.menu-button{display:block}aside{bottom:0;left:0;position:fixed;top:4rem;transform:translateX(-100%);transition:transform 180ms ease;width:min(18rem,84vw);z-index:9}aside.open{transform:translateX(0)}.header-actions>span:not(.status-dot){display:none}}
  `],
})
export class ApplicationLayout {
  protected readonly authentication = inject(AuthenticationService);
  private readonly router = inject(Router);
  protected readonly navigationOpen = signal(false);
  protected readonly navigationItems: readonly NavigationItem[] = [
    {label:'Dashboard',path:'/dashboard',icon:'D'},{label:'Membership',path:'/membership',icon:'M'},{label:'Payments',path:'/payments',icon:'$'},
    {label:'Attendance',path:'/attendance',icon:'A'},{label:'Workouts',path:'/workouts',icon:'W'},{label:'Progress',path:'/progress',icon:'P'},
    {label:'Equipment',path:'/equipment',icon:'E'},{label:'Fault reports',path:'/fault-reports',icon:'F'},{label:'Parking',path:'/parking',icon:'P'},
    {label:'Recommendations',path:'/recommendations',icon:'R'},{label:'Booking',path:'/booking',icon:'B'},{label:'Notifications',path:'/notifications',icon:'N'},
    {label:'Scanner',path:'/attendance/scanner',icon:'S',roles:['Trainer','Administrator']},{label:'Account',path:'/account',icon:'U'},
    {label:'Administration',path:'/administration',icon:'A',roles:['Administrator']},{label:'Fault administration',path:'/administration/fault-reports',icon:'!',roles:['Administrator']},
  ];
  protected hasAnyRole(roles: readonly string[]): boolean { return roles.some(role => this.authentication.hasRole(role)); }
  protected logout(): void { this.authentication.logout().subscribe({next:()=>this.router.navigateByUrl('/login'),error:()=>this.router.navigateByUrl('/login')}); }
}
