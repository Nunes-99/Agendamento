import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../../core/services/api.service';
import { ClienteAuthService } from '../../../core/services/cliente-auth.service';

@Component({
  selector: 'app-minha-conta',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatTabsModule,
    MatProgressSpinnerModule, CurrencyPipe, DatePipe],
  template: `
    <div class="container">
      <header class="topo">
        <div>
          <h1><mat-icon>account_circle</mat-icon> Olá, {{ nome() }}!</h1>
          <p>Acompanhe seus agendamentos, pacotes e pontos.</p>
        </div>
        <button mat-stroked-button (click)="sair()"><mat-icon>logout</mat-icon> Sair</button>
      </header>

      <mat-tab-group>
        <mat-tab label="Agendamentos">
          <ng-template matTabContent>
            <div class="lista" *ngIf="agendamentos().length; else vazioAg">
              <article class="card" *ngFor="let a of agendamentos()" [class]="'st-' + a.status">
                <header>
                  <strong>{{ a.servicoNome }}</strong>
                  <span class="badge">{{ rotuloStatus(a.status) }}</span>
                </header>
                <small>{{ a.data | date:'dd/MM/yyyy' }} · {{ horaFmt(a.horaInicio) }}–{{ horaFmt(a.horaFim) }}</small>
                <span class="valor">{{ a.valorTotal | currency:'BRL' }}</span>
                <a *ngIf="a.tokenSelfService && podeGerenciar(a)"
                   [routerLink]="['/meu-agendamento', a.tokenSelfService]"
                   mat-stroked-button class="acao">
                   <mat-icon>edit_calendar</mat-icon> Reagendar / Cancelar
                </a>
              </article>
            </div>
            <ng-template #vazioAg>
              <div class="vazio">
                <mat-icon>event_busy</mat-icon>
                <p>Você ainda não tem agendamentos.</p>
                <a mat-flat-button color="primary" [routerLink]="['/t', slug]"><mat-icon>event</mat-icon> Agendar agora</a>
              </div>
            </ng-template>
          </ng-template>
        </mat-tab>

        <mat-tab label="Pacotes">
          <ng-template matTabContent>
            <div class="lista" *ngIf="pacotes().length; else vazioPac">
              <article class="card" *ngFor="let p of pacotes()" [class.expirado]="expirado(p.expiraEm)">
                <header>
                  <strong>{{ p.pacoteNome }}</strong>
                  <span class="badge st-{{ p.status }}">{{ p.status }}</span>
                </header>
                <small>{{ p.servicoNome }}</small>
                <div class="saldo">
                  <strong>{{ p.quantidadeRestante }}</strong>
                  <span>de {{ p.quantidadeOriginal }} atendimentos</span>
                </div>
                <small>Válido até {{ p.expiraEm | date:'dd/MM/yyyy' }}</small>
              </article>
            </div>
            <ng-template #vazioPac>
              <div class="vazio">
                <mat-icon>inventory_2</mat-icon>
                <p>Nenhum pacote pré-pago.</p>
                <a mat-flat-button color="primary" [routerLink]="['/t', slug, 'pacotes']">
                  <mat-icon>shopping_cart</mat-icon> Comprar pacote
                </a>
              </div>
            </ng-template>
          </ng-template>
        </mat-tab>

        <mat-tab label="Fidelidade">
          <ng-template matTabContent>
            <div class="card centro">
              <mat-icon>loyalty</mat-icon>
              <h2>{{ saldoPontos() }} pontos</h2>
              <p>A cada agendamento concluído você ganha 10 pontos. Cada 100 pontos vale R$ 10 em desconto — fale com a recepção pra trocar.</p>
            </div>
          </ng-template>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: [`
    .container { max-width: 56rem; margin: 1rem auto; padding: 1rem; }
    .topo { display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem; margin-bottom: 1rem; }
    h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .topo p { margin: 0; color: #666; }
    .lista { display: grid; grid-template-columns: repeat(auto-fill, minmax(18rem, 1fr)); gap: 0.75rem; padding: 1rem 0; }
    .card { background: #fff; padding: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); display: flex; flex-direction: column; gap: 0.25rem; border-left: 4px solid #6366f1; }
    .card header { display: flex; justify-content: space-between; align-items: center; }
    .badge { background: #e0e7ff; color: #3730a3; padding: 0.15rem 0.5rem; border-radius: 0.4rem; font-size: 0.75rem; text-transform: capitalize; }
    .badge.st-pendente { background: #fff7ed; color: #c2410c; }
    .badge.st-ativo { background: #e8f5e9; color: #2e7d32; }
    .badge.st-cancelado { background: #ffebee; color: #c62828; }
    .saldo { display: flex; align-items: baseline; gap: 0.5rem; padding: 0.5rem 0; }
    .saldo strong { font-size: 2rem; color: #2e7d32; }
    .saldo span { color: #666; }
    .valor { font-weight: 600; color: #2e7d32; }
    .acao { margin-top: 0.5rem; }
    .vazio { text-align: center; padding: 3rem 1rem; color: #888; }
    .vazio mat-icon { font-size: 3rem; width: 3rem; height: 3rem; }
    .centro { text-align: center; padding: 2rem; }
    .centro mat-icon { font-size: 4rem; width: 4rem; height: 4rem; color: #fbbf24; }
    .centro h2 { font-size: 3rem; margin: 0.5rem 0; color: #d97706; }
    .card.expirado { opacity: 0.5; border-left-color: #aaa; }
    .card.st-3 { border-left-color: #2e7d32; }
    .card.st-4, .card.st-5 { border-left-color: #aaa; opacity: 0.7; }
  `]
})
export class MinhaContaComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(ClienteAuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  slug = '';
  nome = signal('Cliente');
  agendamentos = signal<any[]>([]);
  pacotes = signal<any[]>([]);
  saldoPontos = signal(0);

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    if (!this.auth.autenticado(this.slug)) {
      this.router.navigate(['/t', this.slug, 'entrar']);
      return;
    }
    this.nome.set(this.auth.session()!.clienteNome);
    this.api.meusAgendamentos(this.slug).subscribe(l => this.agendamentos.set(l));
    this.api.meusPacotes(this.slug).subscribe(l => this.pacotes.set(l));
    this.api.minhaFidelidade(this.slug).subscribe(r => this.saldoPontos.set(r.saldo));
  }

  rotuloStatus(s: number): string {
    return ['Aguardando', 'Confirmado', 'Em andamento', 'Concluído', 'Cancelado', 'Não compareceu'][s] || '';
  }
  podeGerenciar(a: any): boolean {
    return a.status === 0 || a.status === 1; // PendentePagamento ou Confirmado
  }
  horaFmt(h: string) { return h?.length >= 5 ? h.substring(0, 5) : h; }
  expirado(d: string) { return new Date(d).getTime() < Date.now(); }

  sair() {
    this.auth.sair();
    this.router.navigate(['/t', this.slug]);
  }
}
