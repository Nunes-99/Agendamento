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
    }
}
