import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { ClienteAuthService } from '../services/cliente-auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const cliAuth = inject(ClienteAuthService);

  // Endpoints da área "Minha Conta" usam o token do cliente final.
  const minhaConta = req.url.match(/\/t\/([^/]+)\/(?:minha-conta|otp)/);
  if (minhaConta && !req.url.endsWith('/otp/solicitar') && !req.url.endsWith('/otp/validar')) {
    const slug = minhaConta[1];
    const t = cliAuth.token(slug);
    if (t) {
      req = req.clone({ setHeaders: { Authorization: `Bearer ${t}` } });
      return next(req);
    }
  }

  const u = auth.user();
  if (u?.accessToken && !req.url.includes('/auth/login') && !req.url.includes('/auth/refresh')) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${u.accessToken}` } });
  }
  return next(req);
};
