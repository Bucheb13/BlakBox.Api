using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;
using BlakBox.Api.Models;
using BlakBox.Api.Services;

namespace BlakBox.Api.Pages.Planos;

public class IndexModel : PageModel
{
    private readonly LicencaDbContext _context;
    private readonly PlanoGestaoService _gestaoService;

    public IndexModel(
        LicencaDbContext context,
        PlanoGestaoService gestaoService)
    {
        _context = context;
        _gestaoService = gestaoService;
    }

    public List<Plano> Planos { get; set; }
        = new();

    public List<Sistema> Sistemas { get; set; }
        = new();

    public async Task OnGetAsync()
    {
        await CarregarAsync();
    }

    public async Task<IActionResult> OnPostCriarAsync(
        int sistemaId,
        string codigo,
        string nome,
        int? duracaoDias,
        int? duracaoMeses,
        decimal? valor)
    {
        try
        {
            await _gestaoService.CriarAsync(
                sistemaId,
                codigo,
                nome,
                duracaoDias,
                duracaoMeses,
                valor);

            TempData["Sucesso"] =
                "Plano criado com sucesso.";

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
        int planoId,
        int sistemaId,
        string codigo,
        string nome,
        int? duracaoDias,
        int? duracaoMeses,
        decimal? valor)
    {
        try
        {
            await _gestaoService.AtualizarAsync(
                planoId,
                sistemaId,
                codigo,
                nome,
                duracaoDias,
                duracaoMeses,
                valor);

            TempData["Sucesso"] =
                "Plano atualizado com sucesso.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] =
                ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStatusAsync(
        int planoId,
        bool ativo)
    {
        try
        {
            await _gestaoService.AlterarStatusAsync(
                planoId,
                ativo);

            TempData["Sucesso"] =
                ativo
                    ? "Plano ativado com sucesso."
                    : "Plano inativado com sucesso.";
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
        Planos =
            await _gestaoService.ListarAsync();

        Sistemas =
            await _context.Sistemas
                .AsNoTracking()
                .OrderBy(x => x.Nome)
                .ToListAsync();
    }
}
