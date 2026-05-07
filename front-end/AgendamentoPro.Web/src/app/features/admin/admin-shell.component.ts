import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet,
    MatSidenavModule, MatToolbarModule, MatIconModule, MatButtonModule, MatListModule],
  template: `
    <mat-sidenav-container class="shell">
      <mat-sidenav #drawer
        [mode]="isMobile() ? 'over' : 'side'"
        [opened]="!isMobile()"
        class="sidenav">
        <div class="brand">
          <img src="logo.svg" alt="" class="brand-logo" />
          <strong>AgendamentoPro</strong>
        </div>

        <div class="perfil-info">
          <mat-icon>{{ ehSuperAdmin() ? 'shield' : 'badge' }}</mat-icon>
          <div>
            <small>{{ ehSuperAdmin() ? 'SuperAdmin' : auth.user()?.perfil }}</small>
            <strong>{{ auth.user()?.nome }}</strong>
          </div>
        </div>

        <mat-nav-list>
          <ng-container *ngIf="ehSuperAdmin()">
            <a mat-list-item routerLink="/admin/empresas" routerLinkActive="ativo" (click)="aoNavegar(drawer)">
              <mat-icon>apartment</mat-icon> Empresas
            </a>
          </ng-container>

          <ng-container *ngIf="!ehSuperAdmin()">
            <a mat-list-item routerLink="/admin/dashboard" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>dashboard</mat-icon> Dashboard</a>
            <a mat-list-item routerLink="/admin/agenda" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>calendar_today</mat-icon> Agenda</a>
            <a mat-list-item routerLink="/admin/servicos" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>list</mat-icon> Serviços</a>
            <a mat-list-item routerLink="/admin/recursos" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>build</mat-icon> Recursos</a>
            <a mat-list-item routerLink="/admin/clientes" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>people</mat-icon> Clientes</a>
            <a mat-list-item routerLink="/admin/relatorios" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>insert_chart</mat-icon> Relatórios</a>
            <a mat-list-item routerLink="/admin/configuracoes" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>settings</mat-icon> Configurações</a>
          </ng-container>
        </mat-nav-list>
      </mat-sidenav>

      <mat-sidenav-content>
        <mat-toolbar class="topbar">
          <button mat-icon-button (click)="drawer.toggle()" *ngIf="isMobile()">
            <mat-icon>menu</mat-icon>
          </button>
          <span class="spacer"></span>
          <button mat-icon-button (click)="sair()" title="Sair">
            <mat-icon>logout</mat-icon>
          </button>
        </mat-toolbar>
        <main class="conteudo">
          <router-outlet></router-outlet>
        </main>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [`
    .shell { height: 100vh; }
    .sidenav {
      width: 16rem;
      background: linear-gradient(180deg, #1e1b4b 0%, #312e81 100%);
      color: #fff;
      border-right: 0;
    }
    ::ng-deep .sidenav .mat-mdc-list-item.ativo {
      background: rgba(255,255,255,0.12) !important;
      border-left: 0.1875rem solid #a78bfa;
    }
    ::ng-deep .sidenav .mat-mdc-list-item .mdc-list-item__primary-text { color: #e0e7ff !important; }
    ::ng-deep .sidenav .mat-mdc-list-item.ativo .mdc-list-item__primary-text { color: #fff !important; }
    ::ng-deep .sidenav a mat-icon { color: #c7d2fe; }
    ::ng-deep .sidenav a.ativo mat-icon { color: #fff; }
    .brand {
      padding: 1.25rem 1rem;
      display: flex;
      gap: 0.625rem;
      align-items: center;
      border-bottom: 1px solid rgba(255,255,255,0.1);
    }
    .brand-logo { width: 2rem; height: 2rem; border-radius: 0.5rem; }
    .brand strong { font-weight: 700; letter-spacing: -0.02em; font-size: 1.0625rem; }
    .perfil-info {
      padding: 0.875rem 1rem;
      display: flex;
      align-items: center;
      gap: 0.625rem;
      background: rgba(255,255,255,0.05);
      border-bottom: 1px solid rgba(255,255,255,0.1);
    }
    .perfil-info mat-icon {
      color: #a78bfa;
      font-size: 1.5rem;
      width: 1.5rem;
      height: 1.5rem;
      flex-shrink: 0;
    }
    .perfil-info small {
      display: block;
      font-size: 0.6875rem;
      color: #c7d2fe;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      margin-bottom: 0.0625rem;
    }
    .perfil-info strong {
      display: block;
      font-size: 0.875rem;
      color: #fff;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 10rem;
    }
    .topbar { background: #fff; border-bottom: 1px solid #e4e4e7; position: sticky; top: 0; z-index: 5; }
    .spacer { flex: 1; }
    .conteudo { padding: 1.5rem; background: #f9fafb; min-height: calc(100vh - 4rem); }
    @media (max-width: 48rem) { .conteudo { padding: 1rem; } }
  `]
})
export class AdminShellComponent implements OnInit {
  auth = inject(AuthService);
  private router = inject(Router);
  private breakpoint = inject(BreakpointObserver);
  private destroyRef = inject(DestroyRef);

  isMobile = signal(false);

  constructor() {
    this.breakpoint.observe([Breakpoints.Handset, Breakpoints.Small])
      .pipe(takeUntilDestroyed())
      .subscribe(r => this.isMobile.set(r.matches));
  }

  ngOnInit() {
    // Imediato: se SuperAdmin entrou em rota tenant-scoped, manda pra /admin/empresas
    this.corrigirRota(this.router.url);
    this.router.events
      .pipe(
        filter(e => e instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(e => this.corrigirRota((e as NavigationEnd).urlAfterRedirects));
  }

  private corrigirRota(url: string) {
    if (!this.ehSuperAdmin()) return;
    if (url === '/admin' || url.startsWith('/admin/dashboard') || url.startsWith('/admin/agenda') ||
        url.startsWith('/admin/servicos') || url.startsWith('/admin/recursos') ||
        url.startsWith('/admin/clientes') || url.startsWith('/admin/relatorios') ||
        url.startsWith('/admin/configuracoes')) {
      this.router.navigate(['/admin/empresas']);
    }
  }

  ehSuperAdmin(): boolean {
    return this.auth.user()?.perfil === 'SuperAdmin';
  }

  aoNavegar(drawer: { close: () => void }) {
    if (this.isMobile()) drawer.close();
  }

  sair() {
    this.auth.logout();
    this.router.navigate(['/admin/login']);
  }
}
