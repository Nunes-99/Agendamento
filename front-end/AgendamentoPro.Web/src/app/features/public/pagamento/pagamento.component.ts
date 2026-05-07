import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../../core/services/api.service';
import { interval, switchMap, takeWhile } from 'rxjs';
import { Agendamento, CriarAgendamentoResult, StatusPagamento } from '../../../core/models/agendamento.model';

@Component({
  selector: 'app-pagamento',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './pagamento.component.html',
  styleUrls: ['./pagamento.component.scss']
})
export class PagamentoComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = inject(ApiService);
  private destroyRef = inject(DestroyRef);

  slug = '';
  agendamentoId = 0;
  resultado = signal<CriarAgendamentoResult | null>(null);
  grupoAgendamentos = signal<Agendamento[]>([]);
  statusPagamento = StatusPagamento;

  ehCombo = computed(() => this.grupoAgendamentos().length > 1);

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    this.agendamentoId = +(this.route.snapshot.paramMap.get('agendamentoId') || 0);
    const state = history.state.resultado as CriarAgendamentoResult;
    if (state) {
      this.resultado.set(state);
      // Se veio do agendar-combo, history.state também traz o grupoComboId.
      const grupoId = history.state.grupoComboId as string | undefined;
      if (grupoId) this.carregarGrupo(grupoId);
    } else {
      // Sem state: tenta buscar o agendamento e ver se faz parte de combo
      this.api.consultarAgendamento(this.slug, this.agendamentoId).subscribe({
        next: a => {
          if (!this.resultado()) {
            this.resultado.set({ agendamento: a, pagamento: null as any });
          }
          const grupoId = (a as any).grupoComboId;
          if (grupoId) this.carregarGrupo(grupoId);
        }
      });
    }

    // Polling do status do pagamento (cancela ao destruir o componente)
    interval(5000).pipe(
      switchMap(() => this.api.consultarAgendamento(this.slug, this.agendamentoId)),
      takeWhile(a => a.statusPagamento === StatusPagamento.Pendente, true),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(a => {
      const r = this.resultado();
      if (!r) return;
      r.agendamento = a;
      this.resultado.set({ ...r });
      if (a.statusPagamento === StatusPagamento.Aprovado) {
        this.router.navigate(['/t', this.slug, 'confirmacao', this.agendamentoId]);
      }
    });
  }

  private carregarGrupo(grupoId: string) {
    this.api.agendamentosDoGrupoCombo(this.slug, grupoId).subscribe({
      next: lista => this.grupoAgendamentos.set(lista),
      error: () => { /* silencioso - exibe só o agendamento principal */ }
    });
  }

  horaFormatada(hora: string | undefined): string {
    if (!hora) return '';
    return hora.length >= 5 ? hora.substring(0, 5) : hora;
  }

  copiarPix(qr: string) {
    navigator.clipboard.writeText(qr);
  }
}
