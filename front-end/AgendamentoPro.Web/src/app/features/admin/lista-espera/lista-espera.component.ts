import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-lista-espera',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatSlideToggleModule, MatTooltipModule],
  template: `
    <header class="topo">
      <div>
        <h1><mat-icon>hourglass_top</mat-icon> Lista de espera</h1>
        <p>Clientes que tentaram agendar em datas lotadas. Notifique quando vagar.</p>
      </div>
      <mat-slide-toggle [(ngModel)]="somenteNaoNotificados" (change)="carregar()">
        Apenas não notificados
      </mat-slide-toggle>
    </header>

    <div class="lista" *ngIf="itens().length; else vazio">
      <article class="card" *ngFor="let item of itens()">
        <div class="bloco">
          <strong>{{ item.cliente.lesClienteNome }}</strong>
          <small>{{ item.cliente.lesClienteTelefone || item.cliente.lesClienteEmail }}</small>
        </div>
        <div class="bloco">
          <strong>{{ item.servicoNome }}</strong>
          <small>{{ item.dataDesejada | date:'dd/MM/yyyy' }}</small>
        </div>
        <div class="bloco">
          <small *ngIf="item.observacao" class="obs">"{{ item.observacao }}"</small>
        </div>
        <div class="acoes">
          <a *ngIf="item.cliente.lesClienteTelefone"
            [href]="linkWhatsApp(item)" target="_blank"
            mat-stroked-button color="primary" matTooltip="Abrir WhatsApp">
            <mat-icon>chat</mat-icon> Notificar
          </a>
          <button mat-icon-button (click)="marcarNotificado(item)" matTooltip="Marcar como notificado">
            <mat-icon>check</mat-icon>
          </button>
        </div>
      </article>
    </div>

    <ng-template #vazio>
      <div class="vazio">
        <mat-icon>inbox</mat-icon>
        <p>Nenhum cliente na lista de espera.</p>
      </div>
    </ng-template>
  `,
  styles: [`
    .topo { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; margin-bottom: 1rem; }
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .topo p { margin: 0; color: #666; }
    .lista { display: grid; gap: 0.75rem; }
    .card { background: var(--cor-fundo-card); padding: 0.75rem 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); display: grid; grid-template-columns: 1fr 1fr 1fr auto; gap: 1rem; align-items: center; border-left: 4px solid #ff9800; }
    .bloco { display: flex; flex-direction: column; min-width: 0; }
    .bloco small { color: #888; font-size: 0.85rem; }
    .obs { font-style: italic; }
    .acoes { display: flex; gap: 0.25rem; align-items: center; }
    .vazio { text-align: center; padding: 3rem; color: #888; }
    .vazio mat-icon { font-size: 3rem; width: 3rem; height: 3rem; }
    @media (max-width: 40rem) { .card { grid-template-columns: 1fr; } }
  `]
})
export class ListaEsperaComponent implements OnInit {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  itens = signal<any[]>([]);
  somenteNaoNotificados = true;

  ngOnInit() { this.carregar(); }

  carregar() {
    this.api.listarEsperaAdmin(undefined, this.somenteNaoNotificados).subscribe(list => this.itens.set(list));
  }

  linkWhatsApp(item: any): string {
    const tel = item.cliente.lesClienteTelefone || '';
    const numero = tel.replace(/\D/g, '');
    const ddi = numero.length === 10 || numero.length === 11 ? '55' + numero : numero;
    const msg = encodeURIComponent(
      `Olá ${item.cliente.lesClienteNome}! Vaga liberada para ${item.servicoNome} no dia ${new Date(item.dataDesejada).toLocaleDateString('pt-BR')}. Confirme se ainda interessa!`);
    return `https://wa.me/${ddi}?text=${msg}`;
  }

  marcarNotificado(item: any) {
    this.api.notificarEspera(item.id).subscribe({
      next: () => { this.snack.open('Marcado como notificado', 'OK', { duration: 2000 }); this.carregar(); },
      error: () => this.snack.open('Falha', 'OK', { duration: 3000 })
    });
  }
}
