import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ApiService } from '../../../core/services/api.service';
import { Assinatura, StatusAssinatura } from '../../../core/models/assinatura.model';

/// Banner global no admin que avisa quando assinatura está Atrasada ou ReadOnly.
/// Some quando status é Ativa/Trial ou quando não há assinatura.
@Component({
  selector: 'app-billing-banner',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule],
  template: `
    <div *ngIf="banner() as b" class="banner" [class.atrasada]="b.tipo === 'atrasada'" [class.readonly]="b.tipo === 'readonly'">
      <mat-icon>{{ b.tipo === 'readonly' ? 'block' : 'warning' }}</mat-icon>
      <span>{{ b.mensagem }}</span>
      <a mat-flat-button color="primary" routerLink="/admin/minha-assinatura">Regularizar</a>
    </div>
  `,
  styles: [`
    .banner { display: flex; align-items: center; gap: 0.75rem; padding: 0.75rem 1.25rem; border-bottom: 1px solid; }
    .banner.atrasada { background: #fef3c7; color: #92400e; border-color: #f59e0b; }
    .banner.readonly { background: #fee2e2; color: #991b1b; border-color: #dc2626; }
    .banner span { flex: 1; }
  `]
})
export class BillingBannerComponent implements OnInit {
  private api = inject(ApiService);
  banner = signal<{ tipo: 'atrasada' | 'readonly'; mensagem: string } | null>(null);

  ngOnInit() {
    this.api.minhaAssinatura().subscribe({
      next: a => this.banner.set(this.calcular(a)),
      error: () => this.banner.set(null)
    });
  }

  private calcular(a: Assinatura | null): { tipo: 'atrasada' | 'readonly'; mensagem: string } | null {
    if (!a) return null;
    if (a.status === StatusAssinatura.Atrasada)
      return { tipo: 'atrasada', mensagem: 'Pagamento da mensalidade pendente. Após 8 dias o sistema vai para somente-leitura.' };
    if (a.status === StatusAssinatura.ReadOnly || a.status === StatusAssinatura.Expirada)
      return { tipo: 'readonly', mensagem: 'Sistema em modo somente-leitura por falta de pagamento. Regularize para reativar.' };
    return null;
  }
}
