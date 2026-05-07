import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { Agendamento, StatusAgendamento } from '../../../core/models/agendamento.model';
import { AgendamentoDialogComponent } from './agendamento-dialog.component';

type Preset = 'hoje' | 'semana' | 'mes' | 'custom';

@Component({
  selector: 'app-agenda',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatMenuModule, MatProgressSpinnerModule,
    MatDialogModule],
  templateUrl: './agenda.component.html',
  styleUrls: ['./agenda.component.scss']
})
export class AgendaComponent implements OnInit {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  inicio = signal(this.hoje());
  fim = signal(this.hoje());
  preset = signal<Preset>('hoje');
  agendamentos = signal<Agendamento[]>([]);
  carregando = signal(true);

  filtroStatus = signal<'todos' | 'ativos' | 'concluidos' | 'cancelados'>('ativos');

  filtrados = computed(() => {
    const lista = this.agendamentos();
    const f = this.filtroStatus();
    if (f === 'todos') return lista;
    if (f === 'ativos') return lista.filter(a =>
      a.status === StatusAgendamento.PendentePagamento ||
      a.status === StatusAgendamento.Confirmado ||
      a.status === StatusAgendamento.EmAndamento);
    if (f === 'concluidos') return lista.filter(a => a.status === StatusAgendamento.Concluido);
    return lista.filter(a => a.status === StatusAgendamento.Cancelado || a.status === StatusAgendamento.NoShow);
  });

  resumo = computed(() => {
    const lista = this.agendamentos();
    return {
      total: lista.length,
      pendentes: lista.filter(a => a.status === StatusAgendamento.PendentePagamento).length,
      confirmados: lista.filter(a => a.status === StatusAgendamento.Confirmado).length,
      emAndamento: lista.filter(a => a.status === StatusAgendamento.EmAndamento).length,
      concluidos: lista.filter(a => a.status === StatusAgendamento.Concluido).length,
      cancelados: lista.filter(a => a.status === StatusAgendamento.Cancelado || a.status === StatusAgendamento.NoShow).length,
      receita: lista
        .filter(a => a.status === StatusAgendamento.Concluido || a.status === StatusAgendamento.Confirmado || a.status === StatusAgendamento.EmAndamento)
        .reduce((sum, a) => sum + a.valorTotal, 0)
    };
  });

  ngOnInit() { this.carregar(); }

  private hoje(): string {
    return new Date().toISOString().substring(0, 10);
  }

  carregar() {
    this.carregando.set(true);
    this.api.agendaPorPeriodo(this.inicio(), this.fim()).subscribe({
      next: list => {
        this.agendamentos.set(list);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  aplicarPreset(p: Preset) {
    this.preset.set(p);
    const hoje = new Date();
    if (p === 'hoje') {
      this.inicio.set(this.hoje());
      this.fim.set(this.hoje());
    } else if (p === 'semana') {
      // Semana corrente: domingo (0) a sábado (6)
      const dia = hoje.getDay();
      const ini = new Date(hoje); ini.setDate(hoje.getDate() - dia);
      const fim = new Date(ini); fim.setDate(ini.getDate() + 6);
      this.inicio.set(this.toIso(ini));
      this.fim.set(this.toIso(fim));
    } else if (p === 'mes') {
      const ini = new Date(hoje.getFullYear(), hoje.getMonth(), 1);
      const fim = new Date(hoje.getFullYear(), hoje.getMonth() + 1, 0);
      this.inicio.set(this.toIso(ini));
      this.fim.set(this.toIso(fim));
    }
    this.carregar();
  }

  private toIso(d: Date): string {
    const ano = d.getFullYear();
    const mes = String(d.getMonth() + 1).padStart(2, '0');
    const dia = String(d.getDate()).padStart(2, '0');
    return `${ano}-${mes}-${dia}`;
  }

  onDataChange() {
    if (this.fim() < this.inicio()) {
      this.fim.set(this.inicio());
    }
    this.preset.set('custom');
    this.carregar();
  }

  intervaloLabel(): string {
    if (this.inicio() === this.fim()) return '';
    return `${this.inicio()} até ${this.fim()}`;
  }

  classeStatus(s: StatusAgendamento): string {
    return [
      'pendente', 'confirmado', 'andamento', 'concluido', 'cancelado', 'noshow'
    ][s] || '';
  }

  rotuloStatus(s: StatusAgendamento): string {
    return [
      'Aguardando pagamento', 'Confirmado', 'Em andamento',
      'Concluído', 'Cancelado', 'Não compareceu'
    ][s] || '';
  }

  iconeStatus(s: StatusAgendamento): string {
    return [
      'schedule', 'check_circle', 'play_circle',
      'task_alt', 'cancel', 'person_off'
    ][s] || 'event';
  }

  iniciar(a: Agendamento) {
    this.api.iniciarAgendamento(a.id).subscribe({
      next: () => { this.snack.open('Atendimento iniciado', 'OK', { duration: 2000 }); this.carregar(); },
      error: e => this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 4000, panelClass: 'snack-erro' })
    });
  }

  concluir(a: Agendamento) {
    this.api.concluirAgendamento(a.id).subscribe({
      next: () => { this.snack.open('Atendimento concluído', 'OK', { duration: 2000 }); this.carregar(); },
      error: e => this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 4000, panelClass: 'snack-erro' })
    });
  }

  noShow(a: Agendamento) {
    this.api.noShowAgendamento(a.id).subscribe({
      next: () => { this.snack.open('Marcado como não compareceu', 'OK', { duration: 2000 }); this.carregar(); },
      error: e => this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 4000, panelClass: 'snack-erro' })
    });
  }

  cancelar(a: Agendamento) {
    const motivo = prompt('Motivo do cancelamento:') || 'Cancelado pelo admin.';
    this.api.cancelarAgendamento(a.id, motivo).subscribe({
      next: () => { this.snack.open('Agendamento cancelado', 'OK', { duration: 2000 }); this.carregar(); },
      error: e => this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 4000, panelClass: 'snack-erro' })
    });
  }

  novo() {
    const ref = this.dialog.open(AgendamentoDialogComponent, {
      data: { modo: 'novo' },
      width: '40rem',
      maxWidth: '95vw'
    });
    ref.afterClosed().subscribe(ok => { if (ok) this.carregar(); });
  }

  editar(a: Agendamento) {
    const ref = this.dialog.open(AgendamentoDialogComponent, {
      data: { modo: 'reagendar', agendamento: a },
      width: '36rem',
      maxWidth: '95vw'
    });
    ref.afterClosed().subscribe(ok => { if (ok) this.carregar(); });
  }

  iniciaisCliente(nome: string): string {
    return (nome || '?').split(' ').filter(Boolean).slice(0, 2).map(p => p[0]?.toUpperCase()).join('');
  }

  StatusAgendamento = StatusAgendamento;
}
