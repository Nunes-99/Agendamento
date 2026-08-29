import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { Servico } from '../../../core/models/servico.model';

@Component({
  selector: 'app-servico-form',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatDialogModule, MatCheckboxModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>
      <mat-icon>{{ servico.id ? 'edit' : 'add_circle' }}</mat-icon>
      {{ servico.id ? 'Editar serviço' : 'Novo serviço' }}
    </h2>
    <mat-dialog-content>
      <mat-form-field appearance="outline" class="full">
        <mat-label>Nome do serviço</mat-label>
        <input matInput [(ngModel)]="servico.nome" required />
      </mat-form-field>
      <mat-form-field appearance="outline" class="full">
        <mat-label>Descrição</mat-label>
        <textarea matInput rows="2" [(ngModel)]="servico.descricao"
          placeholder="Descreva o que o cliente recebe ao escolher este serviço"></textarea>
      </mat-form-field>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Preço (R$)</mat-label>
          <input matInput type="number" min="0" step="0.01" [(ngModel)]="servico.preco" required />
          <span matPrefix>R$&nbsp;</span>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Duração</mat-label>
          <input matInput type="number" min="5" step="5" [(ngModel)]="servico.duracaoMinutos" required />
          <span matSuffix>min</span>
        </mat-form-field>
      </div>
      <div class="row">
        <mat-form-field appearance="outline">
          <mat-label>Categoria</mat-label>
          <input matInput [(ngModel)]="servico.categoria" placeholder="Ex: Lavagem, Estética..." />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Ordem na lista</mat-label>
          <input matInput type="number" min="0" [(ngModel)]="servico.ordem" />
        </mat-form-field>
      </div>
      <mat-form-field appearance="outline" class="full">
        <mat-label>URL da imagem (opcional)</mat-label>
        <input matInput [(ngModel)]="servico.imagemUrl" placeholder="https://..." />
        <mat-icon matSuffix>image</mat-icon>
      </mat-form-field>
      <mat-checkbox [(ngModel)]="servico.ativo">Serviço ativo (visível para clientes)</mat-checkbox>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-flat-button color="primary" (click)="salvar()"
        [disabled]="!servico.nome || !servico.preco || !servico.duracaoMinutos">
        <mat-icon>check</mat-icon> Salvar
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .full { width: 100%; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; }
    @media (max-width: 30rem) { .row { grid-template-columns: 1fr; } }
  `]
})
export class ServicoFormComponent {
  private data = inject<{ servico?: Servico } | null>(MAT_DIALOG_DATA, { optional: true });
  private ref = inject(MatDialogRef<ServicoFormComponent>);
  servico: Partial<Servico> = this.data?.servico
    ? { ...this.data.servico }
    : { ativo: true, ordem: 0, duracaoMinutos: 30, preco: 0 };

  // [mat-dialog-close]="obj" não entregava o resultado ao afterClosed neste
  // build (o botão fechava como cancelar) — fechamos explicitamente via ref.
  salvar() { this.ref.close(this.servico); }
}

@Component({
  selector: 'app-servicos-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatTooltipModule],
  templateUrl: './servicos-admin.component.html',
  styleUrls: ['./servicos-admin.component.scss']
})
export class ServicosAdminComponent implements OnInit {
  private api = inject(ApiService);
  private dialog = inject(MatDialog);
  private snack = inject(MatSnackBar);

  servicos = signal<Servico[]>([]);
  busca = signal('');
  filtroCategoria = signal<string>('');

  categorias = computed(() => {
    const set = new Set(this.servicos().map(s => s.categoria).filter(Boolean) as string[]);
    return Array.from(set).sort();
  });

  filtrados = computed(() => {
    const b = this.busca().toLowerCase().trim();
    const cat = this.filtroCategoria();
    return this.servicos().filter(s =>
      (!b || s.nome.toLowerCase().includes(b) || (s.descricao || '').toLowerCase().includes(b))
      && (!cat || s.categoria === cat)
    );
  });

  resumo = computed(() => {
    const lista = this.servicos();
    return {
      total: lista.length,
      ativos: lista.filter(s => s.ativo).length,
      precoMedio: lista.length ? lista.reduce((s, x) => s + x.preco, 0) / lista.length : 0,
      duracaoMedia: lista.length ? lista.reduce((s, x) => s + x.duracaoMinutos, 0) / lista.length : 0
    };
  });

  ngOnInit() { this.carregar(); }

  carregar() {
    this.api.servicosAdmin().subscribe(list => this.servicos.set(list));
  }

  novo() {
    const ref = this.dialog.open(ServicoFormComponent, { width: '36rem' });
    ref.afterClosed().subscribe(r => {
      if (!r) return;
      this.api.cadastrarServico(r).subscribe({
        next: () => { this.snack.open('Serviço criado!', 'OK', { duration: 2000 }); this.carregar(); },
        error: e => this.snack.open(e.error?.message || 'Falha ao criar.', 'OK', { duration: 4000, panelClass: 'snack-erro' })
      });
    });
  }

  editar(s: Servico) {
    const ref = this.dialog.open(ServicoFormComponent, { width: '36rem', data: { servico: { ...s } } });
    ref.afterClosed().subscribe(r => {
      if (!r) return;
      this.api.atualizarServico(s.id, r).subscribe({
        next: () => { this.snack.open('Serviço atualizado!', 'OK', { duration: 2000 }); this.carregar(); },
        error: e => this.snack.open(e.error?.message || 'Falha.', 'OK', { duration: 4000, panelClass: 'snack-erro' })
      });
    });
  }

  toggleAtivo(s: Servico) {
    this.api.atualizarServico(s.id, { ...s, ativo: !s.ativo }).subscribe({
      next: () => this.carregar(),
      error: () => this.snack.open('Falha ao alternar status', 'OK', { duration: 3000, panelClass: 'snack-erro' })
    });
  }

  excluir(s: Servico) {
    if (!confirm(`Excluir o serviço "${s.nome}"? Esta ação pode ser desfeita pelo banco de dados.`)) return;
    this.api.excluirServico(s.id).subscribe({
      next: () => { this.snack.open('Serviço excluído', 'OK', { duration: 2000 }); this.carregar(); },
      error: e => this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 3000, panelClass: 'snack-erro' })
    });
  }

  iconePorCategoria(categoria?: string): string {
    const c = (categoria || '').toLowerCase();
    if (c.includes('lavagem')) return 'local_car_wash';
    if (c.includes('estét') || c.includes('polimento')) return 'auto_fix_high';
    if (c.includes('proteç') || c.includes('cera')) return 'shield';
    if (c.includes('combo') || c.includes('pacote')) return 'local_offer';
    return 'work';
  }
}
