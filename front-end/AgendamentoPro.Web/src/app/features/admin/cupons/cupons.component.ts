import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, MAT_DATE_LOCALE } from '@angular/material/core';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-cupom-form',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatDatepickerModule,
    MatNativeDateModule, MatDialogModule],
  providers: [{ provide: MAT_DATE_LOCALE, useValue: 'pt-BR' }],
  template: `
    <h2 mat-dialog-title><mat-icon>local_offer</mat-icon> Novo cupom</h2>
    <mat-dialog-content>
      <mat-form-field appearance="outline" class="full">
        <mat-label>Código (será usado pelo cliente)</mat-label>
        <input matInput [(ngModel)]="form.codigo" required maxlength="50" />
      </mat-form-field>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Tipo</mat-label>
          <mat-select [(ngModel)]="form.tipo">
            <mat-option [value]="1">Percentual (%)</mat-option>
            <mat-option [value]="2">Valor fixo (R$)</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Valor</mat-label>
          <input matInput type="number" [(ngModel)]="form.valor" required />
        </mat-form-field>
      </div>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Válido de</mat-label>
          <input matInput [matDatepicker]="p1" [(ngModel)]="form.validoDe" required />
          <mat-datepicker-toggle matIconSuffix [for]="p1"></mat-datepicker-toggle>
          <mat-datepicker #p1></mat-datepicker>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Válido até</mat-label>
          <input matInput [matDatepicker]="p2" [(ngModel)]="form.validoAte" required />
          <mat-datepicker-toggle matIconSuffix [for]="p2"></mat-datepicker-toggle>
          <mat-datepicker #p2></mat-datepicker>
        </mat-form-field>
      </div>
      <mat-form-field appearance="outline" class="full">
        <mat-label>Limite de usos (0 = ilimitado)</mat-label>
        <input matInput type="number" [(ngModel)]="form.usosMaximos" min="0" />
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-flat-button color="primary" [disabled]="!valido()" (click)="salvar()">
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
export class CupomFormComponent {
  private api = inject(ApiService);
  private ref = inject(MatDialogRef<CupomFormComponent>);
  form: any = {
    codigo: '',
    tipo: 1,
    valor: 10,
    validoDe: new Date(),
    validoAte: new Date(Date.now() + 30 * 86400000),
    usosMaximos: 0
  };

  valido() { return this.form.codigo && this.form.valor > 0 && this.form.validoDe && this.form.validoAte; }

  salvar() {
    this.api.criarCupom({
      codigo: this.form.codigo,
      tipo: this.form.tipo,
      valor: this.form.valor,
      validoDe: (this.form.validoDe as Date).toISOString(),
      validoAte: (this.form.validoAte as Date).toISOString(),
      usosMaximos: this.form.usosMaximos
    }).subscribe(c => this.ref.close(c));
  }
}

@Component({
  selector: 'app-cupons',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatSlideToggleModule],
  template: `
    <header class="topo">
      <div>
        <h1><mat-icon>local_offer</mat-icon> Cupons de desconto</h1>
        <p>Crie códigos promocionais que clientes aplicam no checkout.</p>
      </div>
      <button mat-flat-button color="primary" (click)="novo()">
        <mat-icon>add</mat-icon> Novo cupom
      </button>
    </header>

    <div class="lista" *ngIf="cupons().length; else vazio">
      <article class="card" *ngFor="let c of cupons()" [class.inativo]="!c.cupAtivo">
        <header class="card-head">
          <code class="codigo">{{ c.cupCodigo }}</code>
          <mat-slide-toggle [checked]="c.cupAtivo" (change)="alternar(c, $event.checked)">
            {{ c.cupAtivo ? 'Ativo' : 'Inativo' }}
          </mat-slide-toggle>
        </header>
        <div class="info">
          <strong *ngIf="c.cupTipo === 1">{{ c.cupValor }}% off</strong>
          <strong *ngIf="c.cupTipo === 2">R$ {{ c.cupValor | number:'1.2-2' }} off</strong>
          <small>{{ c.cupValidoDe | date:'dd/MM' }} → {{ c.cupValidoAte | date:'dd/MM/yyyy' }}</small>
        </div>
        <div class="usos">
          <mat-icon>confirmation_number</mat-icon>
          {{ c.cupUsosFeitos }}/{{ c.cupUsosMaximos === 2147483647 ? '∞' : c.cupUsosMaximos }}
        </div>
      </article>
    </div>

    <ng-template #vazio>
      <div class="vazio">
        <mat-icon>local_offer</mat-icon>
        <p>Nenhum cupom criado ainda.</p>
      </div>
    </ng-template>
  `,
  styles: [`
    .topo { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem; margin-bottom: 1rem; }
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .topo p { margin: 0; color: #666; }
    .lista { display: grid; grid-template-columns: repeat(auto-fill, minmax(20rem, 1fr)); gap: 0.75rem; }
    .card { background: #fff; padding: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); display: flex; flex-direction: column; gap: 0.5rem; border-left: 4px solid #6366f1; }
    .card.inativo { opacity: 0.6; border-left-color: #aaa; }
    .card-head { display: flex; justify-content: space-between; align-items: center; }
    .codigo { background: #ede9fe; color: #5b21b6; padding: 0.25rem 0.6rem; border-radius: 0.4rem; font-family: monospace; font-weight: bold; }
    .info { display: flex; gap: 0.5rem; align-items: baseline; }
    .info small { color: #888; }
    .usos { display: flex; align-items: center; gap: 0.25rem; color: #666; font-size: 0.9rem; }
    .usos mat-icon { font-size: 1rem; width: 1rem; height: 1rem; }
    .vazio { text-align: center; padding: 3rem; color: #888; }
    .vazio mat-icon { font-size: 3rem; width: 3rem; height: 3rem; }
  `]
})
export class CuponsComponent implements OnInit {
  private api = inject(ApiService);
  private dialog = inject(MatDialog);
  private snack = inject(MatSnackBar);

  cupons = signal<any[]>([]);

  ngOnInit() { this.carregar(); }

  carregar() { this.api.listarCupons().subscribe(list => this.cupons.set(list)); }

  novo() {
    this.dialog.open(CupomFormComponent, { width: '32rem' }).afterClosed().subscribe(r => {
      if (r) { this.snack.open('Cupom criado', 'OK', { duration: 2000 }); this.carregar(); }
    });
  }

  alternar(c: any, ativo: boolean) {
    this.api.alternarCupomAtivo(c.cupId, ativo).subscribe({
      next: () => { c.cupAtivo = ativo; },
      error: () => this.snack.open('Falha', 'OK', { duration: 3000 })
    });
  }
}
