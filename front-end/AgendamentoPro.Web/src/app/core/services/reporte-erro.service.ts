import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

/**
 * Manda para o servidor os erros que acontecem no navegador.
 *
 * Regra número um: relatar erro NUNCA pode causar erro. Tudo aqui é
 * best-effort — se o envio falhar, engole e segue, porque o usuário já está
 * com um problema e não precisa de um segundo.
 */
@Injectable({ providedIn: 'root' })
export class ReporteErroService {
  private http = inject(HttpClient);
  private router = inject(Router);

  /**
   * Últimas mensagens enviadas, para não inundar o log.
   * Um erro dentro de um *ngFor vira dezenas de eventos idênticos por segundo.
   */
  private recentes = new Map<string, number>();
  private readonly janelaMs = 30_000;

  reportar(dados: {
    mensagem: string;
    pilha?: string;
    status?: number;
    urlChamada?: string;
  }): void {
    const chave = `${dados.status ?? ''}|${dados.mensagem}`.slice(0, 200);
    const agora = Date.now();
    const visto = this.recentes.get(chave);
    if (visto && agora - visto < this.janelaMs) return;
    this.recentes.set(chave, agora);

    // Limpeza preguiçosa: sem isto o Map cresce para sempre numa aba aberta o dia todo.
    if (this.recentes.size > 50) {
      for (const [k, quando] of this.recentes)
        if (agora - quando > this.janelaMs) this.recentes.delete(k);
    }

    try {
      this.http
        .post(`${environment.apiUrl}/erros-cliente`, {
          mensagem: dados.mensagem,
          pilha: dados.pilha ?? '',
          rota: this.router.url,
          status: dados.status ?? 0,
          urlChamada: dados.urlChamada ?? '',
        })
        .subscribe({ next: () => {}, error: () => {} });
    } catch {
      /* nem o envio pode derrubar a tela */
    }
  }
}
