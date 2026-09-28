using System.ComponentModel.DataAnnotations;

namespace BlakBox.Api.Models;

public sealed class ConfiguracaoR2Instalacao
{
    public int InstalacaoId { get; set; }

    [MaxLength(500)]
    public string? Endpoint { get; set; }

    [MaxLength(150)]
    public string? Bucket { get; set; }

    [MaxLength(2000)]
    public string? AccessKeyProtegida { get; set; }

    [MaxLength(4000)]
    public string? SecretKeyProtegida { get; set; }

    [MaxLength(500)]
    public string? PublicBaseUrl { get; set; }

    [MaxLength(63)]
    public string? OficinaSlug { get; set; }

    public bool Ativo { get; set; }

    public DateTime AtualizadoEm { get; set; } = RelogioSistema.Agora;

    public Instalacao Instalacao { get; set; } = null!;
}
