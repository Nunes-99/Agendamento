import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Servico } from '../models/servico.model';
import { Agendamento, CriarAgendamentoInput, CriarAgendamentoResult, SlotDisponivel } from '../models/agendamento.model';

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
}
