import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-lgpd',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule],
  template: `
    <header class="topo">
      <h1><mat-icon>privacy_tip</mat-icon> LGPD — Direitos do titular</h1>
      <p>Atender solicitações dos clientes: portabilidade de dados (exportar) e direito ao esquecimento (anonimizar).</p>
    </header>

    <section class="card">
      <h2>Por cliente</h2>
      <p>Informe o ID do cliente. Você consegue o ID na tela de <a routerLink="/admin/clientes">Clientes</a>.</p>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>ID do cliente</mat-label>
          <input matInput type="number" [(ngModel)]="clienteId" />
        </mat-form-field>
        <button mat-flat-button color="primary" (click)="exportar()" [disabled]="!clienteId || carregando()">
          <mat-icon>download</mat-icon> Exportar JSON
        </button>
        <button mat-stroked-button color="warn" (click)="anonimizar()" [disabled]="!clienteId || carregando()">
          <mat-icon>delete_forever</mat-icon> Anonimizar
        </button>
      </div>
    </section>

    <section class="card">
      <h2>Anonimização em massa</h2>
      <p>Anonimiza todos os clientes que não têm agendamento futuro nem agendamento ativo nos últimos N meses.
        Mantém histórico para integridade contábil mas remove identificação pessoal.</p>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Meses de inatividade</mat-label>
          <input matInput type="number" min="1" max="120" [(ngModel)]="meses" />
        </mat-form-field>
        <button mat-stroked-button color="warn" (click)="anonimizarMassa()" [disabled]="carregando()">
          <mat-icon>auto_delete</mat-icon> Executar
        </button>
      </div>
      <p *ngIf="ultimoResultado()" class="resultado">{{ ultimoResultado() }}</p>
    </section>
  `,
  styles: [`
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .topo p { color: #666; margin-top: 0.25rem; }
    .card { background: var(--cor-fundo-card); border-radius: 0.5rem; padding: 1rem; margin-top: 1rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
    .row { display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: center; }
    mat-form-field { width: 12rem; }
    .resultado { background: #e8f5e9; padding: 0.5rem 0.75rem; border-radius: 0.5rem; margin-top: 0.5rem; }
  `]
})
export class LgpdComponent {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  clienteId: number | null = null;
  meses = 24;
  carregando = signal(false);
  ultimoResultado = signal<string | null>(null);

  exportar() {
    if (!this.clienteId) return;
    this.carregando.set(true);
    this.api.exportarDadosCliente(this.clienteId).subscribe({
      next: (dados: any) => {
        this.carregando.set(false);
        const blob = new Blob([JSON.stringify(dados, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = `cliente-${this.clienteId}-dados.json`; a.click();
        URL.revokeObjectURL(url);
      },
      error: e => { this.carregando.set(false); this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 4000 }); }
    });
  }

  anonimizar() {
    if (!this.clienteId) return;
    if (!confirm(`Anonimizar cliente #${this.clienteId}? Esta ação é IRREVERSÍVEL — dados pessoais serão apagados, mas histórico permanece.`)) return;
    this.carregando.set(true);
    this.api.anonimizarCliente(this.clienteId).subscribe({
      next: () => { this.carregando.set(false); this.snack.open('Cliente anonimizado.', 'OK', { duration: 3000 }); },
      error: e => { this.carregando.set(false); this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 4000 }); }
    });
  }

  anonimizarMassa() {
    if (!confirm(`Anonimizar TODOS os clientes inativos há mais de ${this.meses} meses? IRREVERSÍVEL.`)) return;
    this.carregando.set(true);
    this.ultimoResultado.set(null);
    this.api.anonimizarInativos(this.meses).subscribe({
      next: r => {
        this.carregando.set(false);
        this.ultimoResultado.set(`${r.anonimizados} cliente(s) anonimizado(s).`);
      },
      error: e => { this.carregando.set(false); this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 4000 }); }
    });
  }
}
