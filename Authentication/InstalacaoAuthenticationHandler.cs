using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using BlakBox.Api.Services;

namespace BlakBox.Api.Authentication;

public class InstalacaoAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName =
        "Instalacao";

    private const string HeaderName =
        "X-Installation-Key";

    private readonly CredencialInstalacaoService
        _credencialService;

    public InstalacaoAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        CredencialInstalacaoService credencialService)
        : base(
            options,
            logger,
            encoder)
    {
        _credencialService =
            credencialService;
    }

    protected override async Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(
                HeaderName,
                out var valores))
        {
            return AuthenticateResult.NoResult();
        }

        var chave =
            valores.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(chave))
        {
            return AuthenticateResult.Fail(
                "Credencial da instalação não informada.");
        }

        var resultado =
            await _credencialService.AutenticarAsync(
                chave);

        if (resultado == null)
        {
            return AuthenticateResult.Fail(
                "Credencial da instalação inválida ou revogada.");
        }

        var claims =
            new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    resultado.InstalacaoId.ToString()),

                new(
                    "instalacao_id",
                    resultado.InstalacaoId.ToString()),

                new(
                    "cliente_id",
                    resultado.ClienteId.ToString()),

                new(
                    "sistema_id",
                    resultado.SistemaId.ToString()),

                new(
                    "sistema_codigo",
                    resultado.SistemaCodigo),

                new(
                    "sistema_nome",
                    resultado.SistemaNome),

                new(
                    "instalacao_key",
                    resultado.InstalacaoKey)
            };

        var identidade =
            new ClaimsIdentity(
                claims,
                SchemeName);

        var principal =
            new ClaimsPrincipal(
                identidade);

        var ticket =
            new AuthenticationTicket(
                principal,
                SchemeName);

        return AuthenticateResult.Success(
            ticket);
    }

    protected override Task HandleChallengeAsync(
        AuthenticationProperties properties)
    {
        Response.StatusCode =
            StatusCodes.Status401Unauthorized;

        Response.ContentType =
            "application/json";

        return Response.WriteAsJsonAsync(
            new
            {
                sucesso = false,
                codigo = "NAO_AUTENTICADO",
                mensagem =
                    "Credencial da instalação ausente ou inválida."
            });
    }

    protected override Task HandleForbiddenAsync(
        AuthenticationProperties properties)
    {
        Response.StatusCode =
            StatusCodes.Status403Forbidden;

        Response.ContentType =
            "application/json";

        return Response.WriteAsJsonAsync(
            new
            {
                sucesso = false,
                codigo = "NAO_AUTORIZADO",
                mensagem =
                    "A instalação não possui autorização para este recurso."
            });
    }
}