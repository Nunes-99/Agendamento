namespace AgendamentoPro.Application.ViewModels.Tenants
{
    public class TenantViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Slug { get; set; }
        public string Segmento { get; set; }
        public string Cnpj { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public string WhatsApp { get; set; }
        public string Endereco { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string Cep { get; set; }
        public string Descricao { get; set; }
        public bool Ativo { get; set; }
        public PersonalizacaoViewModel Personalizacao { get; set; }
        public RegrasNegocioViewModel Regras { get; set; }
    }

    public class PersonalizacaoViewModel
    {
        public string LogoUrl { get; set; }
        public string BannerUrl { get; set; }
        public string FaviconUrl { get; set; }
        public string CorPrimaria { get; set; }
        public string CorSecundaria { get; set; }
        public string CorAcento { get; set; }
        public string Fonte { get; set; }
    }

    public class RegrasNegocioViewModel
    {
        public decimal PercentualEntrada { get; set; }
        public int BufferMinutos { get; set; }
        public int AntecedenciaMinHoras { get; set; }
        public int AntecedenciaMaxDias { get; set; }
        public int LimiteCancelamentoHoras { get; set; }
    }
}
