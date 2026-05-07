import { Component, Inject, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, DateAdapter, MAT_DATE_LOCALE, MAT_DATE_FORMATS } from '@angular/material/core';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { Agendamento, SlotDisponivel } from '../../../core/models/agendamento.model';
import { Servico } from '../../../core/models/servico.model';

export interface AgendamentoDialogData {
  modo: 'novo' | 'reagendar';
  agendamento?: Agendamento;
}

@Component({
  selector: 'app-agendamento-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatDialogModule,
    MatProgressSpinnerModule, MatTabsModule, MatDatepickerModule, MatNativeDateModule],
  providers: [
    { provide: MAT_DATE_LOCALE, useValue: 'pt-BR' },
    { provide: MAT_DATE_FORMATS, useValue: {
      parse: { dateInput: 'DD/MM/YYYY' },
      display: {
        dateInput: { day: '2-digit', month: '2-digit', year: 'numeric' },
        monthYearLabel: { year: 'numeric', month: 'short' },
        dateA11yLabel: { year: 'numeric', month: 'long', day: 'numeric' },
        monthYearA11yLabel: { year: 'numeric', month: 'long' }
      }
    }}
  ],
  template: `
    <h2 mat-dialog-title>
      <mat-icon>{{ data.modo === 'novo' ? 'add_circle' : 'edit_calendar' }}</mat-icon>
      {{ data.modo === 'novo' ? 'Novo agendamento' : 'Editar agendamento' }}
    </h2>

    <mat-dialog-content class="conteudo">

      <!-- ===== Reagendar: somente nova data/hora ===== -->
      <ng-container *ngIf="data.modo === 'reagendar'">
        <p class="ajuda">
          <strong>{{ data.agendamento?.clienteNome }}</strong>
          • {{ data.agendamento?.servicoNome }}
        </p>

        <div class="grid">
          <mat-form-field appearance="outline">
            <mat-label>Nova data</mat-label>
            <input matInput [matDatepicker]="picker1" [(ngModel)]="form.dataObj"
              (ngModelChange)="onDataChange()" [min]="hojeDate" required />
            <mat-datepicker-toggle matIconSuffix [for]="picker1"></mat-datepicker-toggle>
            <mat-datepicker #picker1></mat-datepicker>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Novo horário</mat-label>
            <mat-select [(ngModel)]="form.horaInicio" (ngModelChange)="onHorarioSelecionado()"
              [disabled]="carregandoSlots() || !slotsUnicos().length" required>
              <mat-option *ngFor="let s of slotsUnicos()" [value]="s.horaInicio.substring(0,5)">
                {{ s.horaInicio.substring(0,5) }} → {{ s.horaFim.substring(0,5) }}
              </mat-option>
            </mat-select>
            <mat-hint *ngIf="carregandoSlots()">Carregando horários...</mat-hint>
            <mat-hint *ngIf="!carregandoSlots() && !slotsUnicos().length">
              Nenhum horário disponível nessa data
            </mat-hint>
          </mat-form-field>
        </div>
      </ng-container>

      <!-- ===== Novo agendamento ===== -->
      <ng-container *ngIf="data.modo === 'novo'">
        <h3>Serviço</h3>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Serviço</mat-label>
          <mat-select [(ngModel)]="form.servicoId" (ngModelChange)="onServicoChange()" required>
            <mat-option *ngFor="let s of servicos()" [value]="s.id">
              {{ s.nome }} — R$ {{ s.preco | number:'1.2-2' }} ({{ s.duracaoMinutos }} min)
            </mat-option>
          </mat-select>
        </mat-form-field>

        <h3>Data e horário</h3>
        <div class="grid">
          <mat-form-field appearance="outline">
            <mat-label>Data</mat-label>
            <input matInput [matDatepicker]="picker2" [(ngModel)]="form.dataObj"
              (ngModelChange)="onDataChange()" [min]="hojeDate" required />
            <mat-datepicker-toggle matIconSuffix [for]="picker2"></mat-datepicker-toggle>
            <mat-datepicker #picker2></mat-datepicker>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Horário</mat-label>
            <mat-select [(ngModel)]="form.horaInicio" (ngModelChange)="onHorarioSelecionado()"
              [disabled]="!form.servicoId || carregandoSlots() || !slotsUnicos().length" required>
              <mat-option *ngFor="let s of slotsUnicos()" [value]="s.horaInicio.substring(0,5)">
                {{ s.horaInicio.substring(0,5) }} → {{ s.horaFim.substring(0,5) }}
              </mat-option>
            </mat-select>
            <mat-hint *ngIf="!form.servicoId">Selecione um serviço primeiro</mat-hint>
            <mat-hint *ngIf="form.servicoId && carregandoSlots()">Carregando horários...</mat-hint>
            <mat-hint *ngIf="form.servicoId && !carregandoSlots() && !slotsUnicos().length">
              Nenhum horário disponível
            </mat-hint>
            <mat-hint *ngIf="form.servicoId && !carregandoSlots() && slotsUnicos().length"
              align="end">Duração: {{ duracaoServico() }} min</mat-hint>
          </mat-form-field>
        </div>

        <mat-form-field appearance="outline" class="full">
          <mat-label>Recurso (box / profissional / sala)</mat-label>
          <mat-select [(ngModel)]="form.recursoId" [disabled]="!recursosDisponiveis().length" required>
            <mat-option *ngFor="let r of recursosDisponiveis()" [value]="r.recursoId">
              {{ r.recursoNome }}
            </mat-option>
          </mat-select>
          <mat-hint *ngIf="!form.horaInicio">Selecione um horário primeiro</mat-hint>
          <mat-hint *ngIf="form.horaInicio && recursosDisponiveis().length === 1">
            Apenas um recurso livre nesse horário
          </mat-hint>
        </mat-form-field>

        <h3>Cliente</h3>
        <mat-tab-group [(selectedIndex)]="abaCliente" class="abas-cliente">
          <mat-tab label="Cliente novo">
            <div class="grid abas-content">
              <mat-form-field appearance="outline" class="full">
                <mat-label>Nome</mat-label>
                <input matInput [(ngModel)]="form.cliente.nome" required maxlength="100" />
                <mat-hint align="end">{{ form.cliente.nome.length }}/100</mat-hint>
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Telefone</mat-label>
                <input matInput [(ngModel)]="form.cliente.telefone"
                  placeholder="(11) 99999-9999" required maxlength="20" />
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>E-mail</mat-label>
                <input matInput type="email" [(ngModel)]="form.cliente.email" maxlength="100" />
              </mat-form-field>
            </div>
          </mat-tab>
          <mat-tab label="Cliente existente">
            <div class="abas-content">
              <div class="cliente-selecionado" *ngIf="clienteSelecionado()">
                <mat-icon class="ico-ok">check_circle</mat-icon>
                <div>
                  <small>Cliente selecionado</small>
                  <strong>{{ clienteSelecionado()?.nome }}</strong>
                  <span>{{ clienteSelecionado()?.telefone }}<ng-container *ngIf="clienteSelecionado()?.email"> • {{ clienteSelecionado()?.email }}</ng-container></span>
                </div>
                <button mat-icon-button (click)="limparClienteSelecionado()" aria-label="Remover seleção">
                  <mat-icon>close</mat-icon>
                </button>
              </div>

              <mat-form-field appearance="outline" class="full">
                <mat-label>Buscar por nome, telefone ou e-mail</mat-label>
                <input matInput [(ngModel)]="buscaCliente" (ngModelChange)="buscarClientes()" maxlength="100" />
                <mat-icon matSuffix>search</mat-icon>
              </mat-form-field>
              <div class="lista-clientes" *ngIf="resultadosClientes().length || buscaCliente">
                <button class="item-cliente" *ngFor="let c of resultadosClientes()" type="button"
                  [class.ativo]="form.clienteId === c.id"
                  (click)="selecionarCliente(c)">
                  <div>
                    <strong>{{ c.nome }}</strong>
                    <small>{{ c.telefone }}<span *ngIf="c.email"> • {{ c.email }}</span></small>
                  </div>
                  <mat-icon *ngIf="form.clienteId === c.id">check_circle</mat-icon>
                </button>
                <p class="vazio-clientes" *ngIf="!resultadosClientes().length && buscaCliente">
                  Nenhum cliente encontrado.
                </p>
              </div>
            </div>
          </mat-tab>
        </mat-tab-group>

        <h3>Observação</h3>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Observação (opcional)</mat-label>
          <textarea matInput rows="2" [(ngModel)]="form.observacao" maxlength="500"></textarea>
          <mat-hint align="end">{{ (form.observacao || '').length }}/500</mat-hint>
        </mat-form-field>
      </ng-container>

    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close [disabled]="salvando()">Cancelar</button>
      <button mat-flat-button color="primary" class="btn-salvar" (click)="salvar()" [disabled]="!valido() || salvando()">
        <span class="btn-conteudo">
          <mat-spinner diameter="16" *ngIf="salvando()"></mat-spinner>
          <ng-container *ngIf="!salvando()">
            <mat-icon>check</mat-icon>
            <span>{{ data.modo === 'novo' ? 'Criar agendamento' : 'Salvar alterações' }}</span>
          </ng-container>
        </span>
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    :host { display: block; min-width: 28rem; }
    h2 { display: flex; align-items: center; gap: 0.5rem; margin: 0; padding: 1.5rem 1.5rem 0.5rem; }
    h3 { margin: 1rem 0 0.5rem; font-size: 0.9375rem; color: var(--cor-primaria, #6366f1); }
    .conteudo { padding: 0.5rem 1.5rem 0; min-width: 24rem; max-width: 36rem; }
    mat-dialog-actions { padding: 0.75rem 1.5rem 1.5rem; }
    .btn-salvar ::ng-deep .mdc-button__label {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.375rem;
      width: 100%;
    }
    .btn-salvar .btn-conteudo {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
    }
    .btn-salvar mat-icon {
      font-size: 1.125rem;
      width: 1.125rem;
      height: 1.125rem;
      margin: 0;
      line-height: 1.125rem;
    }
    .btn-salvar .btn-conteudo > span {
      line-height: 1;
      display: inline-block;
    }
    .ajuda { margin: 0 0 0.75rem; padding: 0.5rem 0.75rem; background: #f3f4f6; border-radius: 0.5rem; font-size: 0.875rem; }
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
    .full { grid-column: 1 / -1; }
    mat-form-field.full, mat-form-field { width: 100%; }
    .slots { margin: 0.5rem 0 1rem; }
    .slots small { color: #6b7280; font-size: 0.75rem; }
    .chips { display: flex; flex-wrap: wrap; gap: 0.375rem; margin-top: 0.375rem; }
    .chip {
      padding: 0.375rem 0.75rem; border: 1px solid #e5e7eb; background: #fff;
      border-radius: 1rem; cursor: pointer; font-size: 0.8125rem;
      font-family: inherit;
    }
    .chip:hover { border-color: var(--cor-primaria, #6366f1); }
    .chip.ativo { background: var(--cor-primaria, #6366f1); color: #fff; border-color: var(--cor-primaria, #6366f1); }
    .loading-slots { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 0; color: #6b7280; font-size: 0.875rem; }
    .vazio-slots { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 0; color: #ef4444; font-size: 0.875rem; }
    .abas-cliente { margin-bottom: 1rem; }
    .abas-content { padding: 1rem 0.25rem 0; }
    .lista-clientes {
      max-height: 14rem; overflow: auto;
      display: flex; flex-direction: column; gap: 0.25rem;
      border: 1px solid #e5e7eb; border-radius: 0.5rem; padding: 0.25rem;
    }
    .item-cliente {
      display: flex; justify-content: space-between; align-items: center;
      padding: 0.5rem 0.75rem; border: 0; background: transparent; cursor: pointer;
      border-radius: 0.375rem; text-align: left; font-family: inherit;
    }
    .item-cliente:hover { background: #f3f4f6; }
    .item-cliente.ativo { background: #ede9fe; color: var(--cor-primaria, #6366f1); }
    .item-cliente strong { display: block; font-size: 0.875rem; }
    .item-cliente small { color: #6b7280; font-size: 0.75rem; }
    .vazio-clientes { margin: 0.5rem 0; color: #6b7280; text-align: center; font-size: 0.875rem; }
    .cliente-selecionado {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      padding: 0.625rem 0.75rem;
      margin-bottom: 0.75rem;
      background: #ede9fe;
      border: 1px solid #c4b5fd;
      border-radius: 0.5rem;
    }
    .cliente-selecionado .ico-ok {
      color: var(--cor-primaria, #6366f1);
      flex-shrink: 0;
    }
    .cliente-selecionado > div {
      flex: 1;
      display: flex;
      flex-direction: column;
      min-width: 0;
    }
    .cliente-selecionado small {
      font-size: 0.6875rem;
      color: var(--cor-primaria, #6366f1);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }
    .cliente-selecionado strong { font-size: 0.9375rem; color: #111827; }
    .cliente-selecionado span { font-size: 0.8125rem; color: #6b7280; }
  `]
})
export class AgendamentoDialogComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private snack = inject(MatSnackBar);
  private dialogRef = inject(MatDialogRef<AgendamentoDialogComponent, boolean>);

  servicos = signal<Servico[]>([]);
  duracaoServico = computed(() => {
    const id = this.form?.servicoId;
    if (!id) return 0;
    return this.servicos().find(s => s.id === id)?.duracaoMinutos || 0;
  });
  recursosDisponiveis = computed(() => {
    const hora = this.form?.horaInicio;
    if (!hora) return [];
    const vistos = new Set<number>();
    return this.slots().filter(s => {
      if (s.horaInicio.substring(0, 5) !== hora) return false;
      if (vistos.has(s.recursoId)) return false;
      vistos.add(s.recursoId);
      return true;
    });
  });
  slots = signal<SlotDisponivel[]>([]);
  slotsUnicos = computed(() => {
    const vistos = new Set<string>();
    return this.slots().filter(s => {
      const chave = s.horaInicio.substring(0, 5);
      if (vistos.has(chave)) return false;
      vistos.add(chave);
      return true;
    });
  });
  carregandoSlots = signal(false);
  resultadosClientes = signal<any[]>([]);
  clienteSelecionado = signal<any | null>(null);
  buscaCliente = '';
  abaCliente = 0;
  salvando = signal(false);
  hojeDate = new Date();
  private buscaTimer: any = null;

  form: any = {
    servicoId: null as number | null,
    recursoId: null as number | null,
    dataObj: this.amanha(),
    horaInicio: '',
    observacao: '',
    clienteId: null as number | null,
    cliente: { nome: '', telefone: '', email: '' }
  };

  private amanha(): Date {
    const d = new Date();
    d.setDate(d.getDate() + 1);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  constructor(@Inject(MAT_DIALOG_DATA) public data: AgendamentoDialogData) {}

  ngOnInit() {
    if (this.data.modo === 'reagendar' && this.data.agendamento) {
      const a = this.data.agendamento;
      this.form.servicoId = a.servicoId;
      this.form.recursoId = a.recursoId;
      this.form.dataObj = new Date(a.data.substring(0, 10) + 'T00:00:00');
      this.form.horaInicio = a.horaInicio.substring(0, 5);
      this.onDataChange();
    } else {
      this.api.servicosAdmin().subscribe({
        next: list => this.servicos.set(list.filter(s => s.ativo)),
        error: () => this.snack.open('Falha ao carregar serviços', 'OK', { duration: 3000 })
      });
    }
  }

  private dataIso(): string {
    const d = this.form.dataObj as Date;
    if (!d) return '';
    const ano = d.getFullYear();
    const mes = String(d.getMonth() + 1).padStart(2, '0');
    const dia = String(d.getDate()).padStart(2, '0');
    return `${ano}-${mes}-${dia}`;
  }

  onDataChange() {
    if (!this.form.servicoId || !this.form.dataObj) return;
    const slug = this.auth.user()?.tenantSlug;
    if (!slug) return;
    this.carregandoSlots.set(true);
    this.api.slots(slug, this.form.servicoId, this.dataIso()).subscribe({
      next: list => { this.slots.set(list); this.carregandoSlots.set(false); },
      error: () => { this.slots.set([]); this.carregandoSlots.set(false); }
    });
  }

  buscarClientes() {
    if (this.buscaTimer) clearTimeout(this.buscaTimer);
    this.buscaTimer = setTimeout(() => {
      const termo = this.buscaCliente.trim();
      if (termo.length < 2) { this.resultadosClientes.set([]); return; }
      this.api.clientesAdmin(1, 10, termo).subscribe({
        next: (r: any) => this.resultadosClientes.set(r.items || []),
        error: () => this.resultadosClientes.set([])
      });
    }, 300);
  }

  onServicoChange() {
    this.form.horaInicio = '';
    this.form.recursoId = null;
    this.onDataChange();
  }

  onHorarioSelecionado() {
    // Reseta o recurso para que o usuário escolha qual box ele quer entre os disponíveis.
    const disponiveis = this.recursosDisponiveis();
    if (disponiveis.length === 1) {
      this.form.recursoId = disponiveis[0].recursoId;
    } else if (!disponiveis.find(r => r.recursoId === this.form.recursoId)) {
      this.form.recursoId = null;
    }
  }

  selecionarCliente(c: any) {
    this.form.clienteId = c.id;
    this.clienteSelecionado.set(c);
  }

  limparClienteSelecionado() {
    this.form.clienteId = null;
    this.clienteSelecionado.set(null);
  }

  valido = computed(() => {
    if (this.salvando()) return false;
    if (this.data.modo === 'reagendar') {
      return !!(this.form.dataObj && this.form.horaInicio);
    }
    if (!this.form.servicoId || !this.form.dataObj || !this.form.horaInicio) return false;
    if (!this.form.recursoId) return false;
    if (this.abaCliente === 0) {
      return !!(this.form.cliente.nome && this.form.cliente.telefone);
    }
    return !!this.form.clienteId;
  });

  salvar() {
    this.salvando.set(true);

    if (this.data.modo === 'reagendar' && this.data.agendamento) {
      const horaInicio = this.form.horaInicio.length === 5 ? this.form.horaInicio + ':00' : this.form.horaInicio;
      this.api.reagendarAgendamento(this.data.agendamento.id, this.dataIso(), horaInicio).subscribe({
        next: () => {
          this.snack.open('Agendamento atualizado', 'OK', { duration: 2500 });
          this.dialogRef.close(true);
        },
        error: e => {
          this.salvando.set(false);
          this.snack.open(e.error?.message || 'Falha ao salvar', 'OK', { duration: 4000, panelClass: 'snack-erro' });
        }
      });
      return;
    }

    const horaInicio = this.form.horaInicio.length === 5 ? this.form.horaInicio + ':00' : this.form.horaInicio;
    const payload: any = {
      servicoId: this.form.servicoId,
      recursoId: this.form.recursoId,
      data: this.dataIso(),
      horaInicio,
      observacao: this.form.observacao || null
    };
    if (this.abaCliente === 0) {
      payload.cliente = {
        nome: this.form.cliente.nome,
        telefone: this.form.cliente.telefone,
        email: this.form.cliente.email || null
      };
    } else {
      payload.clienteId = this.form.clienteId;
    }

    this.api.criarAgendamentoAdmin(payload).subscribe({
      next: () => {
        this.snack.open('Agendamento criado', 'OK', { duration: 2500 });
        this.dialogRef.close(true);
      },
      error: e => {
        this.salvando.set(false);
        this.snack.open(e.error?.message || 'Falha ao criar', 'OK', { duration: 4000, panelClass: 'snack-erro' });
      }
    });
  }
}
