import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../../core/services/api.service';

interface Dashboard {
  agendamentosHoje: number;
  agendamentosSemana: number;
  agendamentosMes: number;
  receitaHoje: number;
  receitaMes: number;
  pendentesPagamento: number;
  taxaOcupacao: number;
  topServicos: { nome: string; quantidade: number; receitaTotal: number }[];
  proximosAgendamentos: { id: number; cliente: string; servico: string; data: string; hora: string; status: string }[];
}

interface ReceitaPorDia {
  data: string;
  receita: number;
  quantidade: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  private api = inject(ApiService);

  data = signal<Dashboard | null>(null);
  receitaSerie = signal<ReceitaPorDia[]>([]);
  carregando = signal(true);

  receitaMaxima = computed(() => Math.max(1, ...this.receitaSerie().map(r => r.receita)));
  receitaTotal30Dias = computed(() => this.receitaSerie().reduce((s, r) => s + r.receita, 0));

  ngOnInit() {
    this.api.dashboard().subscribe({
      next: d => {
        this.data.set(d);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });

    const fim = new Date().toISOString().substring(0, 10);
    const inicio = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().substring(0, 10);
    this.api.relReceita(inicio, fim).subscribe(r => this.receitaSerie.set(r as ReceitaPorDia[]));
  }

  graficoPath = computed(() => {
    const dados = this.receitaSerie();
    if (dados.length === 0) return '';
    const max = this.receitaMaxima();
    const w = 100, h = 40;
    const passo = w / Math.max(1, dados.length - 1);
    const pontos = dados.map((d, i) => {
      const x = i * passo;
      const y = h - (d.receita / max) * h;
      return `${x},${y}`;
    });
    return `M0,${h} L${pontos.join(' L')} L${w},${h} Z`;
  });

  graficoLinha = computed(() => {
    const dados = this.receitaSerie();
    if (dados.length === 0) return '';
    const max = this.receitaMaxima();
    const w = 100, h = 40;
    const passo = w / Math.max(1, dados.length - 1);
    const pontos = dados.map((d, i) => {
      const x = i * passo;
      const y = h - (d.receita / max) * h;
      return `${x},${y}`;
    });
    return `M${pontos.join(' L')}`;
  });

  iconeStatus(status: string): string {
    const s = status.toLowerCase();
    if (s.includes('pendente')) return 'schedule';
    if (s.includes('confirmado')) return 'check_circle';
    if (s.includes('andamento')) return 'play_circle';
    return 'event';
  }

  classeStatus(status: string): string {
    const s = status.toLowerCase();
    if (s.includes('pendente')) return 'pendente';
    if (s.includes('confirmado')) return 'confirmado';
    if (s.includes('andamento')) return 'andamento';
    return '';
  }
}
