import { ErrorHandler, Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ReporteErroService } from './services/reporte-erro.service';

/**
 * Última rede antes do chão: pega o que explode fora de qualquer `subscribe`
 * — erro de template, exceção em ciclo de vida, promessa rejeitada.
 *
 * Continua escrevendo no console (é onde se depura), mas agora TAMBÉM manda
 * para o servidor. Sem isto, um erro que quebra a tela do cliente às 3h da
 * tarde não deixa rastro nenhum para quem der suporte.
 */
@Injectable()
export class ErroGlobalHandler implements ErrorHandler {
  private reporte = inject(ReporteErroService);

  handleError(erro: unknown): void {
    // Erro HTTP já passou pelo interceptor; relatar de novo só duplicaria.
    if (!(erro instanceof HttpErrorResponse)) {
      const e = erro as { message?: string; stack?: string };
      this.reporte.reportar({
        mensagem: e?.message ?? String(erro),
        pilha: e?.stack,
      });
    }
    console.error(erro);
  }
}
