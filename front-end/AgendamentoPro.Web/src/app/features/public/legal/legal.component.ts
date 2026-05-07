import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-politica-privacidade',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="container">
      <a mat-stroked-button [routerLink]="['/']"><mat-icon>arrow_back</mat-icon> Voltar</a>
      <article>
        <h1>Política de Privacidade</h1>
        <p class="data">Última atualização: {{ hoje }}</p>

        <h2>1. Quem somos</h2>
        <p>O AgendamentoPro é uma plataforma SaaS de agendamento online. Cada estabelecimento (tenant) gerencia
          seus próprios serviços, clientes e dados. Esta política explica como tratamos os dados pessoais.</p>

        <h2>2. Que dados coletamos</h2>
        <ul>
          <li><strong>Dados de identificação:</strong> nome, e-mail, telefone, WhatsApp, CPF (opcional).</li>
          <li><strong>Dados de uso:</strong> agendamentos realizados, avaliações dadas, fotos enviadas pelo estabelecimento.</li>
          <li><strong>Dados técnicos:</strong> endereço IP, registros de acesso, identificadores de correlação para troubleshooting.</li>
        </ul>

        <h2>3. Por que coletamos</h2>
        <p>Para executar o serviço de agendamento (base legal: <em>execução de contrato</em>),
          enviar confirmações e lembretes (base legal: <em>execução de contrato</em>),
          e atender obrigações legais e fiscais (base legal: <em>obrigação legal</em>).</p>

        <h2>4. Compartilhamento</h2>
        <p>Compartilhamos dados estritamente necessários com:</p>
        <ul>
          <li>Mercado Pago — para processar pagamentos.</li>
          <li>Meta (WhatsApp Business Cloud API) — para enviar mensagens transacionais.</li>
          <li>Estabelecimento (tenant) — apenas dados dos clientes que agendaram com ele.</li>
        </ul>
        <p>Não vendemos dados a terceiros.</p>

        <h2>5. Seus direitos (LGPD)</h2>
        <p>Você pode, a qualquer tempo:</p>
        <ul>
          <li><strong>Acessar</strong> os dados que temos sobre você (direito de portabilidade).</li>
          <li><strong>Solicitar correção</strong> de dados incorretos.</li>
          <li><strong>Solicitar exclusão</strong> dos seus dados pessoais (direito ao esquecimento).</li>
          <li><strong>Revogar consentimento</strong> a qualquer momento.</li>
        </ul>
        <p>Para exercer qualquer um desses direitos, entre em contato com o estabelecimento onde você agendou
          ou com nosso DPO via <a href="mailto:dpo@agendamentopro.com.br">dpo&#64;agendamentopro.com.br</a>.</p>

        <h2>6. Retenção</h2>
        <p>Mantemos seus dados enquanto sua conta estiver ativa. Após a anonimização (manual via solicitação ou
          automática após 24 meses de inatividade), seu nome, telefone, e-mail e CPF são removidos, mas o
          histórico anonimizado de agendamentos é mantido para fins contábeis pelo período legal.</p>

        <h2>7. Segurança</h2>
        <p>Usamos criptografia em trânsito (HTTPS), senhas com bcrypt, autenticação de dois fatores (TOTP),
          rate limiting, audit log de alterações e backups regulares. Detalhes técnicos: HSTS, CSP, headers
          Anti-XSS, isolamento por tenant.</p>

        <h2>8. Cookies</h2>
        <p>Usamos apenas cookies essenciais para autenticação e funcionamento da plataforma. Não usamos cookies
          de rastreamento publicitário.</p>

        <h2>9. Alterações</h2>
        <p>Podemos atualizar esta política. A data no topo indica a última versão. Mudanças relevantes serão
          comunicadas por e-mail.</p>
      </article>
    </div>
  `,
  styles: [`
    .container { max-width: 48rem; margin: 0 auto; padding: 2rem 1rem; }
    article { background: var(--cor-fundo-card); padding: 2rem; border-radius: 0.75rem; box-shadow: var(--sombra-card); margin-top: 1rem; }
    h1 { margin-top: 0; }
    h2 { margin-top: 1.5rem; }
    .data { color: var(--cor-texto-suave); font-size: 0.9rem; }
  `]
})
export class PoliticaPrivacidadeComponent {
  hoje = new Date().toLocaleDateString('pt-BR');
}

@Component({
  selector: 'app-termos-uso',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="container">
      <a mat-stroked-button [routerLink]="['/']"><mat-icon>arrow_back</mat-icon> Voltar</a>
      <article>
        <h1>Termos de Uso</h1>
        <p class="data">Última atualização: {{ hoje }}</p>

        <h2>1. Aceitação</h2>
        <p>Ao usar o AgendamentoPro você concorda com estes termos. Se não concordar, não use o serviço.</p>

        <h2>2. O serviço</h2>
        <p>O AgendamentoPro é uma plataforma para agendamento de serviços. Os termos comerciais (preço,
          horários, política de cancelamento) são definidos por cada estabelecimento (tenant).</p>

        <h2>3. Cadastro e cliente final</h2>
        <p>Cliente final pode agendar sem login, usando dados de contato. Ao agendar você confirma que esses
          dados são verdadeiros e que aceita esta política e a Política de Privacidade.</p>

        <h2>4. Pagamentos</h2>
        <p>Pagamentos são processados pelo Mercado Pago. Sinal antecipado pode ser exigido por cada
          estabelecimento. Cancelamentos com antecedência seguem a política do estabelecimento.</p>

        <h2>5. Conduta</h2>
        <p>É proibido usar o sistema para fraude, spam ou qualquer conduta que viole leis aplicáveis.
          Tentativas de invasão, força bruta ou exploração de vulnerabilidades resultam em bloqueio imediato
          e podem ser reportadas às autoridades.</p>

        <h2>6. Limitação de responsabilidade</h2>
        <p>O AgendamentoPro é fornecido "como está". Empenhamos-nos pela disponibilidade mas não garantimos
          serviço ininterrupto. Não somos responsáveis pela qualidade do serviço prestado pelo estabelecimento.</p>

        <h2>7. Foro</h2>
        <p>Foro de São Paulo / SP, com renúncia a qualquer outro.</p>
      </article>
    </div>
  `,
  styles: [`
    .container { max-width: 48rem; margin: 0 auto; padding: 2rem 1rem; }
    article { background: var(--cor-fundo-card); padding: 2rem; border-radius: 0.75rem; box-shadow: var(--sombra-card); margin-top: 1rem; }
    h1 { margin-top: 0; }
    h2 { margin-top: 1.5rem; }
    .data { color: var(--cor-texto-suave); font-size: 0.9rem; }
  `]
})
export class TermosUsoComponent {
  hoje = new Date().toLocaleDateString('pt-BR');
}
