namespace BlakBox.Api.Models;

public sealed class OrcamentoPublico
{
    public string Oficina { get; set; } = string.Empty;
    public string? OficinaCnpj { get; set; }
    public string? OficinaTelefone { get; set; }
    public string? OficinaEndereco { get; set; }
    public string? OficinaNumero { get; set; }
    public string? OficinaCep { get; set; }
    public string? OficinaEmail { get; set; }
    public string? LogoDataUri { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string? ClienteTelefone { get; set; }
    public string? ClienteEmail { get; set; }
    public string? ClienteEndereco { get; set; }
    public int OrdemServicoId { get; set; }
    public DateTime DataEmissao { get; set; }
    public DateTime DataAbertura { get; set; }
    public string? Mecanico { get; set; }
    public string? Status { get; set; }
    public string Veiculo { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public int? VeiculoAno { get; set; }
    public string? VeiculoCor { get; set; }
    public int? Kilometragem { get; set; }
    public string? Descricao { get; set; }
    public string? Checklist { get; set; }
    public string? Observacoes { get; set; }
    public List<ItemOrcamentoPublico> Itens { get; set; } = new();
    public decimal TotalPecas { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
}

public sealed class ItemOrcamentoPublico
{
    public string Descricao { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Total { get; set; }
}
