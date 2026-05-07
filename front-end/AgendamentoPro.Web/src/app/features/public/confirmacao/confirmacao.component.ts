import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../../../core/services/api.service';
import { Agendamento } from '../../../core/models/agendamento.model';

@Component({
  selector: 'app-confirmacao',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="container centro">
      <mat-icon class="check">check_circle</mat-icon>
      <h2>Agendamento confirmado!</h2>
      <p *ngIf="ag() as a">
        {{ a.servicoNome }} em {{ a.data | date:'dd/MM/yyyy' }} às {{ a.horaInicio.substring(0,5) }}
      </p>
      <p>Você receberá lembretes via WhatsApp.</p>
      <a mat-flat-button color="primary" [routerLink]="['/t', slug]">Voltar ao início</a>
    </div>
  `,
  styles: [`
    .centro { text-align: center; padding-top: 4rem; }
    .check { font-size: 5rem; width: 5rem; height: 5rem; color: var(--cor-sucesso); }
    h2 { margin: 1rem 0 0.5rem; }
  `]
})
export class ConfirmacaoComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private api = inject(ApiService);

  slug = '';
  ag = signal<Agendamento | null>(null);

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    const id = +(this.route.snapshot.paramMap.get('agendamentoId') || 0);
    this.api.consultarAgendamento(this.slug, id).subscribe(a => this.ag.set(a));
  }
}
