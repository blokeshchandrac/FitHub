import { Component, inject } from '@angular/core';
import { AuthenticationService } from '../../core/authentication/authentication.service';

@Component({ selector: 'app-account-page', template: `<section class="feature-page"><p class="eyebrow">Account</p><h1>Your profile</h1>@if (authentication.currentUser(); as user) { <div class="foundation-card metric-grid"><div><span>Name</span><strong>{{ user.firstName }} {{ user.lastName }}</strong></div><div><span>Email</span><strong>{{ user.email }}</strong></div><div><span>Roles</span><strong>{{ user.roles.join(', ') }}</strong></div></div> }</section>` })
export class AccountPage { protected readonly authentication = inject(AuthenticationService); }
