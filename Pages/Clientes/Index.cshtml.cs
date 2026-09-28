using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BlakBox.Api.Models;
using BlakBox.Api.Services;

namespace BlakBox.Api.Pages.Clientes;

public class IndexModel : PageModel
{
    private readonly ClienteGestaoService _gestaoService;

    public IndexModel(
        ClienteGestaoService gestaoService)
    {
        _gestaoService =
            gestaoService;
    }

    public List<Cliente> Clientes { get; set; }
        = new();

    public async Task OnGetAsync()
    {
        await CarregarAsync();
    }

public async Task<IActionResult> OnPostCriarAsync(
    string nome,
    string? documento,
    string? email,
    string? telefone,
    string? endereco,
    string? numero)
{
    try
    {
        var cliente =
            await _gestaoService.CriarAsync(
                nome,
                documento,
                email,
                telefone,
                endereco,
                numero);

        TempData["Sucesso"] =
            $"Cliente \"{cliente.Nome}\" criado com sucesso.";

        return RedirectToPage();
    }
    catch (Exception ex)
    {
        TempData["Erro"] =
            ex.Message;

        await CarregarAsync();

        return Page();
    }
}
public async Task<IActionResult> OnPostSalvarAsync(
    int clienteId,
    string nome,
    string? documento,
    string? email,
    string? telefone,
    string? endereco,
    string? numero)
{
    try
    {
        await _gestaoService.AtualizarAsync(
            clienteId,
            nome,
            documento,
            email,
            telefone,
            endereco,
            numero);

        TempData["Sucesso"] =
            "Cliente atualizado com sucesso.";
    }
    catch (Exception ex)
    {
        TempData["Erro"] =
            ex.Message;
    }

    return RedirectToPage();
}    public async Task<IActionResult> OnPostStatusAsync(
        int clienteId,
        bool ativo)
    {
        try
        {
            await _gestaoService.AlterarStatusAsync(
                clienteId,
                ativo);

            TempData["Sucesso"] =
                ativo
                    ? "Cliente ativado com sucesso."
                    : "Cliente inativado com sucesso.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] =
                ex.Message;
        }

        return RedirectToPage();
    }

    private async Task CarregarAsync()
    {
        Clientes =
            await _gestaoService.ListarAsync();
    }
}