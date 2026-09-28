namespace BlakBox.Api.Models.Api;

public class ApiResposta<T>
{
    public bool Sucesso { get; set; }

    public string Codigo { get; set; } = null!;

    public string Mensagem { get; set; } = null!;

    public T? Dados { get; set; }
}

