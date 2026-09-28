namespace BlakBox.Api.Models.Api;

public class LicencaResposta
{
    public int LicencaId { get; set; }

    public int InstalacaoId { get; set; }

    public int ClienteId { get; set; }

    public string? ClienteNome { get; set; }

    public bool? ClienteAtivo { get; set; }

    public string? StatusCliente { get; set; }

    public bool? InstalacaoAtiva { get; set; }

    public string? StatusInstalacao { get; set; }

    public int SistemaId { get; set; }

    public string SistemaCodigo { get; set; } = null!;

    public string SistemaNome { get; set; } = null!;

    public bool? SistemaAtivo { get; set; }

    public string? StatusSistema { get; set; }

    public string InstalacaoKey { get; set; } = null!;

    public string NomeOficina { get; set; } = null!;

    public bool Ativa { get; set; }

    public string Status { get; set; } = null!;

    public string SituacaoComercial { get; set; } = null!;

    public int? PlanoId { get; set; }

    public string? Plano { get; set; }

    public bool? PlanoAtivo { get; set; }

    public string? StatusPlano { get; set; }

    public DateTime? DataInicio { get; set; }

    public DateTime? DataVencimento { get; set; }

    public DateTime? ValidaAte { get; set; }

    public DateTime? UltimaValidacao { get; set; }

    public bool PodeUsarSistema { get; set; }

    public string? Mensagem { get; set; }

    public int DiasTolerancia { get; set; }

public int DiasOffline { get; set; }
}
