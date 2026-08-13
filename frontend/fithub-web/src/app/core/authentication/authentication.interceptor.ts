import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthenticationService } from './authentication.service';

export const authenticationInterceptor: HttpInterceptorFn = (request, next) => {
  const accessToken = inject(AuthenticationService).accessToken();
  if (!accessToken || !request.url.startsWith('/api/')) {
    return next(request);
  }

  return next(request.clone({
    setHeaders: { Authorization: `Bearer ${accessToken}` },
  }));
};
