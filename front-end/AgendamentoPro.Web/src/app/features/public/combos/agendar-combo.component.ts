import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, MAT_DATE_LOCALE } from '@angular/material/core';
import { ApiService } from '../../../core/services/api.service';
import { Combo } from '../../../core/models/combo.model';

@Component({
  selector: 'app-agendar-combo',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatProgressSpinnerModule,
    MatDatepickerModule, MatNativeDateModule, CurrencyPipe],
  providers: [{ provide: MAT_DATE_LOCALE, useValue: 'pt-BR' }],
  template: `
    <div class="container">
      <div *ngIf="carregando(); else conteudo" class="centro">
        <mat-spinner></mat-spinner>
      </div>

      <ng-template #conteudo>
        <header class="topo" *ngIf="combo() as c">
          <h1><mat-icon>local_offer</mat-icon> {{ c.nome }}</h1>
          <p *ngIf="c.descricao">{{ c.descricao }}</p>
          <div class="resumo">
            <span class="orig" *ngIf="c.economia > 0">{{ c.precoOriginal | currency:'BRL' }}</span>
            <strong>{{ c.precoPromocional | currency:'BRL' }}</strong>
            <span class="econ" *ngIf="c.economia > 0">Você economiza {{ c.economia | currency:'BRL' }}</span>
          </div>
          <ul class="servicos">
            <li *ngFor="let s of c.servicos">
              <mat-icon>check_circle</mat-icon>
              <span>{{ s.nome }} ({{ s.duracaoMinutos }}min)</span>
            </li>
          </ul>
          <p class="duracao-total">
            <mat-icon>schedule</mat-icon>
            Duração total: {{ duracaoTotal() }} minutos
          </p>
        </header>

        <form (ngSubmit)="agendar()" class="form">
          <h3>Quando?</h3>
          <div class="row">
            <mat-form-field appearance="outline">
              <mat-label>Data</mat-label>
              <input matInput [matDatepicker]="picker" [(ngModel)]="form.dataObj" name="data"
                [min]="hoje" required />
              <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
              <mat-datepicker #picker></mat-datepicker>
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Hora de início</mat-label>
              <input matInput type="time" [(ngModel)]="form.horaInicio" name="hora" required />
            </mat-form-field>
          </div>

          <h3>Seus dados</h3>
          <mat-form-field appearance="outline" class="full">
            <mat-label>Nome</mat-label>
            <input matInput [(ngModel)]="form.cliente.nome" name="nome" maxlength="100" required />
          </mat-form-field>
          <div class="row">
            <mat-form-field appearance="outline">
              <mat-label>Telefone / WhatsApp</mat-label>
              <input matInput [(ngModel)]="form.cliente.telefone" name="tel" required />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>E-mail (opcional)</mat-label>
              <input matInput type="email" [(ngModel)]="form.cliente.email" name="email" />
            </mat-form-field>
          </div>

          <h3>Pagamento</h3>
          <mat-form-field appearance="outline" class="full">
            <mat-label>Forma de pagamento</mat-label>
            <mat-select [(ngModel)]="form.formaPagamento" name="forma">
              <mat-option [value]="1">PIX</mat-option>
              <mat-option [value]="2">Cartão de crédito</mat-option>
            </mat-select>
            <mat-hint>Sinal de {{ percentEntrada }}% será cobrado agora.</mat-hint>
          </mat-form-field>

          <div class="acoes">
            <button mat-flat-button color="primary" type="submit" [disabled]="enviando() || !valido()">
              <mat-icon>event</mat-icon>
              {{ enviando() ? 'Processando...' : 'Confirmar agendamento' }}
            </button>
          </div>
        </form>
      </ng-template>
    </div>
  `,
  styles: [`
    .container { max-width: 36rem; margin: 0 auto; padding: 1rem; }
    .centro { display: flex; justify-content: center; padding: 4rem; }
    .topo { background: #fff; padding: 1.25rem; border-radius: 0.5rem; margin-bottom: 1rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .resumo { display: flex; gap: 0.5rem; align-items: baseline; margin: 0.75rem 0; }
    .resumo .orig { text-decoration: line-through; color: #999; }
    .resumo strong { font-size: 1.5rem; color: #2e7d32; }
    .resumo .econ { background: #e8f5e9; color: #2e7d32; padding: 0.15rem 0.5rem; border-radius: 0.4rem; font-size: 0.85rem; }
    .servicos { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.25rem; }
    .servicos li { display: flex; align-items: center; gap: 0.5rem; }
    .servicos mat-icon { color: #2e7d32; font-size: 1rem; width: 1rem; height: 1rem; }
    .duracao-total { display: flex; align-items: center; gap: 0.25rem; color: #555; margin: 0.75rem 0 0; font-size: 0.9rem; }
    .form { background: #fff; padding: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
    .form h3 { margin: 0.5rem 0 0.25rem; font-size: 1rem; color: #555; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; }
    .full { width: 100%; }
    mat-form-field { width: 100%; }
    .acoes { display: flex; justify-content: flex-end; margin-top: 1rem; }
    @media (max-width: 30rem) { .row { grid-template-columns: 1fr; } }
  `]
})
export class AgendarComboComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  slug = '';
  comboId = 0;
  combo = signal<Combo | null>(null);
  carregando = signal(true);
  enviando = signal(false);
  hoje = new Date();
  percentEntrada = 20;

  form: any = {
    dataObj: this.amanha(),
    horaInicio: '09:00',
    cliente: { nome: '', telefone: '', email: '' },
    formaPagamento: 1
  };

  duracaoTotal() {
    return this.combo()?.servicos.reduce((s, x) => s + x.duracaoMinutos, 0) || 0;
  }

  valido() {
    return !!this.form.dataObj && !!this.form.horaInicio
      && !!this.form.cliente.nome && !!this.form.cliente.telefone;
  }

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    this.comboId = +(this.route.snapshot.paramMap.get('comboId') || 0);
    this.api.combosPublicos(this.slug).subscribe({
      next: list => {
        this.combo.set(list.find(c => c.id === this.comboId) || null);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  agendar() {
    if (!this.valido()) return;
    this.enviando.set(true);
    const payload = {
      data: this.dataIso(),
      horaInicio: this.form.horaInicio + ':00',
      cliente: this.form.cliente,
      formaPagamento: this.form.formaPagamento
    };
    this.api.agendarCombo(this.slug, this.comboId, payload).subscribe({
      next: r => {
        this.enviando.set(false);
        const primeiroId = r.agendamentos?.[0]?.id;
        if (primeiroId) {
          this.router.navigate(['/t', this.slug, 'pagamento', primeiroId]);
        } else {
          this.snack.open('Agendamento criado!', 'OK', { duration: 3000 });
        }
      },
      error: e => {
        this.enviando.set(false);
        this.snack.open(e.error?.message || 'Falha ao agendar combo.', 'OK',
          { duration: 5000, panelClass: 'snack-erro' });
      }
    });
  }

  private amanha(): Date {
    const d = new Date();
    d.setDate(d.getDate() + 1);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  private dataIso(): string {
    const d = this.form.dataObj as Date;
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
