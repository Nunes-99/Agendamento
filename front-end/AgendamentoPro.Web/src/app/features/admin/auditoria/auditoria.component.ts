import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatExpansionModule } from '@angular/material/expansion';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-auditoria',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatPaginatorModule, MatExpansionModule],
  template: `
    <header class="topo">
      <h1><mat-icon>fact_check</mat-icon> Log de Auditoria</h1>
      <p>Histórico de alterações no sistema (LGPD, troubleshooting, segurança).</p>
    </header>

    <section class="filtros">
      <mat-form-field appearance="outline">
        <mat-label>Tabela</mat-label>
        <input matInput [(ngModel)]="filtros.tabela" (change)="aplicar()" placeholder="ex: Agendamento" />
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>Ação</mat-label>
        <mat-select [(ngModel)]="filtros.acao" (selectionChange)="aplicar()">
          <mat-option [value]="''">Todas</mat-option>
          <mat-option value="Insert">Insert</mat-option>
          <mat-option value="Update">Update</mat-option>
          <mat-option value="Delete">Delete</mat-option>
        </mat-select>
      </mat-form-field>
    </section>

    <mat-accordion>
      <mat-expansion-panel *ngFor="let log of itens()">
        <mat-expansion-panel-header>
          <mat-panel-title>
            <span class="badge {{ log.acao | lowercase }}">{{ log.acao }}</span>
            {{ log.tabela }} #{{ log.chave }}
          </mat-panel-title>
          <mat-panel-description>
            {{ log.usuario || '-' }} • {{ log.quando | date:'dd/MM/yyyy HH:mm:ss' }}
          </mat-panel-description>
        </mat-expansion-panel-header>

        <button mat-stroked-button (click)="verDetalhe(log.id)">
          <mat-icon>visibility</mat-icon> Ver payload completo
        </button>

        <pre *ngIf="detalheCarregado()?.id === log.id" class="detalhe">{{ detalheCarregado() | json }}</pre>
      </mat-expansion-panel>
    </mat-accordion>

    <p *ngIf="!itens().length" class="vazio">Nenhum registro encontrado.</p>

    <mat-paginator [length]="total()" [pageSize]="pageSize" [pageIndex]="pageIndex()"
      [pageSizeOptions]="[20, 50, 100]" (page)="onPage($event)">
    </mat-paginator>
  `,
  styles: [`
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .filtros { display: flex; gap: 0.5rem; flex-wrap: wrap; margin: 1rem 0; }
    .badge { padding: 0.15rem 0.5rem; border-radius: 0.4rem; font-size: 0.75rem; margin-right: 0.5rem; }
    .badge.insert { background: #e8f5e9; color: #2e7d32; }
    .badge.update { background: #e3f2fd; color: #1565c0; }
    .badge.delete { background: #ffcdd2; color: #c62828; }
    .detalhe { background: #f5f5f5; padding: 0.75rem; border-radius: 0.4rem; font-size: 0.8rem; overflow-x: auto; max-height: 24rem; }
    .vazio { text-align: center; padding: 2rem; color: #888; }
  `]
})
export class AuditoriaComponent implements OnInit {
  private api = inject(ApiService);

  itens = signal<any[]>([]);
  total = signal(0);
  pageIndex = signal(0);
  pageSize = 50;
  filtros: any = { tabela: '', acao: '' };
  detalheCarregado = signal<any>(null);

  ngOnInit() { this.carregar(0); }

  aplicar() { this.carregar(0); }

  carregar(page: number) {
    this.api.auditoria(page + 1, this.pageSize, {
      tabela: this.filtros.tabela || undefined,
      acao: this.filtros.acao || undefined
    }).subscribe(r => {
      this.itens.set(r.items || []);
      this.total.set(r.total || 0);
      this.pageIndex.set(page);
    });
  }

  onPage(e: PageEvent) { this.carregar(e.pageIndex); }

  verDetalhe(id: number) {
    this.api.auditoriaDetalhe(id).subscribe(d => this.detalheCarregado.set(d));
  }
}
