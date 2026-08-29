import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../../../core/services/api.service';
import { Servico } from '../../../core/models/servico.model';
import { Tenant } from '../../../core/models/tenant.model';
import { TenantService } from '../../../core/services/tenant.service';
import { ThemeService } from '../../../core/services/theme.service';
import { urlUpload } from '../../../core/utils/url.util';

@Component({
  selector: 'app-servicos-publico',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="container">
      <header class="hero" [style.background-image]="bannerStyle()">
        <div class="hero-overlay">
          <a [routerLink]="['/t', slug]" class="voltar" mat-icon-button>
            <mat-icon>arrow_back</mat-icon>
          </a>
          <img *ngIf="logoUrl()" [src]="logoUrl()" alt="logo" class="logo" />
          <h1>{{ tenant()?.nome || 'Catálogo' }}</h1>
          <p class="subtitulo">Escolha um serviço para agendar</p>

          <div class="contato">
            <span *ngIf="tenant()?.endereco">
              <mat-icon>location_on</mat-icon>
              {{ tenant()?.endereco }}<ng-container *ngIf="tenant()?.cidade">, {{ tenant()?.cidade }}/{{ tenant()?.estado }}</ng-container>
            </span>
            <span *ngIf="tenant()?.telefone">
              <mat-icon>phone</mat-icon>
              {{ tenant()?.telefone }}
            </span>
            <span *ngIf="tenant()?.whatsApp">
              <mat-icon>chat</mat-icon>
              {{ tenant()?.whatsApp }}
            </span>
          </div>
        </div>
      </header>

      <div class="grid" *ngIf="servicos().length; else vazio">
        <article class="card" *ngFor="let s of servicos()">
          <div class="img" *ngIf="s.imagemUrl"
            [style.background-image]="'url(' + s.imagemUrl + ')'"></div>
          <div class="body">
            <h3>{{ s.nome }}</h3>
            <p *ngIf="s.descricao">{{ s.descricao }}</p>
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
  styleUrls: ['./servicos-publico.component.scss'],
  styles: [`
    .hero {
      position: relative;
      min-height: 16rem;
      background-size: cover;
      background-position: center;
      background-color: #1976d2;
      color: #fff;
    }
    .hero-overlay {
      position: relative;
      min-height: 16rem;
      background: linear-gradient(180deg, rgba(0,0,0,0.35) 0%, rgba(0,0,0,0.7) 100%);
      padding: 1rem 1.5rem 2rem;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
    }
    .voltar { position: absolute; top: 0.5rem; left: 0.5rem; color: #fff !important; }
    .logo { max-height: 4rem; max-width: 8rem; margin-bottom: 0.5rem; }
    /* cor explícita: o h1 global usa --cor-texto e venceria a herança do branco */
    .hero-overlay h1 { margin: 0; font-size: 1.75rem; color: #fff; text-shadow: 0 1px 3px rgba(0,0,0,0.4); }
    .subtitulo { margin: 0.25rem 0 1rem; opacity: 0.9; color: #fff; }
    .contato { display: flex; flex-wrap: wrap; gap: 1rem; justify-content: center; font-size: 0.9rem; }
    .contato span { display: flex; align-items: center; gap: 0.25rem; }
    .contato mat-icon { font-size: 1rem; width: 1rem; height: 1rem; }
    @media (min-width: 30rem) {
      .hero, .hero-overlay { min-height: 20rem; }
      .hero-overlay h1 { font-size: 2.25rem; }
    }
  `]
})
export class ServicosPublicoComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private api = inject(ApiService);
  private tenantSvc = inject(TenantService);
  private theme = inject(ThemeService);

  slug = '';
  servicos = signal<Servico[]>([]);
  tenant = signal<Tenant | null>(null);

  bannerStyle() {
    // Uploads são servidos pela API — URL relativa precisa virar absoluta.
    const url = urlUpload(this.tenant()?.personalizacao?.bannerUrl);
    return url ? `url('${url}')` : 'none';
  }

  logoUrl(): string { return urlUpload(this.tenant()?.personalizacao?.logoUrl); }

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    this.tenantSvc.slug = this.slug;
    sessionStorage.setItem('tenant_slug', this.slug);

    this.tenantSvc.carregarTenant(this.slug).subscribe({
      next: t => {
        this.tenant.set(t);
        this.theme.aplicarPersonalizacao(t.personalizacao);
      },
      error: () => { /* segue sem hero personalizado */ }
    });

    this.api.servicosPublicos(this.slug).subscribe(list => this.servicos.set(list));
  }
}
