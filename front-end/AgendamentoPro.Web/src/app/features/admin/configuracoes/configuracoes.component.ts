import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { TenantService } from '../../../core/services/tenant.service';
import { ThemeService } from '../../../core/services/theme.service';
import { WebPushService } from '../../../core/services/web-push.service';
import { AnuncioVitrine, Tenant } from '../../../core/models/tenant.model';
import { environment } from '../../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';

@Component({
  selector: 'app-configuracoes',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule,
    MatTabsModule, MatSlideToggleModule, MatIconModule, MatSelectModule],
  template: `
    <h1>Configurações</h1>
    <mat-tab-group>
      <mat-tab label="Empresa">
        <div class="form" *ngIf="tenant() as t">
          <mat-form-field appearance="outline"><mat-label>Nome</mat-label><input matInput [(ngModel)]="t.nome" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Segmento</mat-label><input matInput [(ngModel)]="t.segmento" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>CNPJ</mat-label><input matInput [(ngModel)]="t.cnpj" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>E-mail</mat-label><input matInput [(ngModel)]="t.email" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Telefone</mat-label><input matInput [(ngModel)]="t.telefone" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>WhatsApp</mat-label><input matInput [(ngModel)]="t.whatsApp" /></mat-form-field>
          <mat-form-field appearance="outline" class="full"><mat-label>Endereço</mat-label><input matInput [(ngModel)]="t.endereco" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Cidade</mat-label><input matInput [(ngModel)]="t.cidade" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Estado</mat-label><input matInput maxlength="2" [(ngModel)]="t.estado" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>CEP</mat-label><input matInput [(ngModel)]="t.cep" /></mat-form-field>
          <mat-form-field appearance="outline" class="full"><mat-label>Descrição</mat-label><textarea matInput rows="3" [(ngModel)]="t.descricao"></textarea></mat-form-field>
          <button mat-flat-button color="primary" (click)="salvarEmpresa()">Salvar</button>
        </div>
      </mat-tab>

      <mat-tab label="Minha página">
        <div class="form" *ngIf="tenant() as t">
          <p class="hint full">
            É assim que sua loja aparece para o cliente.
            <a [href]="'/t/' + t.slug" target="_blank">Ver minha página <mat-icon class="inline-icon">open_in_new</mat-icon></a>
          </p>

          <h3 class="full secao">Identidade visual</h3>
          <mat-form-field appearance="outline" class="full"><mat-label>Logo URL</mat-label><input matInput [(ngModel)]="t.personalizacao.logoUrl" /></mat-form-field>
          <mat-form-field appearance="outline" class="full"><mat-label>Banner URL (imagem de capa)</mat-label><input matInput [(ngModel)]="t.personalizacao.bannerUrl" /></mat-form-field>
          <mat-form-field appearance="outline" class="full"><mat-label>Favicon URL</mat-label><input matInput [(ngModel)]="t.personalizacao.faviconUrl" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Cor primária</mat-label><input matInput type="color" [(ngModel)]="t.personalizacao.corPrimaria" /><mat-hint>Botões e preços</mat-hint></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Cor secundária</mat-label><input matInput type="color" [(ngModel)]="t.personalizacao.corSecundaria" /><mat-hint>Fundo do banner sem imagem</mat-hint></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Cor acento</mat-label><input matInput type="color" [(ngModel)]="t.personalizacao.corAcento" /><mat-hint>Promoções em destaque</mat-hint></mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Fonte</mat-label>
            <mat-select [(ngModel)]="t.personalizacao.fonte">
              <mat-option *ngFor="let f of fontes" [value]="f" [style.font-family]="f">{{ f }}</mat-option>
            </mat-select>
            <mat-hint>Carregada do Google Fonts</mat-hint>
          </mat-form-field>
          <button mat-flat-button color="primary" class="full btn-salvar" (click)="salvarPersonalizacao()">Salvar e aplicar</button>

          <h3 class="full secao">Anúncios e promoções</h3>
          <p class="hint full">Aparecem no topo da sua página pública. Use para promoções da semana, avisos de horário, novidades.</p>

          <div class="anuncio full" *ngFor="let a of anuncios(); let i = index" [class.inativo]="!a.ativo">
            <div class="anuncio-campos">
              <mat-form-field appearance="outline" class="full">
                <mat-label>Título</mat-label>
                <input matInput [(ngModel)]="a.titulo" maxlength="60" placeholder="Ex: Semana do brilho — 20% off" />
              </mat-form-field>
              <mat-form-field appearance="outline" class="full">
                <mat-label>Texto (opcional)</mat-label>
                <input matInput [(ngModel)]="a.texto" maxlength="200" placeholder="Ex: Lavagem completa por R$ 56 até sexta." />
              </mat-form-field>
            </div>
            <div class="anuncio-acoes">
              <mat-slide-toggle [(ngModel)]="a.ativo">Visível</mat-slide-toggle>
              <mat-slide-toggle [(ngModel)]="a.destaque">Destaque</mat-slide-toggle>
              <button mat-icon-button color="warn" (click)="removerAnuncio(i)" aria-label="Remover anúncio">
                <mat-icon>delete</mat-icon>
              </button>
            </div>
          </div>

          <p class="hint full" *ngIf="!anuncios().length">Nenhum anúncio ainda — crie o primeiro.</p>

          <div class="full anuncio-botoes">
            <button mat-stroked-button (click)="adicionarAnuncio()" [disabled]="anuncios().length >= 8">
              <mat-icon>add</mat-icon> Novo anúncio
            </button>
            <button mat-flat-button color="primary" (click)="salvarAnuncios()" [disabled]="salvandoAnuncios()">
              <mat-icon>campaign</mat-icon> Publicar anúncios
            </button>
          </div>
        </div>
      </mat-tab>

      <mat-tab label="Regras de negócio">
        <div class="form" *ngIf="tenant() as t">
          <mat-form-field appearance="outline"><mat-label>Percentual de entrada (%)</mat-label><input matInput type="number" [(ngModel)]="t.regras.percentualEntrada" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Buffer entre atendimentos (min)</mat-label><input matInput type="number" [(ngModel)]="t.regras.bufferMinutos" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Antecedência mínima (h)</mat-label><input matInput type="number" [(ngModel)]="t.regras.antecedenciaMinHoras" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Antecedência máxima (dias)</mat-label><input matInput type="number" [(ngModel)]="t.regras.antecedenciaMaxDias" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Limite cancelamento (h)</mat-label><input matInput type="number" [(ngModel)]="t.regras.limiteCancelamentoHoras" /></mat-form-field>
          <button mat-flat-button color="primary" (click)="salvarRegras()">Salvar</button>
        </div>
      </mat-tab>

      <mat-tab label="Notificações">
        <div class="form-coluna">
          <h3>Notificações push</h3>
          <p class="hint" *ngIf="!push.isSupported">
            Notificações push não estão disponíveis nesta sessão (servir HTTPS + acesso pela versão buildada).
          </p>
          <p class="hint" *ngIf="push.isSupported && !push.serverAtivo()">
            VAPID não está configurado no servidor — defina <code>VAPID_PUBLIC_KEY</code> e <code>VAPID_PRIVATE_KEY</code> em .env e reinicie.
          </p>
          <div *ngIf="push.isSupported && push.serverAtivo()" class="toggle-linha">
            <mat-slide-toggle [checked]="push.isSubscribed()" (change)="alternarPush($event.checked)">
              Receber notificações de novos agendamentos, pagamentos e cancelamentos neste dispositivo
            </mat-slide-toggle>
            <p class="hint" *ngIf="push.isSubscribed()">
              Você vai receber notificações mesmo com o app fechado.
            </p>
          </div>
        </div>
      </mat-tab>
    </mat-tab-group>
  `,
  styles: [`
    h1 { margin: 0 0 1rem; }
    .form { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; padding: 1rem; }
    .form .full { grid-column: 1 / -1; }
    .form-coluna { display: flex; flex-direction: column; gap: 0.75rem; padding: 1rem; max-width: 40rem; }
    .hint { color: var(--cor-texto-secundario); font-size: 0.875rem; }
    .secao { margin: 1rem 0 0; color: var(--cor-primaria); font-size: 1rem; }
    .inline-icon { font-size: 0.9rem; width: 0.9rem; height: 0.9rem; vertical-align: middle; }
    .btn-salvar { justify-self: start; }
    .anuncio {
      display: flex; gap: 0.75rem; align-items: flex-start;
      border: 1px solid var(--cor-borda); border-radius: 0.5rem; padding: 0.75rem;
      background: var(--cor-fundo-card);
    }
    .anuncio.inativo { opacity: 0.55; }
    .anuncio-campos { flex: 1; display: flex; flex-direction: column; }
    .anuncio-acoes { display: flex; flex-direction: column; gap: 0.5rem; padding-top: 0.5rem; }
    .anuncio-botoes { display: flex; gap: 0.75rem; }
    @media (max-width: 36rem) { .anuncio { flex-direction: column; } .anuncio-acoes { flex-direction: row; align-items: center; } }
    .toggle-linha { display: flex; flex-direction: column; gap: 0.5rem; }
    code { background: var(--cor-borda); padding: 0.1rem 0.3rem; border-radius: 0.2rem; font-size: 0.85rem; }
    @media (max-width: 36rem) { .form { grid-template-columns: 1fr; } }
  `]
})
export class ConfiguracoesComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private theme = inject(ThemeService);
  private tenantSvc = inject(TenantService);
  private snack = inject(MatSnackBar);
  private http = inject(HttpClient);
  push = inject(WebPushService);

  tenant = signal<Tenant | null>(null);
  anuncios = signal<AnuncioVitrine[]>([]);
  salvandoAnuncios = signal(false);

  // Fontes populares do Google Fonts — o ThemeService baixa a escolhida.
  fontes = ['Roboto', 'Inter', 'Poppins', 'Montserrat', 'Lato', 'Open Sans',
    'Nunito', 'Raleway', 'Playfair Display', 'Merriweather', 'Bebas Neue', 'Pacifico'];

  ngOnInit() {
    const tid = this.auth.user()?.tenantId;
    if (!tid) return;
    this.http.get<Tenant>(`${environment.apiUrl}/tenants/${tid}`).subscribe(t => this.tenant.set(t));
    this.api.anunciosAdmin().subscribe({
      next: lista => this.anuncios.set(lista || []),
      error: () => { /* sem anúncios ainda */ }
    });
    this.push.carregarVapidKey().catch(() => {});
  }

  adicionarAnuncio() {
    if (this.anuncios().length >= 8) return;
    this.anuncios.update(l => [...l, { titulo: '', texto: '', destaque: false, ativo: true }]);
  }

  removerAnuncio(i: number) {
    this.anuncios.update(l => l.filter((_, idx) => idx !== i));
  }

  salvarAnuncios() {
    const invalido = this.anuncios().find(a => !a.titulo?.trim());
    if (invalido) {
      this.snack.open('Todo anúncio precisa de um título.', 'OK', { duration: 3000 });
      return;
    }
    this.salvandoAnuncios.set(true);
    this.api.salvarAnuncios(this.anuncios()).subscribe({
      next: lista => {
        this.anuncios.set(lista || []);
        this.salvandoAnuncios.set(false);
        this.snack.open('Anúncios publicados na sua página!', 'OK', { duration: 3000 });
      },
      error: e => {
        this.salvandoAnuncios.set(false);
        this.snack.open(e.error?.message || 'Falha ao publicar anúncios.', 'OK',
          { duration: 5000, panelClass: 'snack-erro' });
      }
    });
  }

  async alternarPush(ativar: boolean) {
    try {
      if (ativar) await this.push.subscribe();
      else await this.push.unsubscribe();
      this.snack.open(ativar ? 'Notificações ativadas' : 'Notificações desativadas', 'OK', { duration: 2500 });
    } catch (e: any) {
      this.snack.open('Falha: ' + (e?.message ?? 'erro desconhecido'), 'OK', { duration: 4000 });
    }
  }

  salvarEmpresa() {
    const t = this.tenant();
    if (!t) return;
    this.api.atualizarTenant(t.id, t).subscribe(() => this.snack.open('Salvo!', 'OK', { duration: 2000 }));
  }
  salvarPersonalizacao() {
    const t = this.tenant();
    if (!t) return;
    this.api.atualizarPersonalizacao(t.id, t.personalizacao).subscribe(() => {
      this.theme.aplicarPersonalizacao(t.personalizacao);
      this.snack.open('Personalização aplicada!', 'OK', { duration: 2000 });
    });
  }
  salvarRegras() {
    const t = this.tenant();
    if (!t) return;
    this.api.atualizarRegras(t.id, t.regras).subscribe(() => this.snack.open('Regras salvas!', 'OK', { duration: 2000 }));
  }
}
