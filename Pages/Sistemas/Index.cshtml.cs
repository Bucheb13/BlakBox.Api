using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BlakBox.Api.Models;
using BlakBox.Api.Services;

namespace BlakBox.Api.Pages.Sistemas;

public class IndexModel : PageModel
{
    private readonly SistemaGestaoService _gestaoService;

    public IndexModel(
        SistemaGestaoService gestaoService)
    {
        _gestaoService =
            gestaoService;
    }

    public List<Sistema> Sistemas { get; set; }
        = new();

    [BindProperty]
    public string Codigo { get; set; } = string.Empty;

    [BindProperty]
    public string Nome { get; set; } = string.Empty;

    [BindProperty]
    public int DiasTolerancia { get; set; } = 5;

    [BindProperty]
    public int DiasOffline { get; set; } = 5;

    [BindProperty]
    public int SistemaId { get; set; }

    public async Task OnGetAsync()
    {
        await CarregarAsync();
    }

    public async Task<IActionResult> OnPostCriarAsync()
    {
        try
        {
            var sistema =
                await _gestaoService.CriarAsync(
                    Codigo,
                    Nome,
                    DiasTolerancia,
                    DiasOffline);

            TempData["Sucesso"] =
                $"Sistema \"{sistema.Nome}\" criado com sucesso.";

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
        int sistemaId,
        string codigo,
        string nome,
        int diasTolerancia,
        int diasOffline)
    {
        try
        {
            await _gestaoService.AtualizarAsync(
                sistemaId,
                codigo,
                nome,
                diasTolerancia,
                diasOffline);

            TempData["Sucesso"] =
                "Sistema atualizado com sucesso.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] =
                ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStatusAsync(
        int sistemaId,
        bool ativo)
    {
        try
        {
            await _gestaoService.AlterarStatusAsync(
                sistemaId,
                ativo);

            TempData["Sucesso"] =
                ativo
                    ? "Sistema ativado com sucesso."
                    : "Sistema inativado com sucesso.";
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
        Sistemas =
            await _gestaoService.ListarAsync();
    }
}

