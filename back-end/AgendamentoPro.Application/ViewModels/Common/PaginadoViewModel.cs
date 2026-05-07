namespace AgendamentoPro.Application.ViewModels.Common
{
    public class PaginadoViewModel<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int TamanhoPagina { get; set; }
        public int TotalPaginas => TamanhoPagina == 0 ? 0 : (int)Math.Ceiling((double)Total / TamanhoPagina);
    }
}
