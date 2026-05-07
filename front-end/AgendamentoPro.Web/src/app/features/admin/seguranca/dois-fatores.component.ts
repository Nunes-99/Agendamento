import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-dois-fatores',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule],
  template: `
    <header class="topo">
      <h1><mat-icon>shield</mat-icon> Autenticação em 2 fatores (TOTP)</h1>
      <p>Adicione uma camada extra de segurança usando Google Authenticator, Authy, 1Password ou similar.</p>
    </header>

    <section class="card" *ngIf="!setupAtivo() && !ativo()">
      <h2>Ativar 2FA</h2>
      <p>Clique abaixo para gerar seu código secreto, depois escaneie o QR no app autenticador.</p>
      <button mat-flat-button color="primary" (click)="iniciar()" [disabled]="carregando()">
        <mat-icon>play_arrow</mat-icon> Iniciar configuração
      </button>
    </section>

    <section class="card" *ngIf="setupAtivo()">
      <h2>1️⃣ Escaneie o QR Code</h2>
      <div class="qr-area">
        <img [src]="qrImageUrl()" alt="QR Code 2FA" *ngIf="qrImageUrl()" />
        <div class="manual">
          <p><strong>Não consegue escanear?</strong> Adicione manualmente:</p>
          <code>{{ secret() }}</code>
        </div>
      </div>

      <h2>2️⃣ Confirme o código</h2>
      <p>Digite o código de 6 dígitos exibido no app:</p>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Código de 6 dígitos</mat-label>
          <input matInput [(ngModel)]="codigo" maxlength="6" inputmode="numeric" />
        </mat-form-field>
        <button mat-flat-button color="primary" (click)="confirmar()" [disabled]="codigo.length !== 6 || carregando()">
          <mat-icon>check</mat-icon> Confirmar e ativar
        </button>
      </div>
    </section>

    <section class="card" *ngIf="ativo() && !setupAtivo()">
      <div class="status ok">
        <mat-icon>verified_user</mat-icon>
        <div>
          <strong>2FA está ativo</strong>
          <span>A cada login você precisará informar o código do app autenticador.</span>
        </div>
      </div>

      <h3>Desativar 2FA</h3>
      <p>Para desativar, informe um código atual válido:</p>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Código atual</mat-label>
          <input matInput [(ngModel)]="codigo" maxlength="6" inputmode="numeric" />
        </mat-form-field>
        <button mat-stroked-button color="warn" (click)="desativar()" [disabled]="codigo.length !== 6 || carregando()">
          <mat-icon>shield_off</mat-icon> Desativar 2FA
        </button>
      </div>
    </section>
  `,
  styles: [`
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .card { background: #fff; padding: 1rem 1.25rem; margin-top: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
    .qr-area { display: flex; flex-wrap: wrap; gap: 1.5rem; align-items: center; padding: 1rem 0; }
    .qr-area img { width: 200px; height: 200px; }
    .manual { max-width: 18rem; }
    .manual code { display: block; padding: 0.5rem; background: #f5f5f5; border-radius: 0.25rem; word-break: break-all; }
    .row { display: flex; gap: 0.5rem; align-items: center; flex-wrap: wrap; }
    .status { display: flex; align-items: center; gap: 0.75rem; padding: 0.75rem; border-radius: 0.5rem; }
    .status.ok { background: #e8f5e9; color: #2e7d32; }
    .status mat-icon { font-size: 2rem; width: 2rem; height: 2rem; }
    .status div { display: flex; flex-direction: column; }
  `]
})
export class DoisFatoresComponent {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  carregando = signal(false);
  setupAtivo = signal(false);
  ativo = signal(false);
  secret = signal('');
  qrImageUrl = signal('');
  codigo = '';

  iniciar() {
    this.carregando.set(true);
    this.api.iniciar2FA().subscribe({
      next: r => {
        this.carregando.set(false);
        this.secret.set(r.secret);
        // Renderiza QR via API pública (sem dependência adicional). Para produção,
        // usar lib local (qrcode/angularx-qrcode) pra não vazar URL ao serviço externo.
        this.qrImageUrl.set(`https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(r.otpauthUrl)}`);
        this.setupAtivo.set(true);
        this.ativo.set(r.ativo);
      },
      error: e => { this.carregando.set(false); this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 4000 }); }
    });
  }

  confirmar() {
    this.carregando.set(true);
    this.api.confirmar2FA(this.codigo).subscribe({
      next: r => {
        this.carregando.set(false);
        this.ativo.set(r.ativo);
        this.setupAtivo.set(false);
        this.codigo = '';
        this.snack.open('2FA ativado com sucesso!', 'OK', { duration: 3000 });
      },
      error: e => { this.carregando.set(false); this.snack.open(e.error?.message || 'Código inválido', 'OK', { duration: 4000 }); }
    });
  }

  desativar() {
    this.carregando.set(true);
    this.api.desativar2FA(this.codigo).subscribe({
      next: r => {
        this.carregando.set(false);
        this.ativo.set(r.ativo);
        this.codigo = '';
        this.snack.open('2FA desativado.', 'OK', { duration: 3000 });
      },
      error: e => { this.carregando.set(false); this.snack.open(e.error?.message || 'Código inválido', 'OK', { duration: 4000 }); }
    });
  }
}
