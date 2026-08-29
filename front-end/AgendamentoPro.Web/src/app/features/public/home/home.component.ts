import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TenantService } from '../../../core/services/tenant.service';
import { ThemeService } from '../../../core/services/theme.service';
import { ApiService } from '../../../core/services/api.service';
import { AnuncioVitrine, Tenant } from '../../../core/models/tenant.model';
import { ResumoAvaliacoes } from '../../../core/models/avaliacao.model';
import { Servico } from '../../../core/models/servico.model';
import { TenantNaoEncontradoComponent } from '../tenant-nao-encontrado.component';
import { urlUpload } from '../../../core/utils/url.util';

type EstadoCarga = 'carregando' | 'ok' | 'naoEncontrado';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, TenantNaoEncontradoComponent],
  template: `
    <ng-container [ngSwitch]="estado()">
      <div *ngSwitchCase="'carregando'" class="loading">
        <mat-spinner></mat-spinner>
      </div>

      <app-tenant-nao-encontrado *ngSwitchCase="'naoEncontrado'"></app-tenant-nao-encontrado>

      <ng-container *ngSwitchCase="'ok'">
        <header class="hero" [style.background-image]="bannerStyle()">
          <div class="hero-overlay">
            <img *ngIf="logoUrl()" [src]="logoUrl()" alt="logo" class="logo" />
            <h1>{{ tenant()?.nome }}</h1>
            <p *ngIf="tenant()?.descricao as desc">{{ desc }}</p>
            <div class="cta">
              <a mat-flat-button color="primary" [routerLink]="['/t', slug, 'servicos']">
                <mat-icon>event</mat-icon>
                Agendar agora
              </a>
              <a mat-stroked-button [routerLink]="['/t', slug, 'entrar']" class="entrar">
                <mat-icon>account_circle</mat-icon>
                Minha conta
              </a>
            </div>
          </div>
        </header>

        <section class="anuncios-vitrine" *ngIf="anuncios().length">
          <article class="anuncio-card" *ngFor="let a of anuncios()" [class.destaque]="a.destaque">
            <mat-icon>{{ a.destaque ? 'local_fire_department' : 'campaign' }}</mat-icon>
            <div>
              <strong>{{ a.titulo }}</strong>
              <p *ngIf="a.texto">{{ a.texto }}</p>
            </div>
          </article>
        </section>

        <section class="info">
          <div class="card" *ngIf="tenant()?.endereco">
            <mat-icon>location_on</mat-icon>
            <div>
              <strong>Endereço</strong>
              <p>{{ tenant()?.endereco }} - {{ tenant()?.cidade }} / {{ tenant()?.estado }}</p>
            </div>
          </div>
          <div class="card" *ngIf="tenant()?.telefone">
            <mat-icon>phone</mat-icon>
            <div>
              <strong>Contato</strong>
              <p>{{ tenant()?.telefone }}</p>
            </div>
          </div>
          <div class="card" *ngIf="tenant()?.whatsApp">
            <mat-icon>chat</mat-icon>
            <div>
              <strong>WhatsApp</strong>
              <p>{{ tenant()?.whatsApp }}</p>
            </div>
          </div>
        </section>

        <section class="servicos" *ngIf="servicos().length">
          <h2><mat-icon>list_alt</mat-icon> Nossos serviços</h2>
          <div class="grid-servicos">
            <article class="card-servico" *ngFor="let s of servicos()">
              <div class="corpo">
                <h3>{{ s.nome }}</h3>
                <p *ngIf="s.descricao">{{ s.descricao }}</p>
                <div class="meta">
                  <span><mat-icon>schedule</mat-icon> {{ s.duracaoMinutos }} min</span>
                  <strong>R$ {{ s.preco | number:'1.2-2' }}</strong>
                </div>
              </div>
              <a mat-flat-button color="primary" [routerLink]="['/t', slug, 'agendar', s.id]">
                Agendar
              </a>
            </article>
          </div>
          <a mat-stroked-button [routerLink]="['/t', slug, 'servicos']" class="ver-todos">
            Ver catálogo completo <mat-icon>arrow_forward</mat-icon>
          </a>
        </section>

        <section class="avaliacoes" *ngIf="resumo()?.total">
          <h2><mat-icon>star</mat-icon> O que dizem nossos clientes</h2>
          <div class="resumo">
            <strong class="media">{{ resumo()?.media | number:'1.1-1' }}</strong>
            <div class="estrelas">
              <mat-icon *ngFor="let n of [1,2,3,4,5]"
                [class.ativa]="n <= (resumo()?.media || 0)">
                {{ n <= (resumo()?.media || 0) ? 'star' : 'star_border' }}
              </mat-icon>
            </div>
            <span class="total">{{ resumo()?.total }} avaliações</span>
          </div>
          <div class="recentes">
            <article *ngFor="let r of resumo()?.recentes" class="aval">
              <div class="cabecalho">
                <strong>{{ r.clienteNome }}</strong>
                <div class="estrelas pequenas">
                  <mat-icon *ngFor="let n of [1,2,3,4,5]" [class.ativa]="n <= r.nota">
                    {{ n <= r.nota ? 'star' : 'star_border' }}
                  </mat-icon>
                </div>
              </div>
              <p *ngIf="r.comentario">"{{ r.comentario }}"</p>
            </article>
          </div>
        </section>
      </ng-container>
    </ng-container>
  `,
  styleUrls: ['./home.component.scss'],
  styles: [`
    .cta { display: flex; gap: 0.5rem; flex-wrap: wrap; justify-content: center; }
    .cta .entrar { background: rgba(255,255,255,0.85); }
    .anuncios-vitrine {
      max-width: 60rem; margin: 0 auto; padding: 1.5rem 1rem 0;
      display: grid; gap: 0.75rem;
    }
    .anuncio-card {
      display: flex; gap: 0.75rem; align-items: flex-start;
      background: var(--cor-fundo-card); border-left: 0.25rem solid var(--cor-primaria);
      border-radius: 0.5rem; padding: 0.875rem 1rem; box-shadow: 0 1px 3px rgba(0,0,0,0.08);
    }
    .anuncio-card mat-icon { color: var(--cor-primaria); flex-shrink: 0; }
    .anuncio-card strong { display: block; font-size: 1.05rem; }
    .anuncio-card p { margin: 0.25rem 0 0; color: var(--cor-texto-suave); }
    .anuncio-card.destaque { border-left-color: var(--cor-acento); }
    .anuncio-card.destaque mat-icon { color: var(--cor-acento); }
    .servicos { padding: 2rem 1rem; max-width: 60rem; margin: 0 auto; }
    .servicos h2 { display: flex; align-items: center; gap: 0.5rem; }
    .grid-servicos { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fill, minmax(16rem, 1fr)); }
    .card-servico {
      background: var(--cor-fundo-card); border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.08);
      padding: 1rem; display: flex; flex-direction: column; gap: 0.75rem;
    }
    .card-servico .corpo { flex: 1; }
    .card-servico h3 { margin: 0 0 0.25rem; font-size: 1.125rem; }
    .card-servico p { margin: 0 0 0.5rem; color: #666; font-size: 0.875rem; }
    .card-servico .meta { display: flex; justify-content: space-between; align-items: center; }
    .card-servico .meta span { display: flex; align-items: center; gap: 0.25rem; color: #666; font-size: 0.875rem; }
    .card-servico .meta mat-icon { font-size: 1rem; width: 1rem; height: 1rem; }
    .card-servico .meta strong { color: var(--cor-primaria); font-size: 1.125rem; }
    .ver-todos { margin-top: 1.5rem; }
    .avaliacoes { padding: 2rem 1rem; max-width: 60rem; margin: 0 auto; }
    .avaliacoes h2 { display: flex; align-items: center; gap: 0.5rem; }
    .resumo { display: flex; align-items: center; gap: 1rem; margin-bottom: 1.5rem; }
    .resumo .media { font-size: 2.5rem; }
    .estrelas { display: flex; gap: 0.1rem; }
    .estrelas mat-icon { color: #ccc; }
    .estrelas mat-icon.ativa { color: #fbc02d; }
    .estrelas.pequenas mat-icon { font-size: 1rem; width: 1rem; height: 1rem; }
    .resumo .total { color: #666; }
    .recentes { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fill, minmax(18rem, 1fr)); }
    .aval { background: var(--cor-fundo-card); padding: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.08); }
    .aval .cabecalho { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem; }
    .aval p { margin: 0; font-style: italic; color: #444; }
  `]
})
export class HomeComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private tenantSvc = inject(TenantService);
  private theme = inject(ThemeService);
  private api = inject(ApiService);

  slug = '';
  tenant = signal<Tenant | null>(null);
  estado = signal<EstadoCarga>('carregando');

  // Uploads são servidos pela API — URL relativa precisa virar absoluta.
  logoUrl(): string { return urlUpload(this.tenant()?.personalizacao?.logoUrl); }
  bannerStyle(): string {
    const url = urlUpload(this.tenant()?.personalizacao?.bannerUrl);
    return url ? `url('${url}')` : 'none';
  }
  resumo = signal<ResumoAvaliacoes | null>(null);
  servicos = signal<Servico[]>([]);
  anuncios = signal<AnuncioVitrine[]>([]);

  ngOnInit() {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    if (!this.slug) {
      this.estado.set('naoEncontrado');
      return;
    }
    this.tenantSvc.slug = this.slug;
    sessionStorage.setItem('tenant_slug', this.slug);
    this.tenantSvc.carregarTenant(this.slug).subscribe({
      next: t => {
        this.tenant.set(t);
        this.theme.aplicarPersonalizacao(t.personalizacao);
        this.estado.set('ok');
        // Carrega avaliações e catálogo em paralelo (silenciosamente — falha não bloqueia a home)
        this.api.resumoAvaliacoes(this.slug).subscribe({
          next: r => this.resumo.set(r),
          error: () => { /* sem avaliações ou erro: simplesmente não exibe a seção */ }
        });
        this.api.servicosPublicos(this.slug).subscribe({
          next: list => this.servicos.set(list || []),
          error: () => { /* sem serviços: a home fica só com hero + contato */ }
        });
        this.api.anunciosPublicos(this.slug).subscribe({
          next: list => this.anuncios.set(list || []),
          error: () => { /* sem anúncios: seção não aparece */ }
        });
      },
      error: () => this.estado.set('naoEncontrado')
    });
  }
}
