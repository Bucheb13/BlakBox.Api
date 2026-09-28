using System.ComponentModel.DataAnnotations;

namespace BlakBox.Api.Models;

public class Instalacao
{
    public int InstalacaoId { get; set; }

    public int ClienteId { get; set; }

    public int SistemaId { get; set; }

    [Required]
    [MaxLength(50)]
    public string InstalacaoKey { get; set; } = null!;

    [MaxLength(150)]
    public string? NomeInstalacao { get; set; }

    public bool Ativa { get; set; } = true;

    public DateTime CriadoEm { get; set; } = RelogioSistema.Agora;

    public DateTime? UltimaComunicacao { get; set; }

    public Cliente Cliente { get; set; } = null!;

    public Sistema Sistema { get; set; } = null!;

    public ICollection<Licenca> Licencas { get; set; }
        = new List<Licenca>();

    public ICollection<CredencialInstalacao> Credenciais { get; set; }
        = new List<CredencialInstalacao>();
}