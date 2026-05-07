import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * Redireciona /admin para a rota inicial certa de acordo com o perfil:
 *   SuperAdmin → /admin/empresas
 *   Demais     → /admin/dashboard
 */
export const redirectByRoleGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const perfil = auth.user()?.perfil;
  const destino = perfil === 'SuperAdmin' ? '/admin/empresas' : '/admin/dashboard';
  router.navigate([destino]);
  return false;
};
