import { Injectable } from '@angular/core';
import { Personalizacao } from '../models/tenant.model';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  aplicarPadrao(): void {
    const root = document.documentElement;
    root.style.setProperty('--cor-primaria', '#1976d2');
    root.style.setProperty('--cor-secundaria', '#424242');
    root.style.setProperty('--cor-acento', '#ff4081');
    root.style.setProperty('--app-font', 'Roboto');
  }

  aplicarPersonalizacao(p: Personalizacao | undefined | null): void {
    if (!p) return;
    const root = document.documentElement;
    if (p.corPrimaria) root.style.setProperty('--cor-primaria', p.corPrimaria);
    if (p.corSecundaria) root.style.setProperty('--cor-secundaria', p.corSecundaria);
    if (p.corAcento) root.style.setProperty('--cor-acento', p.corAcento);
    if (p.fonte) root.style.setProperty('--app-font', p.fonte);
    if (p.faviconUrl) {
      const link: HTMLLinkElement = document.querySelector("link[rel*='icon']") || document.createElement('link');
      link.rel = 'icon';
      link.href = p.faviconUrl;
      document.head.appendChild(link);
    }
  }
}
