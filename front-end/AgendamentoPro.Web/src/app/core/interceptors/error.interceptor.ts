import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { catchError, switchMap, throwError } from 'rxjs';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

const ROTAS_AUTH = ['/auth/login', '/auth/refresh'];

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const isAuthRoute = ROTAS_AUTH.some(r => req.url.includes(r));
      if (err.status !== 401 || isAuthRoute) {
        return throwError(() => err);
      }

      // Tenta refresh uma única vez
      const u = auth.user();
      if (!u?.refreshToken) {
        auth.logout();
        router.navigate(['/admin/login']);
        return throwError(() => err);
      }

      return auth.refresh().pipe(
        switchMap(novo => {
          const retry: HttpRequest<unknown> = req.clone({
            setHeaders: { Authorization: `Bearer ${novo.accessToken}` }
          });
          return next(retry);
        }),
        catchError(refreshErr => {
          auth.logout();
          router.navigate(['/admin/login']);
          return throwError(() => refreshErr);
        })
      );
    })
  );
};
