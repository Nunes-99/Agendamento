import { Directive, ElementRef, HostListener, Input, forwardRef, inject } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export type TipoMascara = 'telefone' | 'cpf' | 'cnpj' | 'cep';

/** Só os dígitos, cortados no tamanho máximo do documento. */
function digitos(valor: string, max: number): string {
  return (valor || '').replace(/\D/g, '').slice(0, max);
}

/**
 * Telefone brasileiro: (11) 9532-1234 com 10 dígitos, (11) 95321-1234 com 11.
 * A máscara acompanha a digitação, então os parênteses aparecem já no 3º dígito.
 */
function mascararTelefone(valor: string): string {
  const d = digitos(valor, 11);
  if (d.length <= 2) return d.length ? `(${d}` : '';
  if (d.length <= 6) return `(${d.slice(0, 2)}) ${d.slice(2)}`;
  if (d.length <= 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
}

function mascararCpf(valor: string): string {
  const d = digitos(valor, 11);
  if (d.length <= 3) return d;
  if (d.length <= 6) return `${d.slice(0, 3)}.${d.slice(3)}`;
  if (d.length <= 9) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6)}`;
  return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`;
}

function mascararCnpj(valor: string): string {
  const d = digitos(valor, 14);
  if (d.length <= 2) return d;
  if (d.length <= 5) return `${d.slice(0, 2)}.${d.slice(2)}`;
  if (d.length <= 8) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5)}`;
  if (d.length <= 12) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8)}`;
  return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12)}`;
}

function mascararCep(valor: string): string {
  const d = digitos(valor, 8);
  return d.length <= 5 ? d : `${d.slice(0, 5)}-${d.slice(5)}`;
}

export function aplicarMascara(tipo: TipoMascara, valor: string): string {
  switch (tipo) {
    case 'telefone': return mascararTelefone(valor);
    case 'cpf': return mascararCpf(valor);
    case 'cnpj': return mascararCnpj(valor);
    case 'cep': return mascararCep(valor);
    default: return valor;
  }
}

/** Quantos dígitos o documento precisa ter para estar completo. */
export const DIGITOS_ESPERADOS: Record<TipoMascara, number[]> = {
  telefone: [10, 11],
  cpf: [11],
  cnpj: [14],
  cep: [8]
};

export function documentoCompleto(tipo: TipoMascara, valor: string): boolean {
  const n = (valor || '').replace(/\D/g, '').length;
  return DIGITOS_ESPERADOS[tipo].includes(n);
}

/**
 * Máscara de digitação para os campos brasileiros, como ControlValueAccessor —
 * assim funciona tanto com `[(ngModel)]` quanto com `formControlName`, e o valor
 * que chega ao model é o texto já formatado (o back-end normaliza telefone e
 * documento para dígitos antes de gravar).
 *
 * Digitar é a parte fácil; apagar é onde máscara costuma quebrar. Por isso o
 * cursor é reposicionado contando DÍGITOS à esquerda, não caracteres: sem isso,
 * corrigir o meio de "(11) 95321-1234" jogava o cursor para o fim a cada tecla.
 */
@Directive({
  selector: '[appMascara]',
  standalone: true,
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => MascaraDirective),
    multi: true
  }]
})
export class MascaraDirective implements ControlValueAccessor {
  @Input('appMascara') tipo: TipoMascara = 'telefone';

  private el = inject(ElementRef<HTMLInputElement>).nativeElement as HTMLInputElement;
  private onChange: (v: string) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(valor: string): void {
    this.el.value = valor ? aplicarMascara(this.tipo, valor) : '';
  }

  registerOnChange(fn: (v: string) => void): void { this.onChange = fn; }
  registerOnTouched(fn: () => void): void { this.onTouched = fn; }
  setDisabledState(disabled: boolean): void { this.el.disabled = disabled; }

  @HostListener('blur')
  aoSair() { this.onTouched(); }

  @HostListener('input', ['$event'])
  aoDigitar(evento: Event) {
    const input = evento.target as HTMLInputElement;
    const digitosAntesDoCursor = (input.value.slice(0, input.selectionStart ?? 0).match(/\d/g) || []).length;

    const formatado = aplicarMascara(this.tipo, input.value);
    input.value = formatado;

    // Reposiciona o cursor depois do mesmo número de dígitos que havia antes dele.
    let pos = formatado.length;
    if (digitosAntesDoCursor > 0) {
      let contados = 0;
      for (let i = 0; i < formatado.length; i++) {
        if (/\d/.test(formatado[i])) contados++;
        if (contados === digitosAntesDoCursor) { pos = i + 1; break; }
      }
    } else {
      pos = formatado.search(/\d/) >= 0 ? formatado.search(/\d/) : formatado.length;
    }
    input.setSelectionRange(pos, pos);

    this.onChange(formatado);
  }
}
