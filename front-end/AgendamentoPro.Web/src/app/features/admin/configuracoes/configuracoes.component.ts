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
import { Tenant } from '../../../core/models/tenant.model';
import { environment } from '../../../../environments/environment';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-configuracoes',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatTabsModule],
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

      <mat-tab label="Personalização">
        <div class="form" *ngIf="tenant() as t">
          <mat-form-field appearance="outline" class="full"><mat-label>Logo URL</mat-label><input matInput [(ngModel)]="t.personalizacao.logoUrl" /></mat-form-field>
          <mat-form-field appearance="outline" class="full"><mat-label>Banner URL</mat-label><input matInput [(ngModel)]="t.personalizacao.bannerUrl" /></mat-form-field>
          <mat-form-field appearance="outline" class="full"><mat-label>Favicon URL</mat-label><input matInput [(ngModel)]="t.personalizacao.faviconUrl" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Cor primária</mat-label><input matInput type="color" [(ngModel)]="t.personalizacao.corPrimaria" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Cor secundária</mat-label><input matInput type="color" [(ngModel)]="t.personalizacao.corSecundaria" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Cor acento</mat-label><input matInput type="color" [(ngModel)]="t.personalizacao.corAcento" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Fonte</mat-label><input matInput [(ngModel)]="t.personalizacao.fonte" /></mat-form-field>
          <button mat-flat-button color="primary" (click)="salvarPersonalizacao()">Salvar e aplicar</button>
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
    </mat-tab-group>
  `,
  styles: [`
    h1 { margin: 0 0 1rem; }
    .form { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; padding: 1rem; }
    .form .full { grid-column: 1 / -1; }
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

  tenant = signal<Tenant | null>(null);

  ngOnInit() {
    const tid = this.auth.user()?.tenantId;
    if (!tid) return;
    this.http.get<Tenant>(`${environment.apiUrl}/tenants/${tid}`).subscribe(t => this.tenant.set(t));
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
