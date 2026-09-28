using System.ComponentModel.DataAnnotations;

namespace BlakBox.Api.Models;

public class LicencaHistorico
{
    public int LicencaHistoricoId { get; set; }

    public int LicencaId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Acao { get; set; } = null!;

    [MaxLength(50)]
    public string? Plano { get; set; }

    public DateTime? DataInicioAnterior { get; set; }

    public DateTime? DataInicioNova { get; set; }

    public DateTime? DataVencimentoAnterior { get; set; }

    public DateTime? DataVencimentoNovo { get; set; }

    public DateTime? ValidaAteAnterior { get; set; }

    public DateTime? ValidaAteNova { get; set; }

    public bool? AtivaAnterior { get; set; }

    public bool? AtivaNova { get; set; }

    [MaxLength(50)]
    public string Origem { get; set; } = "Administrador";

    [MaxLength(500)]
    public string? Observacao { get; set; }

    public DateTime CriadoEm { get; set; } = RelogioSistema.Agora;

    public Licenca Licenca { get; set; } = null!;
}