using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;

namespace BlakBox.Api.Services;

public class ApiRequisicaoLimpezaService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ILogger<ApiRequisicaoLimpezaService>
        _logger;

    /*
     * Mantemos 90 dias de histórico.
     */
    private const int DiasRetencao = 90;

    /*
     * Cada DELETE remove no máximo 10.000 registros.
     *
     * Isso evita um DELETE gigantesco quando houver
     * milhões de registros antigos.
     */
    private const int TamanhoLote = 10_000;

    /*
     * Executa a limpeza uma vez por dia.
     */
    private static readonly TimeSpan Intervalo =
        TimeSpan.FromHours(24);

    public ApiRequisicaoLimpezaService(
        IServiceScopeFactory scopeFactory,
        ILogger<ApiRequisicaoLimpezaService> logger)
    {
        _scopeFactory =
            scopeFactory;

        _logger =
            logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        /*
         * Aguarda alguns segundos para a aplicação terminar
         * de inicializar antes da primeira limpeza.
         */
        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(30),
                stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await LimparAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao executar a limpeza das requisições da API.");
            }

            try
            {
                await Task.Delay(
                    Intervalo,
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task LimparAsync(
        CancellationToken cancellationToken)
    {
        var limite =
            DateTime.UtcNow.AddDays(-DiasRetencao);

        var totalRemovido = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var db =
                scope.ServiceProvider
                    .GetRequiredService<LicencaDbContext>();

            /*
             * PostgreSQL não possui DELETE ... LIMIT diretamente.
             *
             * Por isso selecionamos primeiro os IDs do lote e
             * depois removemos esses registros.
             */
            var removidos =
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    DELETE FROM "ApiRequisicao"
                    WHERE "ApiRequisicaoId" IN
                    (
                        SELECT "ApiRequisicaoId"
                        FROM "ApiRequisicao"
                        WHERE "DataHora" < {limite}
                        ORDER BY "DataHora"
                        LIMIT {TamanhoLote}
                    )
                    """,
                    cancellationToken);

            totalRemovido +=
                removidos;

            /*
             * Se removeu menos que o tamanho do lote,
             * não existem mais registros antigos.
             */
            if (removidos < TamanhoLote)
            {
                break;
            }

            /*
             * Pequena pausa entre lotes para não ocupar
             * o banco continuamente quando houver milhões
             * de registros antigos.
             */
            await Task.Delay(
                TimeSpan.FromMilliseconds(250),
                cancellationToken);
        }

        if (totalRemovido > 0)
        {
            _logger.LogInformation(
                "Limpeza de ApiRequisicoes concluída. " +
                "Registros removidos: {Total}. " +
                "Retenção configurada: {Dias} dias.",
                totalRemovido,
                DiasRetencao);
        }
    }
}
