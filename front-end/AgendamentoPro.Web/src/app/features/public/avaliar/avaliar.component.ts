import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { Avaliacao } from '../../../core/models/avaliacao.model';

type Estado = 'carregando' | 'pronto' | 'jaRespondido' | 'naoEncontrado' | 'enviando' | 'concluido';

@Component({
  selector: 'app-avaliar',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule],
  template: `
    <div class="container">
      <ng-container [ngSwitch]="estado()">
        <div *ngSwitchCase="'carregando'" class="centro">
          <mat-spinner></mat-spinner>
        </div>

        <div *ngSwitchCase="'naoEncontrado'" class="centro mensagem">
          <mat-icon class="icone-grande">link_off</mat-icon>
          <h2>Link inválido</h2>
          <p>Esta avaliação não existe ou expirou.</p>
        </div>

        <div *ngSwitchCase="'jaRespondido'" class="centro mensagem">
          <mat-icon class="icone-grande sucesso">check_circle</mat-icon>
          <h2>Avaliação registrada</h2>
          <p>Você já avaliou este atendimento. Obrigado!</p>
        </div>

        <div *ngSwitchCase="'concluido'" class="centro mensagem">
          <mat-icon class="icone-grande sucesso">favorite</mat-icon>
          <h2>Obrigado pelo feedback!</h2>
          <p>Sua avaliação foi registrada.</p>
        </div>

        <form *ngSwitchDefault (ngSubmit)="enviar()" class="form">
          <div class="cabecalho">
            <mat-icon class="icone-grande">star</mat-icon>
            <h2>Como foi seu atendimento?</h2>
            <p *ngIf="avaliacao()?.clienteNome as nome">Olá, {{ nome }}!</p>
          </div>

          <div class="estrelas">
            <button type="button" *ngFor="let n of [1,2,3,4,5]"
              mat-icon-button (click)="nota.set(n)"
              [attr.aria-label]="n + ' estrelas'">
              <mat-icon class="estrela" [class.ativa]="n <= nota()">
                {{ n <= nota() ? 'star' : 'star_border' }}
              </mat-icon>
            </button>
          </div>
          <p class="legenda" *ngIf="nota() > 0">{{ legendaNota() }}</p>

          <mat-form-field appearance="outline" class="full">
            <mat-label>Comentário (opcional)</mat-label>
            <textarea matInput rows="4" [(ngModel)]="comentario" name="comentario"
              maxlength="1000" placeholder="Conte como foi sua experiência..."></textarea>
          </mat-form-field>

          <button mat-flat-button color="primary" type="submit"
            class="full" [disabled]="nota() < 1 || estado() === 'enviando'">
            <mat-icon>send</mat-icon>
            {{ estado() === 'enviando' ? 'Enviando...' : 'Enviar avaliação' }}
          </button>
        </form>
      </ng-container>
    </div>
  `,
  styles: [`
    .container {
      max-width: 28rem;
      margin: 0 auto;
      padding: 2rem 1rem;
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .centro { display: flex; flex-direction: column; align-items: center; gap: 1rem; }
    .mensagem { text-align: center; }
    .mensagem h2 { margin: 0; }
    .icone-grande { font-size: 4rem; width: 4rem; height: 4rem; }
    .icone-grande.sucesso { color: #2e7d32; }
    .form { display: flex; flex-direction: column; gap: 1rem; width: 100%; }
    .cabecalho { text-align: center; }
    .cabecalho h2 { margin: 0.5rem 0 0; }
    .estrelas { display: flex; justify-content: center; gap: 0.25rem; }
    .estrela { font-size: 2.5rem; width: 2.5rem; height: 2.5rem; color: #ccc; }
    .estrela.ativa { color: #fbc02d; }
    .legenda { text-align: center; margin: 0; color: #666; font-weight: 500; }
    .full { width: 100%; }
  `]
})
export class AvaliarComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  token = '';
  estado = signal<Estado>('carregando');
  avaliacao = signal<Avaliacao | null>(null);
  nota = signal<number>(0);
  comentario = '';

  legendaNota() {
    const labels = ['', 'Péssimo', 'Ruim', 'Regular', 'Bom', 'Excelente'];
    return labels[this.nota()] || '';
  }

  ngOnInit() {
    this.token = this.route.snapshot.paramMap.get('token') || '';
    if (!this.token) { this.estado.set('naoEncontrado'); return; }

    this.api.buscarAvaliacaoPorToken(this.token).subscribe({
      next: a => {
        this.avaliacao.set(a);
        if (a.respondidoEm) this.estado.set('jaRespondido');
        else this.estado.set('pronto');
      },
      error: () => this.estado.set('naoEncontrado')
    });
  }

  enviar() {
    if (this.nota() < 1) return;
    this.estado.set('enviando');
    this.api.responderAvaliacao(this.token, {
      nota: this.nota(),
      comentario: this.comentario
    }).subscribe({
      next: () => this.estado.set('concluido'),
      error: e => {
        this.estado.set('pronto');
        this.snack.open(e.error?.message || 'Falha ao enviar.', 'OK',
          { duration: 4000, panelClass: 'snack-erro' });
      }
    });
  }
}
