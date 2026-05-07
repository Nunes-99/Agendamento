import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-redefinir-senha',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule],
  template: `
    <div class="container">
      <div class="card">
        <header>
          <mat-icon>lock_open</mat-icon>
          <h1>Redefinir senha</h1>
          <p *ngIf="!concluido()">Defina uma nova senha. Mínimo 8 caracteres.</p>
        </header>

        <form *ngIf="!concluido(); else sucesso" (ngSubmit)="redefinir()">
          <mat-form-field appearance="outline" class="full">
            <mat-label>Nova senha</mat-label>
            <input matInput [type]="ver() ? 'text' : 'password'" [(ngModel)]="senha" name="s"
              required minlength="8" maxlength="200" />
            <button mat-icon-button matSuffix type="button" (click)="ver.set(!ver())">
              <mat-icon>{{ ver() ? 'visibility_off' : 'visibility' }}</mat-icon>
            </button>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full">
            <mat-label>Confirmar nova senha</mat-label>
            <input matInput [type]="ver() ? 'text' : 'password'" [(ngModel)]="confirmar" name="c" required />
          </mat-form-field>

          <button mat-flat-button color="primary" type="submit" class="full"
            [disabled]="enviando() || !valido()">
            <mat-icon>save</mat-icon>
            {{ enviando() ? 'Salvando...' : 'Redefinir senha' }}
          </button>
        </form>

        <ng-template #sucesso>
          <div class="ok">
            <mat-icon class="check">check_circle</mat-icon>
            <h2>Senha redefinida!</h2>
            <p>Faça login com a nova senha.</p>
            <a mat-flat-button color="primary" routerLink="/admin/login">Ir para o login</a>
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
    .ok { text-align: center; }
    .ok .check { color: #2e7d32; font-size: 3rem; width: 3rem; height: 3rem; }
    .ok h2 { margin: 0.5rem 0; }
    .ok a { margin-top: 1rem; }
  `]
})
export class RedefinirSenhaComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);
  private snack = inject(MatSnackBar);

  token = '';
  senha = '';
  confirmar = '';
  ver = signal(false);
  enviando = signal(false);
  concluido = signal(false);

  valido() {
    return this.senha.length >= 8 && this.senha === this.confirmar;
  }

  ngOnInit() {
    this.token = this.route.snapshot.queryParamMap.get('token') || '';
    if (!this.token) {
      this.snack.open('Token ausente na URL.', 'OK', { duration: 4000, panelClass: 'snack-erro' });
      this.router.navigate(['/admin/login']);
    }
  }

  redefinir() {
    if (!this.valido()) {
      this.snack.open('Senhas não conferem ou são curtas demais.', 'OK', { duration: 3000 });
      return;
    }
    this.enviando.set(true);
    this.http.post(`${environment.apiUrl}/auth/reset-password`, {
      token: this.token,
      novaSenha: this.senha
    }).subscribe({
      next: () => { this.enviando.set(false); this.concluido.set(true); },
      error: e => {
        this.enviando.set(false);
        this.snack.open(e.error?.message || 'Token inválido ou expirado.', 'OK',
          { duration: 5000, panelClass: 'snack-erro' });
      }
    });
  }
}
