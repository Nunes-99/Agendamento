import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-recursos-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatTableModule, MatCheckboxModule],
  template: `
    <header class="cab"><h1>Recursos / Boxes</h1></header>

    <div class="form" *ngIf="editando()">
      <mat-form-field appearance="outline"><mat-label>Nome</mat-label><input matInput [(ngModel)]="form.nome" /></mat-form-field>
      <mat-form-field appearance="outline"><mat-label>Tipo</mat-label><input matInput [(ngModel)]="form.tipo" /></mat-form-field>
      <mat-form-field appearance="outline"><mat-label>Descrição</mat-label><input matInput [(ngModel)]="form.descricao" /></mat-form-field>
      <mat-checkbox [(ngModel)]="form.ativo">Ativo</mat-checkbox>
      <div class="acoes">
        <button mat-button (click)="editando.set(false)">Cancelar</button>
        <button mat-flat-button color="primary" (click)="salvar()">Salvar</button>
      </div>
    </div>

    <button mat-flat-button color="primary" (click)="novo()" *ngIf="!editando()">
      <mat-icon>add</mat-icon> Novo recurso
    </button>

    <table mat-table [dataSource]="recursos()" class="tabela">
      <ng-container matColumnDef="nome"><th mat-header-cell *matHeaderCellDef>Nome</th><td mat-cell *matCellDef="let r">{{ r.nome }}</td></ng-container>
      <ng-container matColumnDef="tipo"><th mat-header-cell *matHeaderCellDef>Tipo</th><td mat-cell *matCellDef="let r">{{ r.tipo }}</td></ng-container>
      <ng-container matColumnDef="ativo"><th mat-header-cell *matHeaderCellDef>Ativo</th><td mat-cell *matCellDef="let r">{{ r.ativo ? 'Sim' : 'Não' }}</td></ng-container>
      <ng-container matColumnDef="acoes"><th mat-header-cell *matHeaderCellDef></th>
        <td mat-cell *matCellDef="let r"><button mat-icon-button (click)="editar(r)"><mat-icon>edit</mat-icon></button></td>
      </ng-container>
      <tr mat-header-row *matHeaderRowDef="cols"></tr>
      <tr mat-row *matRowDef="let row; columns: cols;"></tr>
    </table>
  `,
  styles: [`
    .cab { margin-bottom: 1rem; } h1 { margin: 0; }
    .form { display: grid; gap: 0.5rem; background: #fff; padding: 1rem; border-radius: var(--raio-medio); margin-bottom: 1rem; }
    .acoes { display: flex; gap: 0.5rem; justify-content: flex-end; }
    .tabela { width: 100%; margin-top: 1rem; background: #fff; border-radius: var(--raio-medio); }
  `]
})
export class RecursosAdminComponent implements OnInit {
  private api = inject(ApiService);
  recursos = signal<any[]>([]);
  cols = ['nome', 'tipo', 'ativo', 'acoes'];
  editando = signal(false);
  form: any = { nome: '', tipo: 'Box', descricao: '', ordem: 0, ativo: true };
  edId: number | null = null;

  ngOnInit() { this.carregar(); }
  carregar() { this.api.recursosAdmin().subscribe(list => this.recursos.set(list)); }
  novo() { this.editando.set(true); this.edId = null; this.form = { nome: '', tipo: 'Box', descricao: '', ordem: 0, ativo: true }; }
  editar(r: any) { this.editando.set(true); this.edId = r.id; this.form = { ...r }; }
  salvar() {
    const obs = this.edId ? this.api.atualizarRecurso(this.edId, this.form) : this.api.cadastrarRecurso(this.form);
    obs.subscribe(() => { this.editando.set(false); this.carregar(); });
  }
}
