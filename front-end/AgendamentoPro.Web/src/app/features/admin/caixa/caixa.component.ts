import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, MAT_DATE_LOCALE } from '@angular/material/core';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-caixa',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatDatepickerModule, MatNativeDateModule],
  providers: [{ provide: MAT_DATE_LOCALE, useValue: 'pt-BR' }],
  template: `
    <header class="topo">
      <div>
        <h1><mat-icon>point_of_sale</mat-icon> Caixa do dia</h1>
        <p>Resumo financeiro de todos os agendamentos da data.</p>
      </div>
      <div class="filtros">
        <mat-form-field appearance="outline">
          <mat-label>Data</mat-label>
          <input matInput [matDatepicker]="p" [(ngModel)]="data" (dateChange)="carregar()" />
          <mat-datepicker-toggle matIconSuffix [for]="p"></mat-datepicker-toggle>
          <mat-datepicker #p></mat-datepicker>
        </mat-form-field>
        <button mat-stroked-button (click)="imprimir()">
          <mat-icon>print</mat-icon> Imprimir
        </button>
      </div>
    </header>

    <div class="grid" *ngIf="dados() as d">
      <article class="card receita">
        <span class="label">Receita prevista</span>
        <strong>R$ {{ d.receitaPrevista | number:'1.2-2' }}</strong>
      </article>
      <article class="card receita">
        <span class="label">Receita concluída</span>
        <strong>R$ {{ d.receitaConcluida | number:'1.2-2' }}</strong>
      </article>
      <article class="card receita">
        <span class="label">Recebido (gateway)</span>
        <strong>R$ {{ d.receitaRecebida | number:'1.2-2' }}</strong>
      </article>

      <article class="card">
        <span class="label">Total agendamentos</span>
        <strong>{{ d.totalAgendamentos }}</strong>
      </article>
      <article class="card sucesso">
        <span class="label">Concluídos</span>
        <strong>{{ d.concluidos }}</strong>
      </article>
      <article class="card pendente">
        <span class="label">Pendentes</span>
        <strong>{{ d.pendentes }}</strong>
      </article>
      <article class="card alerta">
        <span class="label">Cancelados</span>
        <strong>{{ d.cancelados }}</strong>
      </article>
      <article class="card alerta">
        <span class="label">No-show</span>
        <strong>{{ d.noShow }}</strong>
      </article>
    </div>
  `,
  styles: [`
    .topo { display: flex; justify-content: space-between; gap: 1rem; flex-wrap: wrap; margin-bottom: 1rem; }
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .filtros { display: flex; gap: 0.5rem; align-items: center; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(14rem, 1fr)); gap: 1rem; }
    .card { background: #fff; padding: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); display: flex; flex-direction: column; gap: 0.5rem; border-left: 4px solid #888; }
    .card.receita { border-left-color: #2e7d32; }
    .card.sucesso { border-left-color: #43a047; }
    .card.pendente { border-left-color: #ff9800; }
    .card.alerta { border-left-color: #c62828; }
    .label { color: #888; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.04em; }
    .card strong { font-size: 1.5rem; }
    @media print {
      .filtros button, header button { display: none; }
    }
  `]
})
export class CaixaComponent implements OnInit {
  private api = inject(ApiService);
  data: Date = new Date();
  dados = signal<any>(null);

  ngOnInit() { this.carregar(); }

  carregar() {
    if (!this.data) return;
    const iso = `${this.data.getFullYear()}-${String(this.data.getMonth() + 1).padStart(2, '0')}-${String(this.data.getDate()).padStart(2, '0')}`;
    this.api.caixaDoDia(iso).subscribe(d => this.dados.set(d));
  }

  imprimir() { window.print(); }
}
