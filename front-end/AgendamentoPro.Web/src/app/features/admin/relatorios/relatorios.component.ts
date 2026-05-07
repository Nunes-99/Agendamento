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
    </div>
  `,
  styles: [`
    h1 { margin: 0 0 1rem; }
    .filtros { display: flex; gap: 0.5rem; flex-wrap: wrap; align-items: center; margin-bottom: 1rem; }
    .grade { display: grid; gap: 1rem; }
    @media (min-width: 60rem) { .grade { grid-template-columns: 1fr 1fr; } }
    table { width: 100%; }
    td { padding: 0.25rem 0; border-bottom: 1px solid var(--cor-borda); font-size: 0.875rem; }
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

  ngOnInit() { this.carregar(); }

  carregar() {
    this.api.relReceita(this.inicio, this.fim).subscribe(r => this.receita.set(r as any[]));
    this.api.relTopServicos(this.inicio, this.fim).subscribe(r => this.top.set(r as any[]));
    this.api.relOcupacao(this.inicio, this.fim).subscribe(r => this.ocupacao.set(r as any[]));
    this.api.relCancelamentos(this.inicio, this.fim).subscribe(r => this.cancelamentos.set(r as any[]));
  }
}
