import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TenantService } from '../services/tenant.service';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenant = inject(TenantService);
  if (tenant.slug && !req.headers.has('X-Tenant-Slug')) {
    req = req.clone({ setHeaders: { 'X-Tenant-Slug': tenant.slug } });
  }
  return next(req);
};
