using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;
using BlakBox.Api.Models;
using BlakBox.Api.Services;

namespace BlakBox.Api.Pages.Instalacoes;

public class IndexModel : PageModel
{
    private readonly LicencaDbContext _context;
    private readonly InstalacaoGestaoService _gestaoService;
    private readonly CredencialInstalacaoService _credencialService;

    public IndexModel(
        LicencaDbContext context,
        InstalacaoGestaoService gestaoService,
        CredencialInstalacaoService credencialService)
    {
        _context = context;
        _gestaoService = gestaoService;
        _credencialService = credencialService;
    }

    public List<Instalacao> Instalacoes { get; set; } = new();

    public List<Cliente> Clientes { get; set; } = new();

    public List<Sistema> Sistemas { get; set; } = new();

    public async Task OnGetAsync()
    {
        await CarregarAsync();
    }

    public async Task<IActionResult> OnPostCriarAsync(
        int clienteId,
        int sistemaId,
        string instalacaoKey,
        string? nomeInstalacao)
    {
        try
        {
            await _gestaoService.CriarAsync(
                clienteId,
                sistemaId,
                instalacaoKey,
                nomeInstalacao);

            TempData["Sucesso"] =
                "Instalação criada com sucesso.";

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
        int instalacaoId,
        int sistemaId,
        string instalacaoKey,
        string? nomeInstalacao)
    {
        try
        {
            await _gestaoService.AtualizarAsync(
                instalacaoId,
                sistemaId,
                instalacaoKey,
                nomeInstalacao);

            TempData["Sucesso"] =
                "Instalação atualizada com sucesso.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] =
                ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStatusAsync(
        int instalacaoId,
        bool ativa)
    {
        try
        {
            await _gestaoService.AlterarStatusAsync(
                instalacaoId,
                ativa);

            TempData["Sucesso"] =
                ativa
                    ? "Instalação ativada com sucesso."
                    : "Instalação inativada com sucesso.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] =
                ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGerarCredencialAsync(
        int instalacaoId)
    {
        try
        {
            var instalacao =
                await _context.Instalacoes
                    .AsNoTracking()
                    .Include(x => x.Cliente)
                    .Include(x => x.Sistema)
                    .FirstOrDefaultAsync(
                        x =>
                            x.InstalacaoId ==
                            instalacaoId);

            if (instalacao == null)
            {
                throw new InvalidOperationException(
                    "Instalação não encontrada.");
            }

            if (!instalacao.Ativa)
            {
                throw new InvalidOperationException(
                    "Não é possível gerar uma credencial para uma instalação inativa.");
            }

            if (!instalacao.Sistema.Ativo)
            {
                throw new InvalidOperationException(
                    "Não é possível gerar uma credencial para um sistema inativo.");
            }

            var resultado =
                await _credencialService.CriarAsync(
                    instalacaoId);

            TempData["CredencialNova"] =
                resultado.Chave;

            TempData["CredencialNovaInstalacao"] =
                instalacao.NomeInstalacao
                ?? instalacao.InstalacaoKey;

            TempData["CredencialNovaSistema"] =
                instalacao.Sistema.Nome;

            TempData["Sucesso"] =
                "Nova credencial gerada com sucesso.";
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
        Instalacoes =
            await _gestaoService.ListarAsync();

        Clientes =
            await _context.Clientes
                .AsNoTracking()
                .Where(x => x.Ativo)
                .OrderBy(x => x.Nome)
                .ToListAsync();

        Sistemas =
            await _context.Sistemas
                .AsNoTracking()
                .Where(x => x.Ativo)
                .OrderBy(x => x.Nome)
                .ToListAsync();
    }
}
