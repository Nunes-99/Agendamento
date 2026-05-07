import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-pacote-form',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title><mat-icon>inventory_2</mat-icon> Novo pacote pré-pago</h2>
    <mat-dialog-content>
      <mat-form-field appearance="outline" class="full">
        <mat-label>Nome do pacote</mat-label>
        <input matInput [(ngModel)]="form.nome" required maxlength="150" />
      </mat-form-field>
      <mat-form-field appearance="outline" class="full">
        <mat-label>Serviço ID</mat-label>
        <input matInput type="number" [(ngModel)]="form.servicoId" required />
      </mat-form-field>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Quantidade</mat-label>
          <input matInput type="number" min="2" [(ngModel)]="form.quantidade" required />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Preço total</mat-label>
          <input matInput type="number" min="0" step="0.01" [(ngModel)]="form.preco" required />
          <span matPrefix>R$&nbsp;</span>
        </mat-form-field>
      </div>
      <mat-form-field appearance="outline" class="full">
        <mat-label>Validade (dias após compra)</mat-label>
        <input matInput type="number" min="1" [(ngModel)]="form.validadeDias" required />
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-flat-button color="primary" [disabled]="!valido()" (click)="salvar()">
        <mat-icon>check</mat-icon> Criar
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .full { width: 100%; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; }
    mat-form-field { width: 100%; }
  `]
})
export class PacoteFormComponent {
  private api = inject(ApiService);
  private ref = inject(MatDialogRef<PacoteFormComponent>);
  form: any = { nome: '', servicoId: null, quantidade: 5, preco: 200, validadeDias: 90 };

  valido() {
    return this.form.nome && this.form.servicoId && this.form.quantidade > 1
      && this.form.preco > 0 && this.form.validadeDias > 0;
  }

  salvar() {
    this.api.criarPacote(this.form).subscribe(r => this.ref.close(r));
  }
}

@Component({
  selector: 'app-pacotes-admin',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, CurrencyPipe],
  template: `
    <header class="topo">
      <div>
        <h1><mat-icon>inventory_2</mat-icon> Pacotes pré-pagos</h1>
        <p>Cliente compra N atendimentos do mesmo serviço com desconto. Pago upfront via PIX.</p>
      </div>
      <button mat-flat-button color="primary" (click)="novo()">
        <mat-icon>add</mat-icon> Novo pacote
      </button>
    </header>

    <div class="lista" *ngIf="itens().length; else vazio">
      <article class="card" *ngFor="let p of itens()">
        <h3>{{ p.pctNome }}</h3>
        <div class="info">
          <span><mat-icon>repeat</mat-icon> {{ p.pctQuantidade }} atendimentos</span>
          <span><mat-icon>schedule</mat-icon> Vale {{ p.pctValidadeDias }} dias</span>
          <strong>{{ p.pctPreco | currency:'BRL' }}</strong>
        </div>
      </article>
    </div>
    <ng-template #vazio>
      <div class="vazio">
        <mat-icon>inventory_2</mat-icon>
        <p>Nenhum pacote cadastrado.</p>
      </div>
    </ng-template>
  `,
  styles: [`
    .topo { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem; margin-bottom: 1rem; }
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .topo p { margin: 0; color: #666; }
    .lista { display: grid; grid-template-columns: repeat(auto-fill, minmax(16rem, 1fr)); gap: 0.75rem; }
    .card { background: #fff; padding: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); display: flex; flex-direction: column; gap: 0.5rem; border-left: 4px solid #2e7d32; }
    .card h3 { margin: 0; }
    .info { display: flex; flex-direction: column; gap: 0.25rem; }
    .info span { display: flex; align-items: center; gap: 0.25rem; color: #666; font-size: 0.9rem; }
    .info mat-icon { font-size: 1rem; width: 1rem; height: 1rem; }
    .info strong { color: #2e7d32; font-size: 1.5rem; margin-top: 0.5rem; }
    .vazio { text-align: center; padding: 3rem; color: #888; }
    .vazio mat-icon { font-size: 3rem; width: 3rem; height: 3rem; }
  `]
})
export class PacotesAdminComponent implements OnInit {
  private api = inject(ApiService);
  private dialog = inject(MatDialog);
  private snack = inject(MatSnackBar);
  itens = signal<any[]>([]);

  ngOnInit() { this.carregar(); }
  carregar() { this.api.listarPacotes().subscribe(list => this.itens.set(list)); }

  novo() {
    this.dialog.open(PacoteFormComponent, { width: '32rem' }).afterClosed().subscribe(r => {
      if (r) { this.snack.open('Pacote criado', 'OK', { duration: 2000 }); this.carregar(); }
    });
  }
}
