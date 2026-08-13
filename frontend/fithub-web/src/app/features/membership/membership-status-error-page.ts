import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AccountContextService } from '../../core/membership/account-context.service';

@Component({selector:'app-membership-status-error-page',template:`<section class="auth-page"><div class="auth-card"><p class="eyebrow">Membership check</p><h1>We could not confirm your access</h1><p class="auth-intro">FitHub will not guess your membership status while the server is unavailable.</p>@if(error()){<div class="api-error" role="alert">{{error()}}</div>}<button class="primary-button" type="button" [disabled]="retrying()" (click)="retry()">{{retrying()?'Checking…':'Try again'}}</button></div></section>`})
export class MembershipStatusErrorPage {
  private readonly context=inject(AccountContextService);private readonly router=inject(Router);protected readonly retrying=signal(false);protected readonly error=signal<string|null>(null);
  protected retry():void{this.retrying.set(true);this.error.set(null);this.context.refresh().pipe(finalize(()=>this.retrying.set(false))).subscribe({next:()=>this.router.navigateByUrl(this.context.destination()),error:()=>this.error.set('Membership status is still unavailable. Please try again.')});}
}
