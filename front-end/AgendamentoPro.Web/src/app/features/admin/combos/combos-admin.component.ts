import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../../core/services/api.service';
import { Combo, ComboInput } from '../../../core/models/combo.model';
import { Servico } from '../../../core/models/servico.model';

@Component({
  selector: 'app-combo-form',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatDialogModule, MatCheckboxModule, MatIconModule, MatSelectModule],
  template: `
    <h2 mat-dialog-title>
      <mat-icon>{{ data?.combo ? 'edit' : 'add_circle' }}</mat-icon>
      {{ data?.combo ? 'Editar combo' : 'Novo combo' }}
    </h2>
    <mat-dialog-content>
      <mat-form-field appearance="outline" class="full">
        <mat-label>Nome do combo</mat-label>
        <input matInput [(ngModel)]="form.nome" required />
      </mat-form-field>
      <mat-form-field appearance="outline" class="full">
        <mat-label>Descrição</mat-label>
        <textarea matInput rows="2" [(ngModel)]="form.descricao"></textarea>
      </mat-form-field>
      <mat-form-field appearance="outline" class="full">
        <mat-label>URL da imagem</mat-label>
        <input matInput [(ngModel)]="form.imagemUrl" placeholder="https://..." />
      </mat-form-field>
      <mat-form-field appearance="outline" class="full">
        <mat-label>Serviços incluídos</mat-label>
        <mat-select [(ngModel)]="form.servicoIds" multiple required>
          <mat-option *ngFor="let s of data?.servicos" [value]="s.id">
            {{ s.nome }} — R$ {{ s.preco | number:'1.2-2' }}
          </mat-option>
        </mat-select>
      </mat-form-field>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Preço promocional</mat-label>
          <input matInput type="number" min="0" step="0.01" [(ngModel)]="form.precoPromocional" required />
          <span matPrefix>R$&nbsp;</span>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Ordem</mat-label>
          <input matInput type="number" min="0" [(ngModel)]="form.ordem" />
        </mat-form-field>
      </div>
      <mat-checkbox [(ngModel)]="form.ativo">Combo ativo</mat-checkbox>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-flat-button color="primary"
        [disabled]="!form.nome || !form.precoPromocional || !form.servicoIds.length"
        (click)="salvar()">
        <mat-icon>check</mat-icon> Salvar
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .full { width: 100%; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; }
    @media (max-width: 30rem) { .row { grid-template-columns: 1fr; } }
  `]
})
export class ComboFormComponent {
  data = inject<{ combo?: Combo; servicos: Servico[] }>(MAT_DIALOG_DATA);
  private ref = inject(MatDialogRef<ComboFormComponent>);

  form: ComboInput = this.data?.combo
    ? {
        nome: this.data.combo.nome,
        descricao: this.data.combo.descricao,
        imagemUrl: this.data.combo.imagemUrl,
        precoPromocional: this.data.combo.precoPromocional,
        ordem: this.data.combo.ordem,
        ativo: this.data.combo.ativo,
        servicoIds: this.data.combo.servicos.map(s => s.servicoId)
      }
    : { nome: '', precoPromocional: 0, ordem: 0, ativo: true, servicoIds: [] };

  salvar() { this.ref.close(this.form); }
}

@Component({
  selector: 'app-combos-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatTooltipModule, CurrencyPipe],
  template: `
    <header class="topo">
      <div>
        <h1><mat-icon>local_offer</mat-icon> Combos</h1>
        <p>Pacotes promocionais agrupando serviços.</p>
      </div>
      <button mat-flat-button color="primary" (click)="novo()">
        <mat-icon>add</mat-icon> Novo combo
      </button>
    </header>

    <div class="lista" *ngIf="combos().length; else vazio">
      <article class="card" *ngFor="let c of combos()" [class.inativo]="!c.ativo">
        <header>
          <strong>{{ c.nome }}</strong>
          <span class="badge" *ngIf="!c.ativo">Inativo</span>
        </header>
        <p *ngIf="c.descricao" class="descricao">{{ c.descricao }}</p>
        <ul>
          <li *ngFor="let s of c.servicos">{{ s.nome }}</li>
        </ul>
        <div class="precos">
          <span class="orig">{{ c.precoOriginal | currency:'BRL' }}</span>
          <strong>{{ c.precoPromocional | currency:'BRL' }}</strong>
          <span class="econ" *ngIf="c.economia > 0">−{{ c.economia | currency:'BRL' }}</span>
        </div>
        <div class="acoes">
          <button mat-icon-button (click)="editar(c)" matTooltip="Editar"><mat-icon>edit</mat-icon></button>
          <button mat-icon-button color="warn" (click)="excluir(c)" matTooltip="Excluir"><mat-icon>delete</mat-icon></button>
        </div>
      </article>
    </div>
    <ng-template #vazio>
      <div class="vazio">
        <mat-icon>inventory_2</mat-icon>
        <p>Nenhum combo cadastrado. Clique em <strong>Novo combo</strong> para começar.</p>
      </div>
    </ng-template>
  `,
  styles: [`
    .topo { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem; margin-bottom: 1rem; }
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .topo p { margin: 0; color: #666; }
    .lista { display: grid; grid-template-columns: repeat(auto-fill, minmax(18rem, 1fr)); gap: 1rem; }
    .card { background: #fff; border-radius: 0.5rem; padding: 1rem; box-shadow: 0 1px 3px rgba(0,0,0,0.08); display: flex; flex-direction: column; gap: 0.5rem; }
    .card.inativo { opacity: 0.6; }
    .card header { display: flex; justify-content: space-between; align-items: center; }
    .badge { background: #ffcdd2; color: #c62828; padding: 0.1rem 0.5rem; border-radius: 0.5rem; font-size: 0.75rem; }
    .descricao { color: #555; margin: 0; font-size: 0.9rem; }
    ul { margin: 0; padding-left: 1.2rem; }
    .precos { display: flex; gap: 0.5rem; align-items: baseline; }
    .precos .orig { text-decoration: line-through; color: #999; }
    .precos strong { color: #2e7d32; font-size: 1.1rem; }
    .precos .econ { background: #e8f5e9; color: #2e7d32; padding: 0.1rem 0.4rem; border-radius: 0.4rem; font-size: 0.8rem; }
    .acoes { display: flex; justify-content: flex-end; gap: 0.25rem; margin-top: 0.25rem; }
    .vazio { text-align: center; padding: 3rem; color: #888; }
    .vazio mat-icon { font-size: 3rem; width: 3rem; height: 3rem; }
  `]
})
export class CombosAdminComponent implements OnInit {
  private api = inject(ApiService);
  private dialog = inject(MatDialog);
  private snack = inject(MatSnackBar);

  combos = signal<Combo[]>([]);
  servicos = signal<Servico[]>([]);

  ngOnInit() {
    this.carregar();
    this.api.servicosAdmin().subscribe(list => this.servicos.set(list));
  }

  carregar() {
    this.api.combosAdmin().subscribe(list => this.combos.set(list));
  }

  novo() {
    const ref = this.dialog.open(ComboFormComponent, {
      width: '36rem',
      data: { servicos: this.servicos() }
    });
    ref.afterClosed().subscribe(r => {
      if (!r) return;
      this.api.cadastrarCombo(r).subscribe({
        next: () => { this.snack.open('Combo criado', 'OK', { duration: 2000 }); this.carregar(); },
        error: e => this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 4000, panelClass: 'snack-erro' })
      });
    });
  }

  editar(c: Combo) {
    const ref = this.dialog.open(ComboFormComponent, {
      width: '36rem',
      data: { combo: c, servicos: this.servicos() }
    });
    ref.afterClosed().subscribe(r => {
      if (!r) return;
      this.api.atualizarCombo(c.id, r).subscribe({
        next: () => { this.snack.open('Combo atualizado', 'OK', { duration: 2000 }); this.carregar(); },
        error: e => this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 4000, panelClass: 'snack-erro' })
      });
    });
  }

  excluir(c: Combo) {
    if (!confirm(`Excluir o combo "${c.nome}"?`)) return;
    this.api.excluirCombo(c.id).subscribe({
      next: () => { this.snack.open('Combo excluído', 'OK', { duration: 2000 }); this.carregar(); },
      error: e => this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 3000, panelClass: 'snack-erro' })
    });
  }
}
