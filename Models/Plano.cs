using System.ComponentModel.DataAnnotations;

namespace BlakBox.Api.Models;

public class Plano
{
    public int PlanoId { get; set; }

    public int SistemaId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = null!;

    public int? DuracaoDias { get; set; }

    public int? DuracaoMeses { get; set; }

    public bool Ativo { get; set; } = true;

    public decimal? Valor { get; set; }

    public DateTime CriadoEm { get; set; } = RelogioSistema.Agora;

    public Sistema Sistema { get; set; } = null!;

    public ICollection<Licenca> Licencas { get; set; }
        = new List<Licenca>();
}
