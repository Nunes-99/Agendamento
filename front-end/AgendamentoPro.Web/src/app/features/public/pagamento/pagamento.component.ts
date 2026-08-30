import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../../core/services/api.service';
import { interval, switchMap, takeWhile } from 'rxjs';
import { Agendamento, CriarAgendamentoResult, StatusPagamento } from '../../../core/models/agendamento.model';
import { QRCodeModule } from 'angularx-qrcode';

@Component({
  selector: 'app-pagamento',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, QRCodeModule],
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
  copiado = signal(false);
  minutosRestantes = signal<number | null>(null);

  ehCombo = computed(() => this.grupoAgendamentos().length > 1);

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    this.agendamentoId = +(this.route.snapshot.paramMap.get('agendamentoId') || 0);
    const state = history.state.resultado as CriarAgendamentoResult;
    if (state) {
      this.resultado.set(state);
      this.iniciarContagem(state.pagamento?.expiracao);
      // Se veio do agendar-combo, history.state também traz o grupoComboId.
      const grupoId = history.state.grupoComboId as string | undefined;
      if (grupoId) this.carregarGrupo(grupoId);
    } else {
      // Sem state (recarregou a página, abriu o link no celular, voltou depois):
      // busca agendamento E cobrança, senão o QR do PIX se perde e o cliente
      // fica numa tela que manda pagar sem mostrar como.
      this.api.consultarAgendamento(this.slug, this.agendamentoId).subscribe({
        next: a => {
          if (!this.resultado()) {
            this.resultado.set({ agendamento: a, pagamento: null as any });
            this.api.cobrancaDoAgendamento(this.slug, this.agendamentoId).subscribe({
              next: cobranca => {
                if (!cobranca) return;
                const atual = this.resultado();
                if (atual) this.resultado.set({ ...atual, pagamento: cobranca });
                this.iniciarContagem(cobranca.expiracao);
              },
              error: () => { /* sem cobrança em aberto: a tela segue só com o status */ }
            });
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
    navigator.clipboard.writeText(qr).then(() => {
      // Sem retorno visual o cliente clica de novo achando que não copiou.
      this.copiado.set(true);
      setTimeout(() => this.copiado.set(false), 2500);
    });
  }

  /**
   * A reserva expira (o slot volta a ser vendido). Mostrar o tempo que resta
   * evita a situação em que o cliente demora, paga, e o horário já não é dele.
   */
  private iniciarContagem(expiracao: string | undefined) {
    if (!expiracao) return;
    // O SQLite devolve DateTime sem fuso, então a API serializa "2026-08-30T00:12:04"
    // sem "Z" e o navegador leria como horário LOCAL — 3h a mais, e o contador
    // dizia "190 min" numa reserva de 15. Sem marcador de fuso, é UTC.
    const temFuso = /[Zz]$|[+-]\d{2}:?\d{2}$/.test(expiracao);
    const fim = new Date(temFuso ? expiracao : expiracao + 'Z').getTime();
    if (isNaN(fim)) return;
    const atualizar = () => {
      const restam = Math.ceil((fim - Date.now()) / 60000);
      this.minutosRestantes.set(restam > 0 ? restam : 0);
    };
    atualizar();
    const timer = setInterval(atualizar, 30000);
    this.destroyRef.onDestroy(() => clearInterval(timer));
  }
}
