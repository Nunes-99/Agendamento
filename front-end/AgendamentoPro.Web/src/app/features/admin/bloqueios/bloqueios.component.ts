import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, MAT_DATE_LOCALE } from '@angular/material/core';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-bloqueio-form',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatDatepickerModule, MatNativeDateModule, MatDialogModule, MatSelectModule],
  providers: [{ provide: MAT_DATE_LOCALE, useValue: 'pt-BR' }],
  template: `
    <h2 mat-dialog-title><mat-icon>block</mat-icon> Novo bloqueio</h2>
    <mat-dialog-content>
      <mat-form-field appearance="outline" class="full">
        <mat-label>Motivo</mat-label>
        <input matInput [(ngModel)]="form.motivo" required maxlength="500" />
      </mat-form-field>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Início</mat-label>
          <input matInput [matDatepicker]="p1" [(ngModel)]="form.dataInicio" required />
          <mat-datepicker-toggle matIconSuffix [for]="p1"></mat-datepicker-toggle>
          <mat-datepicker #p1></mat-datepicker>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Fim</mat-label>
          <input matInput [matDatepicker]="p2" [(ngModel)]="form.dataFim" required />
          <mat-datepicker-toggle matIconSuffix [for]="p2"></mat-datepicker-toggle>
          <mat-datepicker #p2></mat-datepicker>
        </mat-form-field>
      </div>
      <mat-form-field appearance="outline" class="full">
        <mat-label>Recurso (opcional)</mat-label>
        <mat-select [(ngModel)]="form.recursoId">
          <mat-option [value]="null">Todos os recursos</mat-option>
          <mat-option *ngFor="let r of recursos" [value]="r.id">{{ r.nome }}</mat-option>
        </mat-select>
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
export class BloqueioFormComponent {
  private api = inject(ApiService);
  private ref = inject(MatDialogRef<BloqueioFormComponent>);
  recursos: any[] = [];

  form: any = {
    motivo: '',
    dataInicio: new Date(),
    dataFim: new Date(),
    recursoId: null
  };

  constructor() {
    this.api.recursosAdmin().subscribe(list => this.recursos = list);
  }

  valido() { return this.form.motivo && this.form.dataInicio && this.form.dataFim; }

  salvar() {
    const di = this.form.dataInicio as Date;
    const df = this.form.dataFim as Date;
    this.api.criarBloqueio({
      motivo: this.form.motivo,
      dataInicio: di.toISOString(),
      dataFim: df.toISOString(),
      recursoId: this.form.recursoId
    }).subscribe(() => this.ref.close(true));
  }
}

@Component({
  selector: 'app-bloqueios',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  template: `
    <header class="topo">
      <div>
        <h1><mat-icon>block</mat-icon> Bloqueios da agenda</h1>
        <p>Feriados, recesso, manutenção. Datas bloqueadas não geram slots disponíveis.</p>
      </div>
      <button mat-flat-button color="primary" (click)="novo()">
        <mat-icon>add</mat-icon> Novo bloqueio
      </button>
    </header>

    <div class="lista" *ngIf="bloqueios().length; else vazio">
      <article class="card" *ngFor="let b of bloqueios()">
        <div class="bloco-data">
          <strong>{{ b.dataInicio | date:'dd/MM/yyyy' }}</strong>
          <small>até {{ b.dataFim | date:'dd/MM/yyyy' }}</small>
        </div>
        <div class="bloco-motivo">
          <strong>{{ b.motivo }}</strong>
          <small *ngIf="b.recursoId">Recurso #{{ b.recursoId }}</small>
          <small *ngIf="!b.recursoId">Todos os recursos</small>
        </div>
      </article>
    </div>

    <ng-template #vazio>
      <div class="vazio">
        <mat-icon>event_busy</mat-icon>
        <p>Nenhum bloqueio cadastrado.</p>
      </div>
    </ng-template>
  `,
  styles: [`
    .topo { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem; margin-bottom: 1rem; }
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .topo p { margin: 0; color: #666; }
    .lista { display: grid; grid-template-columns: repeat(auto-fill, minmax(20rem, 1fr)); gap: 0.75rem; }
    .card { background: #fff; padding: 0.75rem 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); display: flex; gap: 1rem; align-items: center; border-left: 4px solid #c62828; }
    .bloco-data, .bloco-motivo { display: flex; flex-direction: column; }
    .bloco-data strong { color: #c62828; }
    .bloco-motivo small { color: #888; }
    .vazio { text-align: center; padding: 3rem; color: #888; }
    .vazio mat-icon { font-size: 3rem; width: 3rem; height: 3rem; }
  `]
})
export class BloqueiosComponent implements OnInit {
  private api = inject(ApiService);
  private dialog = inject(MatDialog);
  private snack = inject(MatSnackBar);

  bloqueios = signal<any[]>([]);

  ngOnInit() { this.carregar(); }

  carregar() {
    this.api.listarBloqueios().subscribe(list => this.bloqueios.set(list));
  }

  novo() {
    this.dialog.open(BloqueioFormComponent, { width: '32rem' }).afterClosed().subscribe(r => {
      if (r) { this.snack.open('Bloqueio criado', 'OK', { duration: 2000 }); this.carregar(); }
    });
  }
}
