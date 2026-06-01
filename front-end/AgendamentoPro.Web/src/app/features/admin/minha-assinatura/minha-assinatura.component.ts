import { Component, DestroyRef, HostListener, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { fromEvent, merge, throttleTime } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { Assinatura, Plano, StatusAssinatura } from '../../../core/models/assinatura.model';

@Component({
  selector: 'app-minha-assinatura',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatTableModule],
  template: `
    <section class="shell">
      <header class="cabecalho">
        <h1>Minha assinatura</h1>
        <button mat-icon-button (click)="carregar()" title="Atualizar" *ngIf="assinatura()">
          <mat-icon [class.girando]="carregando()">refresh</mat-icon>
        </button>
      </header>

      <!-- Sem assinatura: form de criação -->
      <mat-card *ngIf="!assinatura() && !carregando()" class="card-criar">
        <h2>Escolha um plano para começar</h2>
        <p>Você ainda não tem uma assinatura ativa.</p>

        <mat-form-field appearance="outline">
          <mat-label>Plano</mat-label>
          <mat-select [(ngModel)]="planoEscolhido">
            <mat-option *ngFor="let p of planos()" [value]="p.id">
              {{ p.nome }} — R$ {{ p.preco | number:'1.2-2' }}/mês
            </mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>E-mail do pagador</mat-label>
          <input matInput type="email" [(ngModel)]="payerEmail" placeholder="email cadastrado no Mercado Pago">
        </mat-form-field>

        <button mat-flat-button color="primary" (click)="criar()" [disabled]="!planoEscolhido || !payerEmail || salvando()">
          <mat-icon>credit_card</mat-icon> Continuar para o cartão
        </button>
      </mat-card>

      <!-- Com assinatura -->
      <ng-container *ngIf="assinatura() as a">
        <mat-card class="card-status" [class.atrasada]="ehAtrasada(a)" [class.readonly]="ehReadOnly(a)" [class.cancelada]="ehCancelada(a)">
          <header>
            <div>
              <h2>{{ a.planoNome }}</h2>
              <strong class="preco">R$ {{ a.planoPreco | number:'1.2-2' }}/mês</strong>
            </div>
            <mat-chip-set>
              <mat-chip>{{ rotuloStatus(a.status) }}</mat-chip>
            </mat-chip-set>
          </header>

          <p *ngIf="a.checkoutUrl" class="cta-cartao">
            <mat-icon>credit_card</mat-icon>
            <span>Para concluir, autorize o débito recorrente no Mercado Pago:</span>
            <a mat-flat-button color="primary" [href]="a.checkoutUrl" target="_blank">Cadastrar cartão</a>
          </p>

          <p *ngIf="ehAtrasada(a)" class="aviso aviso-amarelo">
            <mat-icon>warning</mat-icon>
            <span>Pagamento pendente desde {{ a.atrasoDesde | date:'dd/MM/yyyy' }}. Após 8 dias, o sistema fica em modo somente-leitura.</span>
          </p>
          <p *ngIf="ehReadOnly(a)" class="aviso aviso-vermelho">
            <mat-icon>block</mat-icon>
            <span>Sistema em modo somente-leitura desde {{ a.readOnlyDesde | date:'dd/MM/yyyy' }}. Renove pra continuar operando.</span>
          </p>

          <dl class="datas">
            <div><dt>Início</dt><dd>{{ a.dataInicio | date:'dd/MM/yyyy' }}</dd></div>
            <div><dt>Próximo vencimento</dt><dd>{{ a.proximoVencimento ? (a.proximoVencimento | date:'dd/MM/yyyy') : '—' }}</dd></div>
            <div><dt>Último pagamento</dt><dd>{{ a.ultimoPagamentoEm ? (a.ultimoPagamentoEm | date:'dd/MM/yyyy') : 'Nenhum ainda' }}</dd></div>
          </dl>

          <div class="acoes" *ngIf="!ehCancelada(a)">
            <mat-form-field appearance="outline">
              <mat-label>Trocar plano</mat-label>
              <mat-select [(ngModel)]="novoPlano">
                <mat-option *ngFor="let p of planosTrocaveis(a)" [value]="p.id">
                  {{ p.nome }} — R$ {{ p.preco | number:'1.2-2' }}/mês
                </mat-option>
              </mat-select>
            </mat-form-field>
            <button mat-stroked-button color="primary" (click)="trocarPlano()" [disabled]="!novoPlano || salvando()">
              Confirmar troca
            </button>
            <button mat-stroked-button color="warn" (click)="cancelar()" [disabled]="salvando()">
              <mat-icon>cancel</mat-icon> Cancelar assinatura
            </button>
          </div>
        </mat-card>

        <mat-card *ngIf="a.faturas?.length" class="card-faturas">
          <h3>Histórico de faturas</h3>
          <table mat-table [dataSource]="a.faturas">
            <ng-container matColumnDef="periodo">
              <th mat-header-cell *matHeaderCellDef>Período</th>
              <td mat-cell *matCellDef="let f">{{ f.referenciaInicio | date:'dd/MM/yyyy' }} → {{ f.referenciaFim | date:'dd/MM/yyyy' }}</td>
            </ng-container>
            <ng-container matColumnDef="valor">
              <th mat-header-cell *matHeaderCellDef>Valor</th>
              <td mat-cell *matCellDef="let f">R$ {{ f.valor | number:'1.2-2' }}</td>
            </ng-container>
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let f">{{ f.statusTexto }}</td>
            </ng-container>
            <ng-container matColumnDef="pagoEm">
              <th mat-header-cell *matHeaderCellDef>Pago em</th>
              <td mat-cell *matCellDef="let f">{{ f.pagoEm ? (f.pagoEm | date:'dd/MM/yyyy') : '—' }}</td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="['periodo','valor','status','pagoEm']"></tr>
            <tr mat-row *matRowDef="let row; columns: ['periodo','valor','status','pagoEm']"></tr>
          </table>
        </mat-card>
      </ng-container>

      <p *ngIf="carregando()" class="vazio">Carregando…</p>
    </section>
  `,
  styles: [`
    .shell { max-width: 60rem; }
    .cabecalho { display: flex; align-items: center; justify-content: space-between; margin: 0 0 1.5rem; }
    .cabecalho h1 { margin: 0; }
    .girando { animation: girar 1s linear infinite; }
    @keyframes girar { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
    h1 { margin: 0 0 1.5rem; }
    mat-card { padding: 1.5rem; margin-bottom: 1rem; }
    mat-card.card-criar { display: flex; flex-direction: column; gap: 1rem; max-width: 30rem; }
    .card-status header { display: flex; justify-content: space-between; align-items: start; margin-bottom: 1rem; }
    .card-status h2 { margin: 0 0 0.25rem; }
    .card-status .preco { color: #4f46e5; font-size: 1.25rem; }
    .card-status.atrasada { border-left: 4px solid #f59e0b; }
    .card-status.readonly { border-left: 4px solid #dc2626; }
    .card-status.cancelada { border-left: 4px solid #6b7280; opacity: 0.7; }
    .aviso { display: flex; align-items: center; gap: 0.5rem; padding: 0.75rem 1rem; border-radius: 0.5rem; margin: 1rem 0; }
    .aviso-amarelo { background: #fef3c7; color: #92400e; }
    .aviso-vermelho { background: #fee2e2; color: #991b1b; }
    .cta-cartao { display: flex; align-items: center; gap: 0.75rem; background: #eff6ff; padding: 0.75rem 1rem; border-radius: 0.5rem; margin: 1rem 0; flex-wrap: wrap; }
    .datas { display: grid; grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr)); gap: 1rem; margin: 1rem 0; }
    .datas dt { font-size: 0.75rem; color: #888; text-transform: uppercase; letter-spacing: 0.05em; }
    .datas dd { margin: 0.25rem 0 0; font-weight: 600; }
    .acoes { display: flex; gap: 0.75rem; align-items: center; flex-wrap: wrap; margin-top: 1rem; }
    .acoes mat-form-field { min-width: 16rem; flex: 1; }
    table { width: 100%; }
    .vazio { text-align: center; color: #888; padding: 2rem; }
  `]
})
export class MinhaAssinaturaComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private snack = inject(MatSnackBar);
  private destroyRef = inject(DestroyRef);

  assinatura = signal<Assinatura | null>(null);
  planos = signal<Plano[]>([]);
  carregando = signal(true);
  salvando = signal(false);
  planoEscolhido?: number;
  novoPlano?: number;
  payerEmail = '';

  ngOnInit() {
    this.payerEmail = this.auth.user()?.email || '';
    this.carregar();
    this.api.listarPlanos().subscribe(p => {
      this.planos.set(p);
      const planoQuery = this.route.snapshot.queryParamMap.get('plano');
      if (planoQuery) this.planoEscolhido = +planoQuery;
    });

    // Auto-refresh quando a aba volta ao foco — detecta retorno do checkout do MP.
    // Throttle pra evitar disparos múltiplos em focus + visibilitychange seguidos.
    merge(
      fromEvent(window, 'focus'),
      fromEvent(document, 'visibilitychange')
    ).pipe(
      throttleTime(2000),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(() => {
      if (document.visibilityState === 'visible') this.carregar();
    });
  }

  carregar() {
    this.carregando.set(true);
    this.api.minhaAssinatura().subscribe({
      next: a => { this.assinatura.set(a); this.carregando.set(false); },
      error: e => {
        // 404 = ainda não tem assinatura, é OK
        this.assinatura.set(null);
        this.carregando.set(false);
      }
    });
  }

  criar() {
    if (!this.planoEscolhido || !this.payerEmail) return;
    this.salvando.set(true);
    this.api.criarAssinatura({ planoId: this.planoEscolhido, payerEmail: this.payerEmail }).subscribe({
      next: a => {
        this.assinatura.set(a);
        this.salvando.set(false);
        if (a.checkoutUrl) window.open(a.checkoutUrl, '_blank');
        this.snack.open('Assinatura criada. Cadastre seu cartão para ativar.', 'OK', { duration: 6000 });
      },
      error: e => { this.salvando.set(false); this.snack.open(e?.error?.detail || 'Falha ao criar assinatura.', 'OK', { duration: 5000 }); }
    });
  }

  trocarPlano() {
    if (!this.novoPlano) return;
    this.salvando.set(true);
    this.api.alterarPlano({ novoPlanoId: this.novoPlano }).subscribe({
      next: a => { this.assinatura.set(a); this.novoPlano = undefined; this.salvando.set(false); this.snack.open('Plano alterado.', 'OK', { duration: 3000 }); },
      error: e => { this.salvando.set(false); this.snack.open(e?.error?.detail || 'Falha na troca.', 'OK', { duration: 5000 }); }
    });
  }

  cancelar() {
    if (!confirm('Cancelar a assinatura? O sistema ficará indisponível ao fim do ciclo atual.')) return;
    this.salvando.set(true);
    this.api.cancelarAssinatura().subscribe({
      next: a => { this.assinatura.set(a); this.salvando.set(false); this.snack.open('Assinatura cancelada.', 'OK', { duration: 3000 }); },
      error: e => { this.salvando.set(false); this.snack.open(e?.error?.detail || 'Falha ao cancelar.', 'OK', { duration: 5000 }); }
    });
  }

  planosTrocaveis(a: Assinatura): Plano[] { return this.planos().filter(p => p.id !== a.planoId); }
  ehAtrasada(a: Assinatura) { return a.status === StatusAssinatura.Atrasada; }
  ehReadOnly(a: Assinatura) { return a.status === StatusAssinatura.ReadOnly; }
  ehCancelada(a: Assinatura) { return a.status === StatusAssinatura.Cancelada || a.status === StatusAssinatura.Expirada; }

  rotuloStatus(s: StatusAssinatura): string {
    return ({
      [StatusAssinatura.Trial]: 'Em teste',
      [StatusAssinatura.Ativa]: 'Ativa',
      [StatusAssinatura.Atrasada]: 'Pagamento atrasado',
      [StatusAssinatura.ReadOnly]: 'Somente leitura',
      [StatusAssinatura.Cancelada]: 'Cancelada',
      [StatusAssinatura.Expirada]: 'Expirada'
    } as any)[s] || `Status ${s}`;
  }
}
