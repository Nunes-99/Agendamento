import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Tenant } from '../models/tenant.model';

@Injectable({ providedIn: 'root' })
export class TenantService {
  private http = inject(HttpClient);

  readonly current = signal<Tenant | null>(null);
  slug: string | null = null;

  detectarSlug(): void {
    // Detecta /t/:slug ou subdomínio
    const path = window.location.pathname.split('/').filter(Boolean);
    if (path[0] === 't' && path[1]) {
      this.slug = path[1];
      sessionStorage.setItem('tenant_slug', this.slug);
      return;
    }
    // Subdomínio (acme.agendamentopro.com.br)
    const host = window.location.hostname;
    if (host.includes('.') && !host.startsWith('www.') && !host.startsWith('localhost')) {
      const sub = host.split('.')[0];
      if (sub && sub !== 'admin' && sub !== 'app') {
        this.slug = sub;
        sessionStorage.setItem('tenant_slug', this.slug);
        return;
      }
    }
    this.slug = sessionStorage.getItem('tenant_slug');
  }

  carregarTenant(slug: string): Observable<Tenant> {
    return this.http.get<Tenant>(`${environment.apiUrl}/tenants/public/by-slug/${slug}`)
      .pipe(tap(t => this.current.set(t)));
  }
}
