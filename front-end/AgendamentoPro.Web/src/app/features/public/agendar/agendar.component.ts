import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { TenantService } from '../../../core/services/tenant.service';
import { FormaPagamento, SlotDisponivel } from '../../../core/models/agendamento.model';
import { Servico } from '../../../core/models/servico.model';

@Component({
  selector: 'app-agendar',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatRadioModule],
  templateUrl: './agendar.component.html',
  styleUrls: ['./agendar.component.scss']
})
export class AgendarComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);
  private tenantSvc = inject(TenantService);

  slug = '';
  servicoId = 0;
  servico = signal<Servico | null>(null);
  data = new Date().toISOString().substring(0, 10);
  slots = signal<SlotDisponivel[]>([]);
  slotEscolhido = signal<SlotDisponivel | null>(null);

  cliente = { nome: '', email: '', telefone: '', whatsApp: '', cpf: '' };
  forma: FormaPagamento = FormaPagamento.Pix;
  formas = [
    { value: FormaPagamento.Pix, label: 'PIX' },
    { value: FormaPagamento.CartaoCredito, label: 'Crédito' },
    { value: FormaPagamento.CartaoDebito, label: 'Débito' }
  ];

  passo: 1 | 2 | 3 = 1;
  carregando = false;

  // % de entrada do tenant (default 20 enquanto carrega / se falhar)
  percentualEntrada = signal(20);

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
    this.buscarSlots();
  }

  valorSinal(): number {
    return (this.servico()?.preco || 0) * this.percentualEntrada() / 100;
  }

  // 'YYYY-MM-DD' → 'DD/MM/YYYY' sem passar por Date (o DatePipe interpreta a
  // string como UTC e mostraria o dia anterior no fuso do Brasil).
  dataFormatada(): string {
    const [a, m, d] = this.data.split('-');
    return `${d}/${m}/${a}`;
  }

  buscarSlots() {
    this.api.slots(this.slug, this.servicoId, this.data).subscribe(s => this.slots.set(s));
  }

  selecionarSlot(s: SlotDisponivel) {
    this.slotEscolhido.set(s);
    this.passo = 2;
  }

  prosseguirPagamento() {
    if (!this.cliente.nome || !this.cliente.telefone) {
      this.snack.open('Informe seu nome e telefone.', 'OK', { duration: 3000 });
      return;
    }
    this.passo = 3;
  }

  confirmar() {
    const slot = this.slotEscolhido();
    if (!slot) return;
    this.carregando = true;
    this.api.criarAgendamento(this.slug, {
      servicoId: this.servicoId,
      recursoId: slot.recursoId,
      data: this.data,
      horaInicio: slot.horaInicio,
      cliente: this.cliente,
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
        this.snack.open(err.error?.message || 'Falha ao agendar.', 'OK', { duration: 5000 });
      }
    });
  }
}
