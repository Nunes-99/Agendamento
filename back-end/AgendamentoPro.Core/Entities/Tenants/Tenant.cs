using AgendamentoPro.Core.Entities.Common;
using AgendamentoPro.Core.Exceptions;

namespace AgendamentoPro.Core.Entities.Tenants
{
    /// <summary>
    /// Representa uma empresa/cliente do sistema (multi-tenant).
    /// Cada Tenant tem suas próprias configurações, serviços, agendamentos e personalização visual.
    /// </summary>
    public class Tenant : SoftDeletableEntity
    {
        public int TenId { get; private set; }
        public string TenNome { get; private set; }
        public string TenSlug { get; private set; }
        public string TenSegmento { get; private set; }
        public string TenCnpj { get; private set; }
        public string TenEmail { get; private set; }
        public string TenTelefone { get; private set; }
        public string TenWhatsApp { get; private set; }
        public string TenEndereco { get; private set; }
        public string TenCidade { get; private set; }
        public string TenEstado { get; private set; }
        public string TenCep { get; private set; }
        public string TenLogoUrl { get; private set; }
        public string TenBannerUrl { get; private set; }
        public string TenFaviconUrl { get; private set; }
        public string TenCorPrimaria { get; private set; }
        public string TenCorSecundaria { get; private set; }
        public string TenCorAcento { get; private set; }
        public string TenFonte { get; private set; }
        public string TenDescricao { get; private set; }
        public decimal TenPercentualEntrada { get; private set; }
        public int TenBufferMinutos { get; private set; }
        public int TenAntecedenciaMinHoras { get; private set; }
        public int TenAntecedenciaMaxDias { get; private set; }
        public int TenLimiteCancelamentoHoras { get; private set; }
        public bool TenAtivo { get; private set; }
        public DateTime TenCriadoEm { get; private set; }

        protected Tenant() { }

        public Tenant(string nome, string slug, string segmento, string email, string telefone)
        {
            TenNome = nome;
            TenSlug = (slug ?? string.Empty).ToLowerInvariant().Trim();
            TenSegmento = segmento;
            TenEmail = email;
            TenTelefone = telefone;
            TenPercentualEntrada = 20m;
            TenBufferMinutos = 10;
            TenAntecedenciaMinHoras = 1;
            TenAntecedenciaMaxDias = 60;
            TenLimiteCancelamentoHoras = 24;
            TenCorPrimaria = "#1976d2";
            TenCorSecundaria = "#424242";
            TenCorAcento = "#ff4081";
            TenFonte = "Roboto";
            TenAtivo = true;
            TenCriadoEm = DateTime.UtcNow;

            Validate();
        }

        public void Atualizar(string nome, string segmento, string cnpj, string email, string telefone,
            string whatsapp, string endereco, string cidade, string estado, string cep, string descricao)
        {
            TenNome = nome ?? TenNome;
            TenSegmento = segmento ?? TenSegmento;
            TenCnpj = cnpj;
            TenEmail = email ?? TenEmail;
            TenTelefone = telefone ?? TenTelefone;
            TenWhatsApp = whatsapp;
            TenEndereco = endereco;
            TenCidade = cidade;
            TenEstado = estado;
            TenCep = cep;
            TenDescricao = descricao;
            Validate();
        }

        public void AtualizarPersonalizacao(string logoUrl, string bannerUrl, string faviconUrl,
            string corPrimaria, string corSecundaria, string corAcento, string fonte)
        {
            TenLogoUrl = logoUrl;
            TenBannerUrl = bannerUrl;
            TenFaviconUrl = faviconUrl;
            TenCorPrimaria = string.IsNullOrWhiteSpace(corPrimaria) ? TenCorPrimaria : corPrimaria;
            TenCorSecundaria = string.IsNullOrWhiteSpace(corSecundaria) ? TenCorSecundaria : corSecundaria;
            TenCorAcento = string.IsNullOrWhiteSpace(corAcento) ? TenCorAcento : corAcento;
            TenFonte = string.IsNullOrWhiteSpace(fonte) ? TenFonte : fonte;
        }

        public void AtualizarRegrasNegocio(decimal percentualEntrada, int bufferMinutos,
            int antecedenciaMinHoras, int antecedenciaMaxDias, int limiteCancelamentoHoras)
        {
            if (percentualEntrada < 0 || percentualEntrada > 100)
                throw new TenantException("Percentual de entrada deve estar entre 0 e 100.");
            if (bufferMinutos < 0)
                throw new TenantException("Buffer não pode ser negativo.");
            if (antecedenciaMinHoras < 0)
                throw new TenantException("Antecedência mínima não pode ser negativa.");
            if (antecedenciaMaxDias <= 0)
                throw new TenantException("Antecedência máxima deve ser positiva.");
            if (limiteCancelamentoHoras < 0)
                throw new TenantException("Limite de cancelamento não pode ser negativo.");

            TenPercentualEntrada = percentualEntrada;
            TenBufferMinutos = bufferMinutos;
            TenAntecedenciaMinHoras = antecedenciaMinHoras;
            TenAntecedenciaMaxDias = antecedenciaMaxDias;
            TenLimiteCancelamentoHoras = limiteCancelamentoHoras;
        }

        public void Ativar() => TenAtivo = true;
        public void Inativar() => TenAtivo = false;

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(TenNome))
                throw new TenantException("Nome do tenant é obrigatório.");
            if (string.IsNullOrWhiteSpace(TenSlug))
                throw new TenantException("Slug do tenant é obrigatório.");
            if (TenSlug.Length > 80)
                throw new TenantException("Slug deve ter no máximo 80 caracteres.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(TenSlug, @"^[a-z0-9\-]+$"))
                throw new TenantException("Slug deve conter apenas letras minúsculas, números e hífens.");
            if (string.IsNullOrWhiteSpace(TenEmail))
                throw new TenantException("Email é obrigatório.");
        }
    }
}
