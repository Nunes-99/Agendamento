import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TenantService } from '../../../core/services/tenant.service';
import { ThemeService } from '../../../core/services/theme.service';
import { ApiService } from '../../../core/services/api.service';
import { Tenant } from '../../../core/models/tenant.model';
import { ResumoAvaliacoes } from '../../../core/models/avaliacao.model';
import { TenantNaoEncontradoComponent } from '../tenant-nao-encontrado.component';

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
        <header class="hero" [style.background-image]="'url(' + (tenant()?.personalizacao?.bannerUrl || '') + ')'">
          <div class="hero-overlay">
            <img *ngIf="tenant()?.personalizacao?.logoUrl as logo" [src]="logo" alt="logo" class="logo" />
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
    .aval { background: #fff; padding: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.08); }
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
  resumo = signal<ResumoAvaliacoes | null>(null);

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
        // Carrega avaliações públicas em paralelo (silenciosamente — falha não bloqueia a home)
        this.api.resumoAvaliacoes(this.slug).subscribe({
          next: r => this.resumo.set(r),
          error: () => { /* sem avaliações ou erro: simplesmente não exibe a seção */ }
        });
      },
      error: () => this.estado.set('naoEncontrado')
    });
  }
}
