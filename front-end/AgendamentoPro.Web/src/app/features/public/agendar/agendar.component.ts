import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { TenantService } from '../../../core/services/tenant.service';
import { DiaDisponivel, FormaPagamento, SlotDisponivel } from '../../../core/models/agendamento.model';
import { Servico } from '../../../core/models/servico.model';
import { MascaraDirective, documentoCompleto } from '../../../core/directives/mascara.directive';
import { LIMITES, emailValido, mensagemErroApi } from '../../../core/utils/validacao.util';

/** Dia do seletor de datas, já pronto para o template. */
interface ChipDia {
  iso: string;
  diaSemana: string;
  diaMes: string;
  mes: string;
  vagas: number;
  hoje: boolean;
}

/** Slots agrupados por período do dia — 30 botões numa lista só não se lê. */
interface PeriodoSlots {
  titulo: string;
  icone: string;
  slots: SlotDisponivel[];
}

const DIAS_SEMANA = ['Domingo', 'Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta', 'Sábado'];
const DIAS_CURTOS = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];
const MESES = ['janeiro', 'fevereiro', 'março', 'abril', 'maio', 'junho',
  'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro'];

@Component({
  selector: 'app-agendar',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatRadioModule, MatCheckboxModule,
    MatProgressSpinnerModule, MascaraDirective],
  templateUrl: './agendar.component.html',
  styleUrls: ['./agendar.component.scss']
})
export class AgendarComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);
  private tenantSvc = inject(TenantService);

  readonly limites = LIMITES;

  slug = '';
  servicoId = 0;
  servico = signal<Servico | null>(null);

  /** Não faz sentido agendar no passado; o input nativo já barra. */
  readonly minData = hojeIso();
  data = signal(hojeIso());
  dias = signal<ChipDia[]>([]);
  carregandoDias = signal(false);

  slots = signal<SlotDisponivel[]>([]);
  carregandoSlots = signal(false);
  slotEscolhido = signal<SlotDisponivel | null>(null);

  cliente = { nome: '', email: '', telefone: '', whatsApp: '', cpf: '' };
  whatsIgualTelefone = signal(true);
  tentouAvancar = signal(false);

  forma: FormaPagamento = FormaPagamento.Pix;
  formas = [
    { value: FormaPagamento.Pix, label: 'PIX', icone: 'qr_code_2', ajuda: 'QR Code na hora, confirma em segundos' },
    { value: FormaPagamento.CartaoCredito, label: 'Cartão de crédito', icone: 'credit_card', ajuda: 'Você conclui no ambiente do Mercado Pago' },
    { value: FormaPagamento.CartaoDebito, label: 'Cartão de débito', icone: 'payments', ajuda: 'Você conclui no ambiente do Mercado Pago' }
  ];

  passo: 1 | 2 | 3 = 1;
  carregando = false;

  // % de entrada do tenant (default 20 enquanto carrega / se falhar)
  percentualEntrada = signal(20);

  // ---------- Validação do passo 2 ----------
  // Metodos, nao computed(): estes campos vivem num objeto comum ligado por
  // ngModel, e um computed() so reavalia quando um SIGNAL do qual ele depende
  // muda -- congelaria no resultado da primeira renderizacao (campo sempre
  // "valido", mensagem de erro que nunca aparece).
  erroNome(): string {
    const v = this.cliente.nome.trim();
    if (!v) return 'Informe seu nome.';
    if (v.length < 3) return 'Nome muito curto.';
    if (v.length > LIMITES.nome) return 'Máximo de ' + LIMITES.nome + ' caracteres.';
    return '';
  }
  erroTelefone(): string {
    if (!this.cliente.telefone.trim()) return 'Informe seu telefone.';
    if (!documentoCompleto('telefone', this.cliente.telefone)) return 'Telefone incompleto. Use DDD + número.';
    return '';
  }
  erroWhatsApp(): string {
    const v = this.cliente.whatsApp.trim();
    if (!v) return '';
    return documentoCompleto('telefone', v) ? '' : 'WhatsApp incompleto. Use DDD + número.';
  }
  erroEmail(): string {
    const v = this.cliente.email.trim();
    if (!v) return '';
    if (v.length > LIMITES.email) return 'Máximo de ' + LIMITES.email + ' caracteres.';
    return emailValido(v) ? '' : 'E-mail inválido. Use o formato nome@dominio.com.';
  }
  erroCpf(): string {
    const v = this.cliente.cpf.trim();
    if (!v) return '';
    return documentoCompleto('cpf', v) ? '' : 'CPF incompleto.';
  }
  dadosValidos(): boolean {
    return !this.erroNome() && !this.erroTelefone() && !this.erroWhatsApp()
      && !this.erroEmail() && !this.erroCpf();
  }

  // ---------- Slots por período ----------
  periodos = computed<PeriodoSlots[]>(() => {
    const grupos: PeriodoSlots[] = [
      { titulo: 'Manhã', icone: 'wb_twilight', slots: [] },
      { titulo: 'Tarde', icone: 'light_mode', slots: [] },
      { titulo: 'Noite', icone: 'dark_mode', slots: [] }
    ];
    // Um slot por HORÁRIO: a API devolve uma linha por box livre, então num
    // lava-rápido de 4 boxes o cliente via "08:00" quatro vezes seguidas, todas
    // idênticas. Ele escolhe a hora; qual box atende é problema do sistema.
    const vistos = new Set<string>();
    for (const s of this.slots()) {
      if (vistos.has(s.horaInicio)) continue;
      vistos.add(s.horaInicio);
      const hora = +s.horaInicio.substring(0, 2);
      const i = hora < 12 ? 0 : hora < 18 ? 1 : 2;
      grupos[i].slots.push(s);
    }
    return grupos.filter(g => g.slots.length);
  });

  /** Próxima data com vaga depois da escolhida — a saída do beco sem saída. */
  proximoDiaComVaga = computed<ChipDia | null>(() =>
    this.dias().find(d => d.vagas > 0 && d.iso > this.data()) || null);

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    this.servicoId = +(this.route.snapshot.paramMap.get('servicoId') || 0);
    this.api.servicosPublicos(this.slug).subscribe(list => {
      this.servico.set(list.find(s => s.id === this.servicoId) || null);
    });
    // O sinal é uma regra do tenant (percentualEntrada) — não é fixo em 20%.
    const atual = this.tenantSvc.current();
    if (atual?.regras?.percentualEntrada != null) {
      this.percentualEntrada.set(atual.regras.percentualEntrada);
    } else {
      this.tenantSvc.carregarTenant(this.slug).subscribe({
        next: t => {
          if (t?.regras?.percentualEntrada != null) this.percentualEntrada.set(t.regras.percentualEntrada);
        },
        error: () => { /* mantém default */ }
      });
    }
    this.carregarDias();
    this.buscarSlots();
  }

  valorSinal(): number {
    return (this.servico()?.preco || 0) * this.percentualEntrada() / 100;
  }

  // 'YYYY-MM-DD' → 'DD/MM/YYYY' sem passar por Date (o DatePipe interpreta a
  // string como UTC e mostraria o dia anterior no fuso do Brasil).
  dataFormatada(iso = this.data()): string {
    const [a, m, d] = iso.split('-');
    return d + '/' + m + '/' + a;
  }

  /** Rótulo longo da data escolhida: "Quinta, 10 de setembro". */
  dataPorExtenso(): string {
    const [a, m, d] = this.data().split('-').map(Number);
    const local = new Date(a, m - 1, d);
    return DIAS_SEMANA[local.getDay()] + ', ' + d + ' de ' + MESES[m - 1];
  }

  /**
   * Carrega as vagas dos próximos 14 dias numa chamada só, para os chips já
   * nascerem sabendo onde há horário — o cliente não precisa mais descobrir
   * que um dia está fechado clicando nele.
   */
  carregarDias() {
    this.carregandoDias.set(true);
    const inicio = hojeIso();
    this.api.diasDisponiveis(this.slug, this.servicoId, inicio, 14).subscribe({
      next: ds => {
        this.dias.set(ds.map(d => montarChip(d, inicio)));
        this.carregandoDias.set(false);
      },
      error: () => this.carregandoDias.set(false)
    });
  }

  escolherData(iso: string) {
    if (iso === this.data()) return;
    this.data.set(iso);
    this.buscarSlots();
  }

  /** Campo de data nativo, para quem quer ir além dos 14 dias dos chips. */
  aoTrocarDataLivre(valor: string) {
    if (!valor) return;
    this.data.set(valor);
    this.buscarSlots();
  }

  buscarSlots() {
    this.carregandoSlots.set(true);
    this.slots.set([]);
    this.api.slots(this.slug, this.servicoId, this.data()).subscribe({
      next: s => { this.slots.set(s); this.carregandoSlots.set(false); },
      error: () => this.carregandoSlots.set(false)
    });
  }

  selecionarSlot(s: SlotDisponivel) {
    this.slotEscolhido.set(s);
    this.passo = 2;
    this.tentouAvancar.set(false);
  }

  /** Marcado por padrão: quase todo mundo usa o mesmo número nos dois campos. */
  aoMudarWhatsIgual(igual: boolean) {
    this.whatsIgualTelefone.set(igual);
    if (igual) this.cliente.whatsApp = '';
  }

  prosseguirPagamento() {
    this.tentouAvancar.set(true);
    if (!this.dadosValidos()) {
      this.snack.open('Confira os campos destacados.', 'OK', { duration: 4000 });
      return;
    }
    this.passo = 3;
  }

  voltarParaHorarios() {
    this.passo = 1;
    this.slotEscolhido.set(null);
  }

  confirmar() {
    const slot = this.slotEscolhido();
    if (!slot || this.carregando) return;
    this.carregando = true;
    const telefone = this.cliente.telefone.trim();
    this.api.criarAgendamento(this.slug, {
      servicoId: this.servicoId,
      recursoId: slot.recursoId,
      data: this.data(),
      horaInicio: slot.horaInicio,
      cliente: {
        nome: this.cliente.nome.trim(),
        email: this.cliente.email.trim() || undefined,
        telefone,
        whatsApp: (this.whatsIgualTelefone() ? telefone : this.cliente.whatsApp.trim()) || undefined,
        cpf: this.cliente.cpf.trim() || undefined
      },
      formaPagamento: this.forma
    }).subscribe({
      next: r => {
        this.carregando = false;
        this.router.navigate(['/t', this.slug, 'pagamento', r.agendamento.id], {
          state: { resultado: r }
        });
      },
      error: err => {
        this.carregando = false;
        const msg = mensagemErroApi(err, 'Não foi possível concluir o agendamento.');
        this.snack.open(msg, 'OK', { duration: 7000, panelClass: 'snack-erro' });
        // Horário tomado no meio do caminho: a lista precisa voltar atualizada,
        // senão o cliente insiste no mesmo botão e toma o mesmo erro.
        if (/indispon|conflita|ocupad/i.test(msg)) {
          this.voltarParaHorarios();
          this.carregarDias();
          this.buscarSlots();
        }
      }
    });
  }
}

function hojeIso(): string {
  const d = new Date();
  const mes = ('' + (d.getMonth() + 1)).padStart(2, '0');
  const dia = ('' + d.getDate()).padStart(2, '0');
  return d.getFullYear() + '-' + mes + '-' + dia;
}

function montarChip(d: DiaDisponivel, hojeIsoStr: string): ChipDia {
  const iso = d.data.substring(0, 10);
  const [a, m, dia] = iso.split('-').map(Number);
  // new Date(a, m-1, dia) e não new Date(iso): a string ISO seria lida como UTC
  // e no fuso do Brasil cairia no dia anterior.
  const local = new Date(a, m - 1, dia);
  return {
    iso,
    diaSemana: DIAS_CURTOS[local.getDay()],
    diaMes: '' + dia,
    mes: MESES[m - 1],
    vagas: d.vagas,
    hoje: iso === hojeIsoStr
  };
}
