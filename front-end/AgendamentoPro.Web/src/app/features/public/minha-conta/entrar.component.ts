import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { ClienteAuthService } from '../../../core/services/cliente-auth.service';

type Etapa = 'telefone' | 'codigo';

@Component({
  selector: 'app-entrar-cliente',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule],
  template: `
    <div class="container">
      <h1><mat-icon>account_circle</mat-icon> Acessar Minha Conta</h1>
      <p>Entre com seu telefone — enviamos um código de 6 dígitos via WhatsApp.</p>

      <section class="card" *ngIf="etapa() === 'telefone'">
        <mat-form-field appearance="outline" class="full">
          <mat-label>Seu telefone (com DDD)</mat-label>
          <input matInput [(ngModel)]="telefone" placeholder="11999999999" inputmode="numeric" maxlength="14" />
          <mat-hint>Apenas números, com DDD.</mat-hint>
        </mat-form-field>
        <button mat-flat-button color="primary" [disabled]="!validoTelefone() || enviando()" (click)="solicitar()">
          <mat-icon *ngIf="!enviando()">send</mat-icon>
          <mat-spinner *ngIf="enviando()" [diameter]="20"></mat-spinner>
          Receber código
        </button>
      </section>

      <section class="card" *ngIf="etapa() === 'codigo'">
        <p>Código enviado para <strong>{{ telefone }}</strong>.</p>
        <p *ngIf="codigoDev()" class="dev-aviso">
          <mat-icon>info</mat-icon>
          Modo dev — código: <code>{{ codigoDev() }}</code>
        </p>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Código de 6 dígitos</mat-label>
          <input matInput [(ngModel)]="codigo" maxlength="6" inputmode="numeric" />
        </mat-form-field>
        <div class="acoes">
          <button mat-button (click)="voltar()" [disabled]="enviando()">
            <mat-icon>arrow_back</mat-icon> Trocar telefone
          </button>
          <button mat-flat-button color="primary"
            [disabled]="codigo.length !== 6 || enviando()" (click)="validar()">
            <mat-icon *ngIf="!enviando()">login</mat-icon>
            <mat-spinner *ngIf="enviando()" [diameter]="20"></mat-spinner>
            Entrar
          </button>
        </div>
        <button mat-button (click)="reenviar()" [disabled]="cooldown() > 0">
          <mat-icon>refresh</mat-icon>
          Reenviar código <span *ngIf="cooldown() > 0">({{ cooldown() }}s)</span>
        </button>
      </section>
    </div>
  `,
  styles: [`
    .container { max-width: 28rem; margin: 1rem auto; padding: 1rem; }
    h1 { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.25rem; }
    .card { background: var(--cor-fundo-card); padding: 1.5rem; border-radius: 0.75rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); display: flex; flex-direction: column; gap: 0.5rem; }
    .full { width: 100%; }
    .acoes { display: flex; gap: 0.5rem; justify-content: space-between; }
    .dev-aviso { background: var(--cor-fundo-card)de7; padding: 0.5rem 0.75rem; border-radius: 0.5rem; display: flex; align-items: center; gap: 0.5rem; font-size: 0.85rem; }
    .dev-aviso code { background: var(--cor-fundo-card); padding: 0.15rem 0.4rem; border-radius: 0.25rem; font-family: monospace; }
  `]
})
export class EntrarClienteComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(ClienteAuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snack = inject(MatSnackBar);

  slug = '';
  etapa = signal<Etapa>('telefone');
  telefone = '';
  codigo = '';
  enviando = signal(false);
  cooldown = signal(0);
  codigoDev = signal<string | null>(null);

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    if (this.auth.autenticado(this.slug)) this.router.navigate(['/t', this.slug, 'minha-conta']);
  }

  validoTelefone(): boolean {
    const limpo = this.telefone.replace(/\D/g, '');
    return limpo.length >= 10 && limpo.length <= 13;
  }

  solicitar() {
    this.enviando.set(true);
    const tel = this.telefone.replace(/\D/g, '');
    this.api.solicitarOtp(this.slug, tel).subscribe({
      next: r => {
        this.enviando.set(false);
        if (!r.enviado && r.cooldownSegundos > 0) {
          this.snack.open(`Aguarde ${r.cooldownSegundos}s para tentar novamente`, 'OK', { duration: 4000 });
          return;
        }
        if (!r.enviado) {
          this.snack.open('Não foi possível enviar o código. Tente mais tarde.', 'OK', { duration: 4000 });
          return;
        }
        this.codigoDev.set(r.codigoDev || null);
        this.iniciarCooldown(r.cooldownSegundos);
        this.etapa.set('codigo');
      },
      error: () => {
        this.enviando.set(false);
        this.snack.open('Falha ao solicitar código.', 'OK', { duration: 4000 });
      }
    });
  }

  validar() {
    this.enviando.set(true);
    const tel = this.telefone.replace(/\D/g, '');
    this.api.validarOtp(this.slug, tel, this.codigo).subscribe({
      next: r => {
        this.enviando.set(false);
        if (!r.valido) {
          this.snack.open(r.mensagem || 'Código inválido', 'OK', { duration: 4000 });
          return;
        }
        this.auth.salvar({
          slug: this.slug,
          clienteId: r.clienteId,
          clienteNome: r.clienteNome,
          token: r.token,
          expiracao: r.expiracao
        });
        this.router.navigate(['/t', this.slug, 'minha-conta']);
      },
      error: e => {
        this.enviando.set(false);
        this.snack.open(e.error?.mensagem || 'Código inválido', 'OK', { duration: 4000 });
      }
    });
  }

  voltar() { this.etapa.set('telefone'); this.codigo = ''; this.codigoDev.set(null); }
  reenviar() { this.solicitar(); }

  private iniciarCooldown(segundos: number) {
    this.cooldown.set(segundos);
    const timer = setInterval(() => {
      this.cooldown.update(v => v - 1);
      if (this.cooldown() <= 0) clearInterval(timer);
    }, 1000);
  }
}
