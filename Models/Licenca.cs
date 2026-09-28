using System.ComponentModel.DataAnnotations;

namespace BlakBox.Api.Models;

public class Licenca
{
    public const string SituacaoNormal = "Normal";
    public const string SituacaoSuspensa = "Suspensa";
    public const string SituacaoCancelada = "Cancelada";

    public int LicencaId { get; set; }

    /*
     * Mantido por compatibilidade com a OficinaWeb atual.
     */
    [Required]
    [MaxLength(50)]
    public string OficinaId { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string NomeOficina { get; set; } = null!;

    public int? InstalacaoId { get; set; }

    public int? PlanoId { get; set; }

    public bool Ativa { get; set; }

    /*
     * Mantido temporariamente por compatibilidade.
     *
     * PlanoId é a referência oficial.
     */
    [MaxLength(50)]
    public string? Plano { get; set; }

    /*
     * Situação comercial é diferente do status operacional.
     */
    [Required]
    [MaxLength(30)]
    public string SituacaoComercial { get; set; }
        = SituacaoNormal;

    public DateTime? DataInicio { get; set; }

    public DateTime? DataVencimento { get; set; }

    public DateTime? UltimaValidacao { get; set; }

    public DateTime? ValidaAte { get; set; }

    [MaxLength(500)]
    public string? Mensagem { get; set; }

    public Instalacao? Instalacao { get; set; }

    public Plano? PlanoRelacionamento { get; set; }

    public ICollection<LicencaHistorico> Historico { get; set; }
        = new List<LicencaHistorico>();
}