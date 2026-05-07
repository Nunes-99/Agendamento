import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private auth = inject(AuthService);
  private connection: HubConnection | null = null;

  readonly conectado = signal(false);
  readonly ultimaNotificacao = signal<{ evento: string; payload: any; data: Date } | null>(null);

  async conectar(): Promise<void> {
    if (this.connection && this.connection.state !== HubConnectionState.Disconnected) return;

    const token = this.auth.user()?.accessToken;
    if (!token) return;

    const baseHub = environment.apiUrl.replace(/\/api\/?$/, '');
    this.connection = new HubConnectionBuilder()
      .withUrl(`${baseHub}/hubs/notificacoes`, { accessTokenFactory: () => token })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('novo-agendamento', payload => this.publicar('novo-agendamento', payload));
    this.connection.on('pagamento-aprovado', payload => this.publicar('pagamento-aprovado', payload));
    this.connection.on('foto-enviada', payload => this.publicar('foto-enviada', payload));

    this.connection.onreconnected(() => this.conectado.set(true));
    this.connection.onreconnecting(() => this.conectado.set(false));
    this.connection.onclose(() => this.conectado.set(false));

    try {
      await this.connection.start();
      this.conectado.set(true);
    } catch (e) {
      console.warn('SignalR: falha ao conectar', e);
      this.conectado.set(false);
    }
  }

  on<T = any>(evento: string, handler: (payload: T) => void): () => void {
    this.connection?.on(evento, handler);
    return () => this.connection?.off(evento, handler);
  }

  async desconectar(): Promise<void> {
    if (!this.connection) return;
    await this.connection.stop();
    this.connection = null;
    this.conectado.set(false);
  }

  private publicar(evento: string, payload: any) {
    this.ultimaNotificacao.set({ evento, payload, data: new Date() });
  }
}
