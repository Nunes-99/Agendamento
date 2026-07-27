#nullable enable
using AgendamentoPro.Application.InputModels.Assinaturas;
using AgendamentoPro.Application.UseCases.Assinaturas;
using AgendamentoPro.Core.Entities.Assinaturas;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AgendamentoPro.Tests.UseCases
{
    /// <summary>
    /// A decisão de cobrança — cartão no cadastro, primeiro mês grátis, cobra do
    /// segundo — precisa estar de fato LIGADA na criação da assinatura, não só
    /// possível de ligar. Estes testes provam as duas metades: a assinatura nasce
    /// em Trial (acesso total, sem cobrança) e o gateway é chamado com um período
    /// grátis maior que zero (é o que adia a cobrança no Mercado Pago).
    /// </summary>
    public class CriarAssinaturaTrialTests
    {
        private static Plano PlanoAtivo()
        {
            var p = new Plano("Essencial", "1 unidade", 29.90m, 1, 3, 500);
            typeof(Plano).GetProperty(nameof(Plano.PlnId))!.SetValue(p, 1);
            return p;
        }

        private static (CriarAssinaturaUseCase uc, Mock<IGatewayAssinatura> gw, Mock<IAssinaturaRepository> repo)
            Montar()
        {
            var planos = new Mock<IPlanoRepository>();
            planos.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(PlanoAtivo());

            var repo = new Mock<IAssinaturaRepository>();
            repo.Setup(x => x.GetByTenantAsync(It.IsAny<int>())).ReturnsAsync((Assinatura?)null);
            repo.Setup(x => x.CreateAsync(It.IsAny<Assinatura>())).ReturnsAsync(1);
            // O use case relê a assinatura no fim para montar o ViewModel.
            repo.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new Assinatura(1, 1, "MercadoPago", DateTime.UtcNow.AddMonths(1)));

            var gw = new Mock<IGatewayAssinatura>();
            gw.SetupGet(x => x.Nome).Returns("MercadoPago");
            gw.Setup(x => x.CriarPreapprovalAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new CriarAssinaturaGatewayResult
                {
                    PreapprovalId = "pre-1",
                    InitPointUrl = "https://mp/x",
                    ProximoVencimento = DateTime.UtcNow.AddMonths(1),
                    PayloadBruto = "{}",
                });

            var uow = new Mock<IUnitOfWork>();
            var cache = new Mock<IAssinaturaCacheInvalidator>();
            var config = new ConfigurationBuilder().Build();

            var uc = new CriarAssinaturaUseCase(repo.Object, planos.Object, gw.Object,
                uow.Object, config, cache.Object);
            return (uc, gw, repo);
        }

        [Fact]
        public async Task A_assinatura_nasce_em_trial()
        {
            var (uc, _, repo) = Montar();

            await uc.ExecuteAsync(tenantId: 1,
                new CriarAssinaturaInputModel { PlanoId = 1, PayerEmail = "dono@oficina.com" });

            repo.Verify(x => x.CreateAsync(
                It.Is<Assinatura>(a => a.AssStatus == StatusAssinatura.Trial)), Times.Once);
        }

        [Fact]
        public async Task O_gateway_e_chamado_com_periodo_gratis()
        {
            var (uc, gw, _) = Montar();

            await uc.ExecuteAsync(tenantId: 1,
                new CriarAssinaturaInputModel { PlanoId = 1, PayerEmail = "dono@oficina.com" });

            gw.Verify(x => x.CriarPreapprovalAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.Is<int>(trial => trial >= 1)), Times.Once);
        }
    }
}
