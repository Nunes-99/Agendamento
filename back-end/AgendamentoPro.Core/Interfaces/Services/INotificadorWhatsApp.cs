namespace AgendamentoPro.Core.Interfaces.Services
{
    public interface INotificadorWhatsApp
    {
        /// <summary>Envia uma mensagem de texto livre. Só funciona dentro da janela
        /// de conversa de 24h após resposta do cliente. Para mensagens proativas
        /// (lembretes, confirmações), use <see cref="EnviarTemplateAsync"/>.</summary>
        Task EnviarAsync(string numero, string mensagem);

        /// <summary>Envia uma mensagem usando template pré-aprovado. Único caminho
        /// para enviar mensagens fora da janela de 24h.</summary>
        Task EnviarTemplateAsync(string numero, string templateName,
            string idiomaCodigo = "pt_BR", params string[] parametros);

        /// <summary>Indica se o notificador está configurado e ativo.</summary>
        bool Ativo { get; }

        string GerarLinkWhatsApp(string numero, string mensagem);
    }
}

