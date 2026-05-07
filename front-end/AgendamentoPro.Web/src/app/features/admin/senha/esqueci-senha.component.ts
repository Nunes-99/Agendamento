import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-esqueci-senha',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule],
  template: `
    <div class="container">
      <div class="card">
        <header>
          <mat-icon>lock_reset</mat-icon>
          <h1>Esqueceu sua senha?</h1>
          <p>Informe o e-mail cadastrado. Se houver conta vinculada, você receberá o link de redefinição.</p>
        </header>

        <form *ngIf="!enviado(); else sucesso" (ngSubmit)="solicitar()">
          <mat-form-field appearance="outline" class="full">
            <mat-label>E-mail</mat-label>
            <input matInput type="email" [(ngModel)]="email" name="email" required />
          </mat-form-field>

          <button mat-flat-button color="primary" type="submit" class="full" [disabled]="enviando() || !email">
            <mat-icon>send</mat-icon>
            {{ enviando() ? 'Enviando...' : 'Enviar link de redefinição' }}
          </button>

          <a mat-button routerLink="/admin/login" class="voltar">
            <mat-icon>arrow_back</mat-icon> Voltar para o login
          </a>
        </form>

        <ng-template #sucesso>
          <div class="sucesso">
            <mat-icon class="ok">check_circle</mat-icon>
            <h2>Solicitação registrada</h2>
            <p>Se houver uma conta com este e-mail, o operador entrará em contato com o link
              de redefinição. Esse link expira em 1 hora.</p>
            <a *ngIf="devLink()" [href]="devLink()" class="dev-link">
              [DEV] Abrir link de reset
            </a>
            <a mat-stroked-button routerLink="/admin/login">Voltar para o login</a>
          </div>
        </ng-template>
      </div>
    </div>
  `,
  styles: [`
    .container { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 1rem; background: #f5f5f5; }
    .card { background: #fff; padding: 2rem; border-radius: 0.75rem; max-width: 24rem; width: 100%; box-shadow: 0 2px 10px rgba(0,0,0,0.08); }
    header { text-align: center; margin-bottom: 1.5rem; }
    header mat-icon { font-size: 3rem; width: 3rem; height: 3rem; color: #6366f1; }
    header h1 { margin: 0.5rem 0 0; font-size: 1.5rem; }
    header p { color: #555; margin: 0.5rem 0 0; }
    .full { width: 100%; }
    .voltar { display: flex; justify-content: center; margin-top: 0.5rem; }
    .sucesso { text-align: center; }
    .sucesso .ok { color: #2e7d32; font-size: 3rem; width: 3rem; height: 3rem; }
    .sucesso h2 { margin: 0.5rem 0; }
    .sucesso a { display: inline-block; margin-top: 1rem; }
    .dev-link { display: block; margin: 0.5rem 0; color: #d97706; font-size: 0.85rem; word-break: break-all; }
  `]
})
export class EsqueciSenhaComponent {
  private http = inject(HttpClient);
  private snack = inject(MatSnackBar);

  email = '';
  enviando = signal(false);
  enviado = signal(false);
  devLink = signal<string | null>(null);

  solicitar() {
    if (!this.email) return;
    this.enviando.set(true);
    this.http.post<any>(`${environment.apiUrl}/auth/forgot-password`, { email: this.email }).subscribe({
      next: r => {
        this.enviando.set(false);
        this.enviado.set(true);
        if (r?.devLink) this.devLink.set(r.devLink);
      },
      error: e => {
        this.enviando.set(false);
        this.snack.open(e.error?.message || 'Falha ao processar solicitação.', 'OK',
          { duration: 4000, panelClass: 'snack-erro' });
      }
    });
  }
}
