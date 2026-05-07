import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // Rotas públicas (cliente final agenda)
  {
    path: 't/:slug',
    children: [
      { path: '', loadComponent: () => import('./features/public/home/home.component').then(m => m.HomeComponent) },
      { path: 'servicos', loadComponent: () => import('./features/public/servicos/servicos-publico.component').then(m => m.ServicosPublicoComponent) },
      { path: 'combos', loadComponent: () => import('./features/public/combos/combos-publico.component').then(m => m.CombosPublicoComponent) },
      { path: 'agendar-combo/:comboId', loadComponent: () => import('./features/public/combos/agendar-combo.component').then(m => m.AgendarComboComponent) },
      { path: 'agendar/:servicoId', loadComponent: () => import('./features/public/agendar/agendar.component').then(m => m.AgendarComponent) },
      { path: 'pacotes', loadComponent: () => import('./features/public/pacotes/comprar-pacote.component').then(m => m.ComprarPacoteComponent) },
      { path: 'lista-espera-publica', loadComponent: () => import('./features/public/lista-espera/lista-espera-publica.component').then(m => m.ListaEsperaPublicaComponent) },
      { path: 'pagamento/:agendamentoId', loadComponent: () => import('./features/public/pagamento/pagamento.component').then(m => m.PagamentoComponent) },
      { path: 'confirmacao/:agendamentoId', loadComponent: () => import('./features/public/confirmacao/confirmacao.component').then(m => m.ConfirmacaoComponent) }
    ]
  },

  // Avaliação pública (cliente final via token enviado por WhatsApp)
  {
    path: 'avaliar/:token',
    loadComponent: () => import('./features/public/avaliar/avaliar.component').then(m => m.AvaliarComponent)
  },
  // Self-service do cliente: ver/cancelar/reagendar agendamento sem login
  {
    path: 'meu-agendamento/:token',
    loadComponent: () => import('./features/public/meu-agendamento/meu-agendamento.component').then(m => m.MeuAgendamentoComponent)
  },
  {
    path: 'politica-privacidade',
    loadComponent: () => import('./features/public/legal/legal.component').then(m => m.PoliticaPrivacidadeComponent)
  },
  {
    path: 'termos-uso',
    loadComponent: () => import('./features/public/legal/legal.component').then(m => m.TermosUsoComponent)
  },

  // Login do administrador
  {
    path: 'admin/login',
    loadComponent: () => import('./features/admin/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'esqueci-senha',
    loadComponent: () => import('./features/admin/senha/esqueci-senha.component').then(m => m.EsqueciSenhaComponent)
  },
  {
    path: 'redefinir-senha',
    loadComponent: () => import('./features/admin/senha/redefinir-senha.component').then(m => m.RedefinirSenhaComponent)
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
      { path: 'combos', loadComponent: () => import('./features/admin/combos/combos-admin.component').then(m => m.CombosAdminComponent) },
      { path: 'recursos', loadComponent: () => import('./features/admin/recursos/recursos-admin.component').then(m => m.RecursosAdminComponent) },
      { path: 'clientes', loadComponent: () => import('./features/admin/clientes/clientes-admin.component').then(m => m.ClientesAdminComponent) },
      { path: 'avaliacoes', loadComponent: () => import('./features/admin/avaliacoes/avaliacoes.component').then(m => m.AvaliacoesComponent) },
      { path: 'agendamentos/:id/fotos', loadComponent: () => import('./features/admin/fotos/fotos-agendamento.component').then(m => m.FotosAgendamentoComponent) },
      { path: 'cupons', loadComponent: () => import('./features/admin/cupons/cupons.component').then(m => m.CuponsComponent) },
      { path: 'recorrencias', loadComponent: () => import('./features/admin/recorrencias/recorrencias.component').then(m => m.RecorrenciasComponent) },
      { path: 'pacotes', loadComponent: () => import('./features/admin/pacotes/pacotes-admin.component').then(m => m.PacotesAdminComponent) },
      { path: 'fidelidade', loadComponent: () => import('./features/admin/fidelidade/fidelidade.component').then(m => m.FidelidadeComponent) },
      { path: 'bloqueios', loadComponent: () => import('./features/admin/bloqueios/bloqueios.component').then(m => m.BloqueiosComponent) },
      { path: 'lista-espera', loadComponent: () => import('./features/admin/lista-espera/lista-espera.component').then(m => m.ListaEsperaComponent) },
      { path: 'auditoria', loadComponent: () => import('./features/admin/auditoria/auditoria.component').then(m => m.AuditoriaComponent) },
      { path: 'kpis', loadComponent: () => import('./features/admin/kpis/kpis.component').then(m => m.KpisComponent) },
      { path: 'caixa', loadComponent: () => import('./features/admin/caixa/caixa.component').then(m => m.CaixaComponent) },
      { path: 'importar-clientes', loadComponent: () => import('./features/admin/importar-clientes/importar-clientes.component').then(m => m.ImportarClientesComponent) },
      { path: 'lgpd', loadComponent: () => import('./features/admin/lgpd/lgpd.component').then(m => m.LgpdComponent) },
      { path: 'seguranca/2fa', loadComponent: () => import('./features/admin/seguranca/dois-fatores.component').then(m => m.DoisFatoresComponent) },
      { path: 'relatorios', loadComponent: () => import('./features/admin/relatorios/relatorios.component').then(m => m.RelatoriosComponent) },
      { path: 'configuracoes', loadComponent: () => import('./features/admin/configuracoes/configuracoes.component').then(m => m.ConfiguracoesComponent) }
    ]
  },

  { path: '', pathMatch: 'full', redirectTo: 'admin/login' },
  { path: '**', redirectTo: 'admin/login' }
];
