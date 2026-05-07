import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // Rotas públicas (cliente final agenda)
  {
    path: 't/:slug',
    children: [
      { path: '', loadComponent: () => import('./features/public/home/home.component').then(m => m.HomeComponent) },
      { path: 'servicos', loadComponent: () => import('./features/public/servicos/servicos-publico.component').then(m => m.ServicosPublicoComponent) },
      { path: 'agendar/:servicoId', loadComponent: () => import('./features/public/agendar/agendar.component').then(m => m.AgendarComponent) },
      { path: 'pagamento/:agendamentoId', loadComponent: () => import('./features/public/pagamento/pagamento.component').then(m => m.PagamentoComponent) },
      { path: 'confirmacao/:agendamentoId', loadComponent: () => import('./features/public/confirmacao/confirmacao.component').then(m => m.ConfirmacaoComponent) }
    ]
  },

  // Login do administrador
  {
    path: 'admin/login',
    loadComponent: () => import('./features/admin/login/login.component').then(m => m.LoginComponent)
  },

  // Painel administrativo (autenticado)
  {
    path: 'admin',
    canActivate: [authGuard],
    loadComponent: () => import('./features/admin/admin-shell.component').then(m => m.AdminShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'empresas', loadComponent: () => import('./features/admin/empresas/empresas.component').then(m => m.EmpresasComponent) },
      { path: 'dashboard', loadComponent: () => import('./features/admin/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'agenda', loadComponent: () => import('./features/admin/agenda/agenda.component').then(m => m.AgendaComponent) },
      { path: 'servicos', loadComponent: () => import('./features/admin/servicos/servicos-admin.component').then(m => m.ServicosAdminComponent) },
      { path: 'recursos', loadComponent: () => import('./features/admin/recursos/recursos-admin.component').then(m => m.RecursosAdminComponent) },
      { path: 'clientes', loadComponent: () => import('./features/admin/clientes/clientes-admin.component').then(m => m.ClientesAdminComponent) },
      { path: 'relatorios', loadComponent: () => import('./features/admin/relatorios/relatorios.component').then(m => m.RelatoriosComponent) },
      { path: 'configuracoes', loadComponent: () => import('./features/admin/configuracoes/configuracoes.component').then(m => m.ConfiguracoesComponent) }
    ]
  },

  { path: '', pathMatch: 'full', redirectTo: 'admin/login' },
  { path: '**', redirectTo: 'admin/login' }
];
