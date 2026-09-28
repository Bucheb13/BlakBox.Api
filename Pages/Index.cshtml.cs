using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;
using BlakBox.Api.Models;

namespace BlakBox.Api.Pages;

public class IndexModel : PageModel
{
    private readonly LicencaDbContext _context;

    public IndexModel(LicencaDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // RESUMO
    // ============================================================

    public int ClientesAtivos { get; set; }

    public int InstalacoesAtivas { get; set; }

    public int LicencasAtivas { get; set; }

    public int RequisicoesHoje { get; set; }

    // ============================================================
    // SAÚDE DA API
    // ============================================================

    public int RequisicoesSucessoHoje { get; set; }

    public int RequisicoesErroHoje { get; set; }

    public double TaxaSucesso { get; set; }

    public double TempoMedioMs { get; set; }

    public long MaiorDuracaoMs { get; set; }

    // ============================================================
    // LICENCIAMENTO
    // ============================================================

    public int ValidacoesHoje { get; set; }

    public int InstalacoesSemValidacaoHoje { get; set; }

    public int LicencasVencidas { get; set; }

    public int LicencasVencendo { get; set; }

    // ============================================================
    // INSTALAÇÕES
    // ============================================================

    public int InstalacoesComunicacaoRecente { get; set; }

    public int InstalacoesSemComunicacao { get; set; }

    // ============================================================
    // REQUISIÇÕES RECENTES
    // ============================================================

    public List<RequisicaoDashboard> RequisicoesRecentes { get; set; }
        = new();

    // ============================================================
    // ATIVIDADE POR OPERAÇÃO
    // ============================================================

    public List<OperacaoDashboard> Operacoes { get; set; }
        = new();

    // ============================================================
    // ATIVIDADE POR HORA
    // ============================================================

    public List<HoraDashboard> RequisicoesPorHora { get; set; }
        = new();

    // ============================================================
    // CARREGAMENTO
    // ============================================================

    public async Task OnGetAsync()
    {
        await CarregarDashboardAsync();
    }

    private async Task CarregarDashboardAsync()
    {
        var agora = RelogioSistema.Agora;
        var inicioHojeLocal = RelogioSistema.Agora.Date;
        var inicioAmanhaLocal = inicioHojeLocal.AddDays(1);
        var inicioHojeUtc = RelogioSistema.ParaUtcSemFuso(inicioHojeLocal);
        var inicioAmanhaUtc = RelogioSistema.ParaUtcSemFuso(inicioAmanhaLocal);

        // ========================================================
        // RESUMO
        // ========================================================

        ClientesAtivos =
            await _context.Clientes
                .AsNoTracking()
                .CountAsync(x => x.Ativo);

        InstalacoesAtivas =
            await _context.Instalacoes
                .AsNoTracking()
                .CountAsync(x => x.Ativa);

        LicencasAtivas =
            await _context.Licencas
                .AsNoTracking()
                .CountAsync(x => x.Ativa);

        // ========================================================
        // REQUISIÇÕES DE HOJE
        // ========================================================

        var requisicoesHojeQuery =
            _context.ApiRequisicoes
                .AsNoTracking()
                .Where(x =>
                    x.DataHora >= inicioHojeUtc &&
                    x.DataHora < inicioAmanhaUtc);

        RequisicoesHoje =
            await requisicoesHojeQuery.CountAsync();

        RequisicoesSucessoHoje =
            await requisicoesHojeQuery
                .CountAsync(x => x.Sucesso);

        RequisicoesErroHoje =
            await requisicoesHojeQuery
                .CountAsync(x => !x.Sucesso);

        // ========================================================
        // SAÚDE DA API
        // ========================================================

        if (RequisicoesHoje > 0)
        {
            TaxaSucesso =
                Math.Round(
                    RequisicoesSucessoHoje *
                    100.0 /
                    RequisicoesHoje,
                    1);
        }

        var duracoes =
            await requisicoesHojeQuery
                .Where(x => x.DuracaoMs.HasValue)
                .Select(x => x.DuracaoMs!.Value)
                .ToListAsync();

        if (duracoes.Count > 0)
        {
            TempoMedioMs =
                Math.Round(
                    duracoes.Average(),
                    1);

            MaiorDuracaoMs =
                duracoes.Max();
        }

        // ========================================================
        // LICENÇAS / VALIDAÇÕES
        // ========================================================

        ValidacoesHoje =
            await requisicoesHojeQuery
                .CountAsync(x =>
                    x.Operacao ==
                    "VALIDAR_LICENCA");

        var instalacoesAtivasIds =
            await _context.Instalacoes
                .AsNoTracking()
                .Where(x => x.Ativa)
                .Select(x => x.InstalacaoId)
                .ToListAsync();

        var instalacoesQueValidaramHoje =
            await requisicoesHojeQuery
                .Where(x =>
                    x.Operacao ==
                    "VALIDAR_LICENCA" &&
                    x.InstalacaoId.HasValue)
                .Select(x => x.InstalacaoId!.Value)
                .Distinct()
                .ToListAsync();

        InstalacoesSemValidacaoHoje =
            instalacoesAtivasIds
                .Except(instalacoesQueValidaramHoje)
                .Count();

        // ========================================================
        // LICENÇAS VENCIDAS
        // ========================================================

        LicencasVencidas =
            await _context.Licencas
                .AsNoTracking()
                .CountAsync(x =>
                    x.DataVencimento.HasValue &&
                    x.DataVencimento.Value < agora &&
                    x.Ativa);

        // ========================================================
        // LICENÇAS VENCENDO
        //
        // Consideramos os próximos 30 dias.
        // ========================================================

        var limiteVencimento =
            agora.AddDays(30);

        LicencasVencendo =
            await _context.Licencas
                .AsNoTracking()
                .CountAsync(x =>
                    x.Ativa &&
                    x.DataVencimento.HasValue &&
                    x.DataVencimento.Value >= agora &&
                    x.DataVencimento.Value <= limiteVencimento);

        // ========================================================
        // COMUNICAÇÃO DAS INSTALAÇÕES
        //
        // Consideramos comunicação recente quando ocorreu
        // nas últimas 24 horas.
        // ========================================================

        var limiteComunicacao =
            agora.AddHours(-24);

        InstalacoesComunicacaoRecente =
            await _context.Instalacoes
                .AsNoTracking()
                .CountAsync(x =>
                    x.Ativa &&
                    x.UltimaComunicacao.HasValue &&
                    x.UltimaComunicacao.Value >= limiteComunicacao);

        InstalacoesSemComunicacao =
            await _context.Instalacoes
                .AsNoTracking()
                .CountAsync(x =>
                    x.Ativa &&
                    (
                        !x.UltimaComunicacao.HasValue ||
                        x.UltimaComunicacao.Value < limiteComunicacao
                    ));

        // ========================================================
        // REQUISIÇÕES RECENTES
        // ========================================================

        RequisicoesRecentes =
            await _context.ApiRequisicoes
                .AsNoTracking()
                .OrderByDescending(x => x.DataHora)
                .Take(30)
                .Select(x => new RequisicaoDashboard
                {
                    DataHora = x.DataHora,
                    Metodo = x.Metodo,
                    Rota = x.Rota,
                    Operacao = x.Operacao,
                    InstalacaoId = x.InstalacaoId,
                    ClienteId = x.ClienteId,
                    SistemaId = x.SistemaId,
                    PlanoId = x.PlanoId,
                    StatusCode = x.StatusCode,
                    DuracaoMs = x.DuracaoMs,
                    CorrelationId = x.CorrelationId,
                    Sucesso = x.Sucesso,
                    Erro = x.Erro
                })
                .ToListAsync();

        // ========================================================
        // OPERAÇÕES
        // ========================================================

        Operacoes =
            await requisicoesHojeQuery
                .GroupBy(x =>
                    string.IsNullOrWhiteSpace(x.Operacao)
                        ? "SEM_OPERACAO"
                        : x.Operacao)
                .Select(g => new OperacaoDashboard
                {
                    Operacao = g.Key,
                    Quantidade = g.Count()
                })
                .OrderByDescending(x => x.Quantidade)
                .Take(10)
                .ToListAsync();

        // ========================================================
        // REQUISIÇÕES POR HORA
        // ========================================================

        var requisicoesHora =
            await requisicoesHojeQuery
                .Select(x => new
                {
                    x.DataHora,
                    x.Sucesso
                })
                .ToListAsync();

        RequisicoesPorHora =
            Enumerable
                .Range(0, 24)
                .Select(hora =>
                {
                    var itens =
                        requisicoesHora
                        .Where(x =>
                                    RelogioSistema.DeUtcSemFuso(x.DataHora).Hour == hora)
                            .ToList();

                    return new HoraDashboard
                    {
                        Hora = hora,
                        Quantidade = itens.Count,
                        Sucesso = itens.Count(x => x.Sucesso),
                        Erros = itens.Count(x => !x.Sucesso)
                    };
                })
                .ToList();
    }

    // ============================================================
    // MODELOS DO DASHBOARD
    // ============================================================

    public class RequisicaoDashboard
    {
        public DateTime DataHora { get; set; }

        public string Metodo { get; set; } = string.Empty;

        public string Rota { get; set; } = string.Empty;

        public string? Operacao { get; set; }

        public int? InstalacaoId { get; set; }

        public int? ClienteId { get; set; }

        public int? SistemaId { get; set; }

        public int? PlanoId { get; set; }

        public int? StatusCode { get; set; }

        public long? DuracaoMs { get; set; }

        public string? CorrelationId { get; set; }

        public bool Sucesso { get; set; }

        public string? Erro { get; set; }
    }

    public class OperacaoDashboard
    {
        public string Operacao { get; set; } = string.Empty;

        public int Quantidade { get; set; }
    }

    public class HoraDashboard
    {
        public int Hora { get; set; }

        public int Quantidade { get; set; }

        public int Sucesso { get; set; }

        public int Erros { get; set; }
    }
}
