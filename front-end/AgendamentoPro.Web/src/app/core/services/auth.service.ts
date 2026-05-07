import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface LoginInput {
  email: string;
  senha: string;
  tenantSlug?: string;
  codigoTotp?: string;
  recaptchaToken?: string;
}

export interface LoginResult {
  usuId: number;
  tenantId?: number;
  tenantSlug?: string;
  nome: string;
  email: string;
  perfil: string;
  accessToken: string;
  refreshToken: string;
  expiracao: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  readonly user = signal<LoginResult | null>(this.loadFromStorage());

  login(input: LoginInput): Observable<LoginResult> {
    return this.http.post<LoginResult>(`${environment.apiUrl}/auth/login`, input)
      .pipe(tap(r => this.saveSession(r)));
  }

  logout(): void {
    localStorage.removeItem('agp_session');
    this.user.set(null);
  }

  isAuthenticated(): boolean {
    const u = this.user();
    if (!u) return false;
    return new Date(u.expiracao).getTime() > Date.now();
  }

  refresh(): Observable<LoginResult> {
    const u = this.user();
    return this.http.post<LoginResult>(`${environment.apiUrl}/auth/refresh`, {
      accessToken: u?.accessToken,
      refreshToken: u?.refreshToken
    }).pipe(tap(r => this.saveSession(r)));
  }

  private saveSession(r: LoginResult): void {
    localStorage.setItem('agp_session', JSON.stringify(r));
    this.user.set(r);
  }

  private loadFromStorage(): LoginResult | null {
    const raw = localStorage.getItem('agp_session');
    return raw ? JSON.parse(raw) : null;
  }
}
