import { Component, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-fidelidade',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, CurrencyPipe],
  template: `
    <header class="topo">
      <h1><mat-icon>loyalty</mat-icon> Programa de fidelidade</h1>
      <p>Cada agendamento concluído credita 10 pontos. 100 pontos = R$ 10 de cupom (válido 60 dias, uso único).</p>
    </header>

    <section class="card">
      <h2>Consultar saldo do cliente</h2>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Cliente ID</mat-label>
          <input matInput type="number" [(ngModel)]="clienteId" />
        </mat-form-field>
        <button mat-stroked-button (click)="consultar()" [disabled]="!clienteId">
          <mat-icon>search</mat-icon> Consultar
        </button>
      </div>
      <p *ngIf="saldo() != null" class="saldo">
        Saldo: <strong>{{ saldo() }} pontos</strong>
      </p>
    </section>

    <section class="card" *ngIf="saldo() && saldo()! >= 100">
      <h2>Trocar pontos por cupom</h2>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Pontos a trocar</mat-label>
          <input matInput type="number" min="100" [step]="100" [(ngModel)]="pontosTrocar" />
          <mat-hint>Mínimo 100 pts. Cada 10 pts = R$ 1.</mat-hint>
        </mat-form-field>
        <button mat-flat-button color="primary" (click)="trocar()"
          [disabled]="!pontosTrocar || pontosTrocar < 100 || pontosTrocar > saldo()!">
          <mat-icon>card_giftcard</mat-icon> Gerar cupom
        </button>
      </div>
      <div *ngIf="ultimoCupom()" class="cupom">
        <strong>Cupom gerado:</strong>
        <code>{{ ultimoCupom()!.codigo }}</code>
        <span>{{ ultimoCupom()!.valor | currency:'BRL' }} de desconto · válido até {{ ultimoCupom()!.validoAte | date:'dd/MM/yyyy' }}</span>
      </div>
    </section>
  `,
  styles: [`
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .card { background: #fff; padding: 1rem 1.25rem; margin-top: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
    .row { display: flex; gap: 0.5rem; flex-wrap: wrap; align-items: center; }
    mat-form-field { width: 14rem; }
    .saldo { background: #e8f5e9; padding: 0.5rem 0.75rem; border-radius: 0.5rem; }
    .saldo strong { font-size: 1.5rem; color: #2e7d32; }
    .cupom { background: #ede9fe; padding: 0.75rem; border-radius: 0.5rem; display: flex; flex-direction: column; gap: 0.25rem; margin-top: 0.5rem; }
    .cupom code { background: #fff; padding: 0.25rem 0.5rem; border-radius: 0.25rem; font-family: monospace; width: fit-content; }
  `]
})
export class FidelidadeComponent {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  clienteId: number | null = null;
  saldo = signal<number | null>(null);
  pontosTrocar = 100;
  ultimoCupom = signal<{ codigo: string; valor: number; validoAte: string } | null>(null);

  consultar() {
    if (!this.clienteId) return;
    this.api.saldoPontos(this.clienteId).subscribe({
      next: r => this.saldo.set(r.saldo),
      error: e => this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 3000 })
    });
  }

  trocar() {
    if (!this.clienteId || !this.pontosTrocar) return;
    this.api.trocarPontosPorCupom(this.clienteId, this.pontosTrocar).subscribe({
      next: r => {
        this.ultimoCupom.set(r);
        this.snack.open(`Cupom ${r.codigo} gerado`, 'OK', { duration: 3000 });
        this.consultar(); // atualiza saldo
      },
      error: e => this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 3000 })
    });
  }
}
