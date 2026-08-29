import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { environment } from '../../../../environments/environment';
import { Tenant } from '../../../core/models/tenant.model';

interface CriarEmpresaInput {
  nome: string;
  slug: string;
  segmento: string;
  email: string;
  telefone: string;
  adminNome: string;
  adminEmail: string;
  adminSenha: string;
  comDadosDeExemplo: boolean;
}

@Component({
  selector: 'app-criar-empresa-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatDialogModule, MatIconModule, MatCheckboxModule],
  template: `
    <h2 mat-dialog-title>
      <mat-icon>add_business</mat-icon>
      Nova empresa
    </h2>
    <mat-dialog-content>
      <p class="ajuda">A empresa terá site público em <code>localhost:4200/t/{{ form.slug || 'slug' }}</code></p>

      <h3>Dados da empresa</h3>
      <div class="grid">
        <mat-form-field appearance="outline">
          <mat-label>Nome</mat-label>
          <input matInput [(ngModel)]="form.nome" required />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Slug (URL)</mat-label>
          <input matInput [(ngModel)]="form.slug" required pattern="[a-z0-9\\-]+"
            placeholder="ex: lava-acme" />
          <mat-hint>Apenas minúsculas, números e hífens</mat-hint>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Segmento</mat-label>
          <input matInput [(ngModel)]="form.segmento" placeholder="Lava-rápido, Barbearia..." />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>E-mail de contato</mat-label>
          <input matInput type="email" [(ngModel)]="form.email" required />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Telefone</mat-label>
          <input matInput [(ngModel)]="form.telefone" />
        </mat-form-field>
      </div>

      <h3>Administrador da empresa</h3>
      <div class="grid">
        <mat-form-field appearance="outline" class="full">
          <mat-label>Nome do admin</mat-label>
          <input matInput [(ngModel)]="form.adminNome" required />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>E-mail do admin</mat-label>
          <input matInput type="email" [(ngModel)]="form.adminEmail" required autocomplete="off" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Senha do admin</mat-label>
          <input matInput type="password" [(ngModel)]="form.adminSenha" required minlength="8" autocomplete="new-password" />
        </mat-form-field>
      </div>

      <mat-checkbox [(ngModel)]="form.comDadosDeExemplo" class="exemplo">
        Preencher com dados fictícios (demonstração)
      </mat-checkbox>
      <p class="aviso-exemplo">
        Cria clientes, agendamentos e avaliações inventados. Use só em demonstração —
        num cliente de verdade ele não conseguiria distinguir o que é real.
      </p>
    </mat-dialog-content>
    <div class="problemas" *ngIf="problemas().length">
      <mat-icon>info</mat-icon>
      <div>
        <strong>Preencha para liberar o botão:</strong>
        <ul>
          <li *ngFor="let p of problemas()">{{ p }}</li>
        </ul>
      </div>
    </div>

    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-flat-button color="primary" (click)="criar()" [disabled]="!valido()">
        <mat-icon>check</mat-icon> Criar empresa
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    h3 { margin: 1rem 0 0.5rem; font-size: 0.9375rem; color: #6366f1; }
    .ajuda { color: #71717a; font-size: 0.875rem; margin: 0 0 1rem; }
    .ajuda code { background: #f5f3ff; color: #4f46e5; padding: 0.125rem 0.375rem; border-radius: 0.25rem; }
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; }
    .grid .full { grid-column: 1 / -1; }
    .exemplo { margin-top: 1rem; }
    .aviso-exemplo { margin: 0.25rem 0 0 2rem; font-size: 0.8125rem; color: #71717a; }
    @media (max-width: 36rem) { .grid { grid-template-columns: 1fr; } }
    .problemas {
      display: flex; gap: 0.625rem; align-items: flex-start;
      background: #fef3c7; border-left: 0.25rem solid #f59e0b;
      padding: 0.75rem 1rem; border-radius: 0.5rem; margin: 0.5rem 0;
      mat-icon { color: #d97706; flex-shrink: 0; }
      strong { color: #78350f; display: block; margin-bottom: 0.25rem; }
      ul { margin: 0; padding-left: 1.125rem; color: #78350f; font-size: 0.8125rem; }
    }
  `]
})
export class CriarEmpresaDialogComponent {
  private ref = inject(MatDialogRef<CriarEmpresaDialogComponent>);

  form: CriarEmpresaInput = {
    nome: '', slug: '', segmento: '',
    email: '', telefone: '',
    adminNome: '', adminEmail: '', adminSenha: '', comDadosDeExemplo: false
  };

  problemas(): string[] {
    const p: string[] = [];
    if (!this.form.nome) p.push('Nome da empresa');
    if (!this.form.slug) p.push('Slug (URL)');
    else if (!/^[a-z0-9\-]+$/.test(this.form.slug))
      p.push('Slug deve ter apenas letras minúsculas, números e hífens');
    if (!this.form.email) p.push('E-mail de contato');
    if (!this.form.adminNome) p.push('Nome do admin');
    if (!this.form.adminEmail) p.push('E-mail do admin');
    if (!this.form.adminSenha) p.push('Senha do admin');
    else if (this.form.adminSenha.length < 8) p.push('Senha do admin (mín. 8 caracteres)'); // backend exige 8
    return p;
  }

  valido(): boolean {
    return this.problemas().length === 0;
  }

  // [mat-dialog-close]="form" não entregava o resultado ao afterClosed neste
  // build (o botão fechava como cancelar) — fechamos explicitamente via ref.
  criar() { this.ref.close(this.form); }
}

@Component({
  selector: 'app-empresas',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatCardModule],
  template: `
    <header class="cab">
      <div>
        <h1><mat-icon>apartment</mat-icon> Empresas</h1>
        <p>Gerencie as empresas (tenants) cadastradas na plataforma</p>
      </div>
      <button mat-flat-button color="primary" (click)="abrirCriar()" class="btn-novo">
        <mat-icon>add</mat-icon> Nova empresa
      </button>
    </header>

    <div class="metricas">
      <div class="metrica">
        <mat-icon>apartment</mat-icon>
        <div>
          <strong>{{ empresas().length }}</strong>
          <span>Total de empresas</span>
        </div>
      </div>
      <div class="metrica ativas">
        <mat-icon>check_circle</mat-icon>
        <div>
          <strong>{{ ativas() }}</strong>
          <span>Ativas</span>
        </div>
      </div>
    </div>

    <div class="grid-empresas" *ngIf="empresas().length; else vazio">
      <article class="empresa" *ngFor="let e of empresas()">
        <div class="empresa-header" [style.background]="e.personalizacao.corPrimaria ?
            'linear-gradient(135deg, ' + e.personalizacao.corPrimaria + ', ' + (e.personalizacao.corSecundaria || e.personalizacao.corPrimaria) + ')'
            : 'linear-gradient(135deg, #6366f1, #8b5cf6)'">
          <img *ngIf="e.personalizacao.logoUrl" [src]="e.personalizacao.logoUrl" alt="logo" loading="lazy" decoding="async" />
          <span *ngIf="!e.personalizacao.logoUrl" class="iniciais">{{ iniciais(e.nome) }}</span>
        </div>
        <div class="empresa-corpo">
          <div class="empresa-info">
            <h3>{{ e.nome }}</h3>
            <span class="badge" [class.ativo]="e.ativo">
              <mat-icon>{{ e.ativo ? 'check_circle' : 'block' }}</mat-icon>
              {{ e.ativo ? 'Ativa' : 'Inativa' }}
            </span>
          </div>
          <p class="segmento" *ngIf="e.segmento">{{ e.segmento }}</p>
          <div class="empresa-meta">
            <span><mat-icon>link</mat-icon> /t/{{ e.slug }}</span>
            <span *ngIf="e.email"><mat-icon>mail</mat-icon> {{ e.email }}</span>
          </div>
          <div class="acoes">
            <a mat-stroked-button [href]="'/t/' + e.slug" target="_blank">
              <mat-icon>open_in_new</mat-icon> Ver site
            </a>
          </div>
        </div>
      </article>
    </div>

    <ng-template #vazio>
      <div class="vazio">
        <mat-icon>apartment</mat-icon>
        <h3>Nenhuma empresa cadastrada</h3>
        <p>Comece criando a primeira empresa que vai usar o sistema.</p>
        <button mat-flat-button color="primary" (click)="abrirCriar()">
          <mat-icon>add</mat-icon> Criar primeira empresa
        </button>
      </div>
    </ng-template>
  `,
  styleUrls: ['./empresas.component.scss']
})
export class EmpresasComponent implements OnInit {
  private http = inject(HttpClient);
  private dialog = inject(MatDialog);
  private snack = inject(MatSnackBar);

  empresas = signal<Tenant[]>([]);

  ngOnInit() { this.carregar(); }

  ativas(): number { return this.empresas().filter(e => e.ativo).length; }

  iniciais(nome: string): string {
    return nome.split(' ').filter(Boolean).slice(0, 2).map(p => p[0]).join('').toUpperCase();
  }

  carregar() {
    this.http.get<Tenant[]>(`${environment.apiUrl}/tenants`)
      .subscribe(list => this.empresas.set(list || []));
  }

  abrirCriar() {
    const ref = this.dialog.open(CriarEmpresaDialogComponent, { width: '40rem' });
    ref.afterClosed().subscribe((dados: CriarEmpresaInput | undefined) => {
      if (!dados) return;
      this.http.post<Tenant>(`${environment.apiUrl}/tenants`, dados).subscribe({
        next: () => {
          this.snack.open(`Empresa "${dados.nome}" criada!`, 'OK', { duration: 3000 });
          this.carregar();
        },
        error: err => {
          // ProblemDetails de validação vem como { errors: { Campo: [msgs] } } — sem
          // mostrar isso o operador não sabe o que corrigir.
          const detalhes = err.error?.errors
            ? Object.values(err.error.errors as Record<string, string[]>).flat().join(' ')
            : null;
          this.snack.open(detalhes || err.error?.message || err.error?.detail || 'Falha ao criar empresa.', 'OK',
            { duration: 8000, panelClass: 'snack-erro' });
        }
      });
    });
  }
}
