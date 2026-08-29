import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, MAT_DATE_LOCALE } from '@angular/material/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { Servico } from '../../../core/models/servico.model';

@Component({
  selector: 'app-lista-espera-publica',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatDatepickerModule, MatNativeDateModule],
  providers: [{ provide: MAT_DATE_LOCALE, useValue: 'pt-BR' }],
  template: `
    <div class="container">
      <h1><mat-icon>hourglass_top</mat-icon> Entrar na lista de espera</h1>
      <p>Sem horário disponível na data desejada? Cadastre-se aqui e te avisamos por WhatsApp se algum cliente cancelar.</p>

      <ng-container *ngIf="!confirmado(); else sucesso">
        <section class="card">
          <mat-form-field appearance="outline" class="full">
            <mat-label>Serviço</mat-label>
            <mat-select [(ngModel)]="form.servicoId">
              <mat-option *ngFor="let s of servicos()" [value]="s.id">{{ s.nome }}</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full">
            <mat-label>Data desejada</mat-label>
            <input matInput [matDatepicker]="p" [(ngModel)]="form.dataDesejada" [min]="hoje" />
            <mat-datepicker-toggle matIconSuffix [for]="p"></mat-datepicker-toggle>
            <mat-datepicker #p></mat-datepicker>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full">
            <mat-label>Seu nome</mat-label>
            <input matInput [(ngModel)]="form.clienteNome" required />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full">
            <mat-label>WhatsApp</mat-label>
            <input matInput [(ngModel)]="form.clienteTelefone" placeholder="11999999999" required />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full">
            <mat-label>E-mail (opcional)</mat-label>
            <input matInput type="email" [(ngModel)]="form.clienteEmail" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full">
            <mat-label>Observação (opcional)</mat-label>
            <textarea matInput rows="2" [(ngModel)]="form.observacao" maxlength="200"></textarea>
          </mat-form-field>

          <button mat-flat-button color="primary" [disabled]="!valido() || enviando()" (click)="enviar()">
            <mat-icon>add_alert</mat-icon> Entrar na lista
          </button>
        </section>
      </ng-container>

      <ng-template #sucesso>
        <section class="card sucesso">
          <mat-icon>check_circle</mat-icon>
          <h2>Você está na lista!</h2>
          <p>Sua posição: <strong>#{{ posicao() }}</strong></p>
          <p>Te avisamos por WhatsApp assim que houver vaga.</p>
          <button mat-stroked-button (click)="voltar()">
            <mat-icon>arrow_back</mat-icon> Voltar
          </button>
        </section>
      </ng-template>
    </div>
  `,
  styles: [`
    .container { max-width: 32rem; margin: 1rem auto; padding: 1rem; }
    h1 { display: flex; align-items: center; gap: 0.5rem; }
    .card { background: var(--cor-fundo-card); padding: 1rem 1.25rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); display: flex; flex-direction: column; gap: 0.25rem; }
    .full { width: 100%; }
    .sucesso { text-align: center; padding: 2rem; }
    .sucesso mat-icon { font-size: 4rem; width: 4rem; height: 4rem; color: #2e7d32; }
    .sucesso h2 { margin: 0.5rem 0; }
  `]
})
export class ListaEsperaPublicaComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snack = inject(MatSnackBar);

  slug = '';
  hoje = new Date();
  servicos = signal<Servico[]>([]);
  enviando = signal(false);
  confirmado = signal(false);
  posicao = signal(0);

  form: any = {
    servicoId: null, dataDesejada: null,
    clienteNome: '', clienteTelefone: '', clienteEmail: '', observacao: ''
  };

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    this.api.servicosPublicos(this.slug).subscribe(s => this.servicos.set(s));
  }

  valido(): boolean {
    return this.form.servicoId && this.form.dataDesejada
      && this.form.clienteNome && this.form.clienteTelefone;
  }

  enviar() {
    this.enviando.set(true);
    const payload = {
      ...this.form,
      dataDesejada: (this.form.dataDesejada as Date).toISOString().substring(0, 10)
    };
    this.api.entrarListaEspera(this.slug, payload).subscribe({
      next: (r: any) => {
        this.enviando.set(false);
        this.posicao.set(r.posicao);
        this.confirmado.set(true);
      },
      error: e => {
        this.enviando.set(false);
        this.snack.open(e.error?.message || 'Falha ao entrar na lista', 'OK', { duration: 4000 });
      }
    });
  }

  voltar() { this.router.navigate(['/t', this.slug]); }
}
