using System.ComponentModel.DataAnnotations;

namespace BlakBox.Api.Models;

public class Cliente
{
    public int ClienteId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = null!;

    [MaxLength(30)]
    public string? Documento { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Telefone { get; set; }

    [MaxLength(300)]
    public string? Endereco { get; set; }

    [MaxLength(50)]
    public string? Numero { get; set; }
    
    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = RelogioSistema.Agora;

    public ICollection<Instalacao> Instalacoes { get; set; }
        = new List<Instalacao>();
}
