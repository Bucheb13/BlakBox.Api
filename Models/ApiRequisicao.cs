namespace BlakBox.Api.Models;

public class ApiRequisicao
{
public long ApiRequisicaoId { get; set; }

public DateTime DataHora { get; set; }

public string Metodo { get; set; } = string.Empty;

public string Rota { get; set; } = string.Empty;

public string? Aplicacao { get; set; }

public string? Operacao { get; set; }

public string? QueryString { get; set; }

public int? StatusCode { get; set; }

public long? DuracaoMs { get; set; }

public string? OficinaId { get; set; }

public int? InstalacaoId { get; set; }

public int? ClienteId { get; set; }

public int? SistemaId { get; set; }

public int? PlanoId { get; set; }

public string? Ip { get; set; }

public string? UserAgent { get; set; }

public string? RequestContentType { get; set; }

public string? ResponseContentType { get; set; }

public string? CorrelationId { get; set; }

public bool Sucesso { get; set; }

public string? Erro { get; set; }

}
