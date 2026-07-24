import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { catchError, switchMap, throwError } from 'rxjs';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../services/auth.service';
import { ReporteErroService } from '../services/reporte-erro.service';

const ROTAS_AUTH = ['/auth/login', '/auth/refresh'];

/** A própria rota de relato: reportar falha dela realimentaria o laço. */
const ROTA_RELATO = '/erros-cliente';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const snack = inject(MatSnackBar);
  const reporte = inject(ReporteErroService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      // Falha do SERVIDOR (5xx) ou rede fora (status 0) não pode passar em
      // silêncio. Boa parte das telas trata erro com `error: () => carregando
      // .set(false)`: o spinner some, a tela fica vazia e o usuário conclui que
      // "não há nada aqui" quando na verdade o servidor caiu. Avisar aqui, num
      // lugar só, resolve para todas elas — e o relato vai para o log do
      // servidor, onde alguém pode ver.
      if ((err.status >= 500 || err.status === 0) && !req.url.includes(ROTA_RELATO)) {
        snack.open(
          err.status === 0
            ? 'Sem conexão com o servidor. Verifique a internet.'
            : 'O servidor falhou nesta operação. Tente de novo em instantes.',
          'OK',
          { duration: 6000, panelClass: 'snack-erro' }
        );
        reporte.reportar({
          mensagem: err.message || `HTTP ${err.status}`,
          status: err.status,
          urlChamada: req.url,
        });
      }

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
