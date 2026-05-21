import { Injectable, inject, signal } from '@angular/core';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';

/**
 * Gerencia a subscription Web Push do admin atual.
 * - Em dev (sem service worker): isSupported = false, todos métodos viram no-op.
 * - Em prod: usa SwPush pra criar a PushSubscription via VAPID, registra no backend.
 */
@Injectable({ providedIn: 'root' })
export class WebPushService {
  private swPush = inject(SwPush);
  private api = inject(ApiService);

  readonly isSupported = this.swPush.isEnabled;
  readonly isSubscribed = signal(false);
  readonly chavePublica = signal<string | null>(null);
  readonly serverAtivo = signal(false);

  constructor() {
    if (!this.isSupported) return;
    this.swPush.subscription.subscribe(sub => this.isSubscribed.set(!!sub));
  }

  /** Lê chave pública do servidor (idempotente; armazena em signal). */
  async carregarVapidKey(): Promise<void> {
    if (!this.isSupported) return;
    const { ativo, chavePublica } = await firstValueFrom(this.api.webPushVapidKey());
    this.serverAtivo.set(ativo);
    this.chavePublica.set(chavePublica ?? null);
  }

  async subscribe(): Promise<void> {
    if (!this.isSupported) throw new Error('Service Worker não disponível neste ambiente.');
    if (!this.chavePublica()) await this.carregarVapidKey();
    if (!this.serverAtivo() || !this.chavePublica()) {
      throw new Error('Servidor sem VAPID configurado.');
    }

    const sub = await this.swPush.requestSubscription({ serverPublicKey: this.chavePublica()! });
    const json: any = sub.toJSON();
    await firstValueFrom(this.api.webPushSubscribe({
      endpoint: json.endpoint,
      p256dh: json.keys?.p256dh,
      auth: json.keys?.auth,
      userAgent: navigator.userAgent
    }));
    this.isSubscribed.set(true);
  }

  async unsubscribe(): Promise<void> {
    if (!this.isSupported) return;
    const sub = await firstValueFrom(this.swPush.subscription);
    if (!sub) return;
    const endpoint = sub.endpoint;
    await sub.unsubscribe();
    await firstValueFrom(this.api.webPushUnsubscribe(endpoint));
    this.isSubscribed.set(false);
  }
}
