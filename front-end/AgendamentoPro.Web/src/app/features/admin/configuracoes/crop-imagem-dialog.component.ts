import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ImageCropperComponent, ImageCroppedEvent } from 'ngx-image-cropper';

export interface CropImagemData {
  arquivo: File;
  tipo: 'logo' | 'banner' | 'favicon';
}

/**
 * Seletor visual de corte antes do upload da vitrine. A proporção é travada por
 * tipo (banner 3:1 — a capa do hero; favicon 1:1) e livre para o logo. O servidor
 * continua aplicando o enquadramento automático como rede de segurança, então o
 * que sai daqui só precisa ser a ÁREA que o lojista escolheu.
 */
@Component({
  selector: 'app-crop-imagem-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, ImageCropperComponent],
  template: `
    <h2 mat-dialog-title><mat-icon>crop</mat-icon> {{ titulo }}</h2>
    <mat-dialog-content>
      <p class="dica">{{ dica }}</p>

      <div class="area-crop" [class.oculto]="!carregou">
        <image-cropper
          [imageFile]="data.arquivo"
          [maintainAspectRatio]="manterProporcao"
          [aspectRatio]="aspectRatio"
          [format]="formato"
          output="blob"
          (imageCropped)="aoCortar($event)"
          (imageLoaded)="carregou = true"
          (loadImageFailed)="falhou = true"
        ></image-cropper>
      </div>

      <div class="carregando" *ngIf="!carregou && !falhou">
        <mat-spinner [diameter]="36"></mat-spinner>
      </div>
      <p class="erro" *ngIf="falhou">
        <mat-icon>error</mat-icon> Não foi possível abrir esta imagem. Tente outro arquivo.
      </p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="cancelar()">Cancelar</button>
      <button mat-flat-button color="primary" (click)="confirmar()" [disabled]="!carregou || falhou">
        <mat-icon>check</mat-icon> Cortar e publicar
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 { display: flex; align-items: center; gap: 0.5rem; margin: 0; }
    .dica { color: var(--cor-texto-suave); font-size: 0.875rem; margin: 0 0 0.75rem; }
    .area-crop { max-height: 60vh; }
    .area-crop.oculto { display: none; }
    .carregando { display: flex; justify-content: center; padding: 2rem; }
    .erro { display: flex; align-items: center; gap: 0.5rem; color: var(--cor-erro); }
    image-cropper { max-height: 55vh; }
  `]
})
export class CropImagemDialogComponent {
  data = inject<CropImagemData>(MAT_DIALOG_DATA);
  // Fechamento explícito via ref: [mat-dialog-close] não entrega o resultado neste build.
  private ref = inject(MatDialogRef<CropImagemDialogComponent>);

  carregou = false;
  falhou = false;
  private cortado: Blob | null = null;

  get titulo(): string {
    return this.data.tipo === 'banner' ? 'Enquadrar banner'
      : this.data.tipo === 'favicon' ? 'Enquadrar favicon'
      : 'Enquadrar logo';
  }

  get dica(): string {
    return this.data.tipo === 'banner'
      ? 'Arraste para escolher a faixa que vira a capa da sua página (proporção 3:1).'
      : this.data.tipo === 'favicon'
        ? 'Escolha o quadrado que aparece na aba do navegador.'
        : 'Ajuste a área do logo — proporção livre.';
  }

  get aspectRatio(): number {
    return this.data.tipo === 'banner' ? 3 : 1;
  }

  get manterProporcao(): boolean {
    return this.data.tipo !== 'logo';
  }

  /** JPEG continua JPEG (foto de capa pesa menos); o resto sai PNG (transparência). */
  get formato(): 'png' | 'jpeg' {
    return this.data.arquivo.type === 'image/jpeg' ? 'jpeg' : 'png';
  }

  aoCortar(e: ImageCroppedEvent) {
    this.cortado = e.blob ?? null;
  }

  cancelar() { this.ref.close(null); }
  confirmar() { this.ref.close(this.cortado); }
}
