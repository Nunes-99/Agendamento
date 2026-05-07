import { Component, DestroyRef, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';

interface Slide {
  icone: string;
  titulo: string;
  descricao: string;
  gradient: string;
  destaque: string;
}

interface ErroLogin {
  titulo: string;
  detalhe: string;
  dica?: string;
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatIconModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit, OnDestroy {
  private auth = inject(AuthService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  email = '';
  senha = '';
  tenantSlug = '';
  carregando = signal(false);
  slideAtual = signal(0);
  erro = signal<ErroLogin | null>(null);

  slides: Slide[] = [
    {
      icone: 'event_available',
      titulo: 'Empreenda sem complicar',
      descricao: 'Seus clientes agendam online 24h por dia, sem ligação, sem WhatsApp manual. Você foca no que importa: atender bem.',
      gradient: 'linear-gradient(135deg, #4f46e5 0%, #7c3aed 50%, #ec4899 100%)',
      destaque: '24/7 disponível'
    },
    {
      icone: 'payments',
      titulo: 'Pagamento garantido antes do atendimento',
      descricao: 'Receba 20% (configurável) já no agendamento via PIX, crédito ou débito. Adeus, faltas sem aviso.',
      gradient: 'linear-gradient(135deg, #059669 0%, #10b981 50%, #34d399 100%)',
      destaque: 'PIX, Crédito e Débito'
    },
    {
      icone: 'forum',
      titulo: 'WhatsApp automático',
      descricao: 'Confirmação na hora, lembrete 24h antes, lembrete 2h antes. Tudo enviado sem você levantar um dedo.',
      gradient: 'linear-gradient(135deg, #0891b2 0%, #06b6d4 50%, #22d3ee 100%)',
      destaque: 'Lembretes inteligentes'
    },
    {
      icone: 'palette',
      titulo: 'Sua marca, seu site',
      descricao: 'Logo, banner, cores e fontes 100% personalizáveis. Cada empresa tem o próprio link e identidade visual.',
      gradient: 'linear-gradient(135deg, #ea580c 0%, #f59e0b 50%, #fbbf24 100%)',
      destaque: 'White-label completo'
    },
    {
      icone: 'insights',
      titulo: 'Cresça com dados, não com achismo',
      descricao: 'Receita por dia, serviços mais vendidos, taxa de ocupação, cancelamentos. Decisões guiadas por números.',
      gradient: 'linear-gradient(135deg, #7c3aed 0%, #db2777 50%, #f43f5e 100%)',
      destaque: 'Relatórios em tempo real'
    }
  ];

  vantagens = [
    { icone: 'check_circle', texto: 'Multi-empresa (SaaS-ready)' },
    { icone: 'check_circle', texto: 'Concorrência protegida' },
    { icone: 'check_circle', texto: 'Mobile-first em REM' }
  ];

  ngOnInit() {
    interval(5000).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.proximo());
  }

  ngOnDestroy() {}

  proximo() {
    this.slideAtual.set((this.slideAtual() + 1) % this.slides.length);
  }

  anterior() {
    this.slideAtual.set((this.slideAtual() - 1 + this.slides.length) % this.slides.length);
  }

  irPara(i: number) {
    this.slideAtual.set(i);
  }

  fecharErro() {
    this.erro.set(null);
  }

  entrar() {
    this.erro.set(null);

    // Sanitiza entrada (remove espaços acidentais)
    const email = (this.email || '').trim().toLowerCase();
    const senha = (this.senha || '').trim();
    const slug = (this.tenantSlug || '').trim().toLowerCase();

    if (!email) {
      this.erro.set({
        titulo: 'E-mail obrigatório',
        detalhe: 'Informe o e-mail cadastrado no sistema.'
      });
      return;
    }
    if (!senha) {
      this.erro.set({
        titulo: 'Senha obrigatória',
        detalhe: 'Informe sua senha para continuar.'
      });
      return;
    }

    this.carregando.set(true);
    this.auth.login({ email, senha, tenantSlug: slug || undefined }).subscribe({
      next: result => {
        this.carregando.set(false);
        // SuperAdmin não tem tenant: vai para gestão de empresas.
        // Demais perfis (Administrador, Atendente): vão para dashboard da empresa.
        const destino = result.perfil === 'SuperAdmin' ? '/admin/empresas' : '/admin/dashboard';
        this.router.navigate([destino]);
      },
      error: (err: HttpErrorResponse) => {
        this.carregando.set(false);
        this.erro.set(this.traduzirErro(err, slug));
      }
    });
  }

  private traduzirErro(err: HttpErrorResponse, slug: string): ErroLogin {
    // Sem conexão (status 0 = network error)
    if (err.status === 0 || err.status === undefined) {
      return {
        titulo: 'Não foi possível conectar ao servidor',
        detalhe: 'Verifique sua conexão ou se o backend está rodando em http://localhost:5050.',
        dica: 'Tente novamente em alguns segundos.'
      };
    }

    // Servidor com erro interno
    if (err.status >= 500) {
      return {
        titulo: 'Erro no servidor',
        detalhe: err.error?.message || 'O servidor encontrou um problema. Tente novamente.',
        dica: 'Se persistir, entre em contato com o administrador.'
      };
    }

    // Rate limit
    if (err.status === 429) {
      return {
        titulo: 'Muitas tentativas',
        detalhe: 'Você excedeu o limite de tentativas de login.',
        dica: 'Aguarde 1 minuto e tente novamente.'
      };
    }

    // 401 — credenciais inválidas
    if (err.status === 401) {
      const erroLogin: ErroLogin = {
        titulo: 'E-mail ou senha incorretos',
        detalhe: err.error?.message || 'Verifique se digitou as credenciais corretamente.'
      };

      // Dica contextual baseada no que o usuário preencheu
      if (slug) {
        erroLogin.dica = `Usando empresa "${slug}". Se você é SuperAdmin, deixe o campo "Empresa" vazio.`;
      } else {
        erroLogin.dica = 'É admin de uma empresa? Preencha o campo "Empresa (slug)".';
      }
      return erroLogin;
    }

    // 400 ou outros
    return {
      titulo: 'Não foi possível entrar',
      detalhe: err.error?.message || `Erro ${err.status}. Tente novamente.`
    };
  }
}
