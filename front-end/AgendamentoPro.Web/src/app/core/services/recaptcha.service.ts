import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

declare global { interface Window { grecaptcha: any; } }

@Injectable({ providedIn: 'root' })
export class RecaptchaService {
  private carregado = false;
  private carregando: Promise<void> | null = null;

  get ativo(): boolean { return !!environment.recaptchaSiteKey; }

  async executar(acao: string): Promise<string> {
    if (!this.ativo) return '';
    await this.carregar();
    return new Promise<string>(resolve => {
      window.grecaptcha.ready(() => {
        window.grecaptcha.execute(environment.recaptchaSiteKey, { action: acao }).then(resolve);
      });
    });
  }

  private carregar(): Promise<void> {
    if (this.carregado) return Promise.resolve();
    if (this.carregando) return this.carregando;
    this.carregando = new Promise<void>((resolve, reject) => {
      const s = document.createElement('script');
      s.src = `https://www.google.com/recaptcha/api.js?render=${environment.recaptchaSiteKey}`;
      s.async = true;
      s.defer = true;
      s.onload = () => { this.carregado = true; resolve(); };
      s.onerror = () => reject(new Error('Falha ao carregar reCAPTCHA'));
      document.head.appendChild(s);
    });
    return this.carregando;
  }
}
