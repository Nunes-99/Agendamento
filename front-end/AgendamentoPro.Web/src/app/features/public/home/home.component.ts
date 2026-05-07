import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TenantService } from '../../../core/services/tenant.service';
import { ThemeService } from '../../../core/services/theme.service';
import { Tenant } from '../../../core/models/tenant.model';
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
            <a mat-flat-button color="primary" [routerLink]="['/t', slug, 'servicos']">
              <mat-icon>event</mat-icon>
              Agendar agora
            </a>
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
      </ng-container>
    </ng-container>
  `,
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private tenantSvc = inject(TenantService);
  private theme = inject(ThemeService);

  slug = '';
  tenant = signal<Tenant | null>(null);
  estado = signal<EstadoCarga>('carregando');

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
      },
      error: () => this.estado.set('naoEncontrado')
    });
  }
}
