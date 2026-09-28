using System.Diagnostics;
using BlakBox.Api.Data;
using BlakBox.Api.Models;

namespace BlakBox.Api.Middleware;

public class ApiMonitoramentoMiddleware
{
    private readonly RequestDelegate _next;

    private readonly ILogger<ApiMonitoramentoMiddleware>
        _logger;

    public ApiMonitoramentoMiddleware(
        RequestDelegate next,
        ILogger<ApiMonitoramentoMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        LicencaDbContext db)
    {
        /*
         * ============================================================
         * SOMENTE REQUISIÇÕES DA API
         * ============================================================
         *
         * O painel Razor da própria Central não deve entrar no
         * monitoramento.
         *
         * Exemplos ignorados:
         *
         * /Clientes
         * /Instalacoes
         * /Planos
         * /Licencas
         * /Sistemas
         * /css/...
         * /js/...
         * /swagger/...
         *
         * Exemplos monitorados:
         *
         * /api/licenca/validar
         * /api/licenca/registrar
         * /api/licenca/123
         * /api/licencas/123/renovar
         */

        if (!context.Request.Path
            .StartsWithSegments(
                "/api",
                StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);

            return;
        }

        var stopwatch =
            Stopwatch.StartNew();

        var correlationId =
            context.Request.Headers["X-Correlation-ID"]
                .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId =
                context.TraceIdentifier;
        }

        if (correlationId.Length > 100)
        {
            correlationId =
                correlationId[..100];
        }

        context.Response.Headers["X-Correlation-ID"] =
            correlationId;

        var metodo =
            context.Request.Method;

        var rota =
            context.Request.Path.HasValue
                ? context.Request.Path.Value!
                : "/";

        if (rota.Length > 300)
        {
            rota =
                rota[..300];
        }

        // Values are deliberately omitted: query parameters can contain
        // identifiers, filters or accidental secrets.
        var queryString = context.Request.Query.Count > 0
            ? "?" + string.Join("&", context.Request.Query.Keys
                .Select(Uri.EscapeDataString))
            : null;

        if (queryString?.Length > 1000)
        {
            queryString =
                queryString[..1000];
        }

        var ip =
            context.Connection.RemoteIpAddress?
                .ToString();

        if (ip?.Length > 100)
        {
            ip =
                ip[..100];
        }

        var userAgent =
            context.Request.Headers["User-Agent"]
                .FirstOrDefault();

        if (userAgent?.Length > 1000)
        {
            userAgent =
                userAgent[..1000];
        }

        var requestContentType =
            context.Request.ContentType;

        if (requestContentType?.Length > 200)
        {
            requestContentType =
                requestContentType[..200];
        }

        int? statusCode = null;

        string? erro = null;

        var sucesso = false;

        try
        {
            await _next(context);

            statusCode =
                context.Response.StatusCode;

            sucesso =
                statusCode >= 200 &&
                statusCode < 400;
        }
        catch (OperationCanceledException ex)
        {
            erro =
                context.RequestAborted.IsCancellationRequested
                    ? "Requisição cancelada pelo cliente."
                    : "Requisição cancelada.";

            if (erro.Length > 2000)
            {
                erro =
                    erro[..2000];
            }

            _logger.LogWarning(
                ex,
                "Requisição cancelada: {Metodo} {Rota} - CorrelationId: {CorrelationId}",
                metodo,
                rota,
                correlationId);

            throw;
        }
        catch (Exception ex)
        {
            statusCode =
                StatusCodes.Status500InternalServerError;

            sucesso = false;

            erro =
                ex.GetType().Name;

            if (erro.Length > 2000)
            {
                erro =
                    erro[..2000];
            }

            _logger.LogError(
                ex,
                "Erro não tratado na API: {Metodo} {Rota} - CorrelationId: {CorrelationId}",
                metodo,
                rota,
                correlationId);

            throw;
        }
        finally
        {
            stopwatch.Stop();

            var instalacaoId =
                ObterIntClaim(
                    context,
                    "instalacao_id");

            var clienteId =
                ObterIntClaim(
                    context,
                    "cliente_id");

            var sistemaId =
                ObterIntClaim(
                    context,
                    "sistema_id");

            var oficinaId =
                context.User.FindFirst(
                    "instalacao_key")?.Value;

            if (oficinaId?.Length > 50)
            {
                oficinaId =
                    oficinaId[..50];
            }

            var aplicacao =
                ObterAplicacao(
                    context);

            var operacao =
                ObterOperacao(
                    metodo,
                    rota);

            var responseContentType =
                context.Response.ContentType;

            if (responseContentType?.Length > 200)
            {
                responseContentType =
                    responseContentType[..200];
            }

            try
            {
                var requisicao =
                    new ApiRequisicao
                    {
                        DataHora =
                            DateTime.SpecifyKind(
                                DateTime.UtcNow,
                                DateTimeKind.Unspecified),

                        Metodo =
                            metodo,

                        Rota =
                            rota,

                        Aplicacao =
                            aplicacao,

                        Operacao =
                            operacao,

                        QueryString =
                            queryString,

                        StatusCode =
                            statusCode,

                        DuracaoMs =
                            stopwatch.ElapsedMilliseconds,

                        OficinaId =
                            oficinaId,

                        InstalacaoId =
                            instalacaoId,

                        ClienteId =
                            clienteId,

                        SistemaId =
                            sistemaId,

                        PlanoId =
                            null,

                        Ip =
                            ip,

                        UserAgent =
                            userAgent,

                        RequestContentType =
                            requestContentType,

                        ResponseContentType =
                            responseContentType,

                        CorrelationId =
                            correlationId,

                        Sucesso =
                            sucesso,

                        Erro =
                            erro
                    };

                db.ApiRequisicoes.Add(
                    requisicao);

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                /*
                 * O monitoramento nunca deve derrubar a API.
                 *
                 * Se o INSERT do log falhar, a requisição original
                 * continua tendo o resultado que já teria.
                 */
                _logger.LogError(
                    ex,
                    "Não foi possível registrar a requisição no monitoramento: {Metodo} {Rota} - CorrelationId: {CorrelationId}",
                    metodo,
                    rota,
                    correlationId);
            }
        }
    }

    private static int? ObterIntClaim(
        HttpContext context,
        string tipo)
    {
        var valor =
            context.User.FindFirst(tipo)?.Value;

        if (int.TryParse(
            valor,
            out var resultado))
        {
            return resultado;
        }

        return null;
    }

    private static string ObterAplicacao(
        HttpContext context)
    {
        var sistemaCodigo =
            context.User.FindFirst(
                "sistema_codigo")?.Value;

        if (!string.IsNullOrWhiteSpace(
            sistemaCodigo))
        {
            return sistemaCodigo;
        }

        var userAgent =
            context.Request.Headers["User-Agent"]
                .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(
            userAgent))
        {
            if (userAgent.Contains(
                "OficinaWeb",
                StringComparison.OrdinalIgnoreCase))
            {
                return "OficinaWeb";
            }

            if (userAgent.Contains(
                "BlakBox",
                StringComparison.OrdinalIgnoreCase))
            {
                return "BlakBox";
            }
        }

        return "Desconhecida";
    }

    private static string ObterOperacao(
        string metodo,
        string rota)
    {
        var rotaNormalizada =
            rota.TrimEnd('/');

        if (metodo.Equals(
                "POST",
                StringComparison.OrdinalIgnoreCase) &&
            rotaNormalizada.Equals(
                "/api/licenca/validar",
                StringComparison.OrdinalIgnoreCase))
        {
            return "VALIDAR_LICENCA";
        }

        if (metodo.Equals(
                "GET",
                StringComparison.OrdinalIgnoreCase) &&
            rotaNormalizada.Equals(
                "/api/licenca",
                StringComparison.OrdinalIgnoreCase))
        {
            return "CONSULTAR_LICENCA";
        }

        if (rotaNormalizada.Contains(
            "/api/licencas/registrar",
            StringComparison.OrdinalIgnoreCase))
        {
            return "REGISTRAR_INSTALACAO";
        }

        if (rotaNormalizada.Contains(
            "/api/licencas",
            StringComparison.OrdinalIgnoreCase))
        {
            return "GERENCIAR_LICENCA";
        }

        return
            $"{metodo.ToUpperInvariant()} {rotaNormalizada}";
    }
}
