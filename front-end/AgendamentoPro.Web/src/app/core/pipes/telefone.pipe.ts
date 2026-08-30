import { Pipe, PipeTransform } from '@angular/core';
import { aplicarMascara } from '../directives/mascara.directive';

/**
 * O banco guarda o telefone só com dígitos (Cliente.NormalizarTelefone), o que
 * é certo para busca e para integração — mas "11953179948" numa lista é ilegível.
 * Este pipe devolve a leitura humana só na hora de exibir.
 */
@Pipe({ name: 'telefone', standalone: true })
export class TelefonePipe implements PipeTransform {
  transform(valor: string | null | undefined): string {
    if (!valor) return '';
    return aplicarMascara('telefone', valor) || valor;
  }
}
