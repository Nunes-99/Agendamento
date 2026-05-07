import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-importar-clientes',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule],
  template: `
    <header class="topo">
      <h1><mat-icon>upload_file</mat-icon> Importar clientes (CSV)</h1>
      <p>Cabeçalho esperado: <code>nome,telefone,email,cpf</code> (somente <code>nome</code> obrigatório).</p>
    </header>

    <section class="card">
      <input #fileInput type="file" accept=".csv,text/csv" (change)="lerArquivo($event)" hidden />
      <div class="botoes">
        <button mat-stroked-button (click)="fileInput.click()">
          <mat-icon>folder_open</mat-icon> Selecionar arquivo
        </button>
        <span *ngIf="nomeArquivo()" class="nome">{{ nomeArquivo() }}</span>
      </div>

      <mat-form-field appearance="outline" class="full">
        <mat-label>Conteúdo CSV (preview/edição)</mat-label>
        <textarea matInput rows="10" [(ngModel)]="conteudo"></textarea>
        <mat-hint>Você pode editar antes de enviar.</mat-hint>
      </mat-form-field>

      <button mat-flat-button color="primary" (click)="importar()" [disabled]="!conteudo || enviando()">
        <mat-icon>{{ enviando() ? 'hourglass_empty' : 'cloud_upload' }}</mat-icon>
        {{ enviando() ? 'Importando...' : 'Importar' }}
      </button>
    </section>

    <section *ngIf="resultado()" class="card resultado">
      <h3>Resultado</h3>
      <p><mat-icon class="ok">check_circle</mat-icon> <strong>{{ resultado()!.inseridos }}</strong> inseridos</p>
      <p *ngIf="resultado()!.ignorados"><mat-icon class="warn">info</mat-icon> {{ resultado()!.ignorados }} ignorados (linhas sem nome)</p>
      <div *ngIf="resultado()!.erros?.length">
        <p><mat-icon class="erro">error</mat-icon> {{ resultado()!.erros!.length }} erros:</p>
        <ul>
          <li *ngFor="let e of resultado()!.erros">{{ e }}</li>
        </ul>
      </div>
    </section>
  `,
  styles: [`
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .topo code { background: #f5f5f5; padding: 0.1rem 0.4rem; border-radius: 0.25rem; }
    .card { background: #fff; padding: 1rem 1.25rem; margin-top: 1rem; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
    .botoes { display: flex; gap: 0.5rem; align-items: center; margin-bottom: 1rem; }
    .nome { color: #555; font-size: 0.9rem; }
    .full { width: 100%; }
    .resultado .ok { color: #2e7d32; vertical-align: middle; }
    .resultado .warn { color: #ff9800; vertical-align: middle; }
    .resultado .erro { color: #c62828; vertical-align: middle; }
    .resultado p { margin: 0.5rem 0; }
    ul { margin: 0; padding-left: 1.5rem; max-height: 12rem; overflow-y: auto; }
  `]
})
export class ImportarClientesComponent {
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  conteudo = '';
  nomeArquivo = signal('');
  enviando = signal(false);
  resultado = signal<{ inseridos: number; ignorados: number; erros?: string[] } | null>(null);

  lerArquivo(ev: Event) {
    const input = ev.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];
    this.nomeArquivo.set(file.name);
    const reader = new FileReader();
    reader.onload = () => { this.conteudo = reader.result as string; };
    reader.readAsText(file);
  }

  importar() {
    if (!this.conteudo) return;
    this.enviando.set(true);
    this.resultado.set(null);
    this.api.importarClientesCsv(this.conteudo).subscribe({
      next: r => { this.enviando.set(false); this.resultado.set(r); this.snack.open(`${r.inseridos} cliente(s) importados.`, 'OK', { duration: 3000 }); },
      error: e => { this.enviando.set(false); this.snack.open(e.error?.message || 'Falha', 'OK', { duration: 4000 }); }
    });
  }
}
