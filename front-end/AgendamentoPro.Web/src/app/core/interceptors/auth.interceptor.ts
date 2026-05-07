import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const u = auth.user();
  if (u?.accessToken && !req.url.includes('/auth/login') && !req.url.includes('/auth/refresh')) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${u.accessToken}` } });
  }
  return next(req);
};
