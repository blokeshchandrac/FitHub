import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-unauthorized-page',
  imports: [RouterLink],
  template: `<section class="auth-page"><div class="auth-card"><p class="eyebrow">Access restricted</p><h1>Unauthorized</h1><p class="auth-intro">Your account does not have permission to view this page.</p><a class="primary-button button-link" routerLink="/dashboard">Return to dashboard</a></div></section>`,
})
export class UnauthorizedPage {}
