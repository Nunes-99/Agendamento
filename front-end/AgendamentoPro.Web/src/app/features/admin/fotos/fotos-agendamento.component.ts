import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/services/api.service';
import { FotoAgendamento, TipoFoto, TipoFotoLabel } from '../../../core/models/foto.model';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-fotos-agendamento',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatTabsModule],
  template: `
    <header class="topo">
      <a mat-icon-button routerLink="/admin/agenda"><mat-icon>arrow_back</mat-icon></a>
      <h1><mat-icon>photo_library</mat-icon> Fotos do agendamento #{{ agendamentoId() }}</h1>
    </header>

    <mat-tab-group>
      <mat-tab *ngFor="let t of tipos" [label]="labelTipo(t)">
        <div class="acoes">
          <input #fileInput type="file" accept="image/jpeg,image/png,image/webp,image/gif"
            (change)="upload(t, $event)" hidden multiple />
          <button mat-flat-button color="primary" (click)="fileInput.click()" [disabled]="enviando()">
            <mat-icon>add_a_photo</mat-icon>
            {{ enviando() ? 'Enviando...' : 'Adicionar foto' }}
          </button>
        </div>

        <div class="grid" *ngIf="fotosPorTipo(t).length; else vazio">
          <figure *ngFor="let f of fotosPorTipo(t)" class="foto">
            <img [src]="urlAbsoluta(f.url)" [alt]="labelTipo(f.tipo)" loading="lazy" />
            <button mat-icon-button class="remover" color="warn" (click)="remover(f)">
              <mat-icon>delete</mat-icon>
            </button>
          </figure>
        </div>
        <ng-template #vazio>
          <p class="placeholder">Nenhuma foto deste tipo ainda.</p>
        </ng-template>
      </mat-tab>
    </mat-tab-group>
  `,
  styles: [`
    .topo { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 1rem; }
    .topo h1 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .acoes { padding: 1rem 0; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(12rem, 1fr)); gap: 0.5rem; }
    .foto { margin: 0; position: relative; aspect-ratio: 4/3; overflow: hidden; border-radius: 0.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
    .foto img { width: 100%; height: 100%; object-fit: cover; }
    .foto .remover { position: absolute; top: 0.25rem; right: 0.25rem; background: rgba(255,255,255,0.85); }
    .placeholder { text-align: center; padding: 2rem; color: #888; }
  `]
})
export class FotosAgendamentoComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  agendamentoId = signal<number>(0);
  fotos = signal<FotoAgendamento[]>([]);
  enviando = signal(false);

  tipos: TipoFoto[] = [1, 2, 3];
  labelTipo = (t: TipoFoto) => TipoFotoLabel[t];

  fotosPorTipo = (t: TipoFoto) => computed(() => this.fotos().filter(f => f.tipo === t))();

  ngOnInit() {
    const id = +(this.route.snapshot.paramMap.get('id') || 0);
    this.agendamentoId.set(id);
    this.carregar();
  }

  carregar() {
    this.api.listarFotos(this.agendamentoId()).subscribe(list => this.fotos.set(list));
  }

  upload(tipo: TipoFoto, ev: Event) {
    const input = ev.target as HTMLInputElement;
    if (!input.files?.length) return;
    const arquivos = Array.from(input.files);
    this.enviando.set(true);

    let restantes = arquivos.length;
    arquivos.forEach(arquivo => {
      this.api.uploadFoto(this.agendamentoId(), tipo, arquivo).subscribe({
        next: f => {
          this.fotos.update(list => [...list, f]);
          if (--restantes === 0) { this.enviando.set(false); input.value = ''; }
        },
        error: e => {
          this.snack.open(e.error?.message || `Falha ao enviar ${arquivo.name}`, 'OK',
            { duration: 4000, panelClass: 'snack-erro' });
          if (--restantes === 0) { this.enviando.set(false); input.value = ''; }
        }
      });
    });
  }

  remover(f: FotoAgendamento) {
    if (!confirm('Remover esta foto?')) return;
    this.api.removerFoto(f.id).subscribe({
      next: () => {
        this.fotos.update(list => list.filter(x => x.id !== f.id));
        this.snack.open('Foto removida', 'OK', { duration: 2000 });
      },
      error: () => this.snack.open('Falha ao remover', 'OK', { duration: 3000, panelClass: 'snack-erro' })
    });
  }

  urlAbsoluta(url: string): string {
    if (url.startsWith('http')) return url;
    // environment.apiUrl é tipo http://localhost:5050/api -> precisamos da raiz
    const raiz = environment.apiUrl.replace(/\/api\/?$/, '');
    return raiz + url;
  }
}
