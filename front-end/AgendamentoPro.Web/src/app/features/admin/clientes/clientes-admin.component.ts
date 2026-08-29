import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-clientes-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, MatFormFieldModule, MatInputModule,
    MatTableModule, MatPaginatorModule],
  template: `
    <h1>Clientes</h1>
    <mat-form-field appearance="outline" class="busca">
      <mat-label>Buscar</mat-label>
      <input matInput [(ngModel)]="busca" (ngModelChange)="carregar()" placeholder="Nome, telefone, email..." />
    </mat-form-field>

    <table mat-table [dataSource]="clientes()" class="tabela">
      <ng-container matColumnDef="nome"><th mat-header-cell *matHeaderCellDef>Nome</th><td mat-cell *matCellDef="let c">{{ c.nome }}</td></ng-container>
      <ng-container matColumnDef="telefone"><th mat-header-cell *matHeaderCellDef>Telefone</th><td mat-cell *matCellDef="let c">{{ c.telefone || c.whatsApp }}</td></ng-container>
      <ng-container matColumnDef="email"><th mat-header-cell *matHeaderCellDef>E-mail</th><td mat-cell *matCellDef="let c">{{ c.email }}</td></ng-container>
      <tr mat-header-row *matHeaderRowDef="cols"></tr>
      <tr mat-row *matRowDef="let r; columns: cols;"></tr>
    </table>
    <mat-paginator [length]="total()" [pageSize]="pageSize" [pageSizeOptions]="[10,20,50]"
      (page)="paginar($event)"></mat-paginator>
  `,
  styles: [`
    h1 { margin: 0 0 1rem; }
    .busca { width: 100%; max-width: 24rem; }
    .tabela { width: 100%; margin-top: 1rem; background: var(--cor-fundo-card); }
  `]
})
export class ClientesAdminComponent implements OnInit {
  private api = inject(ApiService);
  clientes = signal<any[]>([]);
  total = signal(0);
  cols = ['nome', 'telefone', 'email'];
  page = 1;
  pageSize = 20;
  busca = '';

  ngOnInit() { this.carregar(); }

  carregar() {
    this.api.clientesAdmin(this.page, this.pageSize, this.busca).subscribe((r: any) => {
      this.clientes.set(r.items);
      this.total.set(r.total);
    });
  }

  paginar(e: PageEvent) {
    this.page = e.pageIndex + 1;
    this.pageSize = e.pageSize;
    this.carregar();
  }
}
