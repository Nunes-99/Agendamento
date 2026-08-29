import { Component, OnInit, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { QRCodeModule } from 'angularx-qrcode';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval, switchMap, takeWhile } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-comprar-pacote',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, CurrencyPipe, QRCodeModule],
  template: `
    <div class="container">
      <h1><mat-icon>inventory_2</mat-icon> Pacotes pré-pagos</h1>
      <p class="subtitulo" *ngIf="!compra()">Escolha um pacote abaixo para comprar com PIX.</p>

      <ng-container *ngIf="!compra(); else pagando">
        <div class="lista" *ngIf="pacotes().length; else listaCarregando">
          <article class="card" *ngFor="let p of pacotes()" (click)="selecionar(p)" [class.sel]="selecionado()?.pctId === p.pctId">
            <h3>{{ p.pctNome }}</h3>
            <div class="info">
              <span><mat-icon>repeat</mat-icon> {{ p.pctQuantidade }} atendimentos</span>
              <span><mat-icon>schedule</mat-icon> Vale {{ p.pctValidadeDias }} dias</span>
            </div>
            <strong>{{ p.pctPreco | currency:'BRL' }}</strong>
            <span class="escolher">
              <mat-icon>{{ selecionado()?.pctId === p.pctId ? 'check_circle' : 'radio_button_unchecked' }}</mat-icon>
              {{ selecionado()?.pctId === p.pctId ? 'Selecionado' : 'Escolher este' }}
            </span>
          </article>
        </div>

        <section *ngIf="selecionado()" class="form">
          <h2>Seus dados</h2>
          <mat-form-field appearance="outline" class="full">
            <mat-label>Nome</mat-label>
            <input matInput [(ngModel)]="cliente.nome" required />
          </mat-form-field>
          <mat-form-field appearance="outline" class="full">
            <mat-label>Telefone</mat-label>
            <input matInput [(ngModel)]="cliente.telefone" placeholder="11999999999" required />
          </mat-form-field>
          <mat-form-field appearance="outline" class="full">
            <mat-label>E-mail (opcional)</mat-label>
            <input matInput type="email" [(ngModel)]="cliente.email" />
          </mat-form-field>
          <button mat-flat-button color="primary" [disabled]="!valido() || carregando()" (click)="comprar()">
            <mat-icon>shopping_cart</mat-icon> Comprar e gerar PIX
          </button>
        </section>
      </ng-container>

      <ng-template #pagando>
        <section class="qr-card" *ngIf="compra() as c">
          <h2>Quase lá! Pague com PIX</h2>
          <p>Valor: <strong>{{ c.valor | currency:'BRL' }}</strong></p>
          <qrcode *ngIf="c.qrCode" [qrdata]="c.qrCode" [width]="240" errorCorrectionLevel="M"></qrcode>
          <p>Escaneie no seu app de banco ou copie o código:</p>
          <pre class="copiavel">{{ c.qrCode }}</pre>
          <button mat-stroked-button (click)="copiar(c.qrCode)">
            <mat-icon>content_copy</mat-icon> Copiar PIX
          </button>
          <div class="status">
            <mat-spinner [diameter]="32" *ngIf="status() === 'pendente'"></mat-spinner>
            <span>{{ status() === 'aprovado' ? 'Pagamento confirmado!' : 'Aguardando confirmação...' }}</span>
          </div>
        </section>
      </ng-template>

      <ng-template #listaCarregando><mat-spinner></mat-spinner></ng-template>
    </div>
  `,
  styles: [`
    .container { max-width: 56rem; margin: 1rem auto; padding: 1rem; }
    h1 { display: flex; align-items: center; gap: 0.5rem; }
    .lista { display: grid; grid-template-columns: repeat(auto-fill, minmax(16rem, 1fr)); gap: 0.75rem; }
    .card { background: var(--cor-fundo-card); padding: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); cursor: pointer; transition: 0.15s; border: 2px solid transparent; }
    .card:hover { transform: translateY(-2px); }
    .card.sel { border-color: #2e7d32; }
    .card h3 { margin: 0 0 0.5rem 0; }
    .info { display: flex; flex-direction: column; gap: 0.25rem; color: #666; margin-bottom: 0.5rem; }
    .info span { display: flex; align-items: center; gap: 0.25rem; font-size: 0.9rem; }
    .info mat-icon { font-size: 1rem; width: 1rem; height: 1rem; }
    .card strong { color: #2e7d32; font-size: 1.5rem; }
    .subtitulo { color: #666; margin: -0.5rem 0 1rem; }
    .escolher { display: flex; align-items: center; gap: 0.25rem; margin-top: 0.5rem;
      color: #2e7d32; font-size: 0.875rem; font-weight: 500; }
    .escolher mat-icon { font-size: 1.125rem; width: 1.125rem; height: 1.125rem; }
    .form { margin-top: 1rem; background: var(--cor-fundo-card); padding: 1rem; border-radius: 0.5rem; }
    .full { width: 100%; }
    .qr-card { background: var(--cor-fundo-card); padding: 1.5rem; border-radius: 0.5rem; text-align: center; }
    .qr-card pre { background: #f5f5f5; padding: 0.5rem; border-radius: 0.25rem; word-break: break-all; white-space: pre-wrap; font-size: 0.75rem; }
    .copiavel { user-select: all; cursor: text; }
    .status { display: flex; align-items: center; justify-content: center; gap: 0.5rem; margin-top: 1rem; }
  `]
})
export class ComprarPacoteComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snack = inject(MatSnackBar);
  private destroyRef = inject(DestroyRef);

  slug = '';
  pacotes = signal<any[]>([]);
  selecionado = signal<any | null>(null);
  cliente = { nome: '', telefone: '', email: '' };
  carregando = signal(false);
  compra = signal<{ saldoPacoteId: number; qrCode: string; valor: number } | null>(null);
  status = signal<'pendente' | 'aprovado'>('pendente');

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    this.api.listarPacotesPublicos(this.slug).subscribe(p => this.pacotes.set(p));
  }

  selecionar(p: any) { this.selecionado.set(p); }
  valido() { return this.selecionado() && this.cliente.nome && this.cliente.telefone; }

  comprar() {
    const p = this.selecionado();
    if (!p) return;
    this.carregando.set(true);
    this.api.comprarPacote(this.slug, p.pctId, this.cliente).subscribe({
      next: r => {
        this.carregando.set(false);
        this.compra.set({ saldoPacoteId: r.saldoPacoteId, qrCode: r.qrCode, valor: p.pctPreco });
        this.iniciarPolling();
      },
      error: e => {
        this.carregando.set(false);
        this.snack.open(e.error?.message || 'Falha ao gerar pagamento', 'OK', { duration: 4000 });
      }
    });
  }

  copiar(t: string) {
    navigator.clipboard.writeText(t);
    this.snack.open('Código copiado', 'OK', { duration: 1500 });
  }

  private iniciarPolling() {
    const c = this.compra();
    if (!c) return;
    interval(5000).pipe(
      switchMap(() => this.api.consultarStatusSaldoPacote(this.slug, c.saldoPacoteId)),
      takeWhile(s => s.status === 'pendente', true),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(s => {
      if (s.status === 'ativo') {
        this.status.set('aprovado');
        this.snack.open('Pacote ativado! Você pode agendar agora.', 'OK', { duration: 4000 });
        setTimeout(() => this.router.navigate(['/t', this.slug]), 2000);
      }
    });
  }
}
