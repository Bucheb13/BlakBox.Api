using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;
using BlakBox.Api.Models;
using BlakBox.Api.Services;

namespace BlakBox.Api.Pages.Licencas;

public class IndexModel : PageModel
{
    private readonly LicencaDbContext _context;
    private readonly LicencaGestaoService _gestaoService;

  public IndexModel(
    LicencaDbContext context,
    LicencaGestaoService gestaoService)
{
    _context = context;
    _gestaoService = gestaoService;
}

    public List<Licenca> Licencas { get; set; } = new();

    public List<Instalacao> InstalacoesParaNovaLicenca { get; set; } = new();

    public Dictionary<int, List<LicencaHistorico>> Historicos { get; set; }
        = new();

    /*
     * Planos disponíveis por SistemaId.
     *
     * A chave é o SistemaId da instalação vinculada
     * à licença.
     */
    public Dictionary<int, List<Plano>> PlanosPorSistema { get; set; }
        = new();

        public Dictionary<int, CredencialInstalacao?>
    CredenciaisPorInstalacao { get; set; }
    = new();

    public async Task OnGetAsync()
    {
        await CarregarDadosAsync();
    }

    public async Task<IActionResult> OnPostCriarAsync(int instalacaoId)
    {
        try
        {
            var licenca = await _gestaoService.CriarNovaAsync(instalacaoId);
            TempData["Sucesso"] =
                $"Nova licença criada para OficinaId \"{licenca.OficinaId}\".";
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAtivarAsync(
        int licencaId,
        string plano)
    {
        var licenca = await ObterLicencaAsync(licencaId);

        if (licenca == null)
        {
            TempData["Erro"] = "Licença não encontrada.";
            return RedirectToPage();
        }

        if (EhCancelada(licenca))
        {
            TempData["Erro"] = "Uma licença cancelada é bloqueada para edição.";
            return RedirectToPage();
        }

        try
        {
            var status =
                StatusLicencaService.ObterStatus(licenca);

            if (status ==
                StatusLicencaService.AguardandoAtivacao)
            {
                if (string.IsNullOrWhiteSpace(plano))
                {
                    throw new InvalidOperationException(
                        "Selecione um plano antes de ativar a licença.");
                }

                await _gestaoService.AtivarAsync(
                    licenca,
                    plano,
                    "Administrador",
                    "Licença ativada manualmente pelo painel.");

                TempData["Sucesso"] =
                    $"Licença da oficina \"{licenca.NomeOficina}\" " +
                    $"ativada com plano {licenca.Plano}.";
            }
            else if (status ==
                     StatusLicencaService.Inativa)
            {
                await _gestaoService.ReativarAsync(
                    licenca,
                    "Administrador",
                    "Licença reativada manualmente pelo painel.");

                TempData["Sucesso"] =
                    $"Licença da oficina \"{licenca.NomeOficina}\" " +
                    "reativada com sucesso.";
            }
            else
            {
                throw new InvalidOperationException(
                    $"Não é possível ativar ou reativar uma licença " +
                    $"com status \"{status}\".");
            }
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostInativarAsync(
        int licencaId)
    {
        var licenca =
            await ObterLicencaAsync(licencaId);

        if (licenca == null)
        {
            TempData["Erro"] =
                "Licença não encontrada.";

            return RedirectToPage();
        }

        if (EhCancelada(licenca))
        {
            TempData["Erro"] = "Uma licença cancelada não pode ser alterada.";
            return RedirectToPage();
        }

        try
        {
            await _gestaoService.AlterarStatusAsync(
                licenca,
                false,
                "Administrador",
                "Licença inativada manualmente pelo painel.");

            TempData["Sucesso"] =
                $"Licença da oficina \"{licenca.NomeOficina}\" " +
                "inativada com sucesso.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelarAsync(
        int licencaId)
    {
        var licenca =
            await ObterLicencaAsync(licencaId);

        if (licenca == null)
        {
            TempData["Erro"] =
                "Licença não encontrada.";

            return RedirectToPage();
        }

        try
        {
            await _gestaoService.CancelarAsync(
                licenca,
                "Administrador",
                "Assinatura cancelada manualmente pelo painel.");

            TempData["Sucesso"] =
                $"Assinatura da oficina \"{licenca.NomeOficina}\" " +
                "cancelada com sucesso.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostRenovarAsync(
        int licencaId)
    {
        var licenca =
            await ObterLicencaAsync(licencaId);

        if (licenca == null)
        {
            TempData["Erro"] =
                "Licença não encontrada.";

            return RedirectToPage();
        }

        if (EhCancelada(licenca))
        {
            TempData["Erro"] = "Uma licença cancelada não pode ser renovada.";
            return RedirectToPage();
        }

        try
        {
            var status =
                StatusLicencaService.ObterStatus(licenca);

            if (!StatusLicencaService.PodeRenovar(licenca))
            {
                throw new InvalidOperationException(
                    $"Não é possível renovar uma licença " +
                    $"com status \"{status}\".");
            }

            await _gestaoService.RenovarAsync(
                licenca,
                "Administrador",
                "Renovação manual realizada pelo painel.");

            TempData["Sucesso"] =
                $"Licença da oficina \"{licenca.NomeOficina}\" " +
                $"renovada com sucesso. " +
                $"Novo vencimento: " +
                $"{licenca.DataVencimento:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSalvarAsync(
        int licencaId,
        string nomeOficina,
        string? plano,
        bool ativa,
        DateTime? dataVencimento)
    {
        var licenca =
            await ObterLicencaAsync(licencaId);

        if (licenca == null)
        {
            TempData["Erro"] =
                "Licença não encontrada.";

            return RedirectToPage();
        }

        if (EhCancelada(licenca))
        {
            TempData["Erro"] = "Uma licença cancelada é bloqueada para edição.";
            return RedirectToPage();
        }

        try
        {
            if (string.IsNullOrWhiteSpace(nomeOficina))
            {
                throw new InvalidOperationException(
                    "O nome da oficina é obrigatório.");
            }

            nomeOficina =
                nomeOficina.Trim();

            licenca.NomeOficina =
                nomeOficina;

            /*
             * Alteração do plano:
             * agora o serviço resolve o PlanoId
             * através do código e do sistema da instalação.
             */
            if (!string.IsNullOrWhiteSpace(plano))
            {
                var planoAtual =
                    licenca.PlanoId.HasValue
                        ? await _context.Planos
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                x => x.PlanoId ==
                                     licenca.PlanoId.Value)
                        : null;

                if (planoAtual == null ||
                    !string.Equals(
                        planoAtual.Codigo,
                        plano,
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (licenca.DataInicio.HasValue)
                    {
                        await _gestaoService.AlterarPlanoAsync(
                            licenca,
                            plano,
                            "Administrador",
                            "Plano alterado manualmente pelo painel.");
                    }
                    else
                    {
                        /*
                         * Licença ainda não ativada:
                         * apenas associa o plano selecionado.
                         *
                         * A ativação posteriormente validará
                         * novamente se o plano existe e está ativo.
                         */
                        var planoNovo =
                            await ObterPlanoDaInstalacaoAsync(
                                licenca,
                                plano);

                        if (planoNovo == null)
                        {
                            throw new InvalidOperationException(
                                $"Plano \"{plano}\" não encontrado " +
                                "para o sistema da instalação.");
                        }

                        if (!planoNovo.Ativo)
                        {
                            throw new InvalidOperationException(
                                $"O plano \"{planoNovo.Nome}\" está inativo.");
                        }

                        licenca.PlanoId =
                            planoNovo.PlanoId;

                        licenca.Plano =
                            planoNovo.Nome;
                    }
                }
            }

            /*
             * Data de vencimento pode ser ajustada
             * somente depois da primeira ativação.
             */
            if (licenca.DataInicio.HasValue &&
                dataVencimento.HasValue &&
                (!licenca.DataVencimento.HasValue ||
                 dataVencimento.Value !=
                 licenca.DataVencimento.Value))
            {
                await _gestaoService.AlterarVencimentoAsync(
                    licenca,
                    dataVencimento.Value,
                    "Administrador",
                    "Data de vencimento ajustada manualmente pelo painel.");
            }

            /*
             * Alteração de status.
             */
            if (licenca.Ativa != ativa)
            {
                await _gestaoService.AlterarStatusAsync(
                    licenca,
                    ativa,
                    "Administrador",
                    ativa
                        ? "Licença ativada manualmente pelo painel."
                        : "Licença inativada manualmente pelo painel.");
            }

            licenca.NomeOficina =
                nomeOficina;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                $"Licença da oficina \"{licenca.NomeOficina}\" " +
                "atualizada com sucesso.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToPage();
    }

    private async Task<Licenca?> ObterLicencaAsync(
        int licencaId)
    {
        return await _context.Licencas
            .Include(x => x.Instalacao)
                .ThenInclude(x => x!.Sistema)
            .FirstOrDefaultAsync(
                x => x.LicencaId == licencaId);
    }

    private async Task<Plano?> ObterPlanoDaInstalacaoAsync(
        Licenca licenca,
        string codigoPlano)
    {
        if (!licenca.InstalacaoId.HasValue)
        {
            return null;
        }

        var instalacao =
            await _context.Instalacoes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.InstalacaoId ==
                         licenca.InstalacaoId.Value);

        if (instalacao == null)
        {
            return null;
        }

        return await _context.Planos
            .FirstOrDefaultAsync(
                x =>
                    x.SistemaId ==
                    instalacao.SistemaId
                    &&
                    x.Codigo == codigoPlano);
    }

    private async Task CarregarDadosAsync()
    {
        var instalacoes = await _context.Instalacoes
            .AsNoTracking()
            .Include(x => x.Cliente)
            .Include(x => x.Sistema)
            .Include(x => x.Licencas)
            .OrderBy(x => x.InstalacaoKey)
            .ToListAsync();

        InstalacoesParaNovaLicenca = instalacoes;

        Licencas =
            await _context.Licencas
                .AsNoTracking()
                .Include(x => x.Instalacao)
                    .ThenInclude(x => x!.Sistema)
                .OrderBy(x => x.NomeOficina)
                .ToListAsync();

        var ids =
            Licencas
                .Select(x => x.LicencaId)
                .ToList();

        if (ids.Count > 0)
        {
            var historicos =
                await _context.LicencaHistoricos
                    .AsNoTracking()
                    .Where(x =>
                        ids.Contains(x.LicencaId))
                    .OrderByDescending(x => x.CriadoEm)
                    .ToListAsync();

            Historicos =
                historicos
                    .GroupBy(x => x.LicencaId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.ToList());
        }
        else
        {
            Historicos = new();
        }

var instalacaoIds =
    Licencas
        .Where(x =>
            x.InstalacaoId.HasValue)
        .Select(x =>
            x.InstalacaoId!.Value)
        .Distinct()
        .ToList();

if (instalacaoIds.Count > 0)
{
    var credenciais =
        await _context
            .CredenciaisInstalacao
            .AsNoTracking()
            .Where(x =>
                instalacaoIds.Contains(
                    x.InstalacaoId) &&
                x.Ativa)
            .ToListAsync();

    CredenciaisPorInstalacao =
        credenciais
            .ToDictionary(
                x => x.InstalacaoId,
                x => (CredencialInstalacao?)x);
}
else
{
    CredenciaisPorInstalacao = new();
}
        /*
         * Descobre quais sistemas estão sendo usados
         * pelas instalações das licenças exibidas.
         */
        var sistemaIds =
            Licencas
                .Where(x =>
                    x.Instalacao != null)
                .Select(x =>
                    x.Instalacao!.SistemaId)
                .Distinct()
                .ToList();

        if (sistemaIds.Count == 0)
        {
            PlanosPorSistema = new();
            return;
        }

        var planos =
            await _context.Planos
                .AsNoTracking()
                .Where(x =>
                    sistemaIds.Contains(x.SistemaId) &&
                    x.Ativo)
                .OrderBy(x => x.Nome)
                .ToListAsync();

        PlanosPorSistema =
            planos
                .GroupBy(x => x.SistemaId)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList());
    }

    private static bool EhCancelada(Licenca licenca) =>
        string.Equals(licenca.SituacaoComercial,
            Licenca.SituacaoCancelada, StringComparison.OrdinalIgnoreCase);

    public static string ObterTextoTempoRestante(
        DateTime? dataVencimento,
        DateTime? validaAte)
    {
        if (!dataVencimento.HasValue)
            return "Sem vencimento";

        var agora =
            RelogioSistema.Agora;

        if (agora <= dataVencimento.Value)
        {
            var restante =
                dataVencimento.Value - agora;

            return FormatarTempo(
                restante,
                "🟢");
        }

        if (validaAte.HasValue &&
            agora <= validaAte.Value)
        {
            var restanteTolerancia =
                validaAte.Value - agora;

            return "🟠 Tolerância: " +
                   FormatarTempo(
                       restanteTolerancia,
                       "").Trim();
        }

        var vencida =
            agora - dataVencimento.Value;

        return "🔴 Vencida há " +
               FormatarTempoVencida(
                   vencida);
    }

    public static string ObterClasseTempoRestante(
        DateTime? dataVencimento,
        DateTime? validaAte)
    {
        if (!dataVencimento.HasValue)
            return "tempo-neutro";

        var agora =
            RelogioSistema.Agora;

        if (agora <= dataVencimento.Value)
        {
            var restante =
                dataVencimento.Value - agora;

            if (restante.TotalDays <= 3)
                return "tempo-alerta";

            return "tempo-ok";
        }

        if (validaAte.HasValue &&
            agora <= validaAte.Value)
        {
            return "tempo-tolerancia";
        }

        return "tempo-vencido";
    }

    private static string FormatarTempo(
        TimeSpan restante,
        string icone)
    {
        if (restante.TotalDays >= 1)
        {
            return $"{icone} " +
                   $"{restante.Days} dia(s), " +
                   $"{restante.Hours}h";
        }

        if (restante.TotalHours >= 1)
        {
            return $"{icone} " +
                   $"{restante.Hours}h " +
                   $"{restante.Minutes}min";
        }

        return $"{icone} " +
               $"{Math.Max(
                   1,
                   restante.Minutes)}min";
    }

    private static string FormatarTempoVencida(
        TimeSpan vencida)
    {
        if (vencida.TotalDays >= 1)
        {
            return $"{Math.Max(
                1,
                vencida.Days)} dia(s)";
        }

        if (vencida.TotalHours >= 1)
        {
            return $"{Math.Max(
                1,
                (int)vencida.TotalHours)} hora(s)";
        }

        return $"{Math.Max(
            1,
            vencida.Minutes)} minuto(s)";
    }
}
