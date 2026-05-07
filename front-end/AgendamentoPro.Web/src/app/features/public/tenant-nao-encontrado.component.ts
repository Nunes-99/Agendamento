import { Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-tenant-nao-encontrado',
  standalone: true,
  imports: [MatIconModule],
  template: `
    <div class="centro">
      <mat-icon>storefront</mat-icon>
      <h1>Estabelecimento não encontrado</h1>
      <p>O link que você acessou não corresponde a nenhum estabelecimento cadastrado.</p>
    </div>
  `,
  styles: [`
    .centro {
      min-height: 70vh;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
      padding: 2rem 1rem;
      color: var(--cor-texto-suave);
    }
    mat-icon { font-size: 5rem; width: 5rem; height: 5rem; margin-bottom: 1rem; color: var(--cor-primaria); }
    h1 { margin: 0 0 0.5rem; color: var(--cor-texto); }
  `]
})
export class TenantNaoEncontradoComponent {}
