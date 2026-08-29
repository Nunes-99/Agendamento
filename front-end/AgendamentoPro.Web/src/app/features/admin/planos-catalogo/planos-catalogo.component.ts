import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { Plano } from '../../../core/models/assinatura.model';

interface PlanoForm {
  id?: number;
  nome: string;
  descricao: string;
  preco: number;
  limiteUnidades: number;
  limiteProfissionais: number;
  limiteAgendamentosMes: number;
  publico: boolean;
  ordem: number;
  ativo?: boolean;
}

@Component({
  selector: 'app-planos-catalogo',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatButtonModule, MatIconModule,
    MatTableModule, MatFormFieldModule, MatInputModule, MatSlideToggleModule],
  template: `
    <section class="shell">
      <header>
        <h1>Catálogo de planos</h1>
        <button mat-flat-button color="primary" (click)="iniciarCriacao()" *ngIf="!editando()">
          <mat-icon>add</mat-icon> Novo plano
        </button>
      </header>

      <mat-card *ngIf="editando() as f" class="form">
        <h2>{{ f.id ? 'Editar plano #' + f.id : 'Novo plano' }}</h2>

        <div class="row">
          <mat-form-field appearance="outline">
            <mat-label>Nome</mat-label>
            <input matInput [(ngModel)]="f.nome">
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Preço (R$/mês)</mat-label>
            <input matInput type="number" step="0.01" min="0.01" [(ngModel)]="f.preco">
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Ordem</mat-label>
            <input matInput type="number" [(ngModel)]="f.ordem">
          </mat-form-field>
        </div>

        <mat-form-field appearance="outline" class="full">
          <mat-label>Descrição</mat-label>
          <textarea matInput rows="2" [(ngModel)]="f.descricao"></textarea>
        </mat-form-field>

        <div class="row">
          <mat-form-field appearance="outline">
            <mat-label>Limite de unidades (-1 = ilimitado)</mat-label>
            <input matInput type="number" [(ngModel)]="f.limiteUnidades">
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Limite de profissionais (-1 = ilimitado)</mat-label>
            <input matInput type="number" [(ngModel)]="f.limiteProfissionais">
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Limite agendamentos/mês (-1 = ilimitado)</mat-label>
            <input matInput type="number" [(ngModel)]="f.limiteAgendamentosMes">
          </mat-form-field>
        </div>

        <mat-slide-toggle [(ngModel)]="f.publico">Visível no catálogo público</mat-slide-toggle>

        <div class="acoes">
          <button mat-stroked-button (click)="cancelar()">Cancelar</button>
          <button mat-flat-button color="primary" (click)="salvar()" [disabled]="salvando() || !f.nome || f.preco <= 0">
            <mat-icon>save</mat-icon> Salvar
          </button>
        </div>
      </mat-card>

      <table mat-table [dataSource]="planos()" class="tabela" *ngIf="!editando()">
        <ng-container matColumnDef="id"><th mat-header-cell *matHeaderCellDef>#</th>
          <td mat-cell *matCellDef="let p">{{ p.id }}</td></ng-container>
        <ng-container matColumnDef="nome"><th mat-header-cell *matHeaderCellDef>Nome</th>
          <td mat-cell *matCellDef="let p">{{ p.nome }}</td></ng-container>
        <ng-container matColumnDef="preco"><th mat-header-cell *matHeaderCellDef>Preço</th>
          <td mat-cell *matCellDef="let p">R$ {{ p.preco | number:'1.2-2' }}</td></ng-container>
        <ng-container matColumnDef="limites"><th mat-header-cell *matHeaderCellDef>Limites</th>
          <td mat-cell *matCellDef="let p">{{ formatarLimites(p) }}</td></ng-container>
        <ng-container matColumnDef="acoes"><th mat-header-cell *matHeaderCellDef>Ações</th>
          <td mat-cell *matCellDef="let p" class="acoes-cell">
            <button mat-icon-button (click)="iniciarEdicao(p)" title="Editar">
              <mat-icon>edit</mat-icon>
            </button>
          </td></ng-container>
        <tr mat-header-row *matHeaderRowDef="['id','nome','preco','limites','acoes']"></tr>
        <tr mat-row *matRowDef="let row; columns: ['id','nome','preco','limites','acoes']"></tr>
      </table>

      <p *ngIf="carregando()" class="vazio">Carregando…</p>
    </section>
  `,
  styles: [`
    .shell { max-width: 64rem; }
    header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    h1 { margin: 0; }
    .form { padding: 1.5rem; display: flex; flex-direction: column; gap: 1rem; margin-bottom: 1rem; }
    .form h2 { margin: 0; }
    .row { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; }
    .full { width: 100%; }
    .acoes { display: flex; gap: 0.5rem; justify-content: flex-end; }
    .tabela { width: 100%; background: var(--cor-fundo-card); border-radius: 0.5rem; }
    .acoes-cell { text-align: right; }
    .vazio { text-align: center; color: #888; padding: 2rem; }
    @media (max-width: 48rem) { .row { grid-template-columns: 1fr; } }
  `]
})
export class PlanosCatalogoComponent implements OnInit {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  planos = signal<Plano[]>([]);
  editando = signal<PlanoForm | null>(null);
  carregando = signal(true);
  salvando = signal(false);

  ngOnInit() { this.carregar(); }

  private carregar() {
    this.carregando.set(true);
    this.api.listarTodosPlanos().subscribe({
      next: l => { this.planos.set(l); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }

  iniciarCriacao() {
    this.editando.set({
      nome: '', descricao: '', preco: 29.90,
      limiteUnidades: 1, limiteProfissionais: 10, limiteAgendamentosMes: -1,
      publico: true, ordem: 0
    });
  }

  iniciarEdicao(p: Plano) {
    this.editando.set({
      id: p.id, nome: p.nome, descricao: p.descricao, preco: p.preco,
      limiteUnidades: p.limiteUnidades, limiteProfissionais: p.limiteProfissionais,
      limiteAgendamentosMes: p.limiteAgendamentosMes, publico: true, ordem: 0
    });
  }

  cancelar() { this.editando.set(null); }

  salvar() {
    const f = this.editando(); if (!f) return;
    this.salvando.set(true);
    const obs = f.id
      ? this.api.atualizarPlano(f.id, f)
      : this.api.criarPlano(f);
    obs.subscribe({
      next: () => { this.salvando.set(false); this.editando.set(null); this.carregar(); this.snack.open('Plano salvo.', 'OK', { duration: 3000 }); },
      error: e => { this.salvando.set(false); this.snack.open(e?.error?.detail || 'Falha ao salvar.', 'OK', { duration: 5000 }); }
    });
  }

  formatarLimites(p: Plano) {
    const u = p.limiteUnidades < 0 ? '∞' : p.limiteUnidades;
    const pr = p.limiteProfissionais < 0 ? '∞' : p.limiteProfissionais;
    const a = p.limiteAgendamentosMes < 0 ? '∞' : p.limiteAgendamentosMes;
    return `${u}u / ${pr}p / ${a}ag`;
  }
}
