import { Injectable, signal } from '@angular/core';

export interface ClienteSession {
  slug: string;
  clienteId: number;
  clienteNome: string;
  token: string;
  expiracao: string;
}

const STORAGE_KEY = 'agp_cli_session';

@Injectable({ providedIn: 'root' })
export class ClienteAuthService {
  readonly session = signal<ClienteSession | null>(this.load());

  salvar(s: ClienteSession): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(s));
    this.session.set(s);
  }

  sair(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.session.set(null);
  }

  autenticado(slug: string): boolean {
    const s = this.session();
    if (!s || s.slug !== slug) return false;
    return new Date(s.expiracao).getTime() > Date.now();
  }

  token(slug: string): string | null {
    return this.autenticado(slug) ? this.session()!.token : null;
  }

  private load(): ClienteSession | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : null;
  }
}
