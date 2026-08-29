import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { Avaliacao } from '../../../core/models/avaliacao.model';

@Component({
  selector: 'app-avaliacoes',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatSlideToggleModule, MatPaginatorModule, MatChipsModule],
  template: `
    <header class="topo">
      <div>
        <h1><mat-icon>star</mat-icon> Avaliações</h1>
        <p class="sub">Lista de avaliações enviadas pelos clientes.</p>
      </div>
      <mat-slide-toggle [(ngModel)]="somenteRespondidas" (change)="carregar(0)">
        Apenas respondidas
      </mat-slide-toggle>
    </header>

    <div class="lista" *ngIf="avaliacoes().length; else vazio">
      <article class="card" *ngFor="let a of avaliacoes()">
        <div class="cabecalho">
          <strong>{{ a.clienteNome || 'Cliente' }}</strong>
          <span class="data">{{ (a.respondidoEm || a.criadoEm) | date:'dd/MM/yyyy HH:mm' }}</span>
        </div>

        <div class="estrelas" *ngIf="a.nota; else aguardando">
          <mat-icon *ngFor="let n of [1,2,3,4,5]"
            [class.ativa]="n <= a.nota!">{{ n <= a.nota! ? 'star' : 'star_border' }}</mat-icon>
          <span class="nota">{{ a.nota }}/5</span>
        </div>
        <ng-template #aguardando>
          <mat-chip class="pendente">Aguardando resposta</mat-chip>
        </ng-template>

        <p *ngIf="a.comentario" class="comentario">"{{ a.comentario }}"</p>

        <div class="rodape" *ngIf="a.respondidoEm">
          <mat-slide-toggle [checked]="a.publica" (change)="alternarVisibilidade(a, $event.checked)">
            Pública (aparece na home)
          </mat-slide-toggle>
        </div>
      </article>
    </div>
    <ng-template #vazio>
      <div class="vazio">
        <mat-icon>inbox</mat-icon>
        <p>Nenhuma avaliação encontrada.</p>
      </div>
    </ng-template>

    <mat-paginator [length]="total()" [pageSize]="pageSize" [pageIndex]="pageIndex()"
      [pageSizeOptions]="[10, 20, 50]" (page)="onPage($event)">
    </mat-paginator>
  `,
  styles: [`
    .topo { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; gap: 1rem; flex-wrap: wrap; }
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .sub { margin: 0; color: #666; }
    .lista { display: grid; grid-template-columns: repeat(auto-fill, minmax(20rem, 1fr)); gap: 1rem; margin-bottom: 1rem; }
    .card { background: var(--cor-fundo-card); border-radius: 0.5rem; padding: 1rem; box-shadow: 0 1px 3px rgba(0,0,0,0.08); display: flex; flex-direction: column; gap: 0.5rem; }
    .cabecalho { display: flex; justify-content: space-between; }
    .data { color: #888; font-size: 0.85rem; }
    .estrelas { display: flex; align-items: center; gap: 0.1rem; }
    .estrelas mat-icon { color: #ccc; }
    .estrelas mat-icon.ativa { color: #fbc02d; }
    .estrelas .nota { margin-left: 0.5rem; font-weight: 600; }
    .comentario { font-style: italic; color: #444; margin: 0; }
    .pendente { background: var(--cor-fundo-card)3e0; color: #e65100; }
    .rodape { border-top: 1px solid #eee; padding-top: 0.5rem; }
    .vazio { text-align: center; color: #888; padding: 3rem; }
    .vazio mat-icon { font-size: 3rem; width: 3rem; height: 3rem; }
  `]
})
export class AvaliacoesComponent implements OnInit {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  avaliacoes = signal<Avaliacao[]>([]);
  total = signal(0);
  pageIndex = signal(0);
  pageSize = 20;
  somenteRespondidas = false;

  ngOnInit() { this.carregar(0); }

  carregar(page: number) {
    this.api.listarAvaliacoes(page + 1, this.pageSize, this.somenteRespondidas).subscribe(r => {
      this.avaliacoes.set(r.items || []);
      this.total.set(r.total || 0);
      this.pageIndex.set(page);
    });
  }

  onPage(e: PageEvent) { this.carregar(e.pageIndex); }

  alternarVisibilidade(a: Avaliacao, publica: boolean) {
    this.api.alterarVisibilidadeAvaliacao(a.id, publica).subscribe({
      next: () => {
        a.publica = publica;
        this.snack.open('Visibilidade atualizada', 'OK', { duration: 2000 });
      },
      error: () => this.snack.open('Falha ao atualizar', 'OK', { duration: 3000, panelClass: 'snack-erro' })
    });
  }
}
