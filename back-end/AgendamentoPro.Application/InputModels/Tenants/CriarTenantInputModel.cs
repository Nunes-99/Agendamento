namespace AgendamentoPro.Application.InputModels.Tenants
{
    public class CriarTenantInputModel
    {
        public string Nome { get; set; }
        public string Slug { get; set; }
        public string Segmento { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public string AdminNome { get; set; }
        public string AdminEmail { get; set; }
        public string AdminSenha { get; set; }

        /// <summary>
        /// Popula o tenant com dados FICTÍCIOS (clientes, agendamentos, avaliações)
        /// para demonstração. Falso por padrão, e é importante que seja: um cliente
        /// pagante não pode receber a conta dele com ~95 agendamentos e 20 clientes
        /// inventados dentro — ele não tem como saber o que é real.
        ///
        /// Use em demonstração comercial e em ambiente de teste.
        /// </summary>
        public bool ComDadosDeExemplo { get; set; } = false;
    }
}
