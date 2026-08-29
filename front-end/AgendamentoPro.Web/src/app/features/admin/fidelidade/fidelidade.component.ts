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

      <mat-form-field appearance="outline" class="busca">
        <mat-label>Buscar por nome, telefone ou e-mail</mat-label>
        <input matInput [(ngModel)]="busca" (ngModelChange)="buscarClientes()" maxlength="100"
          placeholder="Ex: Maria ou 11976543210" />
        <mat-icon matSuffix>search</mat-icon>
      </mat-form-field>

      <div class="lista-clientes" *ngIf="resultados().length || busca.trim().length >= 2">
        <button class="item-cliente" type="button" *ngFor="let c of resultados()"
          [class.ativo]="cliente()?.id === c.id" (click)="selecionar(c)">
          <div>
            <strong>{{ c.nome }}</strong>
            <small>{{ c.telefone }}<span *ngIf="c.email"> • {{ c.email }}</span></small>
          </div>
          <mat-icon *ngIf="cliente()?.id === c.id">check_circle</mat-icon>
        </button>
        <p class="vazio" *ngIf="!resultados().length && busca.trim().length >= 2">
          Nenhum cliente encontrado.
        </p>
      </div>

      <p *ngIf="cliente() && saldo() != null" class="saldo">
        <strong>{{ cliente()!.nome }}</strong> tem <strong>{{ saldo() }} pontos</strong>
        <span class="equivale" *ngIf="saldo()! >= 100">
          — dá para trocar por até {{ (saldo()! - saldo()! % 100) / 10 | currency:'BRL' }} em cupom
        </span>
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
    .card { background: var(--cor-fundo-card); padding: 1rem 1.25rem; margin-top: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
    .row { display: flex; gap: 0.5rem; flex-wrap: wrap; align-items: center; }
    mat-form-field { width: 14rem; }
    .saldo { background: #e8f5e9; padding: 0.75rem; border-radius: 0.5rem; color: #1b5e20; }
    .saldo strong { font-size: 1.125rem; color: #2e7d32; }
    .saldo .equivale { color: #33691e; font-size: 0.875rem; }
    .busca { width: 100%; max-width: 30rem; }
    .lista-clientes {
      display: flex; flex-direction: column; gap: 0.25rem;
      max-height: 15rem; overflow-y: auto; margin-bottom: 0.75rem;
      max-width: 30rem;
    }
    .item-cliente {
      display: flex; align-items: center; justify-content: space-between; gap: 0.5rem;
      text-align: left; cursor: pointer; padding: 0.5rem 0.75rem; border-radius: 0.375rem;
      border: 1px solid var(--cor-borda); background: var(--cor-fundo-card);
    }
    .item-cliente:hover { border-color: var(--cor-primaria); }
    .item-cliente.ativo { border-color: var(--cor-primaria); background: var(--cor-primaria-soft); }
    .item-cliente strong { display: block; }
    .item-cliente small { color: var(--cor-texto-suave); }
    .item-cliente mat-icon { color: var(--cor-primaria); }
    .vazio { color: var(--cor-texto-suave); margin: 0.5rem 0; }
    .cupom { background: #ede9fe; padding: 0.75rem; border-radius: 0.5rem; display: flex; flex-direction: column; gap: 0.25rem; margin-top: 0.5rem; }
    .cupom code { background: var(--cor-fundo-card); padding: 0.25rem 0.5rem; border-radius: 0.25rem; font-family: monospace; width: fit-content; }
  `]
})
export class FidelidadeComponent {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  // Antes esta tela pedia o "Cliente ID" numérico — dado que ninguém na
  // recepção sabe de cor. Agora busca por nome/telefone/e-mail, como no
  // diálogo de novo agendamento.
  busca = '';
  resultados = signal<any[]>([]);
  cliente = signal<any | null>(null);
  private buscaTimer: any = null;

  saldo = signal<number | null>(null);
  pontosTrocar = 100;
  ultimoCupom = signal<{ codigo: string; valor: number; validoAte: string } | null>(null);

  buscarClientes() {
    if (this.buscaTimer) clearTimeout(this.buscaTimer);
    this.buscaTimer = setTimeout(() => {
      const termo = this.busca.trim();
      if (termo.length < 2) { this.resultados.set([]); return; }
      this.api.clientesAdmin(1, 10, termo).subscribe({
        next: (r: any) => this.resultados.set(r.items || []),
        error: () => this.resultados.set([])
      });
    }, 300);
  }

  selecionar(c: any) {
    this.cliente.set(c);
    this.ultimoCupom.set(null);
    this.consultar();
  }

  consultar() {
    const c = this.cliente();
    if (!c) return;
    this.api.saldoPontos(c.id).subscribe({
      next: r => this.saldo.set(r.saldo),
      error: e => this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 3000 })
    });
  }

  trocar() {
    const c = this.cliente();
    if (!c || !this.pontosTrocar) return;
    this.api.trocarPontosPorCupom(c.id, this.pontosTrocar).subscribe({
      next: r => {
        this.ultimoCupom.set(r);
        this.snack.open(`Cupom ${r.codigo} gerado`, 'OK', { duration: 3000 });
        this.consultar(); // atualiza saldo
      },
      error: e => this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 3000 })
    });
  }
}
