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
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-recorrencia-form',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatDatepickerModule, MatNativeDateModule, MatDialogModule],
  providers: [{ provide: MAT_DATE_LOCALE, useValue: 'pt-BR' }],
  template: `
    <h2 mat-dialog-title><mat-icon>event_repeat</mat-icon> Nova série recorrente</h2>
    <mat-dialog-content>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Cliente ID</mat-label>
          <input matInput type="number" [(ngModel)]="form.clienteId" required />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Serviço ID</mat-label>
          <input matInput type="number" [(ngModel)]="form.servicoId" required />
        </mat-form-field>
      </div>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Recurso ID</mat-label>
          <input matInput type="number" [(ngModel)]="form.recursoId" required />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Hora</mat-label>
          <input matInput type="time" [(ngModel)]="form.hora" required />
        </mat-form-field>
      </div>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Dia da semana</mat-label>
          <mat-select [(ngModel)]="form.diaSemana">
            <mat-option [value]="0">Domingo</mat-option>
            <mat-option [value]="1">Segunda</mat-option>
            <mat-option [value]="2">Terça</mat-option>
            <mat-option [value]="3">Quarta</mat-option>
            <mat-option [value]="4">Quinta</mat-option>
            <mat-option [value]="5">Sexta</mat-option>
            <mat-option [value]="6">Sábado</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Frequência</mat-label>
          <mat-select [(ngModel)]="form.frequencia">
            <mat-option [value]="1">Semanal</mat-option>
            <mat-option [value]="2">Quinzenal</mat-option>
            <mat-option [value]="3">Mensal</mat-option>
          </mat-select>
        </mat-form-field>
      </div>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Quantidade</mat-label>
          <input matInput type="number" min="1" max="52" [(ngModel)]="form.quantidade" required />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Data início</mat-label>
          <input matInput [matDatepicker]="p" [(ngModel)]="form.dataInicio" required />
          <mat-datepicker-toggle matIconSuffix [for]="p"></mat-datepicker-toggle>
          <mat-datepicker #p></mat-datepicker>
        </mat-form-field>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-flat-button color="primary" [disabled]="!valido()" (click)="salvar()">
        <mat-icon>check</mat-icon> Criar série
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; }
    @media (max-width: 30rem) { .row { grid-template-columns: 1fr; } }
    mat-form-field { width: 100%; }
  `]
})
export class RecorrenciaFormComponent {
  private api = inject(ApiService);
  private ref = inject(MatDialogRef<RecorrenciaFormComponent>);
  form: any = {
    clienteId: null, servicoId: null, recursoId: null,
    hora: '10:00', diaSemana: 1, frequencia: 1,
    quantidade: 4, dataInicio: new Date()
  };

  valido() {
    return this.form.clienteId && this.form.servicoId && this.form.recursoId
      && this.form.hora && this.form.quantidade > 0;
  }

  salvar() {
    const [h, m] = this.form.hora.split(':');
    this.api.criarRecorrencia({
      clienteId: this.form.clienteId,
      servicoId: this.form.servicoId,
      recursoId: this.form.recursoId,
      diaSemana: this.form.diaSemana,
      horaInicio: `${h.padStart(2, '0')}:${m.padStart(2, '0')}:00`,
      frequencia: this.form.frequencia,
      quantidade: this.form.quantidade,
      dataInicio: (this.form.dataInicio as Date).toISOString()
    }).subscribe(r => this.ref.close(r));
  }
}

@Component({
  selector: 'app-recorrencias',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  template: `
    <header class="topo">
      <div>
        <h1><mat-icon>event_repeat</mat-icon> Agendamentos recorrentes</h1>
        <p>Séries que criam N agendamentos contíguos automaticamente.</p>
      </div>
      <button mat-flat-button color="primary" (click)="novo()">
        <mat-icon>add</mat-icon> Nova série
      </button>
    </header>

    <div class="lista" *ngIf="itens().length; else vazio">
      <article class="card" *ngFor="let r of itens()" [class.inativo]="!r.recAtivo">
        <header class="card-head">
          <strong>Série #{{ r.recId }}</strong>
          <span class="badge" *ngIf="!r.recAtivo">Cancelada</span>
        </header>
        <div class="info">
          <small>Cliente {{ r.r_CliId }} · Serviço {{ r.r_SerId }}</small>
          <small>{{ diaSemana(r.recDiaSemana) }} às {{ horaFormatada(r.recHoraInicio) }}</small>
          <small>{{ frequencia(r.recFrequencia) }} · {{ r.recQuantidadeOcorrencias }}x</small>
        </div>
      </article>
    </div>
    <ng-template #vazio>
      <div class="vazio">
        <mat-icon>event_repeat</mat-icon>
        <p>Nenhuma série recorrente cadastrada.</p>
      </div>
    </ng-template>
  `,
  styles: [`
    .topo { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem; margin-bottom: 1rem; }
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .topo p { margin: 0; color: #666; }
    .lista { display: grid; grid-template-columns: repeat(auto-fill, minmax(18rem, 1fr)); gap: 0.75rem; }
    .card { background: #fff; padding: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); border-left: 4px solid #6366f1; }
    .card.inativo { opacity: 0.6; border-left-color: #aaa; }
    .card-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem; }
    .badge { background: #ffcdd2; color: #c62828; padding: 0.1rem 0.5rem; border-radius: 0.4rem; font-size: 0.75rem; }
    .info { display: flex; flex-direction: column; gap: 0.15rem; color: #666; }
    .vazio { text-align: center; padding: 3rem; color: #888; }
    .vazio mat-icon { font-size: 3rem; width: 3rem; height: 3rem; }
  `]
})
export class RecorrenciasComponent implements OnInit {
  private api = inject(ApiService);
  private dialog = inject(MatDialog);
  private snack = inject(MatSnackBar);
  itens = signal<any[]>([]);

  diaSemana(d: number): string {
    return ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'][d] || '?';
  }
  frequencia(f: number): string {
    return { 1: 'Semanal', 2: 'Quinzenal', 3: 'Mensal' }[f] || '?';
  }
  horaFormatada(h: string): string {
    return h?.length >= 5 ? h.substring(0, 5) : h;
  }

  ngOnInit() { this.carregar(); }

  carregar() { this.api.listarRecorrencias().subscribe(list => this.itens.set(list)); }

  novo() {
    this.dialog.open(RecorrenciaFormComponent, { width: '36rem' }).afterClosed().subscribe(r => {
      if (r) {
        this.snack.open(`${r.criados} agendamentos criados`, 'OK', { duration: 3000 });
        this.carregar();
      }
    });
  }
}
