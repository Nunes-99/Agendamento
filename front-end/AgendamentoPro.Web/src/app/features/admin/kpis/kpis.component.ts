import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-kpis',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <header class="topo">
      <h1><mat-icon>insights</mat-icon> KPIs do mês</h1>
      <p>Comparativo do mês atual com o mês anterior.</p>
    </header>

    <div *ngIf="carregando(); else conteudo" class="centro"><mat-spinner></mat-spinner></div>

    <ng-template #conteudo>
      <div class="grid">
        <article class="card">
          <span class="label">Receita</span>
          <strong class="valor">R$ {{ dados()?.atual?.receita | number:'1.2-2' }}</strong>
          <small>vs R$ {{ dados()?.anterior?.receita | number:'1.2-2' }}</small>
          <span class="variacao" [class.up]="dados()?.variacao?.receita > 0" [class.down]="dados()?.variacao?.receita < 0"
            *ngIf="dados()?.variacao?.receita != null">
            <mat-icon>{{ dados()?.variacao?.receita > 0 ? 'trending_up' : 'trending_down' }}</mat-icon>
            {{ dados()?.variacao?.receita }}%
          </span>
        </article>

        <article class="card">
          <span class="label">Agendamentos</span>
          <strong class="valor">{{ dados()?.atual?.agendamentos }}</strong>
          <small>vs {{ dados()?.anterior?.agendamentos }}</small>
          <span class="variacao" [class.up]="dados()?.variacao?.agendamentos > 0" [class.down]="dados()?.variacao?.agendamentos < 0"
            *ngIf="dados()?.variacao?.agendamentos != null">
            <mat-icon>{{ dados()?.variacao?.agendamentos > 0 ? 'trending_up' : 'trending_down' }}</mat-icon>
            {{ dados()?.variacao?.agendamentos }}%
          </span>
        </article>

        <article class="card">
          <span class="label">Concluídos</span>
          <strong class="valor">{{ dados()?.atual?.concluidos }}</strong>
          <small>vs {{ dados()?.anterior?.concluidos }}</small>
        </article>

        <article class="card">
          <span class="label">Ticket médio</span>
          <strong class="valor">R$ {{ dados()?.atual?.ticketMedio | number:'1.2-2' }}</strong>
          <small>vs R$ {{ dados()?.anterior?.ticketMedio | number:'1.2-2' }}</small>
        </article>

        <article class="card alerta">
          <span class="label">Taxa de cancelamento</span>
          <strong class="valor">{{ dados()?.atual?.taxaCancelamento }}%</strong>
          <small>vs {{ dados()?.anterior?.taxaCancelamento }}%</small>
        </article>

        <article class="card alerta">
          <span class="label">Taxa de no-show</span>
          <strong class="valor">{{ dados()?.atual?.taxaNoShow }}%</strong>
          <small>vs {{ dados()?.anterior?.taxaNoShow }}%</small>
        </article>
      </div>
    </ng-template>
  `,
  styles: [`
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .centro { text-align: center; padding: 4rem; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(14rem, 1fr)); gap: 1rem; margin-top: 1rem; }
    .card { background: var(--cor-fundo-card); padding: 1rem 1.25rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); display: flex; flex-direction: column; gap: 0.25rem; border-left: 4px solid #6366f1; }
    .card.alerta { border-left-color: #f57c00; }
    .label { color: #888; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.04em; }
    .valor { font-size: 1.75rem; }
    small { color: #888; }
    .variacao { display: inline-flex; align-items: center; gap: 0.15rem; padding: 0.15rem 0.5rem; border-radius: 0.4rem; font-size: 0.85rem; width: fit-content; }
    .variacao.up { background: #e8f5e9; color: #2e7d32; }
    .variacao.down { background: #ffcdd2; color: #c62828; }
    .variacao mat-icon { font-size: 1rem; width: 1rem; height: 1rem; }
  `]
})
export class KpisComponent implements OnInit {
  private api = inject(ApiService);
  dados = signal<any>(null);
  carregando = signal(true);

  ngOnInit() {
    this.api.kpisAvancados().subscribe({
      next: d => { this.dados.set(d); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }
}
