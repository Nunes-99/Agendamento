import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/services/auth.service';
import { ThemeModeService } from '../../core/services/theme-mode.service';
import { RealtimeService } from '../../core/services/realtime.service';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet,
    MatSidenavModule, MatToolbarModule, MatIconModule, MatButtonModule, MatListModule,
    MatBadgeModule, MatMenuModule],
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
            <a mat-list-item routerLink="/admin/combos" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>local_offer</mat-icon> Combos</a>
            <a mat-list-item routerLink="/admin/cupons" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>confirmation_number</mat-icon> Cupons</a>
            <a mat-list-item routerLink="/admin/recorrencias" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>event_repeat</mat-icon> Recorrências</a>
            <a mat-list-item routerLink="/admin/pacotes" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>inventory_2</mat-icon> Pacotes</a>
            <a mat-list-item routerLink="/admin/fidelidade" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>loyalty</mat-icon> Fidelidade</a>
            <a mat-list-item routerLink="/admin/avaliacoes" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>star</mat-icon> Avaliações</a>
            <a mat-list-item routerLink="/admin/bloqueios" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>block</mat-icon> Bloqueios</a>
            <a mat-list-item routerLink="/admin/lista-espera" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>hourglass_top</mat-icon> Lista de espera</a>
            <a mat-list-item routerLink="/admin/kpis" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>insights</mat-icon> KPIs</a>
            <a mat-list-item routerLink="/admin/caixa" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>point_of_sale</mat-icon> Caixa</a>
            <a mat-list-item routerLink="/admin/relatorios" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>insert_chart</mat-icon> Relatórios</a>
            <a mat-list-item routerLink="/admin/auditoria" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>fact_check</mat-icon> Auditoria</a>
            <a mat-list-item routerLink="/admin/lgpd" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>privacy_tip</mat-icon> LGPD</a>
            <a mat-list-item routerLink="/admin/seguranca/2fa" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>shield</mat-icon> 2FA</a>
            <a mat-list-item routerLink="/admin/importar-clientes" routerLinkActive="ativo" (click)="aoNavegar(drawer)"><mat-icon>upload_file</mat-icon> Importar CSV</a>
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

          <button mat-icon-button [matMenuTriggerFor]="notifMenu"
            [matBadge]="naoLidas() || null" matBadgeColor="warn"
            aria-label="Notificações" *ngIf="!ehSuperAdmin()">
            <mat-icon>{{ realtime.conectado() ? 'notifications' : 'notifications_off' }}</mat-icon>
          </button>
          <mat-menu #notifMenu="matMenu" class="notif-menu">
            <div class="notif-cabecalho" *ngIf="notificacoes().length; else notifVazio">
              <strong>Notificações</strong>
              <button mat-button (click)="limparNotif($event)">Limpar</button>
            </div>
            <ng-template #notifVazio>
              <div class="notif-vazio">
                <mat-icon>notifications_off</mat-icon>
                <p>Sem notificações novas.</p>
              </div>
            </ng-template>
            <button mat-menu-item *ngFor="let n of notificacoes()" (click)="abrirNotif(n)">
              <mat-icon>{{ iconeNotif(n.evento) }}</mat-icon>
              <div class="notif-item">
                <strong>{{ tituloNotif(n) }}</strong>
                <small>{{ n.data | date:'HH:mm' }}</small>
              </div>
            </button>
          </mat-menu>

          <button mat-icon-button (click)="theme.alternar()"
            [title]="theme.mode() === 'dark' ? 'Modo claro' : 'Modo escuro'"
            aria-label="Alternar tema">
            <mat-icon>{{ theme.mode() === 'dark' ? 'light_mode' : 'dark_mode' }}</mat-icon>
          </button>
          <button mat-icon-button (click)="sair()" title="Sair" aria-label="Sair">
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
    .notif-cabecalho { display: flex; justify-content: space-between; align-items: center; padding: 0.5rem 1rem; border-bottom: 1px solid #eee; }
    .notif-vazio { text-align: center; padding: 1.5rem; color: #888; }
    .notif-vazio mat-icon { font-size: 2rem; width: 2rem; height: 2rem; }
    .notif-item { display: flex; flex-direction: column; line-height: 1.2; }
    .notif-item small { color: #888; font-size: 0.75rem; }
    ::ng-deep .notif-menu .mat-mdc-menu-panel { min-width: 18rem; max-width: 24rem; }
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
  theme = inject(ThemeModeService);
  realtime = inject(RealtimeService);
  private snack = inject(MatSnackBar);
  private router = inject(Router);
  private breakpoint = inject(BreakpointObserver);
  private destroyRef = inject(DestroyRef);

  isMobile = signal(false);
  notificacoes = signal<Array<{ evento: string; payload: any; data: Date }>>([]);
  naoLidas = signal(0);

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

    // Conecta SignalR para notificações realtime do tenant.
    if (!this.ehSuperAdmin()) {
      this.realtime.conectar();
      this.realtime.on('novo-agendamento', (p: any) => {
        this.adicionarNotif('novo-agendamento', p);
        this.snack.open(`Novo agendamento: ${p.clienteNome} - ${p.servicoNome}`, 'Ver', { duration: 6000 })
          .onAction().subscribe(() => this.router.navigate(['/admin/agendamentos', p.agendamentoId]));
      });
      this.realtime.on('pagamento-aprovado', (p: any) => {
        this.adicionarNotif('pagamento-aprovado', p);
        this.snack.open(`Pagamento aprovado: agendamento #${p.agendamentoId}`, 'OK', { duration: 4000 });
      });
      this.realtime.on('agendamento-cancelado', (p: any) => {
        this.adicionarNotif('agendamento-cancelado', p);
        this.snack.open(`Agendamento #${p.agendamentoId} cancelado.`, 'OK', { duration: 4000 });
      });
    }
  }

  private adicionarNotif(evento: string, payload: any) {
    this.notificacoes.update(l => [{ evento, payload, data: new Date() }, ...l].slice(0, 20));
    this.naoLidas.update(n => Math.min(n + 1, 99));
  }

  iconeNotif(e: string): string {
    return ({
      'novo-agendamento': 'event_available',
      'pagamento-aprovado': 'paid',
      'agendamento-cancelado': 'event_busy'
    } as any)[e] || 'info';
  }

  tituloNotif(n: { evento: string; payload: any }): string {
    if (n.evento === 'novo-agendamento') return `Novo agendamento — ${n.payload.clienteNome}`;
    if (n.evento === 'pagamento-aprovado') return `Pagamento aprovado #${n.payload.agendamentoId}`;
    if (n.evento === 'agendamento-cancelado') return `Cancelado #${n.payload.agendamentoId}`;
    return n.evento;
  }

  abrirNotif(n: { evento: string; payload: any }) {
    const id = n.payload?.agendamentoId;
    if (id) this.router.navigate(['/admin/agendamentos', id]);
    this.naoLidas.set(0);
  }

  limparNotif(e: Event) {
    e.stopPropagation();
    this.notificacoes.set([]);
    this.naoLidas.set(0);
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
