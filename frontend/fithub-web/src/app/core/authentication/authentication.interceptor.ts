import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthenticationService } from './authentication.service';
import { RuntimeConfigService } from '../configuration/runtime-config.service';

export const authenticationInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) {
    return next(request);
  }

  const accessToken = inject(AuthenticationService).accessToken();
  const runtimeConfig = inject(RuntimeConfigService);
  const apiRequest = request.clone({
    url: runtimeConfig.apiUrl(request.url),
    // The refresh token remains an HttpOnly cookie on the API origin.
    withCredentials: true,
    ...(accessToken ? { setHeaders: { Authorization: `Bearer ${accessToken}` } } : {}),
  });

  return next(apiRequest);
};
