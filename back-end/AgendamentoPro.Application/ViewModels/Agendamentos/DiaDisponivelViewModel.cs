namespace AgendamentoPro.Application.ViewModels.Agendamentos
{
    /// <summary>
    /// Resumo de um dia para o seletor de data da página pública: quantas vagas
    /// existem e qual é a primeira delas.
    /// </summary>
    public class DiaDisponivelViewModel
    {
        public DateTime Data { get; set; }
        public int Vagas { get; set; }
        public TimeSpan? PrimeiroHorario { get; set; }
    }
}
