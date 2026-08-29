import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../../core/services/api.service';
import { Combo } from '../../../core/models/combo.model';

@Component({
  selector: 'app-combos-publico',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, CurrencyPipe],
  template: `
    <header class="topo">
      <h1><mat-icon>local_offer</mat-icon> Combos promocionais</h1>
      <p>Pacotes com preço especial.</p>
    </header>

    <div *ngIf="carregando(); else lista" class="centro">
      <mat-spinner></mat-spinner>
    </div>

    <ng-template #lista>
      <div class="grid" *ngIf="combos().length; else vazio">
        <article class="combo" *ngFor="let c of combos()">
          <div class="imagem" *ngIf="c.imagemUrl" [style.background-image]="'url(' + c.imagemUrl + ')'"></div>
          <div class="conteudo">
            <h2>{{ c.nome }}</h2>
            <p *ngIf="c.descricao">{{ c.descricao }}</p>

            <ul class="servicos">
              <li *ngFor="let s of c.servicos">
                <mat-icon>check_circle</mat-icon>
                <span>{{ s.nome }} — {{ s.duracaoMinutos }}min</span>
              </li>
            </ul>

            <div class="precos">
              <span class="preco-original" *ngIf="c.economia > 0">{{ c.precoOriginal | currency:'BRL' }}</span>
              <strong class="preco-promo">{{ c.precoPromocional | currency:'BRL' }}</strong>
              <span class="economia" *ngIf="c.economia > 0">Economize {{ c.economia | currency:'BRL' }}</span>
            </div>

            <a mat-flat-button color="primary" [routerLink]="['/t', slug, 'agendar-combo', c.id]">
              <mat-icon>event</mat-icon> Agendar combo
            </a>
          </div>
        </article>
      </div>
    </ng-template>

    <ng-template #vazio>
      <div class="vazio">
        <mat-icon>inventory_2</mat-icon>
        <p>Nenhum combo disponível no momento.</p>
      </div>
    </ng-template>
  `,
  styles: [`
    .topo { padding: 1.5rem 1rem; text-align: center; }
    .topo h1 { display: inline-flex; align-items: center; gap: 0.5rem; margin: 0; }
    .grid { display: grid; gap: 1rem; padding: 0 1rem; grid-template-columns: repeat(auto-fill, minmax(18rem, 1fr)); }
    .combo { background: var(--cor-fundo-card); border-radius: 0.75rem; overflow: hidden; box-shadow: 0 2px 6px rgba(0,0,0,0.08); display: flex; flex-direction: column; }
    .imagem { height: 10rem; background-size: cover; background-position: center; }
    .conteudo { padding: 1rem; display: flex; flex-direction: column; gap: 0.75rem; }
    .conteudo h2 { margin: 0; }
    .conteudo p { margin: 0; color: #555; }
    .servicos { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.25rem; }
    .servicos li { display: flex; align-items: center; gap: 0.5rem; }
    .servicos mat-icon { color: #2e7d32; font-size: 1.1rem; width: 1.1rem; height: 1.1rem; }
    .precos { display: flex; flex-wrap: wrap; align-items: baseline; gap: 0.5rem; }
    .preco-original { text-decoration: line-through; color: #999; }
    .preco-promo { font-size: 1.5rem; color: #2e7d32; }
    .economia { background: #e8f5e9; color: #2e7d32; padding: 0.15rem 0.5rem; border-radius: 0.5rem; font-size: 0.85rem; font-weight: 500; }
    .centro, .vazio { text-align: center; padding: 4rem 1rem; color: #888; }
    .vazio mat-icon { font-size: 3rem; width: 3rem; height: 3rem; }
  `]
})
export class CombosPublicoComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private api = inject(ApiService);
  combos = signal<Combo[]>([]);
  carregando = signal(true);
  slug = '';

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || this.route.parent?.snapshot.paramMap.get('slug') || '';
    this.api.combosPublicos(this.slug).subscribe({
      next: list => { this.combos.set(list); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }
}
