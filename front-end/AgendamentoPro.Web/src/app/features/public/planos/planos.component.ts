import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { Plano } from '../../../core/models/assinatura.model';

@Component({
  selector: 'app-planos',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule],
  template: `
    <section class="planos-shell">
      <header>
        <h1>Escolha seu plano</h1>
        <p>Primeiro mês grátis. Cobrança mensal recorrente via Mercado Pago a partir do segundo mês. Cancele quando quiser.</p>
      </header>

      <div class="grid">
        <mat-card *ngFor="let p of planos()" class="plano">
          <h2>{{ p.nome }}</h2>
          <p class="desc">{{ p.descricao }}</p>
          <div class="preco">
            <strong>R$ {{ p.preco | number:'1.2-2' }}</strong>
            <small>/mês</small>
          </div>
          <ul class="limites">
            <li><mat-icon>store</mat-icon> {{ rotuloUnidades(p) }}</li>
            <li><mat-icon>group</mat-icon> {{ rotuloProfissionais(p) }}</li>
            <li><mat-icon>event</mat-icon> {{ rotuloAgendamentos(p) }}</li>
          </ul>
          <button mat-flat-button color="primary" (click)="escolher(p)">
            {{ logado() ? 'Assinar este plano' : 'Entrar para assinar' }}
          </button>
        </mat-card>
      </div>

      <p *ngIf="!planos().length && !carregando()" class="vazio">Nenhum plano disponível no momento.</p>
      <p *ngIf="carregando()" class="vazio">Carregando…</p>
    </section>
  `,
  styles: [`
    .planos-shell { max-width: 64rem; margin: 2rem auto; padding: 0 1rem; }
    header { text-align: center; margin-bottom: 2rem; }
    header h1 { font-size: 2rem; margin: 0 0 0.5rem; }
    header p { color: #666; margin: 0; }
    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(18rem, 1fr)); gap: 1.5rem; }
    .plano { padding: 1.5rem; display: flex; flex-direction: column; gap: 1rem; }
    .plano h2 { margin: 0; font-size: 1.5rem; }
    .desc { color: #666; margin: 0; min-height: 2.5rem; }
    .preco strong { font-size: 2rem; }
    .preco small { color: #888; margin-left: 0.25rem; }
    .limites { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.5rem; }
    .limites li { display: flex; align-items: center; gap: 0.5rem; }
    .limites mat-icon { color: #4f46e5; font-size: 1.25rem; width: 1.25rem; height: 1.25rem; }
    .vazio { text-align: center; color: #888; padding: 2rem; }
  `]
})
export class PlanosComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);

  planos = signal<Plano[]>([]);
  carregando = signal(true);
  logado = signal(false);

  ngOnInit() {
    this.logado.set(!!this.auth.user());
    this.api.listarPlanos().subscribe({
      next: lista => { this.planos.set(lista); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }

  rotuloUnidades(p: Plano) { return p.limiteUnidades < 0 ? 'Unidades ilimitadas' : `${p.limiteUnidades} unidade${p.limiteUnidades > 1 ? 's' : ''}`; }
  rotuloProfissionais(p: Plano) { return p.limiteProfissionais < 0 ? 'Profissionais ilimitados' : `${p.limiteProfissionais} profissionais`; }
  rotuloAgendamentos(p: Plano) { return p.limiteAgendamentosMes < 0 ? 'Agendamentos ilimitados' : `${p.limiteAgendamentosMes} agendamentos/mês`; }

  escolher(p: Plano) {
    if (!this.auth.user()) {
      this.router.navigate(['/admin/login'], { queryParams: { redirect: '/admin/minha-assinatura', plano: p.id } });
      return;
    }
    this.router.navigate(['/admin/minha-assinatura'], { queryParams: { plano: p.id } });
  }
}
