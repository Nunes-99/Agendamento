import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';

/**
 * Tela de retorno do Checkout Session do Stripe. Stripe redireciona o navegador
 * para cá com `status` (ok|cancelado), `session_id` e `agendamento`. Esta tela
 * é só informativa — o webhook server-side já atualizou o pagamento.
 *
 * Não tenta resolver o tenant slug pra redirecionar à página de confirmação:
 * isso exigiria endpoint público novo. O usuário recebe confirmação por
 * WhatsApp/email com o link direto.
 */
@Component({
  selector: 'app-pagamento-stripe-retorno',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatButtonModule],
  template: `
    <div class="wrap">
      <mat-card>
        <ng-container *ngIf="status() === 'ok'; else cancelado">
          <h1>Pagamento recebido</h1>
          <p>Recebemos o seu pagamento. Em alguns segundos você vai receber a
          confirmação por WhatsApp ou e-mail com os detalhes do agendamento.</p>
        </ng-container>
        <ng-template #cancelado>
          <h1>Pagamento cancelado</h1>
          <p>Você cancelou o pagamento. Seu agendamento continua reservado por
          mais alguns minutos. Tente novamente pelo link recebido.</p>
        </ng-template>

        <p class="sub" *ngIf="agendamento()">Agendamento #{{ agendamento() }}</p>
        <a mat-stroked-button color="primary" routerLink="/">Voltar ao início</a>
      </mat-card>
    </div>
  `,
  styles: [`
    .wrap { display: flex; justify-content: center; padding: 2rem 1rem; }
    mat-card { max-width: 28rem; padding: 1.5rem; text-align: center; }
    h1 { margin: 0 0 1rem; }
    .sub { color: var(--cor-texto-secundario); font-size: 0.875rem; margin: 0.5rem 0 1rem; }
  `]
})
export class PagamentoStripeRetornoComponent {
  private route = inject(ActivatedRoute);
  status = signal<'ok' | 'cancelado' | string>('ok');
  agendamento = signal<string | null>(null);

  constructor() {
    const params = this.route.snapshot.queryParamMap;
    this.status.set(params.get('status') ?? 'ok');
    this.agendamento.set(params.get('agendamento'));
  }
}
