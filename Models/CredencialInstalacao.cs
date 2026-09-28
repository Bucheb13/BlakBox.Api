using System.ComponentModel.DataAnnotations;

namespace BlakBox.Api.Models;

public class CredencialInstalacao
{
    public int CredencialInstalacaoId { get; set; }

    public int InstalacaoId { get; set; }

    [Required]
    [MaxLength(128)]
    public string ChaveHash { get; set; } = null!;

    [MaxLength(8)]
    public string? UltimosCaracteres { get; set; }

    public bool Ativa { get; set; } = true;

    public DateTime CriadoEm { get; set; } = RelogioSistema.Agora;

    public DateTime? UltimaUtilizacao { get; set; }

    public DateTime? RevogadaEm { get; set; }

    public string? ChaveRecuperacaoProtegida { get; set; }

    public DateTime? RecuperacaoExpiraEm { get; set; }

    public Instalacao Instalacao { get; set; } = null!;
}
