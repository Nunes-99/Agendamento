import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../../../core/services/api.service';
import { Servico } from '../../../core/models/servico.model';
import { TenantService } from '../../../core/services/tenant.service';

@Component({
  selector: 'app-servicos-publico',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="container">
      <header class="topo">
        <a [routerLink]="['/t', slug]" class="voltar"><mat-icon>arrow_back</mat-icon></a>
        <h2>Escolha um serviço</h2>
      </header>

      <div class="grid" *ngIf="servicos().length; else vazio">
        <article class="card" *ngFor="let s of servicos()">
          <div class="img" [style.background-image]="'url(' + (s.imagemUrl || '') + ')'"></div>
          <div class="body">
            <h3>{{ s.nome }}</h3>
            <p>{{ s.descricao }}</p>
            <div class="meta">
              <span><mat-icon>schedule</mat-icon> {{ s.duracaoMinutos }} min</span>
              <strong>R$ {{ s.preco | number:'1.2-2' }}</strong>
            </div>
            <a mat-flat-button color="primary" [routerLink]="['/t', slug, 'agendar', s.id]">
              Agendar
            </a>
          </div>
        </article>
      </div>

      <ng-template #vazio>
        <p class="vazio">Nenhum serviço disponível no momento.</p>
      </ng-template>
    </div>
  `,
  styleUrls: ['./servicos-publico.component.scss']
})
export class ServicosPublicoComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private api = inject(ApiService);
  private tenant = inject(TenantService);

  slug = '';
  servicos = signal<Servico[]>([]);

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    this.tenant.slug = this.slug;
    this.api.servicosPublicos(this.slug).subscribe(list => this.servicos.set(list));
  }
}
