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
import { MascaraDirective, documentoCompleto } from '../../../core/directives/mascara.directive';
import { LIMITES, emailValido, mensagemErroApi } from '../../../core/utils/validacao.util';

@Component({
  selector: 'app-lista-espera-publica',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatDatepickerModule,
    MatNativeDateModule, MascaraDirective],
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
            <input matInput [(ngModel)]="form.clienteNome" required
                   autocomplete="name" [maxlength]="limites.nome" />
            <mat-hint *ngIf="tentou() && erroNome()" class="erro">{{ erroNome() }}</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full">
            <mat-label>WhatsApp</mat-label>
            <input matInput appMascara="telefone" [(ngModel)]="form.clienteTelefone" required
                   inputmode="numeric" autocomplete="tel"
                   placeholder="(11) 98888-7777" [maxlength]="limites.telefone" />
            <mat-hint *ngIf="tentou() && erroTelefone()" class="erro">{{ erroTelefone() }}</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full">
            <mat-label>E-mail (opcional)</mat-label>
            <input matInput type="email" [(ngModel)]="form.clienteEmail"
                   autocomplete="email" inputmode="email"
                   placeholder="voce@email.com" [maxlength]="limites.email" />
            <mat-hint *ngIf="tentou() && erroEmail()" class="erro">{{ erroEmail() }}</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full">
            <mat-label>Observação (opcional)</mat-label>
            <textarea matInput rows="2" [(ngModel)]="form.observacao" maxlength="200"></textarea>
            <mat-hint align="end">{{ (form.observacao || '').length }}/200</mat-hint>
          </mat-form-field>

          <button mat-flat-button color="primary" [disabled]="enviando()" (click)="enviar()">
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
    .card { background: var(--cor-fundo-card); padding: 1rem 1.25rem; border-radius: 0.5rem; box-shadow: var(--sombra-card); display: flex; flex-direction: column; gap: 0.25rem; }
    .full { width: 100%; }
    .erro { color: var(--cor-erro) !important; }
    .sucesso { text-align: center; padding: 2rem; }
    .sucesso mat-icon { font-size: 4rem; width: 4rem; height: 4rem; color: var(--cor-sucesso); }
    .sucesso h2 { margin: 0.5rem 0; }
  `]
})
export class ListaEsperaPublicaComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snack = inject(MatSnackBar);

  readonly limites = LIMITES;

  slug = '';
  hoje = new Date();
  servicos = signal<Servico[]>([]);
  enviando = signal(false);
  confirmado = signal(false);
  posicao = signal(0);
  tentou = signal(false);

  form: any = {
    servicoId: null, dataDesejada: null,
    clienteNome: '', clienteTelefone: '', clienteEmail: '', observacao: ''
  };

  // Metodos, nao computed(): estes campos vivem num objeto comum ligado por
  // ngModel, e um computed() so reavalia quando um SIGNAL do qual ele depende
  // muda -- congelaria no resultado da primeira renderizacao (campo sempre
  // "valido", mensagem de erro que nunca aparece).
  erroNome(): string {
    const v = (this.form.clienteNome || '').trim();
    if (!v) return 'Informe seu nome.';
    return v.length < 3 ? 'Nome muito curto.' : '';
  }
  erroTelefone(): string {
    const v = (this.form.clienteTelefone || '').trim();
    if (!v) return 'Informe seu WhatsApp.';
    return documentoCompleto('telefone', v) ? '' : 'Número incompleto. Use DDD + número.';
  }
  erroEmail(): string {
    const v = (this.form.clienteEmail || '').trim();
    if (!v) return '';
    return emailValido(v) ? '' : 'E-mail inválido. Use o formato nome@dominio.com.';
  }

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    // Quem chegou aqui pelo "avise-me quando abrir" já escolheu serviço e data:
    // repetir a escolha é atrito puro.
    const q = this.route.snapshot.queryParamMap;
    const servicoId = +(q.get('servicoId') || 0);
    if (servicoId) this.form.servicoId = servicoId;
    const data = q.get('data');
    if (data) {
      const [a, m, d] = data.split('-').map(Number);
      if (a && m && d) this.form.dataDesejada = new Date(a, m - 1, d);
    }
    this.api.servicosPublicos(this.slug).subscribe(s => this.servicos.set(s));
  }

  valido(): boolean {
    return !!this.form.servicoId && !!this.form.dataDesejada
      && !this.erroNome() && !this.erroTelefone() && !this.erroEmail();
  }

  enviar() {
    this.tentou.set(true);
    if (!this.valido()) {
      this.snack.open(
        !this.form.servicoId ? 'Escolha o serviço.'
          : !this.form.dataDesejada ? 'Escolha a data desejada.'
            : 'Confira os campos destacados.',
        'OK', { duration: 4000 });
      return;
    }
    this.enviando.set(true);
    const d = this.form.dataDesejada as Date;
    const payload = {
      ...this.form,
      // toISOString() converte para UTC e no fuso do Brasil devolve o dia anterior.
      dataDesejada: `${d.getFullYear()}-${`${d.getMonth() + 1}`.padStart(2, '0')}-${`${d.getDate()}`.padStart(2, '0')}`
    };
    this.api.entrarListaEspera(this.slug, payload).subscribe({
      next: (r: any) => {
        this.enviando.set(false);
        this.posicao.set(r.posicao);
        this.confirmado.set(true);
      },
      error: e => {
        this.enviando.set(false);
        this.snack.open(mensagemErroApi(e, 'Falha ao entrar na lista.'), 'OK',
          { duration: 6000, panelClass: 'snack-erro' });
      }
    });
  }

  voltar() { this.router.navigate(['/t', this.slug]); }
}
