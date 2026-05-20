using AgendamentoPro.Application.UseCases.Agendamentos;
using Hangfire;

namespace AgendamentoPro.Infrastructure.Services.Storage
{
    /// <summary>
    /// Implementação Hangfire do <see cref="IFotoResizeEnqueuer"/>. Encapsula a
    /// dependência do Hangfire para que a camada Application fique limpa.
    /// </summary>
    public class HangfireFotoResizeEnqueuer : IFotoResizeEnqueuer
    {
        private readonly IBackgroundJobClient _jobs;

        public HangfireFotoResizeEnqueuer(IBackgroundJobClient jobs)
        {
            _jobs = jobs;
        }

        public void Enfileirar(int fotoId, int tenantId, string urlRelativa)
        {
            _jobs.Enqueue<FotoResizeJob>(j => j.ExecutarAsync(fotoId, tenantId, urlRelativa));
        }
    }
}
