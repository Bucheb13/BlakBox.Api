namespace BlakBox.Api.Models;

public sealed class OrcamentoPublico
{
    public string Oficina { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public int OrdemServicoId { get; set; }
    public DateTime DataEmissao { get; set; }
    public string Veiculo { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public string? Descricao { get; set; }
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
