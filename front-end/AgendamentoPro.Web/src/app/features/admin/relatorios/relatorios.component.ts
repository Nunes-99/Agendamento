import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-relatorios',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatCardModule],
  template: `
    <h1>Relatórios</h1>
    <div class="filtros">
      <mat-form-field appearance="outline"><mat-label>Início</mat-label><input matInput type="date" [(ngModel)]="inicio" /></mat-form-field>
      <mat-form-field appearance="outline"><mat-label>Fim</mat-label><input matInput type="date" [(ngModel)]="fim" /></mat-form-field>
      <button mat-flat-button color="primary" (click)="carregar()">Atualizar</button>
    </div>

    <div class="grade">
      <mat-card>
        <h3>Receita por dia</h3>
        <table>
          <tr *ngFor="let r of receita()">
            <td>{{ r.data | date:'dd/MM' }}</td>
            <td>{{ r.quantidade }}x</td>
            <td><strong>R$ {{ r.receita | number:'1.2-2' }}</strong></td>
          </tr>
        </table>
      </mat-card>
      <mat-card>
        <h3>Top serviços</h3>
        <table>
          <tr *ngFor="let s of top()">
            <td>{{ s.nome }}</td>
            <td>{{ s.quantidade }}x</td>
            <td><strong>R$ {{ s.receitaTotal | number:'1.2-2' }}</strong></td>
          </tr>
        </table>
      </mat-card>
      <mat-card>
        <h3>Taxa de ocupação</h3>
        <table>
          <tr *ngFor="let o of ocupacao()">
            <td>{{ o.recursoNome }}</td>
            <td>{{ o.percentual | number:'1.0-1' }}%</td>
          </tr>
        </table>
      </mat-card>
      <mat-card>
        <h3>Cancelamentos</h3>
        <table>
          <tr *ngFor="let c of cancelamentos()">
            <td>{{ c.data | date:'dd/MM' }}</td>
            <td>{{ c.quantidade }}x</td>
            <td>{{ c.motivoMaisComum }}</td>
          </tr>
        </table>
      </mat-card>

      <mat-card class="largo">
        <h3>Top 20 clientes (LTV)</h3>
        <p class="hint" *ngIf="!ltv().length">Sem agendamentos concluídos no período.</p>
        <table>
          <tr><th>Cliente</th><th>Agendamentos</th><th>Ticket médio</th><th>Receita total</th></tr>
          <tr *ngFor="let c of ltv()">
            <td>
              <strong>{{ c.nome }}</strong>
              <div class="sub">{{ c.telefone }}</div>
            </td>
            <td>{{ c.quantidadeAgendamentos }}</td>
            <td>R$ {{ c.ticketMedio | number:'1.2-2' }}</td>
            <td><strong>R$ {{ c.receitaTotal | number:'1.2-2' }}</strong></td>
          </tr>
        </table>
      </mat-card>

      <mat-card>
        <h3>No-show por dia da semana</h3>
        <table>
          <tr *ngFor="let n of noShowDia()">
            <td>{{ n.bucket }}</td>
            <td class="barra-cel">
              <div class="barra"><div class="preencher" [style.width.%]="n.taxaPercentual"></div></div>
            </td>
            <td>{{ n.taxaPercentual | number:'1.0-1' }}% ({{ n.noShow }}/{{ n.total }})</td>
          </tr>
        </table>
      </mat-card>

      <mat-card>
        <h3>No-show por hora</h3>
        <p class="hint" *ngIf="!noShowHora().length">Sem dados no período.</p>
        <table>
          <tr *ngFor="let n of noShowHora()">
            <td>{{ n.bucket }}</td>
            <td class="barra-cel">
              <div class="barra"><div class="preencher" [style.width.%]="n.taxaPercentual"></div></div>
            </td>
            <td>{{ n.taxaPercentual | number:'1.0-1' }}% ({{ n.noShow }}/{{ n.total }})</td>
          </tr>
        </table>
      </mat-card>

      <mat-card class="largo">
        <h3>Sazonalidade (12 meses)</h3>
        <table>
          <tr *ngFor="let s of sazonalidade()">
            <td>{{ s.rotulo }}</td>
            <td>{{ s.quantidade }} atendimentos</td>
            <td class="barra-cel">
              <div class="barra"><div class="preencher" [style.width.%]="larguraSazonalidade(s.receita)"></div></div>
            </td>
            <td><strong>R$ {{ s.receita | number:'1.2-2' }}</strong></td>
          </tr>
        </table>
      </mat-card>
    </div>
  `,
  styles: [`
    h1 { margin: 0 0 1rem; }
    .filtros { display: flex; gap: 0.5rem; flex-wrap: wrap; align-items: center; margin-bottom: 1rem; }
    .grade { display: grid; gap: 1rem; }
    @media (min-width: 60rem) { .grade { grid-template-columns: 1fr 1fr; }
      .largo { grid-column: 1 / -1; } }
    table { width: 100%; }
    th { text-align: left; padding: 0.25rem 0.5rem 0.5rem 0; font-size: 0.75rem;
      color: var(--cor-texto-secundario); border-bottom: 2px solid var(--cor-borda); }
    td { padding: 0.4rem 0.5rem 0.4rem 0; border-bottom: 1px solid var(--cor-borda); font-size: 0.875rem; vertical-align: middle; }
    .sub { font-size: 0.75rem; color: var(--cor-texto-secundario); }
    .hint { color: var(--cor-texto-secundario); font-size: 0.875rem; }
    .barra-cel { width: 40%; }
    .barra { height: 0.5rem; background: var(--cor-borda); border-radius: 0.25rem; overflow: hidden; }
    .preencher { height: 100%; background: var(--cor-primaria); transition: width 0.3s; }
  `]
})
export class RelatoriosComponent implements OnInit {
  private api = inject(ApiService);
  inicio = new Date(new Date().setDate(1)).toISOString().substring(0, 10);
  fim = new Date().toISOString().substring(0, 10);
  receita = signal<any[]>([]);
  top = signal<any[]>([]);
  ocupacao = signal<any[]>([]);
  cancelamentos = signal<any[]>([]);
  ltv = signal<any[]>([]);
  noShowDia = signal<any[]>([]);
  noShowHora = signal<any[]>([]);
  sazonalidade = signal<any[]>([]);

  ngOnInit() { this.carregar(); }

  carregar() {
    this.api.relReceita(this.inicio, this.fim).subscribe(r => this.receita.set(r as any[]));
    this.api.relTopServicos(this.inicio, this.fim).subscribe(r => this.top.set(r as any[]));
    this.api.relOcupacao(this.inicio, this.fim).subscribe(r => this.ocupacao.set(r as any[]));
    this.api.relCancelamentos(this.inicio, this.fim).subscribe(r => this.cancelamentos.set(r as any[]));
    this.api.relLtv(this.inicio, this.fim, 20).subscribe(r => this.ltv.set(r as any[]));
    this.api.relNoShowDiaSemana(this.inicio, this.fim).subscribe(r => this.noShowDia.set(r as any[]));
    this.api.relNoShowHora(this.inicio, this.fim).subscribe(r => this.noShowHora.set(r as any[]));
    this.api.relSazonalidade(12).subscribe(r => this.sazonalidade.set(r as any[]));
  }

  /** Largura relativa pra barra da sazonalidade (% do mês mais alto). */
  larguraSazonalidade(receita: number): number {
    const max = Math.max(...this.sazonalidade().map(s => s.receita), 1);
    return (receita / max) * 100;
  }
}
