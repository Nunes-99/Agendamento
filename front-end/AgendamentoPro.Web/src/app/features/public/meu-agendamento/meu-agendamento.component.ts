import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, MAT_DATE_LOCALE } from '@angular/material/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-meu-agendamento',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatProgressSpinnerModule, MatDatepickerModule, MatNativeDateModule],
  providers: [{ provide: MAT_DATE_LOCALE, useValue: 'pt-BR' }],
  template: `
    <div class="container">
      <div *ngIf="carregando(); else conteudo" class="centro"><mat-spinner></mat-spinner></div>

      <ng-template #conteudo>
        <div *ngIf="!agendamento(); else detalhes" class="centro mensagem">
          <mat-icon class="grande">link_off</mat-icon>
          <h2>Link inválido</h2>
          <p>Este link de agendamento não existe ou expirou.</p>
        </div>

        <ng-template #detalhes>
          <header class="topo">
            <mat-icon class="ok">event_available</mat-icon>
            <h1>Seu agendamento</h1>
          </header>

          <div class="card">
            <div class="linha"><strong>Cliente:</strong> {{ agendamento()!.clienteNome }}</div>
            <div class="linha"><strong>Serviço:</strong> {{ agendamento()!.servicoNome }}</div>
            <div class="linha"><strong>Recurso:</strong> {{ agendamento()!.recursoNome }}</div>
            <div class="linha">
              <strong>Quando:</strong>
              {{ agendamento()!.data | date:'dd/MM/yyyy' }} às {{ horaFormatada(agendamento()!.horaInicio) }}
            </div>
            <div class="linha"><strong>Valor:</strong> R$ {{ agendamento()!.valorTotal | number:'1.2-2' }}</div>
            <div class="linha"><strong>Status:</strong>
              <span class="status">{{ agendamento()!.status }}</span>
            </div>
            <div class="linha" *ngIf="agendamento()!.ehCombo">
              <mat-icon>local_offer</mat-icon> Faz parte de um combo — cancelar afeta todos os serviços do combo.
            </div>
          </div>

          <div class="acoes" *ngIf="agendamento()!.podeCancelar || agendamento()!.podeReagendar">
            <button mat-flat-button color="primary" *ngIf="agendamento()!.podeReagendar && !agendamento()!.ehCombo"
              (click)="modoReagendar.set(!modoReagendar())">
              <mat-icon>edit_calendar</mat-icon> Reagendar
            </button>
            <button mat-stroked-button color="warn" *ngIf="agendamento()!.podeCancelar" (click)="cancelar()">
              <mat-icon>cancel</mat-icon> Cancelar
            </button>
          </div>

          <div class="card" *ngIf="modoReagendar()">
            <h3>Reagendar</h3>
            <div class="row">
              <mat-form-field appearance="outline">
                <mat-label>Nova data</mat-label>
                <input matInput [matDatepicker]="picker" [(ngModel)]="novaData" [min]="hoje" />
                <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
                <mat-datepicker #picker></mat-datepicker>
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Novo horário</mat-label>
                <input matInput type="time" [(ngModel)]="novaHora" />
              </mat-form-field>
            </div>
            <button mat-flat-button color="primary" (click)="confirmarReagendamento()"
              [disabled]="!novaData || !novaHora || enviando()">
              <mat-icon>check</mat-icon> Confirmar
            </button>
          </div>
        </ng-template>
      </ng-template>
    </div>
  `,
  styles: [`
    .container { max-width: 32rem; margin: 0 auto; padding: 1.5rem 1rem; }
    .centro { display: flex; flex-direction: column; align-items: center; gap: 1rem; padding: 4rem 1rem; }
    .grande { font-size: 4rem; width: 4rem; height: 4rem; color: #c62828; }
    .ok { font-size: 3rem; width: 3rem; height: 3rem; color: #2e7d32; }
    .topo { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; margin-bottom: 1.5rem; }
    .topo h1 { margin: 0; }
    .card { background: #fff; padding: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.08); }
    .linha { padding: 0.4rem 0; border-bottom: 1px solid #f0f0f0; display: flex; gap: 0.5rem; align-items: center; flex-wrap: wrap; }
    .linha:last-child { border-bottom: none; }
    .status { background: #e3f2fd; color: #1565c0; padding: 0.15rem 0.5rem; border-radius: 0.4rem; font-size: 0.85rem; }
    .acoes { display: flex; gap: 0.5rem; justify-content: center; margin: 1.25rem 0; flex-wrap: wrap; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; margin-bottom: 0.75rem; }
    @media (max-width: 30rem) { .row { grid-template-columns: 1fr; } }
  `]
})
export class MeuAgendamentoComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  token = '';
  agendamento = signal<any>(null);
  carregando = signal(true);
  enviando = signal(false);
  modoReagendar = signal(false);
  hoje = new Date();
  novaData: Date | null = null;
  novaHora = '';

  horaFormatada(h: string) { return h?.length >= 5 ? h.substring(0, 5) : h; }

  ngOnInit() {
    this.token = this.route.snapshot.paramMap.get('token') || '';
    if (!this.token) { this.carregando.set(false); return; }
    this.carregar();
  }

  carregar() {
    this.api.obterMeuAgendamento(this.token).subscribe({
      next: a => { this.agendamento.set(a); this.carregando.set(false); },
      error: () => { this.agendamento.set(null); this.carregando.set(false); }
    });
  }

  cancelar() {
    const motivo = prompt('Motivo do cancelamento (opcional):') || '';
    if (!confirm('Confirmar cancelamento?')) return;
    this.api.cancelarMeuAgendamento(this.token, motivo).subscribe({
      next: () => { this.snack.open('Agendamento cancelado.', 'OK', { duration: 3000 }); this.carregar(); },
      error: e => this.snack.open(e.error?.message || 'Falha ao cancelar', 'OK', { duration: 4000 })
    });
  }

  confirmarReagendamento() {
    if (!this.novaData || !this.novaHora) return;
    this.enviando.set(true);
    const dataIso = this.toIso(this.novaData);
    const horaIso = this.novaHora.length === 5 ? this.novaHora + ':00' : this.novaHora;
    this.api.reagendarMeuAgendamento(this.token, dataIso, horaIso).subscribe({
      next: () => {
        this.enviando.set(false);
        this.modoReagendar.set(false);
        this.snack.open('Reagendado com sucesso!', 'OK', { duration: 3000 });
        this.carregar();
      },
      error: e => {
        this.enviando.set(false);
        this.snack.open(e.error?.message || 'Falha ao reagendar', 'OK', { duration: 4000 });
      }
    });
  }

  private toIso(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
