using System.ComponentModel.DataAnnotations;

namespace BlakBox.Api.Models;

public class Sistema
{
    public int SistemaId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = null!;

    public bool Ativo { get; set; } = true;

    /*
     * Quantos dias após o vencimento comercial
     * a licença continua podendo ser utilizada.
     */
    [Range(0, 3650)]
    public int DiasTolerancia { get; set; } = 5;

    public DateTime CriadoEm { get; set; } = RelogioSistema.Agora;

    public ICollection<Instalacao> Instalacoes { get; set; }
        = new List<Instalacao>();

    public ICollection<Plano> Planos { get; set; }
        = new List<Plano>();
}
