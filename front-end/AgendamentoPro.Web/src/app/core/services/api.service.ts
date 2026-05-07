import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Servico } from '../models/servico.model';
import { Agendamento, CriarAgendamentoInput, CriarAgendamentoResult, SlotDisponivel } from '../models/agendamento.model';
import { Avaliacao, ResumoAvaliacoes, ResponderAvaliacaoInput } from '../models/avaliacao.model';
import { Combo, ComboInput } from '../models/combo.model';
import { FotoAgendamento, TipoFoto } from '../models/foto.model';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  // ----- Público -----
  servicosPublicos(slug: string): Observable<Servico[]> {
    return this.http.get<Servico[]>(`${this.base}/t/${slug}/servicos`);
  }

  slots(slug: string, servicoId: number, data: string, recursoId?: number): Observable<SlotDisponivel[]> {
    let params = new HttpParams().set('servicoId', servicoId).set('data', data);
    if (recursoId) params = params.set('recursoId', recursoId);
    return this.http.get<SlotDisponivel[]>(`${this.base}/t/${slug}/slots`, { params });
  }

  criarAgendamento(slug: string, input: CriarAgendamentoInput): Observable<CriarAgendamentoResult> {
    return this.http.post<CriarAgendamentoResult>(`${this.base}/t/${slug}/agendamentos`, input);
  }

  consultarAgendamento(slug: string, id: number): Observable<Agendamento> {
    return this.http.get<Agendamento>(`${this.base}/t/${slug}/agendamentos/${id}`);
  }

  // ----- Admin -----
  dashboard(): Observable<any> { return this.http.get(`${this.base}/admin/dashboard`); }
  agendaDoDia(data: string, recursoId?: number): Observable<Agendamento[]> {
    let params = new HttpParams().set('data', data);
    if (recursoId) params = params.set('recursoId', recursoId);
    return this.http.get<Agendamento[]>(`${this.base}/admin/agendamentos/agenda`, { params });
  }
  agendaPorPeriodo(inicio: string, fim: string, recursoId?: number): Observable<Agendamento[]> {
    let params = new HttpParams().set('inicio', inicio).set('fim', fim);
    if (recursoId) params = params.set('recursoId', recursoId);
    return this.http.get<Agendamento[]>(`${this.base}/admin/agendamentos/agenda`, { params });
  }
  criarAgendamentoAdmin(input: any): Observable<Agendamento> {
    return this.http.post<Agendamento>(`${this.base}/admin/agendamentos`, input);
  }
  reagendarAgendamento(id: number, novaData: string, novaHoraInicio: string): Observable<Agendamento> {
    return this.http.post<Agendamento>(`${this.base}/admin/agendamentos/${id}/reagendar`, {
      novaData,
      novaHoraInicio
    });
  }
  listarAgendamentos(page: number, pageSize: number, data?: string, status?: number): Observable<any> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (data) params = params.set('data', data);
    if (status !== undefined && status !== null) params = params.set('status', status);
    return this.http.get(`${this.base}/admin/agendamentos`, { params });
  }
  cancelarAgendamento(id: number, motivo: string) {
    return this.http.post(`${this.base}/admin/agendamentos/${id}/cancelar`, { motivo });
  }
  iniciarAgendamento(id: number) { return this.http.post(`${this.base}/admin/agendamentos/${id}/iniciar`, {}); }
  concluirAgendamento(id: number) { return this.http.post(`${this.base}/admin/agendamentos/${id}/concluir`, {}); }
  noShowAgendamento(id: number) { return this.http.post(`${this.base}/admin/agendamentos/${id}/no-show`, {}); }
  confirmarPagamentoDinheiro(id: number) { return this.http.post(`${this.base}/admin/agendamentos/${id}/confirmar-pagamento`, {}); }

  servicosAdmin(): Observable<Servico[]> { return this.http.get<Servico[]>(`${this.base}/admin/servicos`); }
  cadastrarServico(input: Partial<Servico>) { return this.http.post<Servico>(`${this.base}/admin/servicos`, input); }
  atualizarServico(id: number, input: Partial<Servico>) { return this.http.put<Servico>(`${this.base}/admin/servicos/${id}`, input); }
  excluirServico(id: number) { return this.http.delete<void>(`${this.base}/admin/servicos/${id}`); }

  recursosAdmin(): Observable<any[]> { return this.http.get<any[]>(`${this.base}/admin/recursos`); }
  cadastrarRecurso(input: any) { return this.http.post(`${this.base}/admin/recursos`, input); }
  atualizarRecurso(id: number, input: any) { return this.http.put(`${this.base}/admin/recursos/${id}`, input); }

  clientesAdmin(page: number, pageSize: number, busca?: string): Observable<any> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (busca) params = params.set('busca', busca);
    return this.http.get(`${this.base}/admin/clientes`, { params });
  }

  relReceita(inicio: string, fim: string) { return this.http.get(`${this.base}/admin/relatorios/receita?inicio=${inicio}&fim=${fim}`); }
  relTopServicos(inicio: string, fim: string) { return this.http.get(`${this.base}/admin/relatorios/servicos-mais-vendidos?inicio=${inicio}&fim=${fim}`); }
  relOcupacao(inicio: string, fim: string) { return this.http.get(`${this.base}/admin/relatorios/ocupacao?inicio=${inicio}&fim=${fim}`); }
  relCancelamentos(inicio: string, fim: string) { return this.http.get(`${this.base}/admin/relatorios/cancelamentos?inicio=${inicio}&fim=${fim}`); }

  atualizarTenant(id: number, input: any) { return this.http.put(`${this.base}/tenants/${id}`, input); }
  atualizarPersonalizacao(id: number, input: any) { return this.http.put(`${this.base}/tenants/${id}/personalizacao`, input); }
  atualizarRegras(id: number, input: any) { return this.http.put(`${this.base}/tenants/${id}/regras`, input); }

  // ----- Avaliações -----
  buscarAvaliacaoPorToken(token: string): Observable<Avaliacao> {
    return this.http.get<Avaliacao>(`${this.base}/avaliacoes/${token}`);
  }
  responderAvaliacao(token: string, input: ResponderAvaliacaoInput): Observable<Avaliacao> {
    return this.http.post<Avaliacao>(`${this.base}/avaliacoes/${token}`, input);
  }
  resumoAvaliacoes(slug: string, top = 5): Observable<ResumoAvaliacoes> {
    return this.http.get<ResumoAvaliacoes>(`${this.base}/t/${slug}/avaliacoes`,
      { params: new HttpParams().set('top', top) });
  }
  listarAvaliacoes(page: number, pageSize: number, somenteRespondidas = false): Observable<any> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize)
      .set('somenteRespondidas', somenteRespondidas);
    return this.http.get(`${this.base}/admin/avaliacoes`, { params });
  }
  alterarVisibilidadeAvaliacao(id: number, publica: boolean) {
    return this.http.post(`${this.base}/admin/avaliacoes/${id}/visibilidade?publica=${publica}`, {});
  }
  obterLinkAvaliacao(agendamentoId: number): Observable<{ token: string; path: string }> {
    return this.http.post<{ token: string; path: string }>(
      `${this.base}/admin/agendamentos/${agendamentoId}/avaliacao-link`, {});
  }

  // ----- Combos -----
  combosPublicos(slug: string): Observable<Combo[]> {
    return this.http.get<Combo[]>(`${this.base}/t/${slug}/combos`);
  }
  combosAdmin(somenteAtivos = false): Observable<Combo[]> {
    return this.http.get<Combo[]>(`${this.base}/admin/combos`,
      { params: new HttpParams().set('somenteAtivos', somenteAtivos) });
  }
  obterCombo(id: number): Observable<Combo> {
    return this.http.get<Combo>(`${this.base}/admin/combos/${id}`);
  }
  cadastrarCombo(input: ComboInput) { return this.http.post<Combo>(`${this.base}/admin/combos`, input); }
  atualizarCombo(id: number, input: ComboInput) { return this.http.put<Combo>(`${this.base}/admin/combos/${id}`, input); }
  excluirCombo(id: number) { return this.http.delete<void>(`${this.base}/admin/combos/${id}`); }
  agendarCombo(slug: string, comboId: number, input: any): Observable<any> {
    return this.http.post(`${this.base}/t/${slug}/combos/${comboId}/agendar`, input);
  }
  agendamentosDoGrupoCombo(slug: string, grupoComboId: string): Observable<Agendamento[]> {
    return this.http.get<Agendamento[]>(`${this.base}/t/${slug}/combos/grupos/${grupoComboId}`);
  }

  // ----- Fotos antes/depois -----
  uploadFoto(agendamentoId: number, tipo: TipoFoto, arquivo: File): Observable<FotoAgendamento> {
    const form = new FormData();
    form.append('tipo', tipo.toString());
    form.append('arquivo', arquivo);
    return this.http.post<FotoAgendamento>(`${this.base}/admin/agendamentos/${agendamentoId}/fotos`, form);
  }
  listarFotos(agendamentoId: number): Observable<FotoAgendamento[]> {
    return this.http.get<FotoAgendamento[]>(`${this.base}/admin/agendamentos/${agendamentoId}/fotos`);
  }
  removerFoto(fotoId: number) {
    return this.http.delete<void>(`${this.base}/admin/agendamentos/fotos/${fotoId}`);
  }

  // ----- LGPD -----
  exportarDadosCliente(clienteId: number) {
    return this.http.get(`${this.base}/admin/lgpd/clientes/${clienteId}/exportar`);
  }
  anonimizarCliente(clienteId: number) {
    return this.http.post(`${this.base}/admin/lgpd/clientes/${clienteId}/anonimizar`, {});
  }
  anonimizarInativos(meses: number) {
    return this.http.post<{ anonimizados: number }>(
      `${this.base}/admin/lgpd/clientes/anonimizar-inativos?inativoHaMeses=${meses}`, {});
  }

  // ----- 2FA -----
  iniciar2FA() {
    return this.http.post<{ secret: string; otpauthUrl: string; ativo: boolean }>(
      `${this.base}/admin/2fa/iniciar`, {});
  }
  confirmar2FA(codigo: string) {
    return this.http.post<{ ativo: boolean }>(
      `${this.base}/admin/2fa/confirmar?codigo=${encodeURIComponent(codigo)}`, {});
  }
  desativar2FA(codigo: string) {
    return this.http.post<{ ativo: boolean }>(
      `${this.base}/admin/2fa/desativar?codigo=${encodeURIComponent(codigo)}`, {});
  }

  // ----- Self-service cliente (token público) -----
  obterMeuAgendamento(token: string): Observable<any> {
    return this.http.get(`${this.base}/agendamentos/acesso/${token}`);
  }
  cancelarMeuAgendamento(token: string, motivo: string) {
    return this.http.post(`${this.base}/agendamentos/acesso/${token}/cancelar`, { motivo });
  }
  reagendarMeuAgendamento(token: string, novaData: string, novaHoraInicio: string) {
    return this.http.post(`${this.base}/agendamentos/acesso/${token}/reagendar`,
      { novaData, novaHoraInicio });
  }

  // ----- Bloqueios admin -----
  listarBloqueios(inicio?: string, fim?: string) {
    let p = new HttpParams();
    if (inicio) p = p.set('inicio', inicio);
    if (fim) p = p.set('fim', fim);
    return this.http.get<any[]>(`${this.base}/admin/bloqueios`, { params: p });
  }
  criarBloqueio(input: { recursoId?: number; dataInicio: string; dataFim: string; motivo: string }) {
    return this.http.post(`${this.base}/admin/bloqueios`, input);
  }

  // ----- Lista de espera -----
  entrarListaEspera(slug: string, input: any) {
    return this.http.post(`${this.base}/t/${slug}/lista-espera`, input);
  }
  listarEsperaAdmin(data?: string, somenteNaoNotificados = true) {
    let p = new HttpParams().set('somenteNaoNotificados', somenteNaoNotificados);
    if (data) p = p.set('data', data);
    return this.http.get<any[]>(`${this.base}/admin/lista-espera`, { params: p });
  }
  notificarEspera(id: number) {
    return this.http.post(`${this.base}/admin/lista-espera/${id}/notificar`, {});
  }

  // ----- Audit log + KPIs + Caixa + CSV -----
  auditoria(page: number, pageSize: number, filtros: { tabela?: string; acao?: string; de?: string; ate?: string } = {}) {
    let p = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (filtros.tabela) p = p.set('tabela', filtros.tabela);
    if (filtros.acao) p = p.set('acao', filtros.acao);
    if (filtros.de) p = p.set('de', filtros.de);
    if (filtros.ate) p = p.set('ate', filtros.ate);
    return this.http.get<any>(`${this.base}/admin/tools/auditoria`, { params: p });
  }
  auditoriaDetalhe(id: number) {
    return this.http.get(`${this.base}/admin/tools/auditoria/${id}`);
  }
  kpisAvancados(mesRef?: string) {
    const p = mesRef ? new HttpParams().set('mesRef', mesRef) : new HttpParams();
    return this.http.get<any>(`${this.base}/admin/tools/kpis`, { params: p });
  }
  caixaDoDia(data?: string) {
    const p = data ? new HttpParams().set('data', data) : new HttpParams();
    return this.http.get<any>(`${this.base}/admin/tools/caixa`, { params: p });
  }
  importarClientesCsv(csvConteudo: string) {
    return this.http.post<{ inseridos: number; ignorados: number; erros: string[] }>(
      `${this.base}/admin/tools/clientes/importar-csv`, { csvConteudo });
  }

  // ----- Cupons -----
  listarCupons() { return this.http.get<any[]>(`${this.base}/admin/cupons`); }
  criarCupom(input: any) { return this.http.post<any>(`${this.base}/admin/cupons`, input); }
  alternarCupomAtivo(id: number, ativo: boolean) {
    return this.http.post(`${this.base}/admin/cupons/${id}/ativar?ativo=${ativo}`, {});
  }
  validarCupom(slug: string, codigo: string, valorBase: number) {
    return this.http.get<any>(
      `${this.base}/t/${slug}/cupons/${encodeURIComponent(codigo)}/validar?valorBase=${valorBase}`);
  }

  // ----- Recorrência -----
  criarRecorrencia(input: any) {
    return this.http.post<any>(`${this.base}/admin/recorrencias`, input);
  }
  listarRecorrencias() {
    return this.http.get<any[]>(`${this.base}/admin/recorrencias`);
  }

  // ----- Pacotes pré-pagos -----
  listarPacotes(slug?: string) {
    const url = slug ? `${this.base}/t/${slug}/pacotes` : `${this.base}/admin/pacotes`;
    return this.http.get<any[]>(url);
  }
  criarPacote(input: any) {
    return this.http.post<any>(`${this.base}/admin/pacotes`, input);
  }
  comprarPacote(slug: string, pacoteId: number, cliente: any) {
    return this.http.post<any>(`${this.base}/t/${slug}/pacotes/${pacoteId}/comprar`, cliente);
  }
  consultarStatusSaldoPacote(slug: string, saldoPacoteId: number) {
    return this.http.get<{ saldoPacoteId: number; status: string; restante: number }>(
      `${this.base}/t/${slug}/saldos-pacote/${saldoPacoteId}`);
  }
  listarSaldosPacoteCliente(clienteId: number) {
    return this.http.get<any[]>(`${this.base}/admin/saldos-pacote/cliente/${clienteId}`);
  }

  // ----- Fidelidade -----
  saldoPontos(clienteId: number) {
    return this.http.get<{ clienteId: number; saldo: number }>(
      `${this.base}/admin/fidelidade/clientes/${clienteId}`);
  }
  trocarPontosPorCupom(clienteId: number, pontos: number) {
    return this.http.post<{ codigo: string; valor: number; validoAte: string }>(
      `${this.base}/admin/fidelidade/trocar-por-cupom`, { clienteId, pontos });
  }
}
